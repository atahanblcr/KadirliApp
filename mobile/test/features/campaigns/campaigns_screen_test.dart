import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/router/app_router.dart';
import 'package:kadirli_app/features/campaigns/data/models/campaign.dart';

import 'package:kadirli_app/core/network/network.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';
import '../../helpers/profile_fixtures.dart';

/// Kampanyalar: liste + detay + indirim kodu (11.10).
void main() {
  const guest = {'auth.guestChoice': true};

  String isoFromNow(int inDays) =>
      DateTime.now().toUtc().add(Duration(days: inDays)).toIso8601String();

  Map<String, dynamic> campaign({
    String id = 'c1',
    String title = 'Yaz İndirimi',
    String? business = 'Kadirli Kırtasiye',
    num? discount = 25,
    String? code = 'YAZ25',
    String? terms = 'Tek kullanımlıktır.',
    int endsInDays = 20,
  }) => {
    'id': id,
    'businessId': 'b1',
    'businessName': business,
    'title': title,
    'description': 'Tüm kırtasiye ürünlerinde geçerli.',
    'discountPercentage': discount,
    'discountCode': code,
    'terms': terms,
    'startDate': isoFromNow(-5),
    'endDate': isoFromNow(endsInDays),
    'codeViewCount': 3,
    'coverImageId': null,
    'coverImageUrl': null,
    'status': 'approved',
    'createdAt': isoFromNow(-5),
  };

  Map<String, dynamic> pagedBody(List<Map<String, dynamic>> items) =>
      successEnvelope({
        'items': items,
        'totalCount': items.length,
        'pageSize': 20,
        'currentPage': 1,
        'totalPages': items.isEmpty ? 0 : 1,
      });

  Future<FakeHttpAdapter> openCampaigns(
    WidgetTester tester, {
    Map<String, Future<ResponseBody> Function(RequestOptions)> routes = const {},
    String location = '/kampanyalar',
    Map<String, Object> prefs = guest,
    TokenStore? tokenStore,
  }) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final adapter = routedAdapter({...homeStubs(), ...routes});
    final container = await pumpApp(
      tester,
      prefs: prefs,
      adapter: adapter,
      tokenStore: tokenStore,
    );
    container.read(routerProvider).go(location);
    await tester.pumpAndSettle();
    return adapter;
  }

  testWidgets('kampanyalar işletme adı ve indirim rozetiyle listelenir', (
    tester,
  ) async {
    await openCampaigns(
      tester,
      routes: {
        '/v1/campaigns': (_) async => jsonResponse(pagedBody([campaign()])),
      },
    );

    expect(find.text('Yaz İndirimi'), findsOneWidget);
    expect(find.text('Kadirli Kırtasiye'), findsOneWidget);
    expect(find.text('%25 indirim'), findsOneWidget);
    expect(find.text('İndirim kodu'), findsOneWidget);
  });

  testWidgets('bitişine az kalan kampanyada aciliyet rozeti çıkar', (
    tester,
  ) async {
    await openCampaigns(
      tester,
      routes: {
        '/v1/campaigns': (_) async =>
            jsonResponse(pagedBody([campaign(endsInDays: 0)])),
      },
    );

    expect(find.text('Son gün!'), findsOneWidget);
  });

  testWidgets('arama uca search olarak gider', (tester) async {
    final adapter = await openCampaigns(
      tester,
      routes: {
        '/v1/campaigns': (_) async => jsonResponse(pagedBody([campaign()])),
      },
    );

    await tester.enterText(find.byType(TextField), 'kırtasiye');
    await tester.pumpAndSettle();

    expect(
      adapter.lastOf('/v1/campaigns')?.queryParameters['search'],
      'kırtasiye',
    );
  });

  testWidgets('kampanya yoksa açıklayıcı boş durum gösterilir', (tester) async {
    await openCampaigns(
      tester,
      routes: {
        '/v1/campaigns': (_) async => jsonResponse(pagedBody(const [])),
      },
    );

    expect(find.text('Şu an kampanya yok'), findsOneWidget);
    // Filtre yokken "temizle" butonu da olmamalı (işlevsiz buton yok).
    expect(find.text('Aramayı temizle'), findsNothing);
  });

  testWidgets('detayda kod butonu vardır; misafirde giriş daveti çıkar', (
    tester,
  ) async {
    final adapter = await openCampaigns(
      tester,
      location: '/kampanyalar/c1',
      routes: {
        '/v1/campaigns': (_) async => jsonResponse(pagedBody([campaign()])),
        '/v1/campaigns/c1': (_) async =>
            jsonResponse(successEnvelope(campaign())),
      },
    );

    expect(find.text('%25 indirim'), findsOneWidget);
    expect(find.text('Koşullar'), findsOneWidget);

    await tester.tap(find.text('İndirim kodunu göster'));
    await tester.pumpAndSettle();

    // Anonim kullanıcı router'la giriş ekranına ATILMAZ: davet gösterilir ve
    // uca hiç istek gitmez.
    expect(find.textContaining('giriş yap'), findsWidgets);
    expect(adapter.lastOf('/v1/campaigns/c1/view-code'), isNull);
  });

  testWidgets('oturum açıkken kod ucu çağrılır ve modalda gösterilir', (
    tester,
  ) async {
    final adapter = await openCampaigns(
      tester,
      location: '/kampanyalar/c1',
      prefs: const {},
      tokenStore: InMemoryTokenStore(
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
      ),
      routes: {
        '/v1/users/me': (_) async => jsonResponse(successEnvelope(profileBody())),
        '/v1/campaigns': (_) async => jsonResponse(pagedBody([campaign()])),
        '/v1/campaigns/c1': (_) async =>
            jsonResponse(successEnvelope(campaign())),
        '/v1/campaigns/c1/view-code': (_) async => jsonResponse(
          successEnvelope({
            'code': 'YAZ25',
            'viewedAt': DateTime.utc(2026, 8, 1, 9).toIso8601String(),
          }),
        ),
      },
    );

    await tester.tap(find.text('İndirim kodunu göster'));
    await tester.pumpAndSettle();

    expect(adapter.lastOf('/v1/campaigns/c1/view-code'), isNotNull);
    expect(find.text('İndirim kodunuz'), findsOneWidget);
    expect(find.text('YAZ25'), findsOneWidget);
    expect(find.text('Kodu kopyala'), findsOneWidget);
  });

  testWidgets('kodsuz kampanyada buton yerine açıklama çıkar', (tester) async {
    await openCampaigns(
      tester,
      location: '/kampanyalar/c1',
      routes: {
        '/v1/campaigns': (_) async => jsonResponse(pagedBody(const [])),
        '/v1/campaigns/c1': (_) async =>
            jsonResponse(successEnvelope(campaign(code: null))),
      },
    );

    // Uç kodsuz kampanyada 400 döndürüyor → buton hiç çizilmemeli.
    expect(find.text('İndirim kodunu göster'), findsNothing);
    expect(find.textContaining('indirim kodu yok'), findsOneWidget);
  });

  testWidgets('süresi dolan kampanya nazik mesaj gösterir', (tester) async {
    await openCampaigns(
      tester,
      location: '/kampanyalar/yok',
      routes: {
        '/v1/campaigns': (_) async => jsonResponse(pagedBody(const [])),
        '/v1/campaigns/yok': (_) async => jsonResponse(
          errorEnvelope('NOT_FOUND', 'Kampanya bulunamadı.'),
          statusCode: 404,
        ),
      },
    );

    expect(find.text('Kampanya bulunamadı'), findsOneWidget);
    expect(find.text('Tekrar dene'), findsNothing);
  });

  group('model', () {
    Campaign build({num? discount = 25, String? code = 'KOD', int endsIn = 10}) =>
        Campaign(
          id: 'c',
          businessId: 'b',
          title: 'Kampanya',
          discountPercentage: discount?.toDouble(),
          discountCode: code,
          startDate: DateTime.utc(2026, 8, 1),
          endDate: DateTime.utc(2026, 8, 1).add(Duration(days: endsIn)),
        );

    test('indirim etiketi yalnız pozitif oranda yazılır', () {
      expect(build().discountLabel, '%25');
      expect(build(discount: 12.5).discountLabel, '%12,50');
      expect(build(discount: null).discountLabel, isNull);
      expect(build(discount: 0).discountLabel, isNull);
    });

    test('kod boşsa hasCode false', () {
      expect(build().hasCode, isTrue);
      expect(build(code: null).hasCode, isFalse);
      expect(build(code: '   ').hasCode, isFalse);
    });

    test('aciliyet etiketi yalnız son bir haftada çıkar', () {
      final now = DateTime.utc(2026, 8, 1, 6);
      expect(build(endsIn: 0).urgencyLabel(now: now), 'Son gün!');
      expect(build(endsIn: 1).urgencyLabel(now: now), 'Son 1 gün');
      expect(build(endsIn: 4).urgencyLabel(now: now), '4 gün kaldı');
      // Uzun süren kampanyada rozet olmaz (rozet anlamını yitirmesin).
      expect(build(endsIn: 30).urgencyLabel(now: now), isNull);
      // Bitmiş kampanya public uçtan zaten gelmez.
      expect(build(endsIn: -3).urgencyLabel(now: now), isNull);
    });

    test('bitiş günü Kadirli saatine göre hesaplanır', () {
      // 21:00 UTC = ertesi gün 00:00 TR → kampanya TR takviminde 2 Ağustos'ta
      // bitiyor; 1 Ağustos sabahı "yarın bitiyor" denmelidir.
      final campaign = Campaign(
        id: 'c',
        businessId: 'b',
        title: 'Kampanya',
        startDate: DateTime.utc(2026, 7, 20),
        endDate: DateTime.utc(2026, 8, 1, 21),
      );
      expect(campaign.daysLeft(now: DateTime.utc(2026, 8, 1, 6)), 1);
    });
  });
}
