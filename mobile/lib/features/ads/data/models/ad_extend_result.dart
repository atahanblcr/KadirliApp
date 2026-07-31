import 'package:freezed_annotation/freezed_annotation.dart';

part 'ad_extend_result.freezed.dart';
part 'ad_extend_result.g.dart';

/// `POST /v1/ads/{id}/extend` yanıtı (`ExtendAdResultDto`).
///
/// Uzatma 30 gün ekler; süresi dolmuş ilan yeniden `approved` olur (içerik
/// zaten moderasyondan geçmişti). Hak dolduysa uç **409 CONFLICT** verir →
/// ekran bu modeli hiç görmez, `ApiException.isConflict` dalına düşer.
@freezed
abstract class AdExtendResult with _$AdExtendResult {
  const factory AdExtendResult({
    required String adId,
    @Default('approved') String status,
    required DateTime expiresAt,
    @Default(0) int extensionCount,
    @Default(0) int maxExtensions,
    @Default(0) int remainingExtensions,
  }) = _AdExtendResult;

  factory AdExtendResult.fromJson(Map<String, dynamic> json) =>
      _$AdExtendResultFromJson(json);
}
