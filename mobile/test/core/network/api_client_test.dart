import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';

import 'fake_http_adapter.dart';

/// Zarf açma + hata eşleme (Faz 11.2 "bitti kriteri").
void main() {
  const baseUrl = 'http://localhost:5005';

  ApiClient clientWith(
    Future<ResponseBody> Function(RequestOptions options) handler, {
    TokenStore? tokenStore,
    void Function()? onSessionExpired,
    FakeHttpAdapter? adapter,
  }) {
    final dio = DioClient.create(
      tokenStore: tokenStore ?? InMemoryTokenStore(),
      baseUrl: baseUrl,
      onSessionExpired: onSessionExpired,
      adapter: adapter ?? FakeHttpAdapter(handler),
    );
    return ApiClient(dio);
  }

  group('zarf açma', () {
    test('success:true → yalnız data döner (liste)', () async {
      final client = clientWith(
        (_) async => jsonResponse(
          successEnvelope([
            {'id': '1', 'name': 'Cengiz Topel'},
            {'id': '2', 'name': 'Savrun'},
          ]),
        ),
      );

      final names = await client.getList('/v1/neighborhoods', (json) => json['name'] as String);

      expect(names, ['Cengiz Topel', 'Savrun']);
    });

    test('sayfalı gövde PagedResult olarak ayrıştırılır', () async {
      final client = clientWith(
        (_) async => jsonResponse(
          successEnvelope({
            'items': [
              {'id': '1', 'title': 'Duyuru'},
            ],
            'totalCount': 37,
            'pageSize': 20,
            'currentPage': 1,
            'totalPages': 2,
          }),
        ),
      );

      final page = await client.getPaged(
        '/v1/announcements',
        (json) => json['title'] as String,
        page: 1,
        limit: 20,
      );

      expect(page.items, ['Duyuru']);
      expect(page.totalCount, 37);
      expect(page.hasNextPage, isTrue);
    });

    test('zarfsız yanıt (ör. /health) olduğu gibi geçer', () async {
      final client = clientWith((_) async => jsonResponse({'status': 'Healthy'}));

      final data = await client.get('/health');

      expect(data, {'status': 'Healthy'});
    });
  });

  group('hata eşleme', () {
    test('HTTP 200 + success:false (announcements quirk) → ApiException', () async {
      final client = clientWith(
        (_) async => jsonResponse(errorEnvelope('NOT_FOUND', 'Duyuru bulunamadı.')),
      );

      final error = await _captureError(() => client.get('/v1/announcements/xyz'));

      expect(error.code, ApiErrorCodes.notFound);
      expect(error.isNotFound, isTrue);
      expect(error.message, 'Duyuru bulunamadı.'); // sunucu mesajı korunur
      expect(error.traceId, 'trace-err');
      expect(error.statusCode, 200);
    });

    test('HTTP 404 hata zarfı → NOT_FOUND', () async {
      final client = clientWith(
        (_) async => jsonResponse(
          errorEnvelope('NOT_FOUND', 'İlan bulunamadı.'),
          statusCode: 404,
        ),
      );

      final error = await _captureError(() => client.get('/v1/ads/xyz'));

      expect(error.code, ApiErrorCodes.notFound);
      expect(error.statusCode, 404);
      expect(error.traceId, 'trace-err');
    });

    test('HTTP 429 → RATE_LIMITED + Retry-After okunur', () async {
      final client = clientWith(
        (_) async => jsonResponse(
          errorEnvelope('RATE_LIMITED', 'Çok fazla istek.'),
          statusCode: 429,
          headers: {
            'retry-after': ['42'],
          },
        ),
      );

      final error = await _captureError(() => client.post('/v1/complaints'));

      expect(error.isRateLimited, isTrue);
      expect(error.retryAfter, const Duration(seconds: 42));
    });

    test('bağlantı kurulamadı → NETWORK_ERROR + sözlük mesajı', () async {
      final client = clientWith(
        (options) async => throw DioException(
          requestOptions: options,
          type: DioExceptionType.connectionError,
        ),
      );

      final error = await _captureError(() => client.get('/v1/neighborhoods'));

      expect(error.code, ApiErrorCodes.networkError);
      expect(error.isConnectionProblem, isTrue);
      expect(error.message, ApiErrorMessages.forCode(ApiErrorCodes.networkError));
      expect(error.statusCode, isNull);
    });

    test('zaman aşımı → TIMEOUT', () async {
      final client = clientWith(
        (options) async => throw DioException(
          requestOptions: options,
          type: DioExceptionType.receiveTimeout,
        ),
      );

      final error = await _captureError(() => client.get('/v1/ads'));

      expect(error.code, ApiErrorCodes.timeout);
    });

    test('beklenen şekil gelmezse UNEXPECTED_RESPONSE', () async {
      final client = clientWith(
        (_) async => jsonResponse(successEnvelope({'items': 'liste değil'})),
      );

      final error = await _captureError(
        () => client.getList('/v1/neighborhoods', (json) => json['name'] as String),
      );

      expect(error.code, ApiErrorCodes.unexpectedResponse);
    });
  });

  group('auth interceptor', () {
    test('token varsa Bearer header eklenir', () async {
      final adapter = FakeHttpAdapter((_) async => jsonResponse(successEnvelope({'id': '1'})));
      final client = clientWith(
        (_) async => jsonResponse(successEnvelope({'id': '1'})),
        tokenStore: InMemoryTokenStore(accessToken: 'ACCESS-1', refreshToken: 'REFRESH-1'),
        adapter: adapter,
      );

      await client.get('/v1/users/me');

      expect(adapter.lastOf('/v1/users/me')?.headers['Authorization'], 'Bearer ACCESS-1');
    });

    test('401 → refresh → istek tekrarlanır, yeni token'
        ' saklanır', () async {
      final store = InMemoryTokenStore(accessToken: 'OLD', refreshToken: 'REFRESH-1');
      late final FakeHttpAdapter adapter;
      adapter = FakeHttpAdapter((options) async {
        if (options.path == '/v1/auth/refresh') {
          return jsonResponse(
            successEnvelope({'accessToken': 'NEW', 'refreshToken': 'REFRESH-2'}),
          );
        }
        if (options.headers['Authorization'] == 'Bearer NEW') {
          return jsonResponse(successEnvelope({'username': 'atahan'}));
        }
        return jsonResponse(
          errorEnvelope('UNAUTHORIZED', 'Oturum geçersiz.'),
          statusCode: 401,
        );
      });
      final client = clientWith((_) async => throw UnimplementedError(),
          tokenStore: store, adapter: adapter);

      final data = await client.get('/v1/users/me');

      expect(data, {'username': 'atahan'});
      expect(adapter.countOf('/v1/auth/refresh'), 1);
      expect(adapter.countOf('/v1/users/me'), 2); // ilk 401 + tekrar
      expect(await store.readAccessToken(), 'NEW');
      expect(await store.readRefreshToken(), 'REFRESH-2'); // rotasyon saklandı
    });

    test('eşzamanlı 401\'ler tek refresh tetikler', () async {
      final store = InMemoryTokenStore(accessToken: 'OLD', refreshToken: 'REFRESH-1');
      final adapter = FakeHttpAdapter((options) async {
        if (options.path == '/v1/auth/refresh') {
          await Future<void>.delayed(const Duration(milliseconds: 20));
          return jsonResponse(
            successEnvelope({'accessToken': 'NEW', 'refreshToken': 'REFRESH-2'}),
          );
        }
        if (options.headers['Authorization'] == 'Bearer NEW') {
          return jsonResponse(successEnvelope({'ok': true}));
        }
        return jsonResponse(errorEnvelope('UNAUTHORIZED', 'Oturum geçersiz.'), statusCode: 401);
      });
      final client = clientWith((_) async => throw UnimplementedError(),
          tokenStore: store, adapter: adapter);

      await Future.wait([
        client.get('/v1/users/me'),
        client.get('/v1/notifications'),
        client.get('/v1/users/me/ads'),
      ]);

      expect(adapter.countOf('/v1/auth/refresh'), 1);
    });

    test('refresh de reddedilirse oturum temizlenir ve sinyal gider', () async {
      final store = InMemoryTokenStore(accessToken: 'OLD', refreshToken: 'REFRESH-1');
      var expiredSignals = 0;
      final adapter = FakeHttpAdapter(
        (_) async => jsonResponse(
          errorEnvelope('UNAUTHORIZED', 'Oturum geçersiz.'),
          statusCode: 401,
        ),
      );
      final client = clientWith(
        (_) async => throw UnimplementedError(),
        tokenStore: store,
        adapter: adapter,
        onSessionExpired: () => expiredSignals++,
      );

      final error = await _captureError(() => client.get('/v1/users/me'));

      expect(error.isUnauthorized, isTrue);
      expect(expiredSignals, 1);
      expect(await store.readAccessToken(), isNull);
      expect(await store.hasSession(), isFalse);
    });

    test('çevrimdışıyken refresh başarısız olursa oturum SİLİNMEZ', () async {
      final store = InMemoryTokenStore(accessToken: 'OLD', refreshToken: 'REFRESH-1');
      var expiredSignals = 0;
      final adapter = FakeHttpAdapter((options) async {
        if (options.path == '/v1/auth/refresh') {
          throw DioException(
            requestOptions: options,
            type: DioExceptionType.connectionError,
          );
        }
        return jsonResponse(errorEnvelope('UNAUTHORIZED', 'Oturum geçersiz.'), statusCode: 401);
      });
      final client = clientWith(
        (_) async => throw UnimplementedError(),
        tokenStore: store,
        adapter: adapter,
        onSessionExpired: () => expiredSignals++,
      );

      await _captureError(() => client.get('/v1/users/me'));

      expect(expiredSignals, 0);
      expect(await store.readRefreshToken(), 'REFRESH-1');
    });

    test('login 401\'i yenileme denemez (kimlik hatası)', () async {
      final store = InMemoryTokenStore(accessToken: 'OLD', refreshToken: 'REFRESH-1');
      final adapter = FakeHttpAdapter(
        (_) async => jsonResponse(
          errorEnvelope('INVALID_OTP', 'Doğrulama kodu hatalı.'),
          statusCode: 400,
        ),
      );
      final client = clientWith((_) async => throw UnimplementedError(),
          tokenStore: store, adapter: adapter);

      final error = await _captureError(() => client.post('/v1/auth/verify-otp'));

      expect(error.code, ApiErrorCodes.invalidOtp);
      expect(adapter.countOf('/v1/auth/refresh'), 0);
      expect(await store.readRefreshToken(), 'REFRESH-1');
    });
  });
}

Future<ApiException> _captureError(Future<void> Function() action) async {
  try {
    await action();
  } on ApiException catch (error) {
    return error;
  }
  fail('ApiException bekleniyordu');
}
