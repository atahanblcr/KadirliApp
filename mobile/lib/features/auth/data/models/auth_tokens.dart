import 'package:freezed_annotation/freezed_annotation.dart';

part 'auth_tokens.freezed.dart';
part 'auth_tokens.g.dart';

/// `POST /v1/auth/register` ve `/refresh` yanıtı (API_CONTRACT §4).
///
/// `refresh` **tek kullanımlık**: her yenilemede dönen yeni değer saklanır
/// (rotasyon — yenileme işini `AuthInterceptor` yapar, bu model kayıt/giriş
/// akışında kullanılır).
@freezed
abstract class AuthTokens with _$AuthTokens {
  const factory AuthTokens({
    required String accessToken,
    required String refreshToken,

    /// Access token ömrü (saniye) — bilgi amaçlı; istemci süreyi takip etmez,
    /// 401 gelince yenileme yapar.
    int? expiresIn,
  }) = _AuthTokens;

  factory AuthTokens.fromJson(Map<String, dynamic> json) => _$AuthTokensFromJson(json);
}
