import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/push/push_messaging.dart';
import 'package:kadirli_app/features/notifications/application/push_controller.dart';
import 'package:kadirli_app/features/notifications/data/fcm_token_service.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// Push borusu (11.13): izin → token kaydı → mesaj yönlendirmesi.
///
/// 🔑 Firebase'e hiç dokunulmuyor: tüm mantık [PushMessaging] arayüzüne bağlı
/// olduğu için sahte bir sağlayıcıyla test edilebiliyor. **11.14'ün dersi
/// (bayrakla kapatılmış kod yolu = test edilmemiş kod yolu) burada bilinçli
/// olarak uygulandı** — `FcmPushService` sunucuda tam bu yüzden bozuk kalmıştı.
class FakePushMessaging implements PushMessaging {
  FakePushMessaging({
    this.available = true,
    this.permission = PushPermission.granted,
    this.token = 'FAKE-TOKEN',
    this.initial,
  });

  final bool available;
  PushPermission permission;
  String? token;
  PushPayload? initial;

  int requestPermissionCalls = 0;
  int getTokenCalls = 0;
  bool initialConsumed = false;

  final foreground = StreamController<PushPayload>.broadcast();
  final opened = StreamController<PushPayload>.broadcast();
  final tokenRefresh = StreamController<String>.broadcast();

  @override
  bool get isAvailable => available;

  @override
  Future<PushPermission> requestPermission() async {
    requestPermissionCalls++;
    return permission;
  }

  @override
  Future<PushPermission> currentPermission() async => permission;

  @override
  Future<String?> getToken() async {
    getTokenCalls++;
    return token;
  }

  @override
  Stream<PushPayload> get onForegroundMessage => foreground.stream;

  @override
  Stream<PushPayload> get onMessageOpenedApp => opened.stream;

  @override
  Stream<String> get onTokenRefresh => tokenRefresh.stream;

  @override
  Future<PushPayload?> initialMessage() async {
    if (initialConsumed) return null;
    initialConsumed = true;
    return initial;
  }

  void dispose() {
    foreground.close();
    opened.close();
    tokenRefresh.close();
  }
}

