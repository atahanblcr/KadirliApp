import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/router/app_router.dart';
import 'package:kadirli_app/features/transport/data/models/intercity_route.dart';
import 'package:kadirli_app/features/transport/data/models/intracity_route.dart';
import 'package:kadirli_app/features/transport/presentation/widgets/intercity_route_card.dart';
import 'package:kadirli_app/features/transport/presentation/widgets/intracity_route_card.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// Ulaşım ekranı: iki sekme, arama, kart açma (11.12).
void main() {
  const guest = {'auth.guestChoice': true};

  Map<String, dynamic> intercity({
    String id = 'ic-1',
    String destination = 'Adana',
    num? price = 220,
    int? durationMinutes = 105,
    String? company = 'Kadirli Seyahat',
    List<String> times = const ['07:00', '10:30', '14:00', '17:30'],
  }) => {
    'id': id,
    'destination': destination,
    'price': price,
    'durationMinutes': durationMinutes,
    'company': company,
    'isActive': true,
    'schedules': [
      for (var i = 0; i < times.length; i++)
        {'id': '$id-s$i', 'departureTime': times[i]},
    ],
  };

  Map<String, dynamic> intracity({
    String id = 'ia-1',
    String routeNumber = '1',
    String routeName = 'Merkez - Devlet Hastanesi',
    // ⚠️ Sunucu TimeSpan → "HH:mm:ss" gönderiyor (şehirlerarasındaki "HH:mm"
    // biçiminden farklı); fixture bunu bilerek taklit ediyor.
    String? first = '06:30:00',
    String? last = '22:00:00',
    int? frequency = 20,
    List<(String, int, int?)> stops = const [
      ('Cumhuriyet Meydanı', 1, 0),
      ('Belediye', 2, 7),
      ('Devlet Hastanesi', 3, 21),
    ],
  }) => {
    'id': id,
    'routeNumber': routeNumber,
    'routeName': routeName,
    'firstDeparture': first,
    'lastDeparture': last,
    'frequencyMinutes': frequency,
    'isActive': true,
    'stops': [
      for (final stop in stops)
        {
          'id': '$id-${stop.$2}',
          'stopName': stop.$1,
          'stopOrder': stop.$2,
          'timeFromStart': stop.$3,
        },
    ],
  };

  Map<String, dynamic> pagedBody(List<Map<String, dynamic>> items) =>
      successEnvelope({
        'items': items,
        'totalCount': items.length,
        'pageSize': 20,
        'currentPage': 1,
        'totalPages': items.isEmpty ? 0 : 1,
      });

  Future<FakeHttpAdapter> openTransport(
    WidgetTester tester, {
    List<Map<String, dynamic>>? intercityItems,
    List<Map<String, dynamic>>? intracityItems,
    Map<String, Future<ResponseBody> Function(RequestOptions)> routes =
        const {},
  }) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/transport/intercity-routes': (_) async =>
          jsonResponse(pagedBody(intercityItems ?? [intercity()])),
      '/v1/transport/intracity-routes': (_) async =>
          jsonResponse(pagedBody(intracityItems ?? [intracity()])),
      ...routes,
    });
    final container = await pumpApp(tester, prefs: guest, adapter: adapter);
    container.read(routerProvider).go('/ulasim');
    await tester.pumpAndSettle();
    return adapter;
  }

  testWidgets('şehirlerarası hatlar sıradaki kalkışla listelenir', (
    tester,
  ) async {
    await openTransport(tester);

    expect(find.text('Kadirli → Adana'), findsOneWidget);
    expect(find.text('Kadirli Seyahat'), findsOneWidget);
    expect(find.text('1 sa 45 dk'), findsOneWidget);
    expect(find.text('220 ₺'), findsOneWidget);
    // "Sıradaki HH:mm · …" satırı saate göre değişiyor; sabit kısmı doğrula.
    expect(find.textContaining('Sıradaki '), findsOneWidget);
    expect(find.text('Toplam 1 hat'), findsOneWidget);
  });

  testWidgets('kart açılınca tüm kalkış saatleri görünür, tekrar dokunmak kapatır', (
    tester,
  ) async {
    await openTransport(tester);

    expect(find.text('Kalkış saatleri'), findsNothing);
    await tester.tap(find.text('Kadirli → Adana'));
    await tester.pumpAndSettle();

    expect(find.text('Kalkış saatleri'), findsOneWidget);
    for (final time in ['07:00', '10:30', '14:00', '17:30']) {
      expect(find.text(time), findsOneWidget);
    }
    expect(find.text('Saatleri paylaş'), findsOneWidget);

    await tester.tap(find.text('Kadirli → Adana'));
    await tester.pumpAndSettle();
    expect(find.text('Kalkış saatleri'), findsNothing);
  });

  testWidgets('aynı anda tek kart açık kalır', (tester) async {
    await openTransport(
      tester,
      intercityItems: [
        intercity(),
        intercity(id: 'ic-2', destination: 'Osmaniye', company: 'Birlik'),
      ],
    );

    await tester.tap(find.text('Kadirli → Adana'));
    await tester.pumpAndSettle();
    expect(find.text('Kalkış saatleri'), findsOneWidget);

    await tester.tap(find.text('Kadirli → Osmaniye'));
    await tester.pumpAndSettle();
    // İkincisi açıldı, ilki kapandı → hâlâ tek başlık.
    expect(find.text('Kalkış saatleri'), findsOneWidget);
  });

  testWidgets('arama uca searchTerm olarak gider (search DEĞİL)', (
    tester,
  ) async {
    final adapter = await openTransport(tester);

    await tester.enterText(find.byType(TextField).first, 'adana');
    await tester.pumpAndSettle();

    final query = adapter
        .lastOf('/v1/transport/intercity-routes')
        ?.queryParameters;
    expect(query?['searchTerm'], 'adana');
    expect(query?.containsKey('search'), isFalse);
  });

  testWidgets('şehir içi sekmesinde servis durumu ve durak sayısı görünür', (
    tester,
  ) async {
    await openTransport(tester);

    await tester.tap(find.text('Şehir içi'));
    await tester.pumpAndSettle();

    expect(find.text('Merkez - Devlet Hastanesi'), findsOneWidget);
    expect(find.text('06:30 – 22:00'), findsOneWidget);
    expect(find.text('3 durak · dokununca güzergâh açılır'), findsOneWidget);
  });

  testWidgets('şehir içi hat açılınca durak çizelgesi sırayla çıkar', (
    tester,
  ) async {
    await openTransport(tester);

    await tester.tap(find.text('Şehir içi'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Merkez - Devlet Hastanesi'));
    await tester.pumpAndSettle();

    expect(find.text('Güzergâh'), findsOneWidget);
    expect(find.text('Cumhuriyet Meydanı'), findsOneWidget);
    expect(find.text('Belediye'), findsOneWidget);
    expect(find.text('Devlet Hastanesi'), findsOneWidget);
    // İlk durağın "+0 dk"sı yazılmaz, sonrakiler yazılır.
    expect(find.text('+7 dk'), findsOneWidget);
    expect(find.text('+21 dk'), findsOneWidget);
    expect(find.text('+0 dk'), findsNothing);
  });

  testWidgets('sunucu sırasız durak gönderse de çizelge stopOrder ile sıralanır', (
    tester,
  ) async {
    await openTransport(
      tester,
      intracityItems: [
        intracity(
          stops: const [
            ('Son Durak', 3, 21),
            ('İlk Durak', 1, 0),
            ('Orta Durak', 2, 7),
          ],
        ),
      ],
    );

    await tester.tap(find.text('Şehir içi'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Merkez - Devlet Hastanesi'));
    await tester.pumpAndSettle();

    final first = tester.getTopLeft(find.text('İlk Durak')).dy;
    final middle = tester.getTopLeft(find.text('Orta Durak')).dy;
    final last = tester.getTopLeft(find.text('Son Durak')).dy;
    expect(first, lessThan(middle));
    expect(middle, lessThan(last));
  });

  testWidgets('kalkış saati girilmemiş hat "işlevsiz vurgu" göstermez', (
    tester,
  ) async {
    await openTransport(
      tester,
      intercityItems: [intercity(times: const [])],
    );

    expect(find.text('Kalkış saati girilmemiş'), findsOneWidget);
    expect(find.textContaining('Sıradaki '), findsNothing);

    await tester.tap(find.text('Kadirli → Adana'));
    await tester.pumpAndSettle();
    expect(
      find.text('Bu hat için kalkış saati henüz girilmemiş.'),
      findsOneWidget,
    );
  });

  testWidgets('liste boşken aramayı temizleme kutuyu da temizler', (
    tester,
  ) async {
    var searched = false;
    await openTransport(
      tester,
      routes: {
        '/v1/transport/intercity-routes': (options) async {
          searched = options.queryParameters.containsKey('searchTerm');
          return jsonResponse(
            pagedBody(searched ? const [] : [intercity()]),
          );
        },
      },
    );

    await tester.enterText(find.byType(TextField).first, 'yokşehir');
    await tester.pumpAndSettle();
    expect(find.text('Sonuç bulunamadı'), findsOneWidget);

    await tester.tap(find.text('Aramayı temizle'));
    await tester.pumpAndSettle();

    // 11.7/11.8'de iki kez çıkan hata: filtre sıfırlanıyor ama kutuda metin kalıyordu.
    final field = tester.widget<TextField>(find.byType(TextField).first);
    expect(field.controller?.text, isEmpty);
    expect(find.text('Kadirli → Adana'), findsOneWidget);
  });

  testWidgets('liste alınamazsa hata ekranı ve tekrar dene çıkar', (
    tester,
  ) async {
    await openTransport(
      tester,
      routes: {
        // ⚠️ 5xx `apiRetry` için geçici hata sayılır → kalıcı hata (404) şart.
        '/v1/transport/intercity-routes': (_) async => jsonResponse(
          errorEnvelope('NOT_FOUND', 'Kayıt bulunamadı.'),
          statusCode: 404,
        ),
      },
    );

    expect(find.text('Tekrar dene'), findsOneWidget);
    expect(find.byType(IntercityRouteCard), findsNothing);
  });

  testWidgets('hiç hat yoksa açıklayıcı boş durum çıkar', (tester) async {
    await openTransport(tester, intercityItems: const []);

    expect(find.text('Henüz hat eklenmemiş'), findsOneWidget);
    expect(find.byType(IntercityRouteCard), findsNothing);
  });

  group('kart yerleşimi', () {
    testWidgets('uzun şehir/firma adı 1.4 yazı ölçeğinde taşmaz', (
      tester,
    ) async {
      // Dar sütunda `Row` içindeki `Text` bu projede beş fazda taştı (11.7–11.11).
      await tester.pumpWidget(
        MediaQuery(
          data: const MediaQueryData(
            size: Size(320, 640),
            textScaler: TextScaler.linear(1.4),
          ),
          child: MaterialApp(
            home: Scaffold(
              body: SingleChildScrollView(
                child: SizedBox(
                  width: 320,
                  child: IntercityRouteCard(
                    route: IntercityRoute.fromJson(
                      intercity(
                        destination: 'Kahramanmaraş Elbistan Otogarı',
                        company: 'Kadirli Öz Seyahat Turizm Taşımacılık',
                      ),
                    ),
                    expanded: true,
                    onToggle: () {},
                  ),
                ),
              ),
            ),
          ),
        ),
      );
      await tester.pump();
      expect(tester.takeException(), isNull);
    });

    testWidgets('uzun durak adı dar ekranda taşmaz', (tester) async {
      await tester.pumpWidget(
        MediaQuery(
          data: const MediaQueryData(
            size: Size(320, 640),
            textScaler: TextScaler.linear(1.4),
          ),
          child: MaterialApp(
            home: Scaffold(
              body: SingleChildScrollView(
                child: SizedBox(
                  width: 320,
                  child: IntracityRouteCard(
                    route: IntracityRoute.fromJson(
                      intracity(
                        routeName: 'Merkez - Organize Sanayi Bölgesi Kavşağı',
                        stops: const [
                          ('Cumhuriyet Meydanı Belediye Otobüs Durağı', 1, 0),
                          ('Devlet Hastanesi Acil Servis Girişi', 2, 21),
                        ],
                      ),
                    ),
                    expanded: true,
                    onToggle: () {},
                  ),
                ),
              ),
            ),
          ),
        ),
      );
      await tester.pump();
      expect(tester.takeException(), isNull);
    });
  });
}
