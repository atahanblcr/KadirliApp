import 'package:freezed_annotation/freezed_annotation.dart';

part 'ad_summary.freezed.dart';
part 'ad_summary.g.dart';

/// `GET /v1/ads` satırı (`AdResponseDto`).
///
/// ⚠️ **Liste DTO'su kategori ve mahalle taşımıyor** (yalnız detayda var).
/// MOBILE_UX_PLAN'daki kart taslağı "mahalle" diyordu; backend kontratı
/// donduruldu (Faz 10) ve `Ad` varlığında mahalle alanı yok — kart bu yüzden
/// mahalle yerine **ilan tarihi + görüntülenme** gösteriyor (bkz. `AdCard`).
///
/// [imageUrls] **göreli** gelir (`/uploads/...`) ve kapak görseli başa
/// sıralanmış olur (sunucu `IsCover DESC, DisplayOrder ASC`).
@freezed
abstract class AdSummary with _$AdSummary {
  const factory AdSummary({
    required String id,
    required String title,
    String? description,
    double? price,
    @Default('approved') String status,
    @Default('') String contactPhone,
    @Default(0) int viewCount,
    required DateTime createdAt,
    @Default(<String>[]) List<String> imageUrls,
  }) = _AdSummary;

  const AdSummary._();

  factory AdSummary.fromJson(Map<String, dynamic> json) =>
      _$AdSummaryFromJson(json);

  /// Kapak görseli (yoksa null → `AppNetworkImage` nötr ikona düşer).
  String? get coverImageUrl => imageUrls.isEmpty ? null : imageUrls.first;

  bool get hasImage => imageUrls.isNotEmpty;
}
