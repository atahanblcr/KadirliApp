import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';

void main() {
  test('sunucu mesajı varsa o gösterilir (daha spesifik)', () {
    final error = ApiException(
      code: ApiErrorCodes.conflict,
      message: 'Uzatma hakkınız doldu.',
    );

    expect(error.message, 'Uzatma hakkınız doldu.');
  });

  test('sunucu mesajı yoksa sözlükten Türkçe karşılık gelir', () {
    final error = ApiException(code: ApiErrorCodes.usernameChangeLimit);

    expect(error.message, 'Kullanıcı adınızı 30 günde bir değiştirebilirsiniz.');
  });

  test('sunucunun teknik NotFoundException metni kullanıcıya gösterilmez', () {
    // Backend geneli: `Entity "Ad" (guid) was not found.` (İngilizce, teknik).
    final error = ApiException(
      code: ApiErrorCodes.notFound,
      message: 'Entity "Ad" (00000000-0000-0000-0000-000000000000) was not found.',
    );

    expect(error.message, 'Aradığınız kayıt bulunamadı.');
  });

  test('handler\'ın yazdığı özel Türkçe NOT_FOUND mesajı korunur', () {
    final error = ApiException(code: ApiErrorCodes.notFound, message: 'Duyuru bulunamadı.');

    expect(error.message, 'Duyuru bulunamadı.');
  });

  test('bilinmeyen kod → genel mesaj (patlamaz)', () {
    final error = ApiException(code: 'BILINMEYEN_KOD');

    expect(error.message, 'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.');
    expect(ApiErrorMessages.forCode('BILINMEYEN_KOD'), isNull);
  });

  test('boş sunucu mesajı sözlüğe düşer', () {
    expect(
      ApiErrorMessages.resolve(ApiErrorCodes.notFound, serverMessage: '   '),
      'Aradığınız kayıt bulunamadı.',
    );
  });

  test('sözleşmedeki tüm kodların Türkçe karşılığı var', () {
    const codes = [
      ApiErrorCodes.validationError,
      ApiErrorCodes.invalidOtp,
      ApiErrorCodes.invalidPassword,
      ApiErrorCodes.invalidRole,
      ApiErrorCodes.usernameChangeLimit,
      ApiErrorCodes.neighborhoodChangeLimit,
      ApiErrorCodes.selfDeleteForbidden,
      ApiErrorCodes.duplicate,
      ApiErrorCodes.unauthorized,
      ApiErrorCodes.forbidden,
      ApiErrorCodes.notFound,
      ApiErrorCodes.conflict,
      ApiErrorCodes.rateLimited,
      ApiErrorCodes.internalError,
      ApiErrorCodes.networkError,
      ApiErrorCodes.timeout,
      ApiErrorCodes.cancelled,
      ApiErrorCodes.unexpectedResponse,
    ];

    for (final code in codes) {
      expect(ApiErrorMessages.forCode(code), isNotNull, reason: '$code için mesaj yok');
    }
  });
}
