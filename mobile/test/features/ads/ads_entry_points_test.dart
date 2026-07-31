import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/router/app_router.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';
import '../../helpers/profile_fixtures.dart';

/// 11.9 giriş noktaları: İlanlar sekmesindeki "İlan ver" düğmesi ve Profil
/// sekmesindeki "İlanlarım" / "Favorilerim" satırları.
///
/// 11.4'ün "işlevsiz buton yok" denetiminin devamı: bu satırlar 11.5-11.8
/// boyunca "Yakında" etiketiyle duruyordu, artık gerçek ekrana gitmeli.
void main() {
  Map<String, dynamic> paged(List<Map<String, dynamic>> items) =>
      successEnvelope({
        'items': items,
        'totalCount': items.length,
        'pageSize': 20,
        'currentPage': 1,
        'totalPages': items.isEmpty ? 0 : 1,
      });

  Map<String, Future<ResponseBody> Function(RequestOptions)> baseRoutes({
    int myAdsCount = 0,
  }) => {
    ...homeStubs(),
    '/v1/users/me': (_) async => jsonResponse(successEnvelope(profileBody())),
    '/v1/users/me/favorites': (_) async => jsonResponse(paged(const [])),
    '/v1/users/me/ads': (_) async => jsonResponse(
      successEnvelope({
        'items': const <Map<String, dynamic>>[],
        'totalCount': myAdsCount,
        'pageSize': 20,
        'currentPage': 1,
        'totalPages': 0,
      }),
    ),
    '/v1/ads/categories': (_) async =>
        jsonResponse(successEnvelope(const <Map<String, dynamic>>[])),
    '/v1/ads': (_) async => jsonResponse(paged(const [])),
  };

  Future<void> openTab(
    WidgetTester tester,
    String location, {
    bool signedIn = true,
    Map<String, Future<ResponseBody> Function(RequestOptions)>? routes,
  }) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final container = await pumpApp(
      tester,
      prefs: signedIn ? const {} : const {'auth.guestChoice': true},
      tokenStore: signedIn
          ? InMemoryTokenStore(accessToken: 'A', refreshToken: 'R')
          : InMemoryTokenStore(),
      adapter: routedAdapter(routes ?? baseRoutes()),
    );
    container.read(routerProvider).go(location);
    await tester.pumpAndSettle();
  }

  testWidgets('İlanlar sekmesinde "İlan ver" düğmesi formu açar', (
    tester,
  ) async {
    await openTab(tester, '/ilanlar');

    await tester.tap(find.text('İlan ver'));
    await tester.pumpAndSettle();

    expect(find.text('İlanınız hangi kategoride?'), findsOneWidget);
  });

  testWidgets('misafir "İlan ver"e basınca sert yönlendirme değil davet görür', (
    tester,
  ) async {
    await openTab(tester, '/ilanlar', signedIn: false);

    await tester.tap(find.text('İlan ver'));
    await tester.pumpAndSettle();

    // Kabuk kapanmaz; nazik davet açılır (11.3/11.4 kararı).
    expect(
      find.textContaining('İlan verebilmek için giriş yapmanız gerekiyor.'),
      findsOneWidget,
    );
    expect(find.text('İlanınız hangi kategoride?'), findsNothing);
  });

  testWidgets('Profil → İlanlarım gerçek ekrana gider ("Yakında" değil)', (
    tester,
  ) async {
    await openTab(tester, '/profil');

    expect(find.text('Yakında'), findsNothing);
    await tester.tap(find.text('İlanlarım'));
    await tester.pumpAndSettle();

    expect(find.text('Henüz ilanınız yok'), findsOneWidget);
  });

  testWidgets('Profil → Favorilerim gerçek ekrana gider', (tester) async {
    await openTab(tester, '/profil');

    await tester.tap(find.text('Favorilerim'));
    await tester.pumpAndSettle();

    expect(find.text('Favoriniz yok'), findsOneWidget);
  });

  testWidgets('İlanlarım satırında ilan sayısı rozeti çıkar', (tester) async {
    await openTab(tester, '/profil', routes: baseRoutes(myAdsCount: 4));

    expect(find.text('4'), findsOneWidget);
  });

  testWidgets('anonim kullanıcıda me-scoped uçlara hiç istek gitmez', (
    tester,
  ) async {
    final adapter = routedAdapter(baseRoutes());
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final container = await pumpApp(
      tester,
      prefs: const {'auth.guestChoice': true},
      adapter: adapter,
    );
    container.read(routerProvider).go('/profil');
    await tester.pumpAndSettle();

    expect(adapter.countOf('/v1/users/me/ads'), 0);
    expect(adapter.countOf('/v1/users/me/favorites'), 0);
  });
}
