import 'api_error_codes.dart';

/// `code` → Türkçe kullanıcı mesajı sözlüğü.
///
/// **Öncelik kuralı** ([resolve]): sunucunun `error.message`'ı zaten Türkçe ve
/// kullanıcıya gösterilebilir (API_CONTRACT §2) — üstelik çoğu zaman daha
/// spesifiktir ("Uzatma hakkınız doldu" > "Bir çakışma oluştu"). Bu yüzden
/// sunucu mesajı varsa o kullanılır; yoksa (ya da istemci kaynaklı bir kodsa)
/// bu sözlük devreye girer; kod da bilinmiyorsa genel mesaj döner.
abstract final class ApiErrorMessages {
  static const _generic = 'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.';

  static const Map<String, String> _messages = {
    ApiErrorCodes.validationError: 'Girdiğiniz bilgilerde bir sorun var. Lütfen kontrol edin.',
    ApiErrorCodes.invalidOtp: 'Doğrulama kodu hatalı ya da süresi dolmuş.',
    ApiErrorCodes.invalidPassword: 'Mevcut şifreniz hatalı.',
    ApiErrorCodes.invalidRole: 'Bu işlem için yetkiniz uygun değil.',
    ApiErrorCodes.usernameChangeLimit: 'Kullanıcı adınızı 30 günde bir değiştirebilirsiniz.',
    ApiErrorCodes.neighborhoodChangeLimit: 'Mahallenizi 30 günde bir değiştirebilirsiniz.',
    ApiErrorCodes.selfDeleteForbidden: 'Bu hesap uygulamadan silinemez.',
    ApiErrorCodes.duplicate: 'Bu kayıt zaten mevcut.',
    ApiErrorCodes.unauthorized: 'Oturumunuzun süresi doldu. Lütfen tekrar giriş yapın.',
    ApiErrorCodes.forbidden: 'Bu işlem için yetkiniz yok.',
    ApiErrorCodes.notFound: 'Aradığınız kayıt bulunamadı.',
    ApiErrorCodes.conflict: 'Bu işlem şu an yapılamıyor.',
    ApiErrorCodes.rateLimited: 'Çok fazla deneme yaptınız. Lütfen biraz bekleyin.',
    ApiErrorCodes.internalError: 'Sunucuda bir sorun oluştu. Lütfen daha sonra deneyin.',
    ApiErrorCodes.networkError: 'İnternet bağlantısı kurulamadı. Bağlantınızı kontrol edin.',
    ApiErrorCodes.timeout: 'Sunucu yanıt vermedi. Lütfen tekrar deneyin.',
    ApiErrorCodes.cancelled: 'İşlem iptal edildi.',
    ApiErrorCodes.unexpectedResponse: 'Sunucudan beklenmeyen bir yanıt geldi.',
  };

  /// Sunucunun **teknik** (kullanıcıya gösterilemez) mesaj kalıpları.
  ///
  /// Backend'in genel `NotFoundException`'ı İngilizce bir geliştirici metni
  /// üretiyor: `Entity "Ad" (00000000-…) was not found.` — bu 29 Tem 2026'da
  /// canlı tanılamada yakalandı. Kontrat dondurulduğu için (Faz 11 kural 3)
  /// backend'e dokunulmadı; istemci bu kalıbı eleyip kendi Türkçe mesajını
  /// gösteriyor. Handler'ların yazdığı özel Türkçe mesajlar ("Duyuru
  /// bulunamadı.") etkilenmez.
  static final RegExp _technicalMessage = RegExp(r'^Entity ".*" \(.*\) was not found\.?$');

  /// Bu kod için sözlükteki mesaj (yoksa null) — testler ve özel akışlar için.
  static String? forCode(String code) => _messages[code];

  /// Kullanıcıya gösterilecek nihai mesaj.
  static String resolve(String code, {String? serverMessage}) {
    final trimmed = serverMessage?.trim();
    if (trimmed != null && trimmed.isNotEmpty && !_technicalMessage.hasMatch(trimmed)) {
      return trimmed;
    }
    return _messages[code] ?? _generic;
  }
}
