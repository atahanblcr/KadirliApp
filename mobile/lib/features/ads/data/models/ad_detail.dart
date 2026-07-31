import 'package:freezed_annotation/freezed_annotation.dart';

part 'ad_detail.freezed.dart';
part 'ad_detail.g.dart';

/// `GET /v1/ads/{id}` gövdesi (`AdDetailDto`).
///
/// ⚠️ **Her başarılı çağrı sunucuda `view_count`'u artırır** (backend
/// `GetAdByIdQueryHandler`) ve yanıttaki [viewCount] **artıştan önceki**
/// değerdir. Bu yüzden detay provider'ı gereksiz yere invalidate edilmemeli;
/// ekranda gösterilen sayı "sen açmadan önce" anlamına gelir.
///
/// ⚠️ **`isFavorited` alanı YOK** — favori durumu `GET /v1/users/me/favorites`
/// üzerinden ayrıca öğreniliyor (bkz. `favorite_ads_controller.dart`).
@freezed
abstract class AdDetail with _$AdDetail {
  const factory AdDetail({
    required String id,
    required String title,
    @Default('') String description,
    double? price,
    @Default('approved') String status,
    required String categoryId,
    @Default('') String categoryName,
    @Default('') String userId,
    String? sellerName,
    @Default('') String contactPhone,
    @Default(0) int viewCount,
    required DateTime createdAt,
    required DateTime expiresAt,
    @Default(<AdImage>[]) List<AdImage> images,
    @Default(<AdPropertyValue>[]) List<AdPropertyValue> properties,
  }) = _AdDetail;

  const AdDetail._();

  factory AdDetail.fromJson(Map<String, dynamic> json) =>
      _$AdDetailFromJson(json);

  /// Galeride gösterilecek URL'ler (boş url'li kayıtlar elenir).
  List<String> get imageUrls => [
    for (final image in images)
      if ((image.url ?? '').trim().isNotEmpty) image.url!.trim(),
  ];

  bool get hasPhone => contactPhone.trim().isNotEmpty;

  /// Gösterilebilir kategoriye özel alanlar (değeri boş olanlar yazılmaz).
  List<AdPropertyValue> get visibleProperties => [
    for (final property in properties)
      if (property.displayValue != null) property,
  ];
}

/// İlan görseli (`AdImageDto`). [url] göreli gelir → `AppNetworkImage`
/// mutlaklaştırır.
@freezed
abstract class AdImage with _$AdImage {
  const factory AdImage({
    required String id,
    @Default('') String fileId,
    String? url,
    @Default(false) bool isCover,
    @Default(0) int displayOrder,
  }) = _AdImage;

  const AdImage._();

  factory AdImage.fromJson(Map<String, dynamic> json) =>
      _$AdImageFromJson(json);
}

/// Kategoriye özel alan değeri (`AdPropertyValueDto`).
///
/// [propertyType] sunucuda enum: `Text | Number | Boolean | Select |
/// MultiSelect`. Değer her zaman **metin** olarak geliyor; gösterim tipe göre
/// biçimlenir (11.9 form üretiminde aynı tip listesi kullanılacak).
@freezed
abstract class AdPropertyValue with _$AdPropertyValue {
  const factory AdPropertyValue({
    required String propertyId,
    @Default('') String propertyName,
    @Default('Text') String propertyType,
    @Default('') String value,
  }) = _AdPropertyValue;

  const AdPropertyValue._();

  factory AdPropertyValue.fromJson(Map<String, dynamic> json) =>
      _$AdPropertyValueFromJson(json);

  bool get isBoolean => propertyType.toLowerCase() == 'boolean';

  /// Ekranda yazılacak değer; gösterilecek bir şey yoksa `null`
  /// (satır hiç çizilmez — "işlevsiz/boş satır yok").
  String? get displayValue {
    final raw = value.trim();
    if (raw.isEmpty) return null;

    if (isBoolean) {
      return switch (raw.toLowerCase()) {
        'true' || '1' || 'evet' => 'Var',
        'false' || '0' || 'hayır' || 'hayir' => 'Yok',
        _ => raw,
      };
    }

    // MultiSelect virgülle ayrılmış geliyor → "A, B, C" olarak düzeltilir.
    if (propertyType.toLowerCase() == 'multiselect') {
      final parts = raw
          .split(',')
          .map((part) => part.trim())
          .where((part) => part.isNotEmpty);
      return parts.isEmpty ? null : parts.join(', ');
    }

    return raw;
  }
}
