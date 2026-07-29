import 'package:freezed_annotation/freezed_annotation.dart';

import 'auth_tokens.dart';

part 'verify_otp_result.freezed.dart';
part 'verify_otp_result.g.dart';

/// `POST /v1/auth/verify-otp` yanıtı (API_CONTRACT §4).
///
/// İki farklı gövde tek şemada gelir:
/// - kayıtlı kullanıcı → `{isNewUser:false, accessToken, refreshToken, expiresIn}`
/// - yeni kullanıcı → `{isNewUser:true, tempToken}` (**hesap henüz YOK**;
///   kayıt `POST /v1/auth/register` ile tamamlanır)
@freezed
abstract class VerifyOtpResult with _$VerifyOtpResult {
  const factory VerifyOtpResult({
    @Default(false) bool isNewUser,

    /// Yalnız yeni kullanıcıda: 30 dk ömürlü kayıt token'ı. **Saklanmaz** —
    /// kayıt ekranı bitene kadar bellekte taşınır.
    String? tempToken,
    String? accessToken,
    String? refreshToken,
    int? expiresIn,
  }) = _VerifyOtpResult;

  const VerifyOtpResult._();

  factory VerifyOtpResult.fromJson(Map<String, dynamic> json) =>
      _$VerifyOtpResultFromJson(json);

  /// Kayıtlı kullanıcının token çifti; yeni kullanıcıda null.
  AuthTokens? get tokens =>
      (accessToken != null && refreshToken != null && !isNewUser)
      ? AuthTokens(
          accessToken: accessToken!,
          refreshToken: refreshToken!,
          expiresIn: expiresIn,
        )
      : null;
}
