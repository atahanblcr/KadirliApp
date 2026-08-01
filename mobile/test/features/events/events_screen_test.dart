import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/router/app_router.dart';
import 'package:kadirli_app/features/events/application/events_providers.dart';
import 'package:kadirli_app/features/events/data/models/event.dart';
import 'package:kadirli_app/features/events/data/models/event_calendar_item.dart';
import 'package:kadirli_app/features/events/data/models/event_category.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// Etkinlikler: liste + takvim + detay (11.10).
void main() {
  const guest = {'auth.guestChoice': true};

  const categories = [
    {'id': 'cat-konser', 'name': 'Konser', 'slug': 'konser'},
    {'id': 'cat-spor', 'name': 'Spor', 'slug': 'spor'},
  ];

  /// Bugünden [inDays] gün sonrası — testler takvim gününe bağlı olmasın diye
  /// tarihler hep "şimdi"ye göre üretiliyor.
  String dayFromNow(int inDays) {
    final date = DateTime.now().toUtc().add(Duration(days: inDays));
    return '${date.year.toString().padLeft(4, '0')}-'
        '${date.month.toString().padLeft(2, '0')}-'
        '${date.day.toString().padLeft(2, '0')}';
  }

  Map<String, dynamic> event({
    String id = 'e1',
    String title = 'Karakucak Güreş Festivali',
    String? categoryName = 'Spor',
    String categoryId = 'cat-spor',
    int inDays = 3,
    String time = '10:00:00',
    String? venue = 'Şehir Stadyumu',
    bool isFree = true,
    num? ticketPrice,
    String? coverImageUrl,
    double? latitude,
    double? longitude,
  }) => {
    'id': id,
    'title': title,
    'description': 'Geleneksel karakucak güreşleri ve yöresel etkinlikler.',
    'categoryId': categoryId,
    'categoryName': categoryName,
    'eventDate': '${dayFromNow(inDays)}T00:00:00Z',
    'eventTime': time,
    'venueName': venue,
    'address': 'Cumhuriyet Mah.',
    'latitude': latitude,
    'longitude': longitude,
    'hasLocation': latitude != null,
    'organizer': 'Kadirli Kaymakamlığı',
    'ticketPrice': ticketPrice,
    'isFree': isFree,
    'isLocal': true,
    'coverImageId': null,
    'coverImageUrl': coverImageUrl,
    'status': 'approved',
    'createdAt': '2026-07-03T21:19:26Z',
  };

  Map<String, dynamic> pagedBody(List<Map<String, dynamic>> items) =>
      successEnvelope({
        'items': items,
        'totalCount': items.length,
        'pageSize': 20,
        'currentPage': 1,
        'totalPages': items.isEmpty ? 0 : 1,
      });

  Future<FakeHttpAdapter> openEvents(
    WidgetTester tester, {
    Map<String, Future<ResponseBody> Function(RequestOptions)> routes = const {},
    String location = '/etkinlikler',
  }) async {
    // ⚠️ Varsayılan 800x600 yüzey uzun ekranlarda `tap`'i reddediyor (11.7).
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/events/categories': (_) async => jsonResponse(
        successEnvelope(
          categories.map((c) => Map<String, dynamic>.from(c)).toList(),
        ),
      ),
      '/v1/events/calendar': (_) async => jsonResponse(successEnvelope(const [])),
      ...routes,
    });
    final container = await pumpApp(tester, prefs: guest, adapter: adapter);
    container.read(routerProvider).go(location);
    await tester.pumpAndSettle();
    return adapter;
  }

  testWidgets('yaklaşan etkinlikler listelenir', (tester) async {
    await openEvents(
      tester,
      routes: {'/v1/events': (_) async => jsonResponse(pagedBody([event()]))},
    );

    expect(find.text('Karakucak Güreş Festivali'), findsOneWidget);
    expect(find.text('Şehir Stadyumu'), findsOneWidget);
    expect(find.text('10:00'), findsOneWidget, reason: 'saniye gösterilmez');
    expect(find.text('Ücretsiz'), findsWidgets);
  });

  testWidgets(
    'yaklaşan liste startDate=bugün ve sort=date_asc gönderir',
    (tester) async {
      final adapter = await openEvents(
        tester,
        routes: {'/v1/events': (_) async => jsonResponse(pagedBody([event()]))},
      );

      final query = adapter.lastOf('/v1/events')?.queryParameters;
      expect(query?['startDate'], dayFromNow(0));
      // ⚠️ Sunucunun varsayılanı date_desc: ilk sayfa EN UZAK tarihli
      // etkinlikleri getirirdi.
      expect(query?['sort'], 'date_asc');
      expect(query?.containsKey('endDate'), isFalse);
    },
  );

  testWidgets('Geçmiş sekmesi endDate=dün ve sort=date_desc gönderir', (
    tester,
  ) async {
    final adapter = await openEvents(
      tester,
      routes: {'/v1/events': (_) async => jsonResponse(pagedBody([event()]))},
    );

    await tester.tap(find.text('Geçmiş'));
    await tester.pumpAndSettle();

    final query = adapter.lastOf('/v1/events')?.queryParameters;
    expect(query?['endDate'], dayFromNow(-1));
    expect(query?['sort'], 'date_desc');
    expect(query?.containsKey('startDate'), isFalse);
  });

  testWidgets('kategori chip\'i uca categoryId gönderir', (tester) async {
    final adapter = await openEvents(
      tester,
      routes: {'/v1/events': (_) async => jsonResponse(pagedBody([event()]))},
    );

    await tester.tap(find.text('Konser'));
    await tester.pumpAndSettle();

    expect(adapter.lastOf('/v1/events')?.queryParameters['categoryId'], 'cat-konser');
  });

  testWidgets('Ücretsiz filtresi isFree=true gönderir ve kaldırılabilir', (
    tester,
  ) async {
    final adapter = await openEvents(
      tester,
      routes: {'/v1/events': (_) async => jsonResponse(pagedBody([event()]))},
    );

    await tester.tap(find.text('Ücretsiz').first);
    await tester.pumpAndSettle();
    expect(adapter.lastOf('/v1/events')?.queryParameters['isFree'], true);

    await tester.tap(find.text('Ücretsiz').first);
    await tester.pumpAndSettle();
    expect(
      adapter.lastOf('/v1/events')?.queryParameters.containsKey('isFree'),
      isFalse,
    );
  });

  testWidgets('arama uca search olarak gider', (tester) async {
    final adapter = await openEvents(
      tester,
      routes: {'/v1/events': (_) async => jsonResponse(pagedBody([event()]))},
    );

    await tester.enterText(find.byType(TextField).first, 'güreş');
    await tester.pumpAndSettle();

    expect(adapter.lastOf('/v1/events')?.queryParameters['search'], 'güreş');
  });

  testWidgets('sonuç yoksa filtreler temizlenebilir ve kutu da temizlenir', (
    tester,
  ) async {
    await openEvents(
      tester,
      routes: {
        '/v1/events': (options) async => jsonResponse(
          options.queryParameters.containsKey('search')
              ? pagedBody(const [])
              : pagedBody([event()]),
        ),
      },
    );

    await tester.enterText(find.byType(TextField).first, 'olmayan');
    await tester.pumpAndSettle();
    expect(find.text('Yaklaşan etkinlik yok'), findsOneWidget);

    await tester.tap(find.text('Filtreleri temizle'));
    await tester.pumpAndSettle();

    expect(find.text('Karakucak Güreş Festivali'), findsOneWidget);
    // 11.7/11.8 regresyonu: filtre sıfırlanırken kutuda eski metin kalıyordu.
    expect(find.text('olmayan'), findsNothing);
  });

  testWidgets('kategoriler alınamazsa şerit çizilmez ama liste çalışır', (
    tester,
  ) async {
    await openEvents(
      tester,
      routes: {
        '/v1/events/categories': (_) async => jsonResponse(
          errorEnvelope('INTERNAL_ERROR', 'Sunucu hatası.'),
          statusCode: 500,
        ),
        '/v1/events': (_) async => jsonResponse(pagedBody([event()])),
      },
    );

    expect(find.text('Tüm türler'), findsNothing);
    expect(find.text('Konser'), findsNothing);
    expect(find.text('Karakucak Güreş Festivali'), findsOneWidget);
    // Zaman filtreleri kategoriden bağımsız çalışmaya devam eder.
    expect(find.text('Yaklaşan'), findsOneWidget);
  });

  testWidgets('takvimde etkinlikli güne dokununca o günün listesi gelir', (
    tester,
  ) async {
    final now = DateTime.now();
    final day = now.day;
    await openEvents(
      tester,
      routes: {
        '/v1/events': (_) async => jsonResponse(pagedBody(const [])),
        '/v1/events/calendar': (_) async => jsonResponse(
          successEnvelope([
            {
              'id': 'e1',
              'title': 'Amatör Tiyatro Gecesi',
              'eventDate': '${dayFromNow(0)}T00:00:00Z',
              'eventTime': '19:00:00',
              'venueName': 'Kültür Merkezi',
              'categoryName': 'Tiyatro',
              'status': 'approved',
            },
          ]),
        ),
      },
    );

    await tester.tap(find.text('Takvim'));
    await tester.pumpAndSettle();

    expect(find.text('Ayrıntı için takvimden işaretli bir güne dokunun.'),
        findsOneWidget);

    await tester.tap(find.text('$day').first);
    await tester.pumpAndSettle();

    expect(find.text('Amatör Tiyatro Gecesi'), findsOneWidget);
    expect(find.text('19:00'), findsOneWidget);
  });

  testWidgets('etkinliksiz güne dokunmak hiçbir şey yapmaz (ölü buton yok)', (
    tester,
  ) async {
    // Ayın 1'i ile bugün aynı gün olursa test anlamını yitirir → bugünün
    // dışında, kesin boş bir gün seçiliyor.
    final now = DateTime.now();
    final emptyDay = now.day == 1 ? 2 : 1;

    await openEvents(
      tester,
      routes: {
        '/v1/events': (_) async => jsonResponse(pagedBody(const [])),
        '/v1/events/calendar': (_) async => jsonResponse(
          successEnvelope([
            {
              'id': 'e1',
              'title': 'Amatör Tiyatro Gecesi',
              'eventDate': '${dayFromNow(0)}T00:00:00Z',
              'eventTime': '19:00:00',
              'venueName': 'Kültür Merkezi',
              'categoryName': 'Tiyatro',
              'status': 'approved',
            },
          ]),
        ),
      },
    );

    await tester.tap(find.text('Takvim'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('$emptyDay').first);
    await tester.pumpAndSettle();

    expect(find.text('Amatör Tiyatro Gecesi'), findsNothing);
  });

  testWidgets('ay ileri gidince yeni ayın takvimi istenir', (tester) async {
    final adapter = await openEvents(
      tester,
      routes: {'/v1/events': (_) async => jsonResponse(pagedBody(const []))},
    );

    await tester.tap(find.text('Takvim'));
    await tester.pumpAndSettle();
    final before = adapter.lastOf('/v1/events/calendar')?.queryParameters;

    await tester.tap(find.byTooltip('Sonraki ay'));
    await tester.pumpAndSettle();
    final after = adapter.lastOf('/v1/events/calendar')?.queryParameters;

    expect(after, isNot(equals(before)));
  });

  testWidgets('karta dokununca detay açılır', (tester) async {
    await openEvents(
      tester,
      routes: {
        '/v1/events': (_) async => jsonResponse(pagedBody([event()])),
        '/v1/events/e1': (_) async => jsonResponse(
          successEnvelope(event(latitude: 37.37, longitude: 36.09)),
        ),
      },
    );

    await tester.tap(find.text('Karakucak Güreş Festivali'));
    await tester.pumpAndSettle();

    expect(find.text('Etkinlik'), findsOneWidget, reason: 'detay başlığı');
    expect(find.text('Saat 10:00'), findsOneWidget);
    expect(find.text('Mekan'), findsOneWidget);
    expect(find.text('Yol tarifi'), findsOneWidget);
    expect(find.textContaining('Düzenleyen'), findsOneWidget);
  });

  testWidgets('bulunamayan etkinlik nazik mesaj gösterir', (tester) async {
    await openEvents(
      tester,
      location: '/etkinlikler/yok',
      routes: {
        '/v1/events': (_) async => jsonResponse(pagedBody(const [])),
        '/v1/events/yok': (_) async => jsonResponse(
          errorEnvelope('NOT_FOUND', 'Etkinlik bulunamadı.'),
          statusCode: 404,
        ),
      },
    );

    expect(find.text('Etkinlik bulunamadı'), findsOneWidget);
    expect(find.text('Tekrar dene'), findsNothing);
  });

  group('model', () {
    Event build({
      String date = '2026-08-12T00:00:00Z',
      String time = '19:30:00',
      bool isFree = false,
      double? price,
    }) => Event(
      id: 'e',
      title: 'Konser',
      eventDate: DateTime.parse(date),
      eventTime: time,
      isFree: isFree,
      ticketPrice: price,
    );

    test('eventDate saat dilimiyle KAYDIRILMAZ (sunucu TR günü yazıyor)', () {
      // +03 uygulansaydı 12 Ağustos 03:00 olurdu; gün anahtarı değişmemeli.
      expect(build().dayKey, '2026-08-12');
    });

    test('saat "HH:mm" olarak gösterilir, gün+saat birleşir', () {
      final event = build();
      expect(event.timeLabel, '19:30');
      expect(event.startsAt, DateTime(2026, 8, 12, 19, 30));
    });

    test('geri sayım etiketi bugün/yarın/N gün sonra', () {
      final now = DateTime.utc(2026, 8, 10, 6);
      expect(build(date: '2026-08-10T00:00:00Z').countdownLabel(now: now), 'Bugün');
      expect(build(date: '2026-08-11T00:00:00Z').countdownLabel(now: now), 'Yarın');
      expect(
        build(date: '2026-08-13T00:00:00Z').countdownLabel(now: now),
        '3 gün sonra',
      );
      // Bir haftadan uzağa rozet yazılmaz (rozet anlamını yitirmesin).
      expect(build(date: '2026-09-10T00:00:00Z').countdownLabel(now: now), isNull);
    });

    test('bugünkü etkinlik gün bitene kadar "geçmiş" sayılmaz', () {
      // Sabah 06:00'da, akşam 19:30'daki etkinlik hâlâ yaklaşan.
      final morning = DateTime.utc(2026, 8, 12, 3);
      expect(build().isPast(now: morning), isFalse);
      expect(build().isToday(now: morning), isTrue);
      expect(
        build(date: '2026-08-11T00:00:00Z').isPast(now: morning),
        isTrue,
      );
    });

    test('ücretsizde "Ücretsiz", fiyatsız ücretlide etiket yazılmaz', () {
      expect(build(isFree: true).priceLabel, 'Ücretsiz');
      expect(build(price: 50).priceLabel, '50 ₺');
      // "0 ₺" yazmak yanlış bilgi olur (AppMoney kararı).
      expect(build().priceLabel, isNull);
      expect(build(price: 0).priceLabel, isNull);
    });

    test('koordinat yoksa harita sorgusu mekan + adresten kurulur', () {
      final event = Event(
        id: 'e',
        title: 'Konser',
        eventDate: DateTime.parse('2026-08-12T00:00:00Z'),
        venueName: 'Kültür Merkezi',
        address: 'Cumhuriyet Mah.',
      );
      expect(event.canOpenMap, isFalse);
      expect(event.mapQuery, 'Kültür Merkezi, Cumhuriyet Mah.');
    });

    test('kategori slug\'ı Material ikonuna eşlenir', () {
      expect(
        const EventCategory(id: 'c', name: 'Konser', slug: 'konser').materialIcon,
        Icons.music_note_rounded,
      );
      expect(
        const EventCategory(id: 'c', name: 'Bilinmeyen', slug: 'xyz').materialIcon,
        Icons.local_activity_rounded,
      );
    });

    test('filtre eşitliği tüm alanları kapsar', () {
      expect(const EventFilter().isActive, isFalse);
      expect(const EventFilter(onlyFree: true).isActive, isTrue);
      expect(const EventFilter(scope: EventScope.past).isActive, isTrue);
      expect(const EventFilter(search: '  ').isActive, isFalse);
      expect(
        const EventFilter(categoryId: 'a'),
        isNot(const EventFilter(categoryId: 'b')),
      );
    });

    test('gün eşlemesi ve gün süzmesi takvim için doğru', () {
      EventCalendarItem calendarItem(String id, String day, String time) =>
          EventCalendarItem(
            id: id,
            title: id,
            eventDate: DateTime.parse('${day}T00:00:00Z'),
            eventTime: time,
          );

      final items = [
        calendarItem('a', '2026-08-12', '19:00:00'),
        calendarItem('b', '2026-08-12', '09:00:00'),
        calendarItem('c', '2026-08-13', '10:00:00'),
      ];
      expect(eventCountsByDay(items), {'2026-08-12': 2, '2026-08-13': 1});
      // Gün içi liste saate göre sıralı olmalı.
      expect(
        eventsOfDay(items, '2026-08-12').map((e) => e.id).toList(),
        ['b', 'a'],
      );
      expect(eventsOfDay(items, null), isEmpty);
    });
  });
}
