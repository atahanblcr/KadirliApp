import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/features/auth/application/auth_controller.dart';
import 'package:kadirli_app/features/auth/application/auth_state.dart';
import 'package:kadirli_app/features/auth/data/auth_repository.dart';
import 'package:kadirli_app/features/auth/data/models/verify_otp_result.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// Oturum durum makinesi: açılış, giriş, kayıt, çıkış, oturum düşmesi.
void main() {
  Map<String, dynamic> meBody({String username = 'ahmetk'}) => {
    'id': '11111111-1111-1111-1111-111111111111',
    'phone': '+905321110001',
    'username': username,
    'role': 'user',
    'primaryNeighborhoodId': '22222222-2222-2222-2222-222222222222',
    'primaryNeighborhoodName': 'Savrun',
  };

  group('bootstrap', () {
    test('oturum yoksa anonim', () async {
      final container = await testContainer(
        adapter: routedAdapter({}),
      );

      await container.read(authControllerProvider.notifier).bootstrap();

      expect(container.read(authControllerProvider), isA<AuthAnonymous>());
    });

    test('token varsa profil çekilir ve önbelleğe yazılır', () async {
      final container = await testContainer(
        tokenStore: InMemoryTokenStore(accessToken: 'A', refreshToken: 'R'),
        adapter: routedAdapter({
          '/v1/users/me': (_) async => jsonResponse(successEnvelope(meBody())),
        }),
      );

      await container.read(authControllerProvider.notifier).bootstrap();

      final state = container.read(authControllerProvider);
      expect(state, isA<AuthAuthenticated>());
      expect(state.user?.username, 'ahmetk');

      final prefs = await SharedPreferences.getInstance();
      expect(prefs.getString('auth.cachedUser'), isNotNull);
    });

    test('sunucu oturumu reddederse (401) anonime düşer ve token silinir', () async {
      final store = InMemoryTokenStore(accessToken: 'A', refreshToken: 'R');
      final container = await testContainer(
        tokenStore: store,
        adapter: FakeHttpAdapter(
          (_) async => jsonResponse(
            errorEnvelope('UNAUTHORIZED', 'Oturum geçersiz.'),
            statusCode: 401,
          ),
        ),
      );

      await container.read(authControllerProvider.notifier).bootstrap();

      expect(container.read(authControllerProvider), isA<AuthAnonymous>());
      expect(await store.hasSession(), isFalse);
    });

    test('çevrimdışıyken önbellekteki profille oturum korunur, token silinmez', () async {
      final store = InMemoryTokenStore(accessToken: 'A', refreshToken: 'R');
      final container = await testContainer(
        prefs: {'auth.cachedUser': jsonEncode(meBody(username: 'onbellek'))},
        tokenStore: store,
        adapter: FakeHttpAdapter(
          (options) async => throw DioException(
            requestOptions: options,
            type: DioExceptionType.connectionError,
          ),
        ),
      );

      await container.read(authControllerProvider.notifier).bootstrap();

      final state = container.read(authControllerProvider);
      expect(state, isA<AuthAuthenticated>());
      expect(state.user?.username, 'onbellek');
      expect(await store.readRefreshToken(), 'R'); // oturum düşürülmedi
    });
  });

  group('giriş / kayıt', () {
    test('yeni kullanıcı → registering; tempToken DEPOLANMAZ', () async {
      final store = InMemoryTokenStore();
      final container = await testContainer(tokenStore: store, adapter: routedAdapter({}));

      await container.read(authControllerProvider.notifier).completeOtpVerification(
        phoneE164: '+905339990001',
        result: const VerifyOtpResult(isNewUser: true, tempToken: 'TEMP'),
      );

      final state = container.read(authControllerProvider);
      expect(state, isA<AuthRegistering>());
      expect((state as AuthRegistering).tempToken, 'TEMP');
      expect(await store.readAccessToken(), isNull);
      expect(await store.readRefreshToken(), isNull);
    });

    test('kayıtlı kullanıcı → token saklanır, profil okunur, oturum açılır', () async {
      final store = InMemoryTokenStore();
      final adapter = routedAdapter({
        '/v1/users/me': (_) async => jsonResponse(successEnvelope(meBody())),
      });
      final container = await testContainer(tokenStore: store, adapter: adapter);

      await container.read(authControllerProvider.notifier).completeOtpVerification(
        phoneE164: '+905321110001',
        result: const VerifyOtpResult(
          accessToken: 'ACCESS',
          refreshToken: 'REFRESH',
          expiresIn: 86400,
        ),
      );

      expect(container.read(authControllerProvider), isA<AuthAuthenticated>());
      expect(await store.readAccessToken(), 'ACCESS');
      expect(await store.readRefreshToken(), 'REFRESH');
      // FCM kaydı: 11.3'te cihaz token'ı yok → uç ÇAĞRILMAZ.
      expect(adapter.countOf('/v1/notifications/fcm-token'), 0);
    });

    test('kayıt tamamlanınca oturum açılır', () async {
      final store = InMemoryTokenStore();
      final container = await testContainer(
        tokenStore: store,
        adapter: routedAdapter({
          '/v1/auth/register': (_) async => jsonResponse(
            successEnvelope({'accessToken': 'ACCESS', 'refreshToken': 'REFRESH'}),
          ),
          '/v1/users/me': (_) async => jsonResponse(successEnvelope(meBody(username: 'yeni'))),
        }),
      );
      final controller = container.read(authControllerProvider.notifier);

      await controller.completeOtpVerification(
        phoneE164: '+905339990001',
        result: const VerifyOtpResult(isNewUser: true, tempToken: 'TEMP'),
      );
      await controller.completeRegistration(
        username: 'yeni',
        neighborhoodId: '22222222-2222-2222-2222-222222222222',
        age: 30,
      );

      expect(container.read(authControllerProvider).user?.username, 'yeni');
      expect(await store.readAccessToken(), 'ACCESS');
    });

    test('kayıt akışından vazgeçilince anonime döner', () async {
      final container = await testContainer(adapter: routedAdapter({}));
      final controller = container.read(authControllerProvider.notifier);

      await controller.completeOtpVerification(
        phoneE164: '+905339990001',
        result: const VerifyOtpResult(isNewUser: true, tempToken: 'TEMP'),
      );
      controller.cancelRegistration();

      expect(container.read(authControllerProvider), isA<AuthAnonymous>());
    });
  });

  group('çıkış ve oturum düşmesi', () {
    test('çıkışta sunucuya haber verilir, token ve önbellek temizlenir', () async {
      final store = InMemoryTokenStore(accessToken: 'A', refreshToken: 'R');
      final adapter = routedAdapter({
        '/v1/users/me': (_) async => jsonResponse(successEnvelope(meBody())),
        '/v1/auth/logout': (_) async =>
            jsonResponse(successEnvelope({'message': 'Çıkış yapıldı'})),
      });
      final container = await testContainer(tokenStore: store, adapter: adapter);
      final controller = container.read(authControllerProvider.notifier);
      await controller.bootstrap();

      await controller.logout();

      expect(container.read(authControllerProvider), isA<AuthAnonymous>());
      expect(await store.hasSession(), isFalse);
      expect(adapter.countOf('/v1/auth/logout'), 1);
      final prefs = await SharedPreferences.getInstance();
      expect(prefs.getString('auth.cachedUser'), isNull);
    });

    test('sunucu çıkışı hata verse de yerel oturum kapanır', () async {
      final store = InMemoryTokenStore(accessToken: 'A', refreshToken: 'R');
      final container = await testContainer(
        tokenStore: store,
        adapter: routedAdapter({
          '/v1/users/me': (_) async => jsonResponse(successEnvelope(meBody())),
          '/v1/auth/logout': (_) async => jsonResponse(
            errorEnvelope('INTERNAL_ERROR', 'Sunucu hatası'),
            statusCode: 500,
          ),
        }),
      );
      final controller = container.read(authControllerProvider.notifier);
      await controller.bootstrap();

      await controller.logout();

      expect(container.read(authControllerProvider), isA<AuthAnonymous>());
      expect(await store.hasSession(), isFalse);
    });

    test('yenileme reddedilince (oturum düştü) durum anonime iner + bilgi mesajı', () async {
      final store = InMemoryTokenStore(accessToken: 'A', refreshToken: 'R');
      var meCalls = 0;
      final container = await testContainer(
        tokenStore: store,
        adapter: FakeHttpAdapter((options) async {
          if (options.path == '/v1/users/me' && meCalls++ == 0) {
            return jsonResponse(successEnvelope(meBody()));
          }
          return jsonResponse(
            errorEnvelope('UNAUTHORIZED', 'Oturum geçersiz.'),
            statusCode: 401,
          );
        }),
      );
      final controller = container.read(authControllerProvider.notifier);
      await controller.bootstrap();
      expect(container.read(authControllerProvider), isA<AuthAuthenticated>());

      // Korumalı bir uç 401 → interceptor yeniler → o da 401 → oturum düşer.
      await expectLater(
        container.read(authRepositoryProvider).fetchCurrentUser(),
        throwsA(isA<ApiException>()),
      );

      expect(container.read(authControllerProvider), isA<AuthAnonymous>());
      expect(container.read(authNoticeProvider), contains('süresi doldu'));
    });

    test('misafir seçimi kalıcıdır (açılışta bir daha giriş sorulmaz)', () async {
      final container = await testContainer(adapter: routedAdapter({}));
      final controller = container.read(authControllerProvider.notifier);

      expect(controller.hasChosenGuest, isFalse);
      await controller.continueAsGuest();

      expect(controller.hasChosenGuest, isTrue);
      expect(container.read(authControllerProvider), isA<AuthAnonymous>());
    });
  });
}
