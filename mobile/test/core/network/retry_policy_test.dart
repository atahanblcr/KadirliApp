import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';

/// Riverpod 3'ün otomatik yeniden deneme politikası (11.4).
void main() {
  ApiException error(String code, {int? statusCode, Duration? retryAfter}) =>
      ApiException(
        code: code,
        message: 'test',
        statusCode: statusCode,
        retryAfter: retryAfter,
      );

  test('kalıcı hatalarda yeniden denenmez', () {
    expect(apiRetry(0, error(ApiErrorCodes.notFound, statusCode: 404)), isNull);
    expect(apiRetry(0, error(ApiErrorCodes.unauthorized, statusCode: 401)), isNull);
    expect(apiRetry(0, error(ApiErrorCodes.validationError, statusCode: 400)), isNull);
    expect(apiRetry(0, error(ApiErrorCodes.unexpectedResponse)), isNull);
  });

  test('bağlantı sorunu ve sunucu hatası geçici sayılır', () {
    expect(apiRetry(0, error(ApiErrorCodes.networkError)), isNotNull);
    expect(apiRetry(0, error(ApiErrorCodes.timeout)), isNotNull);
    expect(apiRetry(0, error(ApiErrorCodes.internalError, statusCode: 500)), isNotNull);
  });

  test('en fazla iki tekrar (toplam üç deneme)', () {
    expect(apiRetry(0, error(ApiErrorCodes.networkError)), isNotNull);
    expect(apiRetry(1, error(ApiErrorCodes.networkError)), isNotNull);
    expect(apiRetry(2, error(ApiErrorCodes.networkError)), isNull);
  });

  test('429\'da sunucunun verdiği süreye uyulur', () {
    final wait = apiRetry(
      0,
      error(
        ApiErrorCodes.rateLimited,
        statusCode: 429,
        retryAfter: const Duration(seconds: 30),
      ),
    );
    expect(wait, const Duration(seconds: 30));
  });

  test('ApiException olmayan hatalarda denenmez (kod hatası tekrarla düzelmez)', () {
    expect(apiRetry(0, StateError('bug')), isNull);
  });
}
