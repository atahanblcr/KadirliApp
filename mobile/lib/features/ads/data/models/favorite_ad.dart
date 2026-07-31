import 'package:freezed_annotation/freezed_annotation.dart';

part 'favorite_ad.freezed.dart';
part 'favorite_ad.g.dart';

/// `GET /v1/users/me/favorites` satırı (`FavoriteAdDto`).
///
/// 11.8'de yalnız **favori kimlik kümesini** kurmak için okunuyor (kalp
/// ikonunun dolu mu boş mu çizileceği); "Favorilerim" ekranı 11.9'da bu
/// modelin tamamını kullanacak.
///
/// [isAvailable] false → ilan silinmemiş ama yayından düşmüş (yeniden
/// moderasyona girmiş ya da süresi geçmiş); 11.9 bunları soluk gösterecek.
@freezed
abstract class FavoriteAd with _$FavoriteAd {
  const factory FavoriteAd({
    required String adId,
    @Default('') String title,
    double? price,
    @Default('') String status,
    @Default(true) bool isAvailable,
    @Default(0) int viewCount,
    required DateTime favoritedAt,
    @Default(<String>[]) List<String> imageUrls,
  }) = _FavoriteAd;

  const FavoriteAd._();

  factory FavoriteAd.fromJson(Map<String, dynamic> json) =>
      _$FavoriteAdFromJson(json);

  String? get coverImageUrl => imageUrls.isEmpty ? null : imageUrls.first;
}
