import 'package:freezed_annotation/freezed_annotation.dart';

part 'my_consent.freezed.dart';
part 'my_consent.g.dart';

/// `GET /v1/users/me/consents` öğesi (`MyConsentDto`, Faz 12.16).
///
/// 🔑 Liste **yayında olan her belgeyi** taşır — kullanıcının hiç karar
/// vermediklerini de ([consentedVersionId] `null`). Yalnız karar verilenler
/// gelseydi ayarlar ekranı, hiç sorulmamış bir izni (ör. ticari ileti)
/// göstermez ve kullanıcının onu verme yolu **hiç var olmazdı**.
@freezed
abstract class MyConsent with _$MyConsent {
  const factory MyConsent({
    required String type,
    required String title,
    @Default(false) bool isMandatory,

    /// Yayındaki sürüm — "onayınız güncel mi?" karşılaştırmasının sol tarafı.
    required String currentVersionId,
    @Default(1) int currentVersionNumber,

    /// Kullanıcının karar verdiği sürüm; hiç karar vermemişse `null`.
    String? consentedVersionId,
    int? consentedVersionNumber,

    /// ⚠️ `false`, "hiç sorulmadı" demek **değildir** — onun cevabı
    /// [decidedAt]'in `null` olmasıdır ("sormadık" ≠ "sorduk, hayır dedi").
    @Default(false) bool granted,
    DateTime? decidedAt,
    DateTime? revokedAt,

    /// 🔑 **Sunucuda türetilir.** İstemcide hesaplansaydı iki sahip doğardı ve
    /// mağazadaki eski sürümler kuralın eski hâlini uygulamaya devam ederdi.
    @Default(false) bool needsReconsent,
  }) = _MyConsent;

  const MyConsent._();

  factory MyConsent.fromJson(Map<String, dynamic> json) =>
      _$MyConsentFromJson(json);

  /// Kullanıcı bu belge hakkında hiç karar vermiş mi?
  bool get hasDecision => decidedAt != null;

  /// Onayladığı sürüm yayındakinden eski mi (yeniden onay gerekmese de)?
  bool get isOutdated =>
      consentedVersionNumber != null &&
      consentedVersionNumber! < currentVersionNumber;

  /// 🔴 Zorunlu rıza ayarlardan **geri alınamaz** — karşılığı hesap silmedir
  /// (`DELETE /v1/users/me`). Anahtarın kapalı çizilmesi değil, **hiç
  /// çizilmemesi** doğru: kapatılamayan bir anahtar işlevsiz butondur.
  bool get canRevoke => !isMandatory && granted;
}
