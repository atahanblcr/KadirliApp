import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/router/app_router.dart';
import 'package:kadirli_app/core/utils/app_date.dart';
import 'package:kadirli_app/features/deaths/data/models/death_notice.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';
import '../../helpers/profile_fixtures.dart';

/// Vefat ilanları: liste + detay + bildirim (11.11).
void main() {
  const guest = {'auth.guestChoice': true};

  /// Bugünden [inDays] gün sonrası — **Kadirli takvim günü**, 00:00 UTC.
  /// (`funeralDate` sunucuda bu konvansiyonla yazılıyor; UTC "şimdi"si
  /// kullanılırsa gece 00:00-03:00 arasında gün kayar — 11.10 dersinin aynısı.)
  String dayFromNow(int inDays) {
    final day = AppDate.nowInTurkey.add(Duration(days: inDays));
    return '${day.year.toString().padLeft(4, '0')}-'
        '${day.month.toString().padLeft(2, '0')}-'
        '${day.day.toString().padLeft(2, '0')}';
  }

  Map<String, dynamic> notice({
    String id = 'd1',
    String name = 'Emine Kaya',
    int inDays = 0,
    String time = '11:00:00',
    String? mosque = 'Yenimahalle Cami',
    String? cemetery = 'Karataş Mezarlığı',
    String? condolenceAddress,
    double? lat,
    double? lng,
    String status = 'approved',
    String? photoUrl,
  }) => {
    'id': id,
    'deceasedName': name,
    'photoFileId': photoUrl == null ? null : 'file-1',
    'photoUrl': photoUrl,
    'funeralDate': '${dayFromNow(inDays)}T00:00:00Z',
    'funeralTime': time,
    'cemeteryId': cemetery == null ? null : 'cem-1',
    'cemeteryName': cemetery,
    'mosqueId': mosque == null ? null : 'mos-1',
    'mosqueName': mosque,
    'neighborhoodId': null,
    'condolenceAddress': condolenceAddress,
    'condolenceLatitude': lat,
    'condolenceLongitude': lng,
    'hasCondolenceLocation': lat != null && lng != null,
    'addedBy': '11111111-1111-1111-1111-111111111111',
    'status': status,
    'createdAt': '2026-07-30T09:00:00Z',
  };

  Map<String, dynamic> pagedBody(List<Map<String, dynamic>> items) =>
      successEnvelope({
        'items': items,
        'totalCount': items.length,
        'pageSize': 20,
        'currentPage': 1,
        'totalPages': items.isEmpty ? 0 : 1,
      });

  Future<FakeHttpAdapter> openDeaths(
    WidgetTester tester, {
    Map<String, Future<ResponseBody> Function(RequestOptions)> routes = const {},
    String location = '/vefat',
    Map<String, Object> prefs = guest,
    TokenStore? tokenStore,
  }) async {
    // ⚠️ Varsayılan 800x600 yüzey uzun ekranlarda `tap`'i reddediyor (11.7).
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/deaths/cemeteries': (_) async => jsonResponse(
        successEnvelope([
          {
            'id': 'cem-1',
            'name': 'Karataş Mezarlığı',
            'address': null,
            'latitude': null,
            'longitude': null,
          },
        ]),
      ),
      '/v1/deaths/mosques': (_) async => jsonResponse(
        successEnvelope([
          {
            'id': 'mos-1',
            'name': 'Yenimahalle Cami',
            'address': null,
            'latitude': 37.37,
            'longitude': 36.09,
          },
        ]),
      ),
      ...routes,
    });
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

  testWidgets('ilanlar ad ve cenaze saatiyle listelenir', (tester) async {
    await openDeaths(
      tester,
      routes: {'/v1/deaths': (_) async => jsonResponse(pagedBody([notice()]))},
    );

    expect(find.text('Emine Kaya'), findsOneWidget);
    expect(find.text('Bugün, 11:00'), findsOneWidget);
    expect(
      find.text('Yenimahalle Cami · Karataş Mezarlığı'),
      findsOneWidget,
      reason: 'cami ve mezarlık tek satırda',
    );
  });

  testWidgets('bugün cenazesi olanlar için üstte hatırlatma çıkar', (
    tester,
  ) async {
    await openDeaths(
      tester,
      routes: {
        '/v1/deaths': (_) async =>
            jsonResponse(pagedBody([notice(), notice(id: 'd2', inDays: 3)])),
      },
    );

    expect(
      find.textContaining('Bugün cenaze namazı: Emine Kaya'),
      findsOneWidget,
    );
  });

  testWidgets('bugün cenazesi yoksa hatırlatma hiç çizilmez', (tester) async {
    await openDeaths(
      tester,
      routes: {
        '/v1/deaths': (_) async =>
            jsonResponse(pagedBody([notice(inDays: 4)])),
      },
    );

    expect(find.textContaining('Bugün cenaze namazı'), findsNothing);
  });

  testWidgets('arama uca search olarak gider', (tester) async {
    final adapter = await openDeaths(
      tester,
      routes: {'/v1/deaths': (_) async => jsonResponse(pagedBody([notice()]))},
    );

    await tester.enterText(find.byType(TextField).first, 'kaya');
    await tester.pumpAndSettle();

    expect(adapter.lastOf('/v1/deaths')?.queryParameters['search'], 'kaya');
  });

  testWidgets('sonuç yoksa arama temizlenir ve kutu da boşalır', (tester) async {
    await openDeaths(
      tester,
      routes: {
        '/v1/deaths': (options) async => jsonResponse(
          options.queryParameters.containsKey('search')
              ? pagedBody(const [])
              : pagedBody([notice()]),
        ),
      },
    );

    await tester.enterText(find.byType(TextField).first, 'olmayan');
    await tester.pumpAndSettle();
    expect(find.text('Sonuç bulunamadı'), findsOneWidget);

    await tester.tap(find.text('Aramayı temizle'));
    await tester.pumpAndSettle();

    // 11.7/11.8 hatası: filtre sıfırlanıyordu ama kutuda eski metin kalıyordu.
    final field = tester.widget<TextField>(find.byType(TextField).first);
    expect(field.controller?.text, isEmpty);
    expect(find.text('Emine Kaya'), findsOneWidget);
  });

  testWidgets('boş listede sade ve açıklayıcı mesaj çıkar', (tester) async {
    await openDeaths(
      tester,
      routes: {'/v1/deaths': (_) async => jsonResponse(pagedBody(const []))},
    );

    expect(find.text('Güncel vefat ilanı yok'), findsOneWidget);
    expect(find.textContaining('bir hafta sonra arşivlenir'), findsOneWidget);
  });

  testWidgets('karta dokununca detay açılır ve başsağlığı dileği yazılır', (
    tester,
  ) async {
    await openDeaths(
      tester,
      routes: {
        '/v1/deaths': (_) async => jsonResponse(pagedBody([notice()])),
        '/v1/deaths/d1': (_) async => jsonResponse(
          successEnvelope(
            notice(condolenceAddress: 'Yenimahalle, 1234 Sk. No: 5'),
          ),
        ),
      },
    );

    await tester.tap(find.text('Emine Kaya'));
    await tester.pumpAndSettle();

    expect(find.text('Vefat İlanı'), findsOneWidget, reason: 'detay başlığı');
    expect(find.text('Cenaze namazı'), findsOneWidget);
    expect(find.text('Cami'), findsOneWidget);
    expect(find.text('Defnedileceği yer'), findsOneWidget);
    expect(find.text('Taziye adresi'), findsOneWidget);
    // Adres verilmişse koordinat olmasa da harita araması yapılabilir.
    expect(find.text('Yol tarifi'), findsOneWidget);
    expect(find.textContaining('başsağlığı dileriz'), findsOneWidget);
  });

  testWidgets(
    'taziye yeri yokken caminin lookup koordinatı yol tarifi verir',
    (tester) async {
      await openDeaths(
        tester,
        location: '/vefat/d1',
        routes: {
          '/v1/deaths': (_) async => jsonResponse(pagedBody(const [])),
          '/v1/deaths/d1': (_) async => jsonResponse(successEnvelope(notice())),
        },
      );

      expect(find.text('Yol tarifi'), findsOneWidget);
    },
  );

  testWidgets('hiçbir konum bilgisi yoksa yol tarifi butonu çizilmez', (
    tester,
  ) async {
    await openDeaths(
      tester,
      location: '/vefat/d1',
      routes: {
        '/v1/deaths': (_) async => jsonResponse(pagedBody(const [])),
        '/v1/deaths/d1': (_) async => jsonResponse(
          successEnvelope(notice(mosque: null, cemetery: null)),
        ),
        // Cami listesi de gelmesin: koordinat kaynağı kalmasın.
        '/v1/deaths/mosques': (_) async =>
            jsonResponse(successEnvelope(const <Object>[])),
      },
    );

    expect(find.text('Yol tarifi'), findsNothing);
    expect(find.textContaining('bilgisi girilmemiş'), findsOneWidget);
  });

  testWidgets('bekleyen kendi ilanında "yayında değil" uyarısı çıkar', (
    tester,
  ) async {
    await openDeaths(
      tester,
      location: '/vefat/d1',
      routes: {
        '/v1/deaths': (_) async => jsonResponse(pagedBody(const [])),
        '/v1/deaths/d1': (_) async =>
            jsonResponse(successEnvelope(notice(status: 'pending'))),
      },
    );

    expect(find.textContaining('henüz yayında değil'), findsOneWidget);
  });

  testWidgets('arşivlenmiş/silinmiş ilan nazik mesaj gösterir', (tester) async {
    await openDeaths(
      tester,
      location: '/vefat/yok',
      routes: {
        '/v1/deaths': (_) async => jsonResponse(pagedBody(const [])),
        '/v1/deaths/yok': (_) async => jsonResponse(
          errorEnvelope('NOT_FOUND', 'Vefat ilanı bulunamadı.'),
          statusCode: 404,
        ),
      },
    );

    expect(find.text('İlan bulunamadı'), findsOneWidget);
    expect(find.text('Tekrar dene'), findsNothing);
  });

  testWidgets('misafir "Vefat bildir"e basınca uca istek gitmez', (
    tester,
  ) async {
    final adapter = await openDeaths(
      tester,
      routes: {'/v1/deaths': (_) async => jsonResponse(pagedBody(const []))},
    );

    await tester.tap(find.text('Vefat bildir'));
    await tester.pumpAndSettle();

    // Router'la Giriş'e ATILMAZ: davet gösterilir (11.10 kararı).
    expect(find.textContaining('giriş yap'), findsWidgets);
    expect(find.text('Vefat bildir'), findsWidgets, reason: 'liste kapanmadı');
    expect(
      adapter.requests.where((r) => r.method == 'POST'),
      isEmpty,
    );
  });

  testWidgets('oturum açıkken bildirim formu açılır ve alanları taşır', (
    tester,
  ) async {
    await openDeaths(
      tester,
      prefs: const {},
      tokenStore: InMemoryTokenStore(
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
      ),
      routes: {
        '/v1/users/me': (_) async =>
            jsonResponse(successEnvelope(profileBody())),
        '/v1/neighborhoods': (_) async =>
            jsonResponse(successEnvelope(neighborhoodsBody())),
        '/v1/deaths': (_) async => jsonResponse(pagedBody(const [])),
      },
    );

    await tester.tap(find.text('Vefat bildir'));
    await tester.pumpAndSettle();

    expect(find.text('Merhumun adı soyadı'), findsOneWidget);
    expect(find.text('Cenaze namazı tarihi'), findsOneWidget);
    expect(find.text('Cenaze namazının kılınacağı cami'), findsOneWidget);
    expect(find.text('Defnedileceği mezarlık'), findsOneWidget);
    expect(find.textContaining('görevlilerce kontrol'), findsWidgets);
  });

  testWidgets('zorunlu alanlar boşken istek atılmaz, hatalar alan altına düşer', (
    tester,
  ) async {
    final adapter = await openDeaths(
      tester,
      location: '/vefat-bildir',
      prefs: const {},
      tokenStore: InMemoryTokenStore(
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
      ),
      routes: {
        '/v1/users/me': (_) async =>
            jsonResponse(successEnvelope(profileBody())),
        '/v1/neighborhoods': (_) async =>
            jsonResponse(successEnvelope(neighborhoodsBody())),
        '/v1/deaths': (_) async => jsonResponse(pagedBody(const [])),
      },
    );

    // Form uzun bir `ListView` — gönder butonu ekranın altında kalıyor.
    await tester.dragUntilVisible(
      find.text('Bildirimi gönder'),
      find.byType(ListView).last,
      const Offset(0, -250),
    );
    await tester.tap(find.text('Bildirimi gönder'));
    await tester.pumpAndSettle();

    expect(find.text('Merhumun adı soyadı zorunlu.'), findsOneWidget);
    expect(find.text('Cenaze namazı tarihini seçin.'), findsOneWidget);
    expect(find.text('Cenaze namazı saatini seçin.'), findsOneWidget);
    // Sunucuda bu uç için doğrulayıcı yok → istemci hiç göndermemeli.
    expect(adapter.requests.where((r) => r.method == 'POST'), isEmpty);
  });

  group('model', () {
    DeathNotice parse(Map<String, dynamic> json) => DeathNotice.fromJson(json);

    test('funeralDate saat dilimiyle KAYDIRILMAZ (sunucu TR günü yazıyor)', () {
      final item = parse(notice(inDays: 0));
      expect(item.isToday(), isTrue);
      expect(item.daysFromToday(), 0);
    });

    test('saat "HH:mm" gösterilir, gün+saat etiketi birleşir', () {
      final item = parse(notice(inDays: 1, time: '13:30:00'));
      expect(item.timeLabel, '13:30');
      expect(item.funeralLabel(), 'Yarın, 13:30');
    });

    test('gün etiketi bugün/yarın/dün, diğerinde tam tarih', () {
      expect(parse(notice(inDays: 0)).dayLabel(), 'Bugün');
      expect(parse(notice(inDays: 1)).dayLabel(), 'Yarın');
      expect(parse(notice(inDays: -1)).dayLabel(), 'Dün');
      expect(parse(notice(inDays: 5)).dayLabel(), contains(' '));
    });

    test('bugünkü cenaze gün bitene kadar "geçmiş" sayılmaz', () {
      expect(parse(notice(inDays: 0, time: '01:00:00')).isPast(), isFalse);
      expect(parse(notice(inDays: -1)).isPast(), isTrue);
    });

    test('geri sayım yalnız bugünkü cenazede üretilir', () {
      final today = AppDate.nowInTurkey;
      final later = DateTime(today.year, today.month, today.day, 23, 59);
      final item = parse(notice(inDays: 0, time: '23:59:00'));
      expect(item.timeUntilFuneral(), isNotNull);
      expect(item.funeralAt, later);

      expect(parse(notice(inDays: 3)).timeUntilFuneral(), isNull);
      // Saati geçmiş bugünkü cenazede de sayaç yazılmaz.
      expect(parse(notice(inDays: 0, time: '00:00:00')).timeUntilFuneral(), isNull);
    });

    test('paylaşım metni cami, defin ve başsağlığı dileğini taşır', () {
      final text = parse(
        notice(condolenceAddress: 'Yenimahalle 1234 Sk.'),
      ).shareText();

      expect(text, contains('Emine Kaya vefat etmiştir.'));
      expect(text, contains('Cami: Yenimahalle Cami'));
      expect(text, contains('Defin: Karataş Mezarlığı'));
      expect(text, contains('Taziye: Yenimahalle 1234 Sk.'));
      expect(text, contains('başsağlığı dileriz'));
    });

    test('taziye yeri koordinat ya da adresten anlaşılır', () {
      expect(parse(notice()).hasCondolencePlace, isFalse);
      expect(parse(notice(condolenceAddress: 'X Sk.')).hasCondolencePlace, isTrue);
      expect(
        parse(notice(lat: 37.37, lng: 36.09)).hasCondolencePlace,
        isTrue,
      );
    });
  });
}
