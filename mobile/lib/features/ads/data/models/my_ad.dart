import 'package:freezed_annotation/freezed_annotation.dart';

part 'my_ad.freezed.dart';
part 'my_ad.g.dart';

/// İlan durumu — sunucunun `status` metni (`pending|approved|rejected|expired`).
///
/// Kullanıcı "pending" kelimesini görmemeli: her durumun Türkçe etiketi,
/// bir cümlelik açıklaması ve rengi burada.
enum AdStatus {
  pending('pending', 'Onay bekliyor'),
  approved('approved', 'Yayında'),
  rejected('rejected', 'Reddedildi'),
  expired('expired', 'Süresi doldu'),
  unknown('', 'Bilinmiyor');

  const AdStatus(this.apiValue, this.label);

  final String apiValue;
  final String label;

  static AdStatus parse(String? raw) {
    final value = raw?.trim().toLowerCase();
    for (final status in values) {
      if (status != unknown && status.apiValue == value) return status;
    }
    return unknown;
  }

  /// "Benim ilanlarım" filtre şeridinde gösterilen durumlar (Tümü hariç).
  static const filterable = [pending, approved, rejected, expired];

  String get description => switch (this) {
    pending =>
      'İlanınız yönetici onayında. Onaylandığında herkes tarafından görülecek.',
    approved => 'İlanınız yayında ve arama sonuçlarında görünüyor.',
    rejected => 'İlanınız yayınlanmadı. Gerekçeyi düzeltip yeniden gönderebilirsiniz.',
    expired => 'Yayın süresi doldu. Uzatarak yeniden yayına alabilirsiniz.',
    unknown => '',
  };
}

/// `GET /v1/users/me/ads` satırı (`MyAdDto`).
///
/// Public `AdSummary`'den farkı: sahibin görmesi gereken alanları da taşır —
/// **red gerekçesi**, uzatma hakkı ve iletişim tıklama sayaçları (ilan
/// performansı). Bu yüzden ayrı bir model.
@freezed
abstract class MyAd with _$MyAd {
  const factory MyAd({
    required String id,
    required String title,
    String? description,
    double? price,
    @Default('pending') String status,
    @Default('') String categoryId,
    @Default('') String categoryName,
    @Default('') String contactPhone,
    @Default(0) int viewCount,
    @Default(0) int phoneClickCount,
    @Default(0) int whatsappClickCount,
    @Default(0) int favoriteCount,
    @Default(0) int extensionCount,
    @Default(0) int maxExtensions,
    String? rejectedReason,
    required DateTime createdAt,
    required DateTime expiresAt,
    @Default(<String>[]) List<String> imageUrls,
  }) = _MyAd;

  const MyAd._();

  factory MyAd.fromJson(Map<String, dynamic> json) => _$MyAdFromJson(json);

  String? get coverImageUrl => imageUrls.isEmpty ? null : imageUrls.first;

  AdStatus get statusKind => AdStatus.parse(status);

  int get remainingExtensions {
    final left = maxExtensions - extensionCount;
    return left < 0 ? 0 : left;
  }

  /// Uzatma yalnız **yayındaki ya da süresi dolmuş** ilanda anlamlı
  /// (`ExtendMyAdCommandHandler`: pending/rejected 400 verir) ve hak kalmışsa.
  bool get canExtend =>
      (statusKind == AdStatus.approved || statusKind == AdStatus.expired) &&
      remainingExtensions > 0;

  /// Yayın bitimine kalan gün (geçmişse 0). Gün başına yuvarlanmaz —
  /// "yarım gün kaldı" da 1 gün sayılır ki "0 gün kaldı" yazmayalım.
  int get daysUntilExpiry {
    final left = expiresAt.toUtc().difference(DateTime.now().toUtc());
    if (left.isNegative) return 0;
    return left.inHours ~/ 24 + (left.inHours % 24 == 0 ? 0 : 1);
  }

  /// Yayındaki ilan bir haftadan az kaldıysa kullanıcı uyarılır (uzatma hakkı
  /// varsa CTA da gösterilir) — süresi dolduktan sonra fark etmek geç olur.
  bool get isExpiringSoon =>
      statusKind == AdStatus.approved && daysUntilExpiry <= 7;

  /// İlan performansı: toplam iletişim denemesi.
  int get contactCount => phoneClickCount + whatsappClickCount;
}
