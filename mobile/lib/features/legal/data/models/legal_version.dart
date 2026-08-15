import 'package:freezed_annotation/freezed_annotation.dart';

part 'legal_version.freezed.dart';
part 'legal_version.g.dart';

/// `GET /v1/legal/versions/{id}` gövdesi (`LegalVersionDto`, Faz 12.17 eki).
///
/// 🔑 **"Ben neyi onaylamıştım?"** sorusunun cevabı. `GET /v1/users/me/consents`
/// onaylanan sürümün *kimliğini* söylüyordu ama o kimlikten **metne** giden bir
/// yol yoktu: yeni sürüm yayınlandığı an kullanıcı kabul ettiği metni bir daha
/// göremiyordu.
///
/// ⚠️ [isLive] `false` ise metin **yürürlükten kalkmıştır** ve ekran bunu
/// **söylemek zorundadır** — söylemezse kullanıcı eski metni güncel sanar.
/// Alan sunucudan gelir; [supersededAt]'ten istemcide türetilmez (§7 madde 43'ün
/// "tek sahip" kuralı).
@freezed
abstract class LegalVersion with _$LegalVersion {
  const factory LegalVersion({
    required String id,
    required String documentType,
    required String documentTitle,
    @Default(1) int versionNumber,
    String? summary,
    @Default('') String body,
    DateTime? effectiveFrom,
    DateTime? publishedAt,
    @Default(false) bool isLive,
    DateTime? supersededAt,
  }) = _LegalVersion;

  const LegalVersion._();

  factory LegalVersion.fromJson(Map<String, dynamic> json) =>
      _$LegalVersionFromJson(json);
}
