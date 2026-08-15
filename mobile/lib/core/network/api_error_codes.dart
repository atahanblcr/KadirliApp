/// API hata kodları (API_CONTRACT §3) + istemcinin ürettiği kodlar.
///
/// İstemci **her zaman `code`'a göre dallanır**, mesaj metnine göre değil —
/// sunucu mesajları kullanıcıya gösterilebilir ama değişebilir.
abstract final class ApiErrorCodes {
  // --- Sunucu kaynaklı (API_CONTRACT §3) ---
  static const validationError = 'VALIDATION_ERROR';
  static const invalidOtp = 'INVALID_OTP';
  static const invalidPassword = 'INVALID_PASSWORD';
  static const invalidRole = 'INVALID_ROLE';
  static const usernameChangeLimit = 'USERNAME_CHANGE_LIMIT';
  static const neighborhoodChangeLimit = 'NEIGHBORHOOD_CHANGE_LIMIT';
  static const selfDeleteForbidden = 'SELF_DELETE_FORBIDDEN';

  /// Faz 12.17 — kayıt akışında zorunlu bir KVKK onayı eksik.
  ///
  /// ⚠️ Sunucunun mesajı **hangi belgenin** eksik olduğunu söyler ve
  /// `ApiErrorMessages.resolve` sunucu mesajını tercih eder; buradaki karşılık
  /// yalnız mesaj boş geldiğinde devreye girer.
  static const missingConsent = 'MISSING_CONSENT';

  /// Faz 12.17 — zorunlu bir rıza ayarlardan geri alınmaya çalışıldı.
  static const mandatoryConsent = 'MANDATORY_CONSENT';
  static const duplicate = 'DUPLICATE';
  static const unauthorized = 'UNAUTHORIZED';
  static const forbidden = 'FORBIDDEN';
  static const notFound = 'NOT_FOUND';
  static const conflict = 'CONFLICT';
  static const rateLimited = 'RATE_LIMITED';
  static const internalError = 'INTERNAL_ERROR';

  // --- İstemci kaynaklı (sunucuya hiç ulaşılamadığı ya da
  //     yanıtın kontrata uymadığı durumlar) ---
  /// Bağlantı kurulamadı (internet yok, sunucu kapalı, DNS hatası).
  static const networkError = 'NETWORK_ERROR';

  /// İstek zaman aşımına uğradı.
  static const timeout = 'TIMEOUT';

  /// İstek iptal edildi (ekran kapandı vb.) — kullanıcıya gösterilmez.
  static const cancelled = 'CANCELLED';

  /// Yanıt zarfı beklenen biçimde değil / gövde ayrıştırılamadı.
  static const unexpectedResponse = 'UNEXPECTED_RESPONSE';
}
