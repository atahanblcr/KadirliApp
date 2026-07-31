import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/router/app_router.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';
import '../../helpers/profile_fixtures.dart';

/// "Favorilerim" (11.9) — liste, yayında olmayan favorinin işaretlenmesi,
/// favoriden çıkarma + geri alma.
void main() {
  Map<String, dynamic> favorite({
    String adId = 'ad-1',
    String title = 'Sahibinden Temiz Fiat Egea',
    num? price = 750000,
    String status = 'approved',
    bool isAvailable = true,
    int viewCount = 12,
  }) => {
    'adId': adId,
    'title': title,
    'price': price,
    'status': status,
    'isAvailable': isAvailable,
    'viewCount': viewCount,
    'favoritedAt': DateTime.now()
        .toUtc()
        .subtract(const Duration(hours: 2))
        .toIso8601String(),
    'imageUrls': const <String>[],
  };

  Map<String, dynamic> paged(List<Map<String, dynamic>> items) =>
      successEnvelope({
        'items': items,
        'totalCount': items.length,
        'pageSize': 20,
        'currentPage': 1,
        'totalPages': items.isEmpty ? 0 : 1,
      });

  Future<FakeHttpAdapter> openFavorites(
    WidgetTester tester, {
    List<Map<String, dynamic>>? favorites,
    Map<String, Future<ResponseBody> Function(RequestOptions)> extraRoutes =
        const {},
  }) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/users/me': (_) async => jsonResponse(successEnvelope(profileBody())),
      '/v1/users/me/ads': (_) async => jsonResponse(paged(const [])),
      '/v1/users/me/favorites': (_) async =>
          jsonResponse(paged(favorites ?? [favorite()])),
      ...extraRoutes,
    });

    final container = await pumpApp(
      tester,
      tokenStore: InMemoryTokenStore(accessToken: 'A', refreshToken: 'R'),
      adapter: adapter,
    );
    container.read(routerProvider).go('/profil/favorilerim');
    await tester.pumpAndSettle();
    return adapter;
  }

  testWidgets('favoriler başlık, fiyat ve görüntülenmeyle listelenir', (
    tester,
  ) async {
    await openFavorites(tester);

    expect(find.text('Sahibinden Temiz Fiat Egea'), findsOneWidget);
    expect(find.text('750.000 ₺'), findsOneWidget);
    expect(find.text('12 görüntülenme'), findsOneWidget);
    expect(find.text('Toplam 1 favori'), findsOneWidget);
  });

  testWidgets('yayında olmayan favori metinli rozetle işaretlenir', (
    tester,
  ) async {
    await openFavorites(
      tester,
      favorites: [favorite(status: 'expired', isAvailable: false)],
    );

    expect(find.text('Şu an yayında değil'), findsOneWidget);
    // Detayına girilmez (404 ekranı göstermek yerine dokunuş kapalı).
    final tile = tester.widget<InkWell>(
      find
          .ancestor(
            of: find.text('Sahibinden Temiz Fiat Egea'),
            matching: find.byType(InkWell),
          )
          .first,
    );
    expect(tile.onTap, isNull);
  });

  testWidgets('favoriden çıkarma satırı anında düşürür ve uca DELETE atar', (
    tester,
  ) async {
    final adapter = await openFavorites(
      tester,
      extraRoutes: {
        '/v1/ads/ad-1/favorite': (_) async =>
            jsonResponse(successEnvelope(true)),
      },
    );

    await tester.tap(find.byTooltip('Favorilerden çıkar'));
    await tester.pumpAndSettle();

    expect(find.text('Sahibinden Temiz Fiat Egea'), findsNothing);
    expect(adapter.lastOf('/v1/ads/ad-1/favorite')?.method, 'DELETE');
    expect(find.text('Favorilerden çıkarıldı'), findsOneWidget);
  });

  testWidgets('geri al satırı yerine koyar ve favoriyi yeniden ekler', (
    tester,
  ) async {
    final adapter = await openFavorites(
      tester,
      extraRoutes: {
        '/v1/ads/ad-1/favorite': (_) async =>
            jsonResponse(successEnvelope(true)),
      },
    );

    await tester.tap(find.byTooltip('Favorilerden çıkar'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Geri al'));
    await tester.pumpAndSettle();

    expect(find.text('Sahibinden Temiz Fiat Egea'), findsOneWidget);
    expect(adapter.lastOf('/v1/ads/ad-1/favorite')?.method, 'POST');
  });

  testWidgets('istek başarısız olursa satır geri gelir ve sebep yazılır', (
    tester,
  ) async {
    await openFavorites(
      tester,
      extraRoutes: {
        '/v1/ads/ad-1/favorite': (_) async => jsonResponse(
          errorEnvelope('SERVER_ERROR', 'Sunucu hatası.'),
          statusCode: 500,
        ),
      },
    );

    await tester.tap(find.byTooltip('Favorilerden çıkar'));
    await tester.pumpAndSettle();

    expect(find.text('Sahibinden Temiz Fiat Egea'), findsOneWidget);
    expect(find.text('Sunucu hatası.'), findsOneWidget);
  });

  testWidgets('favori yoksa ilanlara yönlendiren boş durum çıkar', (
    tester,
  ) async {
    await openFavorites(tester, favorites: const []);

    expect(find.text('Favoriniz yok'), findsOneWidget);
    expect(find.text('İlanlara göz at'), findsOneWidget);
  });
}
