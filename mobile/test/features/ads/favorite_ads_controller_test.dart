import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/features/ads/application/favorite_ads_controller.dart';
import 'package:kadirli_app/features/auth/application/auth_controller.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';
import '../../helpers/profile_fixtures.dart';

/// Favori kimlik kümesi (11.8) — `AdDetailDto`'da `isFavorited` olmadığı için
/// kalbin durumu bu denetleyiciden geliyor.
void main() {
  Map<String, dynamic> favoritesBody(
    List<String> adIds, {
    int totalPages = 1,
    int currentPage = 1,
  }) => successEnvelope({
    'items': [
      for (final id in adIds)
        {
          'adId': id,
          'title': 'İlan $id',
          'price': 100,
          'status': 'approved',
          'isAvailable': true,
          'viewCount': 0,
          'favoritedAt': '2026-07-30T10:00:00Z',
          'imageUrls': <String>[],
        },
    ],
    'totalCount': adIds.length,
    'pageSize': 50,
    'currentPage': currentPage,
    'totalPages': totalPages,
  });

  Future<(FavoriteAdsController, FakeHttpAdapter)> signedInController({
    required Map<String, Future<ResponseBody> Function(RequestOptions)> routes,
  }) async {
    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/users/me': (_) async => jsonResponse(successEnvelope(profileBody())),
      ...routes,
    });

    final container = await testContainer(
      tokenStore: InMemoryTokenStore(accessToken: 'A', refreshToken: 'R'),
      adapter: adapter,
    );
    await container.read(authControllerProvider.notifier).bootstrap();
    container.read(favoriteAdsProvider);
    // ⚠️ Sabit bekleme tüm süit paralel koşarken yetmiyor (flaky).
    await waitUntil(
      () => !container.read(favoriteAdsProvider).isLoading,
      reason: 'favori kümesi yüklensin',
    );
    return (container.read(favoriteAdsProvider.notifier), adapter);
  }

  test('anonim kullanıcıda favori ucuna hiç istek gitmez', () async {
    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/users/me/favorites': (_) async =>
          jsonResponse(favoritesBody(const [])),
    });
    final container = await testContainer(adapter: adapter);

    final state = container.read(favoriteAdsProvider);
    // Negatif iddia (istek gitmemeli) → sınırlı bekleme.
    await Future<void>.delayed(const Duration(milliseconds: 150));

    expect(state.ids, isEmpty);
    expect(state.isLoading, isFalse);
    expect(adapter.countOf('/v1/users/me/favorites'), 0);
  });

  test('oturum açıkken favori kimlikleri okunur', () async {
    final (controller, adapter) = await signedInController(
      routes: {
        '/v1/users/me/favorites': (_) async =>
            jsonResponse(favoritesBody(['ad-1', 'ad-2'])),
      },
    );

    expect(controller.state.ids, {'ad-1', 'ad-2'});
    expect(adapter.countOf('/v1/users/me/favorites'), 1);
  });

  test('birden çok sayfa varsa hepsi okunur', () async {
    var call = 0;
    final (controller, adapter) = await signedInController(
      routes: {
        '/v1/users/me/favorites': (_) async {
          call++;
          return jsonResponse(
            call == 1
                ? favoritesBody(['ad-1'], totalPages: 2)
                : favoritesBody(['ad-2'], totalPages: 2, currentPage: 2),
          );
        },
      },
    );

    expect(controller.state.ids, {'ad-1', 'ad-2'});
    expect(adapter.countOf('/v1/users/me/favorites'), 2);
  });

  test('favori listesi patlasa da ekran çalışmaya devam eder', () async {
    final (controller, _) = await signedInController(
      routes: {
        '/v1/users/me/favorites': (_) async => jsonResponse(
          errorEnvelope('INTERNAL_ERROR', 'Sunucu hatası.'),
          statusCode: 500,
        ),
      },
    );

    expect(controller.state.ids, isEmpty);
    expect(controller.state.isLoading, isFalse);
  });

  test('toggle iyimser ekler ve sunucuya yazar', () async {
    final (controller, adapter) = await signedInController(
      routes: {
        '/v1/users/me/favorites': (_) async =>
            jsonResponse(favoritesBody(const [])),
        '/v1/ads/ad-9/favorite': (_) async =>
            jsonResponse(successEnvelope(true)),
      },
    );

    final future = controller.toggle('ad-9');
    expect(
      controller.state.contains('ad-9'),
      isTrue,
      reason: 'kalp yanıtı beklemeden dolmalı',
    );

    expect(await future, isTrue);
    expect(adapter.countOf('/v1/ads/ad-9/favorite'), 1);
    expect(controller.state.busyId, isNull);
  });

  test('hata olursa iyimser değişiklik geri alınır', () async {
    final (controller, _) = await signedInController(
      routes: {
        '/v1/users/me/favorites': (_) async =>
            jsonResponse(favoritesBody(const [])),
        '/v1/ads/ad-9/favorite': (_) async => jsonResponse(
          errorEnvelope('NOT_FOUND', 'İlan bulunamadı.'),
          statusCode: 404,
        ),
      },
    );

    await expectLater(controller.toggle('ad-9'), throwsA(isA<ApiException>()));
    expect(controller.state.contains('ad-9'), isFalse);
    expect(controller.state.busyId, isNull);
  });

  test('çıkış yapılınca favori kümesi sıfırlanır', () async {
    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/users/me': (_) async => jsonResponse(successEnvelope(profileBody())),
      '/v1/users/me/favorites': (_) async =>
          jsonResponse(favoritesBody(['ad-1'])),
      '/v1/auth/logout': (_) async => jsonResponse(successEnvelope(true)),
    });

    final container = await testContainer(
      tokenStore: InMemoryTokenStore(accessToken: 'A', refreshToken: 'R'),
      adapter: adapter,
    );
    await container.read(authControllerProvider.notifier).bootstrap();
    container.read(favoriteAdsProvider);
    await waitUntil(() => container.read(favoriteAdsProvider).ids.isNotEmpty);
    expect(container.read(favoriteAdsProvider).ids, {'ad-1'});

    await container.read(authControllerProvider.notifier).logout();
    await waitUntil(() => container.read(favoriteAdsProvider).ids.isEmpty);

    // Başka bir hesap açan kullanıcı öncekinin favorilerini görmemeli.
    expect(container.read(favoriteAdsProvider).ids, isEmpty);
  });

  test('favorideki ilana ikinci dokunuş DELETE gönderir', () async {
    final (controller, adapter) = await signedInController(
      routes: {
        '/v1/users/me/favorites': (_) async =>
            jsonResponse(favoritesBody(['ad-1'])),
        '/v1/ads/ad-1/favorite': (_) async =>
            jsonResponse(successEnvelope(true)),
      },
    );

    expect(controller.state.contains('ad-1'), isTrue);
    expect(await controller.toggle('ad-1'), isFalse);
    expect(controller.state.contains('ad-1'), isFalse);
    expect(adapter.lastOf('/v1/ads/ad-1/favorite')?.method, 'DELETE');
  });
}
