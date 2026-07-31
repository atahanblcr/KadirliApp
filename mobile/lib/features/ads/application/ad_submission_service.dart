import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../files/data/files_repository.dart';
import '../data/ads_repository.dart';
import '../data/models/ad_detail.dart';

/// İlan formundaki tek görsel: ya **sunucuda duran** bir görsel (düzenlemede)
/// ya da kullanıcının cihazdan yeni seçtiği bir dosya.
@immutable
class AdFormImage {
  const AdFormImage.existing({
    required this.adImageId,
    required this.fileId,
    this.remoteUrl,
  }) : localPath = null;

  const AdFormImage.picked(this.localPath)
    : adImageId = null,
      fileId = null,
      remoteUrl = null;

  /// `AdImage.id` — silme isteğinde bu gönderilir (dosya id'si DEĞİL).
  final String? adImageId;

  /// `AdImage.fileId` — sıralama değiştiğinde görsel bu id ile yeniden bağlanır.
  final String? fileId;

  final String? remoteUrl;

  /// Cihazdaki geçici dosya yolu (yeni seçilen görsel).
  final String? localPath;

  bool get isExisting => adImageId != null;

  factory AdFormImage.fromDetail(AdImage image) => AdFormImage.existing(
    adImageId: image.id,
    fileId: image.fileId,
    remoteUrl: image.url,
  );

  @override
  bool operator ==(Object other) =>
      other is AdFormImage &&
      other.adImageId == adImageId &&
      other.localPath == localPath;

  @override
  int get hashCode => Object.hash(adImageId, localPath);
}

/// Formun sunucuya gidecek alanları (görseller hariç).
@immutable
class AdFormValues {
  const AdFormValues({
    required this.categoryId,
    required this.title,
    required this.description,
    required this.contactPhone,
    this.price,
    this.sellerName,
    this.propertyValues = const {},
  });

  final String categoryId;
  final String title;
  final String description;
  final String contactPhone;
  final num? price;
  final String? sellerName;
  final Map<String, String> propertyValues;
}

/// Görsel yükleme ilerlemesi — "3 / 5 görsel yüklendi".
typedef UploadProgress = void Function(int uploaded, int total);

/// İlan oluşturma/güncellemenin **çok adımlı** işini tek yerde toplar:
/// önce görseller `POST /v1/files/upload` ile tek tek yüklenir, sonra dönen
/// dosya kimlikleriyle ilan yazılır.
///
/// **Neden ayrı bir servis:** ekran zaten uzun bir form; "hangi görsel yeni,
/// hangisi silinecek, sıra değişti mi" kararları saf mantık ve testi ekransız
/// yazılabiliyor.
class AdSubmissionService {
  AdSubmissionService(this._ads, this._files);

  final AdsRepository _ads;
  final FilesRepository _files;

  /// Yeni ilan: görseller sırayla yüklenir (**ilki kapak olur**), sonra ilan
  /// `pending` olarak oluşturulur. Dönen değer yeni ilanın kimliği.
  Future<String> create({
    required AdFormValues values,
    required List<AdFormImage> images,
    UploadProgress? onProgress,
  }) async {
    final fileIds = await _uploadAll(images, onProgress);
    return _ads.create(
      categoryId: values.categoryId,
      title: values.title,
      description: values.description,
      contactPhone: values.contactPhone,
      price: values.price,
      sellerName: values.sellerName,
      imageFileIds: fileIds,
      propertyValues: values.propertyValues,
    );
  }

  /// Düzenleme. [originalImages] ilanın açılışta gelen görselleri, [images]
  /// kullanıcının bıraktığı son sıralama.
  ///
  /// ⚠️ **Kapak/sıra değişimi**: uç yalnız "ekle" ve "sil" biliyor; yeni
  /// eklenen görseller sona ve `isCover=false` olarak yazılıyor. Kullanıcı
  /// sırayı değiştirdiyse (ör. kapağı değiştirdiyse) mevcut görseller
  /// **dosya kimlikleriyle yeniden bağlanır**: hepsi silinip yeni sırada
  /// eklenir — sunucu kapaksız kalan ilanda en düşük sıradakini kapak yapar,
  /// bu da tam olarak kullanıcının seçtiği görseldir. Sıra değişmediyse bu
  /// yola hiç girilmez (gereksiz satır silme/ekleme yok).
  Future<void> update({
    required String adId,
    required AdFormValues values,
    required List<AdFormImage> images,
    required List<AdFormImage> originalImages,
    Map<String, String>? propertyValues,
    UploadProgress? onProgress,
  }) async {
    final keptExisting = images.where((image) => image.isExisting).toList();
    final originalIds = originalImages
        .map((image) => image.adImageId!)
        .toList(growable: false);
    final keptIds = keptExisting
        .map((image) => image.adImageId!)
        .toList(growable: false);

    final removedIds = originalIds
        .where((id) => !keptIds.contains(id))
        .toList(growable: false);

    // Kalanların sırası bozulmuş mu, ya da yeni bir görsel mevcutların
    // ÖNÜNE alınmış mı (kapak değişimi)?
    final survivingOriginalOrder = originalIds
        .where(keptIds.contains)
        .toList(growable: false);
    final orderChanged =
        !listEquals(survivingOriginalOrder, keptIds) ||
        (images.isNotEmpty && !images.first.isExisting && keptExisting.isNotEmpty);

    final uploadedByIndex = <int, String>{};
    final pickedIndexes = [
      for (var i = 0; i < images.length; i++)
        if (!images[i].isExisting) i,
    ];
    var uploaded = 0;
    for (final index in pickedIndexes) {
      onProgress?.call(uploaded, pickedIndexes.length);
      final file = await _files.upload(
        filePath: images[index].localPath!,
        moduleType: 'ad',
      );
      uploadedByIndex[index] = file.id;
      uploaded++;
    }
    onProgress?.call(uploaded, pickedIndexes.length);

    final List<String> removeImageIds;
    final List<String> newImageFileIds;
    if (orderChanged) {
      removeImageIds = [...originalIds];
      newImageFileIds = [
        for (var i = 0; i < images.length; i++)
          images[i].isExisting ? images[i].fileId! : uploadedByIndex[i]!,
      ];
    } else {
      removeImageIds = removedIds;
      newImageFileIds = [
        for (final index in pickedIndexes) uploadedByIndex[index]!,
      ];
    }

    await _ads.update(
      id: adId,
      title: values.title,
      description: values.description,
      contactPhone: values.contactPhone,
      price: values.price,
      sellerName: values.sellerName,
      newImageFileIds: newImageFileIds,
      removeImageIds: removeImageIds,
      propertyValues: propertyValues,
    );
  }

  Future<List<String>> _uploadAll(
    List<AdFormImage> images,
    UploadProgress? onProgress,
  ) async {
    final picked = images.where((image) => !image.isExisting).toList();
    final ids = <String>[];
    onProgress?.call(0, picked.length);
    for (final image in picked) {
      final file = await _files.upload(
        filePath: image.localPath!,
        moduleType: 'ad',
      );
      ids.add(file.id);
      onProgress?.call(ids.length, picked.length);
    }
    return ids;
  }
}

final adSubmissionServiceProvider = Provider<AdSubmissionService>(
  (ref) => AdSubmissionService(
    ref.watch(adsRepositoryProvider),
    ref.watch(filesRepositoryProvider),
  ),
);