void main() {
  const guid = '3f0d0a1e-6a4c-4c7e-9f1a-2b3c4d5e6f70';

  ProviderContainer makeContainer(
    FakePushMessaging messaging,
    FakeHttpAdapter adapter,
  ) {
    final container = ProviderContainer(
      overrides: [
        pushMessagingProvider.overrideWithValue(messaging),
        dioProvider.overrideWith(
          (ref) => DioClient.create(
            tokenStore: InMemoryTokenStore(accessToken: 'ACCESS'),
            adapter: adapter,
            onSessionExpired: () {},
          ),
        ),
      ],
    );
    addTearDown(container.dispose);
    addTearDown(messaging.dispose);
    return container;
  }

  FakeHttpAdapter okAdapter() => routedAdapter({
    '/v1/notifications/fcm-token': (_) async =>
        jsonResponse(successEnvelope({'message': 'ok'})),
    '/v1/notifications': (_) async => jsonResponse(
      successEnvelope({
        'unreadCount': 0,
        'items': <Object>[],
        'totalCount': 0,
        'pageSize': 20,
        'currentPage': 1,
        'totalPages': 0,
      }),
    ),
  });

  group('deviceFcmTokenProvider (11.3 stub → 11.13 gerçek)', () {
    test('izin verilirse token döner', () async {
      final messaging = FakePushMessaging();
      final container = makeContainer(messaging, okAdapter());

      final token = await container.read(deviceFcmTokenProvider)();

      expect(token, 'FAKE-TOKEN');
      expect(messaging.requestPermissionCalls, 1);
      expect(
        messaging.getTokenCalls,
        1,
        reason: 'token izinden SONRA istenmeli',
      );
    });

    test('izin reddedilirse token istenmez', () async {
      final messaging = FakePushMessaging(permission: PushPermission.denied);
      final container = makeContainer(messaging, okAdapter());

      expect(await container.read(deviceFcmTokenProvider)(), isNull);
      expect(
        messaging.getTokenCalls,
        0,
        reason: 'izinsiz token almanın anlamı yok',
      );
    });

    test('sağlayıcı yoksa izin bile istenmez (no-op derleme)', () async {
      final messaging = FakePushMessaging(available: false);
      final container = makeContainer(messaging, okAdapter());

      expect(await container.read(deviceFcmTokenProvider)(), isNull);
      expect(messaging.requestPermissionCalls, 0);
    });
  });

  group('FcmTokenService', () {
    test('token varsa uca gönderilir', () async {
      final messaging = FakePushMessaging();
      final adapter = okAdapter();
      final container = makeContainer(messaging, adapter);

      await container.read(fcmTokenServiceProvider).registerAfterLogin();

      final request = adapter.lastOf('/v1/notifications/fcm-token');
      expect(request, isNotNull);
      expect((request!.data as Map)['token'], 'FAKE-TOKEN');
    });

    test('token yoksa hiç istek atılmaz', () async {
      final messaging = FakePushMessaging(token: null);
      final adapter = okAdapter();
      final container = makeContainer(messaging, adapter);

      await container.read(fcmTokenServiceProvider).registerAfterLogin();

      expect(adapter.countOf('/v1/notifications/fcm-token'), 0);
    });

    test('uç hata verse bile oturum bozulmaz (hata yutulur)', () async {
      final messaging = FakePushMessaging();
      final adapter = routedAdapter({
        '/v1/notifications/fcm-token': (_) async => jsonResponse(
          {
            'success': false,
            'error': {'code': 'NOT_FOUND', 'message': 'Yok.'},
          },
          statusCode: 404,
        ),
      });
      final container = makeContainer(messaging, adapter);

      await expectLater(
        container.read(fcmTokenServiceProvider).registerAfterLogin(),
        completes,
      );
    });
  });

  group('PushCoordinator', () {
    test('başlangıçta izin durumunu yayınlar', () async {
      final messaging = FakePushMessaging(
        permission: PushPermission.notDetermined,
      );
      final container = makeContainer(messaging, okAdapter());

      await container.read(pushCoordinatorProvider).start();

      expect(
        container.read(pushPermissionProvider),
        PushPermission.notDetermined,
      );
    });

    test('token yenilenince sunucudaki kayıt tazelenir', () async {
      final messaging = FakePushMessaging();
      final adapter = okAdapter();
      final container = makeContainer(messaging, adapter);

      await container.read(pushCoordinatorProvider).start();
      messaging.tokenRefresh.add('YENI-TOKEN');
      // ⚠️ Sabit `Future.delayed` provider testlerinde flaky (11.8 dersi):
      // akış olayı + POST birkaç mikro-göreve yayılıyor.
      await waitUntil(
        () => adapter.countOf('/v1/notifications/fcm-token') > 0,
        reason: 'token yenilemesi uca gitmedi',
      );

      final request = adapter.lastOf('/v1/notifications/fcm-token');
      expect(request, isNotNull);
      expect((request!.data as Map)['token'], 'YENI-TOKEN');
    });

    test('ön plan mesajı şeride düşer', () async {
      final messaging = FakePushMessaging();
      final container = makeContainer(messaging, okAdapter());

      await container.read(pushCoordinatorProvider).start();
      const payload = PushPayload(
        notificationId: guid,
        title: 'Duyuru',
        body: 'Gövde',
      );
      messaging.foreground.add(payload);
      await waitUntil(() => container.read(foregroundPushProvider) != null);

      expect(container.read(foregroundPushProvider), payload);
    });

    test('bildirim kimliği varsa okundu işaretlenir', () async {
      final messaging = FakePushMessaging();
      final adapter = routedAdapter({
        '/v1/notifications/$guid/read': (_) async =>
            jsonResponse(successEnvelope({'message': 'ok'})),
        '/v1/notifications': (_) async => jsonResponse(
          successEnvelope({
            'unreadCount': 0,
            'items': <Object>[],
            'totalCount': 0,
            'pageSize': 20,
            'currentPage': 1,
            'totalPages': 0,
          }),
        ),
      });
      final container = makeContainer(messaging, adapter);

      await container
          .read(pushCoordinatorProvider)
          .openNotification(notificationId: guid);
      await waitUntil(
        () => adapter.countOf('/v1/notifications/$guid/read') > 0,
        reason: 'okundu işaretlemesi uca gitmedi',
      );

      expect(adapter.countOf('/v1/notifications/$guid/read'), 1);
    });

    test('okundu işaretleme hatası akışı kesmez', () async {
      final messaging = FakePushMessaging();
      final adapter = routedAdapter({
        '/v1/notifications/$guid/read': (_) async => jsonResponse(
          {
            'success': false,
            'error': {'code': 'NOT_FOUND', 'message': 'Yok.'},
          },
          statusCode: 404,
        ),
      });
      final container = makeContainer(messaging, adapter);

      await expectLater(
        container
            .read(pushCoordinatorProvider)
            .openNotification(notificationId: guid),
        completes,
      );
    });
  });

  group('NoopPushMessaging (yapılandırılmamış derleme)', () {
    test('hiçbir şey yapmaz ama çökmez', () async {
      const messaging = NoopPushMessaging();

      expect(messaging.isAvailable, isFalse);
      expect(await messaging.getToken(), isNull);
      expect(await messaging.requestPermission(), PushPermission.unavailable);
      expect(await messaging.initialMessage(), isNull);
      expect(await messaging.onForegroundMessage.toList(), isEmpty);
      expect(await messaging.onTokenRefresh.toList(), isEmpty);
    });
  });
}
