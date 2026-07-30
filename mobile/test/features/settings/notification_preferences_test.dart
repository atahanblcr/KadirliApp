import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/features/auth/application/auth_controller.dart';
import 'package:kadirli_app/features/auth/data/models/notification_preferences.dart';
import 'package:kadirli_app/features/settings/application/notification_preferences_controller.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/profile_fixtures.dart';
import '../../helpers/pump_app.dart';

/// Bildirim anahtarları: iyimser güncelleme + hata olunca geri alma.
void main() {
  Future<void> signIn(container) =>
      container.read(authControllerProvider.notifier).bootstrap();

  test('anahtar dokunulduğu an değişir ve sunucuya yazılır', () async {
    final adapter = routedAdapter({
      '/v1/users/me': (_) async => jsonResponse(successEnvelope(profileBody())),
      '/v1/users/me/notifications': (_) async => jsonResponse(
        successEnvelope(const {
          'announcements': true,
          'deaths': true,
          'pharmacy': true,
          'events': true,
          'ads': true,
          'campaigns': false,
        }),
      ),
    });
    final container = await testContainer(
      tokenStore: InMemoryTokenStore(accessToken: 'A', refreshToken: 'R'),
      adapter: adapter,
    );
    await signIn(container);

    expect(container.read(currentUserProvider)!.notificationPreferences.ads, isFalse);

    await container
        .read(notificationPreferencesProvider.notifier)
        .toggle(NotificationTopic.ads, true);

    expect(container.read(currentUserProvider)!.notificationPreferences.ads, isTrue);
    expect(adapter.lastOf('/v1/users/me/notifications')!.data, {'ads': true});
    expect(container.read(notificationPreferencesProvider).pending, isEmpty);
    expect(container.read(notificationPreferencesProvider).error, isNull);
  });

  test('sunucu reddederse eski değere dönülür ve sebep gösterilir', () async {
    final adapter = routedAdapter({
      '/v1/users/me': (_) async => jsonResponse(successEnvelope(profileBody())),
      '/v1/users/me/notifications': (_) async => jsonResponse(
        errorEnvelope(ApiErrorCodes.internalError, 'Sunucu hatası.'),
        statusCode: 500,
      ),
    });
    final container = await testContainer(
      tokenStore: InMemoryTokenStore(accessToken: 'A', refreshToken: 'R'),
      adapter: adapter,
    );
    await signIn(container);

    await container
        .read(notificationPreferencesProvider.notifier)
        .toggle(NotificationTopic.announcements, false);

    // Geri alındı: duyuru bildirimleri hâlâ açık.
    expect(
      container.read(currentUserProvider)!.notificationPreferences.announcements,
      isTrue,
    );
    expect(container.read(notificationPreferencesProvider).error, 'Sunucu hatası.');
    expect(container.read(notificationPreferencesProvider).pending, isEmpty);
  });

  test('sunucunun döndüğü tüm tercihler senkronlanır (başka cihaz değişikliği)', () async {
    final adapter = routedAdapter({
      '/v1/users/me': (_) async => jsonResponse(successEnvelope(profileBody())),
      '/v1/users/me/notifications': (_) async => jsonResponse(
        successEnvelope(const {
          'announcements': true,
          'deaths': false, // başka cihazda kapatılmış
          'pharmacy': true,
          'events': true,
          'ads': true,
          'campaigns': false,
        }),
      ),
    });
    final container = await testContainer(
      tokenStore: InMemoryTokenStore(accessToken: 'A', refreshToken: 'R'),
      adapter: adapter,
    );
    await signIn(container);

    await container
        .read(notificationPreferencesProvider.notifier)
        .toggle(NotificationTopic.ads, true);

    final prefs = container.read(currentUserProvider)!.notificationPreferences;
    expect(prefs.ads, isTrue);
    expect(prefs.deaths, isFalse);
  });

  test('oturum yokken istek atılmaz', () async {
    final adapter = routedAdapter({});
    final container = await testContainer(adapter: adapter);
    await signIn(container);

    await container
        .read(notificationPreferencesProvider.notifier)
        .toggle(NotificationTopic.events, false);

    expect(adapter.requests, isEmpty);
  });
}
