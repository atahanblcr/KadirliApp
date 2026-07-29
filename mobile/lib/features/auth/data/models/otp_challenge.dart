import 'package:freezed_annotation/freezed_annotation.dart';

part 'otp_challenge.freezed.dart';
part 'otp_challenge.g.dart';

/// `POST /v1/auth/login` yanıtı — OTP gönderildi bilgisi.
///
/// ⚠️ **Alan adları kontrat dokümanından farklı:** API_CONTRACT §4
/// `expiresInSeconds` / `retryAfterSeconds` / `devOtp` diyor ama
/// `AuthController.Login` gerçekte `{message, expiresIn, retryAfter, otp}`
/// döndürüyor (canlı doğrulandı, 30 Tem 2026). Kontrat dokümanı düzeltildi;
/// model **gerçek yanıta** göre yazıldı.
///
/// [otp] YALNIZ `Otp:DevMode=true` iken dolu gelir (prod'da alan hiç yok) —
/// geliştirme kolaylığı olarak kod alanını otomatik doldurmakta kullanılır.
@freezed
abstract class OtpChallenge with _$OtpChallenge {
  const factory OtpChallenge({
    String? message,

    /// Kodun geçerlilik süresi (saniye) — sunucu varsayılanı 300.
    @Default(300) int expiresIn,

    /// "Tekrar gönder" için beklenmesi gereken süre (saniye) — sunucu sabiti 60.
    @Default(60) int retryAfter,

    /// Dev modda dönen sabit kod (`123456`).
    String? otp,
  }) = _OtpChallenge;

  const OtpChallenge._();

  factory OtpChallenge.fromJson(Map<String, dynamic> json) => _$OtpChallengeFromJson(json);

  /// Dev modda mıyız (sunucu kodu yanıta koymuş)?
  bool get hasDevOtp => otp != null && otp!.isNotEmpty;

  Duration get expiry => Duration(seconds: expiresIn);
  Duration get resendCooldown => Duration(seconds: retryAfter);
}
