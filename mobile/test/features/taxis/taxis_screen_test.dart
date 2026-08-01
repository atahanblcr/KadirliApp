import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/router/app_router.dart';
import 'package:kadirli_app/features/taxis/data/models/taxi_driver.dart';
import 'package:kadirli_app/features/taxis/data/recent_taxi_calls_store.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';
import '../../helpers/profile_fixtures.dart';

/// Taksiciler: liste + detay + çağrı (11.11).
void main() {
  const guest = {'auth.guestChoice': true};

  /// `url_launcher` platform kanalı — testte gerçek çevirici açılmaz; açılan
  /// URI yakalanıp doğrulanır (11.8 `AppLinks` testinin deseni).
  late List<String> launched;
  late bool launchSucceeds;

  setUp(() {
    launched = [];
    launchSucceeds = true;
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(
          const MethodChannel('plugins.flutter.io/url_launcher'),
          (call) async {
            if (call.method == 'launch') {
              launched.add(call.arguments['url'] as String);
              return launchSucceeds;
            }
            if (call.method == 'canLaunch') return true;
            return null;
          },
        );
  });

  tearDown(() {
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(
          const MethodChannel('plugins.flutter.io/url_launcher'),
          null,
        );
  });

  Map<String, dynamic> driver({
    String id = 't1',
    String name = 'Osman Kılıç',
    String phone = '+905331230001',
    String? plaka = '80 T 0101',
    String? vehicle = 'Fiat Egea, Sarı, 2022',
  }) => {
    'id': id,
    'userId': null,
    'name': name,
    'phone': phone,
    'plaka': plaka,
    'vehicleInfo': vehicle,
    'isVerified': true,
    'isActive': true,
  };

  Map<String, dynamic> pagedBody(List<Map<String, dynamic>> items) =>
      successEnvelope({
        'items': items,
        'totalCount': items.length,
        'pageSize': 20,
        'currentPage': 1,
        'totalPages': items.isEmpty ? 0 : 1,
      });

  Future<FakeHttpAdapter> openTaxis(
    WidgetTester tester, {
    Map<String, Future<ResponseBody> Function(RequestOptions)> routes = const {},
    String location = '/taksi',
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

  Map<String, Object> signedInPrefs() => const {};

  TokenStore signedInTokens() => InMemoryTokenStore(
    accessToken: 'access-token',
    refreshToken: 'refresh-token',
  );

  testWidgets('sürücüler ad, plaka ve araçla listelenir', (tester) async {
    await openTaxis(
      tester,
      routes: {
        '/v1/taxis/drivers': (_) async => jsonResponse(pagedBody([driver()])),
      },
    );

    expect(find.text('Osman Kılıç'), findsOneWidget);
    expect(find.text('80 T 0101'), findsOneWidget);
    expect(find.text('Fiat Egea, Sarı, 2022'), findsOneWidget);
    expect(find.text('Ara'), findsOneWidget, reason: 'listede doğrudan arama');
  });

  testWidgets('arama uca searchTerm olarak gider (search DEĞİL)', (
    tester,
  ) async {
    final adapter = await openTaxis(
      tester,
      routes: {
        '/v1/taxis/drivers': (_) async => jsonResponse(pagedBody([driver()])),
      },
    );

    await tester.enterText(find.byType(TextField).first, '80 T');
    await tester.pumpAndSettle();

    final query = adapter.lastOf('/v1/taxis/drivers')?.queryParameters;
    // ⚠️ `QueryTaxiDriverDto` alan adı `SearchTerm`; diğer modüllerdeki
    // `search` burada sessizce yok sayılırdı.
    expect(query?['searchTerm'], '80 T');
    expect(query?.containsKey('search'), isFalse);
  });

  testWidgets('telefonu olmayan sürücüde "Ara" butonu çizilmez', (
    tester,
  ) async {
    await openTaxis(
      tester,
      routes: {
        '/v1/taxis/drivers': (_) async =>
            jsonResponse(pagedBody([driver(phone: '')])),
      },
    );

    expect(find.text('Osman Kılıç'), findsOneWidget);
    expect(find.text('Ara'), findsNothing);
  });

  testWidgets('misafir "Ara"ya basınca çağrı ucuna istek gitmez', (
    tester,
  ) async {
    final adapter = await openTaxis(
      tester,
      routes: {
        '/v1/taxis/drivers': (_) async => jsonResponse(pagedBody([driver()])),
      },
    );

    await tester.tap(find.text('Ara'));
    await tester.pumpAndSettle();

    expect(find.textContaining('giriş yap'), findsWidgets);
    expect(adapter.lastOf('/v1/taxis/drivers/t1/call'), isNull);
    expect(launched, isEmpty);
  });

  testWidgets(
    'oturum açıkken çağrı ucu çağrılır ve dönen numara çevirilir',
    (tester) async {
      final adapter = await openTaxis(
        tester,
        prefs: signedInPrefs(),
        tokenStore: signedInTokens(),
        routes: {
          '/v1/users/me': (_) async =>
              jsonResponse(successEnvelope(profileBody())),
          '/v1/taxis/drivers': (_) async => jsonResponse(pagedBody([driver()])),
          '/v1/taxis/drivers/t1/call': (_) async =>
              jsonResponse(successEnvelope({'phone': '+905331230001'})),
        },
      );

      await tester.tap(find.text('Ara'));
      await tester.pumpAndSettle();

      expect(adapter.lastOf('/v1/taxis/drivers/t1/call')?.method, 'POST');
      expect(launched, ['tel:+905331230001']);
    },
  );

  testWidgets(
    'çağrı kaydı başarısızsa bile aranır ve sebebi kullanıcıya yazılır',
    (tester) async {
      await openTaxis(
        tester,
        prefs: signedInPrefs(),
        tokenStore: signedInTokens(),
        routes: {
          '/v1/users/me': (_) async =>
              jsonResponse(successEnvelope(profileBody())),
          '/v1/taxis/drivers': (_) async => jsonResponse(pagedBody([driver()])),
          '/v1/taxis/drivers/t1/call': (_) async => jsonResponse(
            errorEnvelope('INTERNAL_ERROR', 'Sunucu hatası.'),
            statusCode: 500,
          ),
        },
      );

      await tester.tap(find.text('Ara'));
      await tester.pumpAndSettle();

      // Taksi ihtiyacı acil: kayıt tutulamasa da arama denenir…
      expect(launched, ['tel:+905331230001']);
      // …ama sessizce değil.
      expect(
        find.textContaining('Çağrı kaydı oluşturulamadı'),
        findsOneWidget,
      );
    },
  );

  testWidgets('çağrıdan sonra "Son aradıklarınız" listede belirir', (
    tester,
  ) async {
    await openTaxis(
      tester,
      prefs: signedInPrefs(),
      tokenStore: signedInTokens(),
      routes: {
        '/v1/users/me': (_) async =>
            jsonResponse(successEnvelope(profileBody())),
        '/v1/taxis/drivers': (_) async => jsonResponse(pagedBody([driver()])),
        '/v1/taxis/drivers/t1/call': (_) async =>
            jsonResponse(successEnvelope({'phone': '+905331230001'})),
      },
    );

    expect(find.text('Son aradıklarınız'), findsNothing);

    await tester.tap(find.text('Ara'));
    await tester.pumpAndSettle();

    expect(find.text('Son aradıklarınız'), findsOneWidget);
    expect(find.text('Osman Kılıç · 80 T 0101'), findsOneWidget);
  });

  testWidgets('karta dokununca detay açılır', (tester) async {
    await openTaxis(
      tester,
      routes: {
        '/v1/taxis/drivers': (_) async => jsonResponse(pagedBody([driver()])),
        '/v1/taxis/drivers/t1': (_) async =>
            jsonResponse(successEnvelope(driver())),
      },
    );

    await tester.tap(find.text('Osman Kılıç'));
    await tester.pumpAndSettle();

    expect(find.text('Taksi'), findsOneWidget, reason: 'detay başlığı');
    expect(find.text('Doğrulanmış sürücü'), findsOneWidget);
    expect(find.text('Plaka'), findsOneWidget);
    expect(find.text('Araç'), findsOneWidget);
    expect(find.text('Taksiyi ara'), findsOneWidget);
  });

  testWidgets('bulunamayan sürücü nazik mesaj gösterir', (tester) async {
    await openTaxis(
      tester,
      location: '/taksi/yok',
      routes: {
        '/v1/taxis/drivers': (_) async => jsonResponse(pagedBody(const [])),
        '/v1/taxis/drivers/yok': (_) async => jsonResponse(
          errorEnvelope('NOT_FOUND', 'Sürücü bulunamadı.'),
          statusCode: 404,
        ),
      },
    );

    expect(find.text('Sürücü bulunamadı'), findsOneWidget);
    expect(find.text('Tekrar dene'), findsNothing);
  });

  testWidgets('boş listede açıklayıcı mesaj çıkar', (tester) async {
    await openTaxis(
      tester,
      routes: {
        '/v1/taxis/drivers': (_) async => jsonResponse(pagedBody(const [])),
      },
    );

    expect(find.text('Kayıtlı taksici yok'), findsOneWidget);
  });

  testWidgets('uzun ad + uzun araç bilgisi dar ekranda taşmaz', (tester) async {
    await openTaxis(
      tester,
      routes: {
        '/v1/taxis/drivers': (_) async => jsonResponse(
          pagedBody([
            driver(
              name: 'Abdurrahman Kemalettin Büyükşahinoğlu',
              vehicle: 'Renault Symbol Joy Otomatik Vites, Sarı, 2019 Model',
            ),
          ]),
        ),
      },
    );

    // Dar ekran: 720/3 = 240 dp.
    tester.view.physicalSize = const Size(720, 1600);
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
  });

  group('model', () {
    test('boş plaka/araç alanları etiket üretmez', () {
      final item = TaxiDriver.fromJson(driver(plaka: '  ', vehicle: ''));
      expect(item.plateLabel, isNull);
      expect(item.vehicleLabel, isNull);
      expect(item.hasPhone, isTrue);
    });

    test('paylaşım metni plaka ve telefonu taşır', () {
      final text = TaxiDriver.fromJson(driver()).shareText;
      expect(text, contains('Osman Kılıç'));
      expect(text, contains('80 T 0101'));
      expect(text, contains('+905331230001'));
    });
  });

  group('son aranan kayıt deposu', () {
    test('mükerrer kayıt üretmez, en yeni başa geçer ve üçle sınırlıdır', () async {
      final container = await testContainer();
      final store = container.read(recentTaxiCallsStoreProvider);

      TaxiDriver d(String id) =>
          TaxiDriver.fromJson(driver(id: id, name: 'Sürücü $id'));

      await store.remember(d('a'));
      await store.remember(d('b'));
      await store.remember(d('a'));
      var items = store.read();
      expect(items.map((e) => e.id), ['a', 'b']);

      await store.remember(d('c'));
      await store.remember(d('d'));
      items = store.read();
      expect(items, hasLength(3));
      expect(items.first.id, 'd');
      expect(items.map((e) => e.id), isNot(contains('b')));
    });

    test('telefon numarası SAKLANMAZ (her arama uçtan taze gelmeli)', () async {
      final container = await testContainer();
      final store = container.read(recentTaxiCallsStoreProvider);
      await store.remember(TaxiDriver.fromJson(driver()));

      final entry = store.read().single;
      expect(entry.name, 'Osman Kılıç');
      expect(entry.plaka, '80 T 0101');
      expect(entry.toJson().containsKey('phone'), isFalse);
    });
  });
}
