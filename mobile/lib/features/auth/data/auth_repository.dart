import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../legal/data/legal_repository.dart' show ConsentDecision;
import 'models/auth_tokens.dart';
import 'models/current_user.dart';
import 'models/otp_challenge.dart';
import 'models/verify_otp_result.dart';

/// Auth uçlarının tek sarmalayıcısı (API_CONTRACT §4).
///
/// Hata olarak yalnız [ApiException] fırlatır (zarf açma ve çeviri ağ
/// katmanında). Token **saklama** işi burada değil [AuthController]'da:
/// repository yalnız HTTP konuşur, oturum durumunu tutmaz.
class AuthRepository {
  AuthRepository(this._api);

  final ApiClient _api;

  /// OTP ister. Telefon **E.164** olmalı (`+905321110001`) — sunucu OTP'yi ham
  /// telefon string'iyle anahtarlıyor, biçim tutarsızlığı ayrı kayıt üretir.
  Future<OtpChallenge> requestOtp(String phoneE164) async {
    final data = await _api.post('/v1/auth/login', body: {'phone': phoneE164});
    if (data is! Map) throw ApiException.unexpectedResponse(cause: data);
    return OtpChallenge.fromJson(Map<String, dynamic>.from(data));
  }

  /// Kodu doğrular. Yeni kullanıcıda `tempToken`, kayıtlıda token çifti döner.
  Future<VerifyOtpResult> verifyOtp({required String phoneE164, required String otp}) async {
    final data = await _api.post(
      '/v1/auth/verify-otp',
      body: {'phone': phoneE164, 'otp': otp},
    );
    if (data is! Map) throw ApiException.unexpectedResponse(cause: data);
    return VerifyOtpResult.fromJson(Map<String, dynamic>.from(data));
  }

  /// Kaydı tamamlar (yeni kullanıcı). `age` boş bırakılabilir.
  ///
  /// [consents] — Faz 12.17: kullanıcının **gördüğü sürümlere** verdiği KVKK
  /// kararları. Alan sunucuda **additive** (12.16): boş gönderildiğinde
  /// davranış, yayında zorunlu bir belge olmadığı sürece birebir eskisi gibi.
  ///
  /// 🔴 Zorunlu bir belge varsa ve onayı gelmezse sunucu **400
  /// `MISSING_CONSENT`** döner ve **hangi belgenin** eksik olduğunu söyler —
  /// istemci o mesajı kullanıcıya olduğu gibi gösterir.
  ///
  /// ⚠️ Rıza kaydının bağlamı (IP · tarayıcı) **sunucuda** doldurulur;
  /// gövdeden gönderilen bir değer ezilir. İstemci onu hiç göndermez.
  Future<AuthTokens> register({
    required String tempToken,
    required String username,
    required String primaryNeighborhoodId,
    int? age,
    List<ConsentDecision> consents = const [],
  }) async {
    final data = await _api.post(
      '/v1/auth/register',
      body: {
        'tempToken': tempToken,
        'username': username,
        'primaryNeighborhoodId': primaryNeighborhoodId,
        'age': age,
        'consents': consents.map((decision) => decision.toJson()).toList(),
      },
    );
    if (data is! Map) throw ApiException.unexpectedResponse(cause: data);
    return AuthTokens.fromJson(Map<String, dynamic>.from(data));
  }

  /// Sunucu tarafında refresh'i iptal eder ve cihazın FCM token'ını temizler.
  /// `[Authorize]` — access token geçerli değilse sunucu 401 verir; çağıran
  /// yerel temizliği yine yapar (bkz. [AuthController.logout]).
  Future<void> logout({String? refreshToken}) =>
      _api.post('/v1/auth/logout', body: {'refreshToken': refreshToken});

  /// Oturum sahibinin profili. Aynı zamanda **oturum geçerlilik testi**:
  /// 401 gelirse `AuthInterceptor` sessizce yeniler, o da olmazsa
  /// `UNAUTHORIZED` fırlar.
  Future<CurrentUser> fetchCurrentUser() =>
      _api.getObject('/v1/users/me', CurrentUser.fromJson);
}

final authRepositoryProvider = Provider<AuthRepository>(
  (ref) => AuthRepository(ref.watch(apiClientProvider)),
);
