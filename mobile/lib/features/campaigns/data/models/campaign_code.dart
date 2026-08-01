import 'package:freezed_annotation/freezed_annotation.dart';

part 'campaign_code.freezed.dart';
part 'campaign_code.g.dart';

/// `POST /v1/campaigns/{id}/view-code` yanıtı (`CampaignCodeDto`).
///
/// [viewedAt] kullanıcının kodu **ilk** gördüğü an: uç aynı kullanıcı için yeni
/// satır açmaz, mevcut kaydı döndürür (10.12) → "bu kodu 3 gün önce almıştınız"
/// bilgisi ekranda gösterilebiliyor.
@freezed
abstract class CampaignCode with _$CampaignCode {
  const factory CampaignCode({
    required String code,
    required DateTime viewedAt,
  }) = _CampaignCode;

  factory CampaignCode.fromJson(Map<String, dynamic> json) =>
      _$CampaignCodeFromJson(json);
}
