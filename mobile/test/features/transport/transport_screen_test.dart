import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/router/app_router.dart';
import 'package:kadirli_app/core/utils/app_date.dart';
import 'package:kadirli_app/features/transport/data/models/intercity_route.dart';
import 'package:kadirli_app/features/transport/data/models/intracity_route.dart';
import 'package:kadirli_app/features/transport/presentation/widgets/intercity_route_card.dart';
import 'package:kadirli_app/features/transport/presentation/widgets/intracity_route_card.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// Ulaşım ekranı: iki sekme, arama, kart açma (11.12).
void main() {
  const guest = {'auth.guestChoice': true};

  const weekdayCodes = ['mon', 'tue', 'wed', 'thu', 'fri'];

  Map<String, dynamic> intercity({
    String id = 'ic-1',
    String destination = 'Adana',
    num? price = 220,
    int? durationMinutes = 105,
    String? company = 'Kadirli Seyahat',
    List<String> times = const ['07:00', '10:30', '14:00', '17:30'],
    String vehicleType = 'bus',
    String? departurePointName,
    String? departurePointAddress,
    num? departurePointLatitude,
    num? departurePointLongitude,
    // `null` → alan hiç gönderilmez (12.5 öncesi kayıt / eski sunucu).
    List<String>? days,
  }) => {
    'id': id,
    'destination': destination,
    'price': price,
    'durationMinutes': durationMinutes,
    'company': company,
    'isActive': true,
    'vehicleType': vehicleType,
    'departurePointName': departurePointName,
    'departurePointAddress': departurePointAddress,
    'departurePointLatitude': departurePointLatitude,
    'departurePointLongitude': departurePointLongitude,
    'schedules': [
      for (var i = 0; i < times.length; i++)
        {
          'id': '$id-s$i',
          'departureTime': times[i],
          'days': ?days,
          if (days != null) 'runsDaily': days.length == 7,
        },
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

  /// Bugün **kesin olarak ileride** olan iki kalkış saati üretir (Kadirli saatiyle).
  /// Gece yarısına yakın koşulduğunda ertesi güne taşmamak için son sefer 23:30'da
  /// sabitlenir; o pencerede de en az bir "sıradaki" kalkış kalır.
  /// Şimdiye göre "yakında" iki kalkış saati.
  ///
  /// ⚠️ **Bunun üzerine "sıradaki sefer var" iddiası KURULAMAZ.** Gün taşmasında
  /// sabit 23:30'a kırpıyor, yani saat 23:30'u geçtiğinde ürettiği saatler
  /// **geçmişte** kalır — kart haklı olarak "Bugünkü seferler bitti" der.
  /// Saate bağlı iddialar `now` enjekte edilebilen **kart** testinde kurulur.
  List<String> soonTimes({DateTime? now}) {
    final t = now == null ? AppDate.nowInTurkey : AppDate.toTurkey(now);
    String at(int minutesFromNow) {
      final target = t.add(Duration(minutes: minutesFromNow));
      final clamped = target.day == t.day
          ? target
          : DateTime(t.year, t.month, t.day, 23, 30);
      return '${clamped.hour.toString().padLeft(2, '0')}:'
          '${clamped.minute.toString().padLeft(2, '0')}';
    }

    return [at(45), at(150)];
  }

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

  testWidgets('şehirlerarası hatlar listelenir', (tester) async {
    await openTransport(tester, intercityItems: [intercity(times: soonTimes())]);

    expect(find.text('Kadirli → Adana'), findsOneWidget);
    expect(find.text('Kadirli Seyahat'), findsOneWidget);
    expect(find.text('1 sa 45 dk'), findsOneWidget);
    expect(find.text('220 ₺'), findsOneWidget);
    expect(find.text('Toplam 1 hat'), findsOneWidget);
  });

  /// 🐛 **"Sıradaki" iddiası buraya TAŞINDI (12.15 oturumunda kırmızı bulundu).**
  ///
  /// Ekran seviyesinde bu satır **duvar saatine** bağımlıydı ve iki kez yamandı:
  /// önce sabit saatli fixture akşamları patladı, sonra `soonTimes()` "şimdiye göre"
  /// üretmeye çevrildi — ama gün taşmasını **sabit 23:30**'a kırpıyordu. Yani saat
  /// 23:30'u geçtiğinde fixture'ın ürettiği iki sefer de **geçmişte** kalıyor, kart
  /// haklı olarak *"Bugünkü seferler bitti"* yazıyor ve test kırmızıya dönüyordu.
  /// (Bu oturumda 23:5x'te yakalandı.)
  ///
  /// 🔑 Asıl hata yamanın kendisi değil **yeri**: ekran testi `now` enjekte edemiyor,
  /// yani iddia günün son yarım saatinde *tanım gereği* doğru olamaz. Kart seviyesinde
  /// `now` enjekte edilebiliyor (§ checklist: *"tarih gösteren karta `now` enjekte
  /// edilebilmeli"* — bu projede 4 kez tekrarlamış sınıf) ve iddia orada **her saatte**
  /// geçerli. Ekran testine kalan, saatten bağımsız olan kısım.
  testWidgets('kart sıradaki kalkışı yazar (now enjekte edilir)', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    // 3 Ağustos 2026 Pazartesi, Kadirli 12:00 (UTC+3) — 14:00 seferi henüz gelmedi.
    final monday = DateTime.utc(2026, 8, 3, 9);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: SingleChildScrollView(
            child: IntercityRouteCard(
              now: monday,
              expanded: false,
              onToggle: () {},
              route: IntercityRoute.fromJson(intercity(times: ['14:00'])),
            ),
          ),
        ),
      ),
    );
    await tester.pump();

    expect(find.textContaining('Sıradaki 14:00'), findsOneWidget);
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

  // ------------------------------------------------------------ Faz 12.6

  group('araç tipi süzgeci (12.6)', () {
    testWidgets('şerit üç seçenek gösterir ve "Tümü" uca parametre GÖNDERMEZ', (
      tester,
    ) async {
      final adapter = await openTransport(tester);

      expect(find.text('Tümü'), findsOneWidget);
      expect(find.text('Otobüs'), findsWidgets);
      expect(find.text('Minibüs'), findsOneWidget);

      final query = adapter
          .lastOf('/v1/transport/intercity-routes')
          ?.queryParameters;
      expect(query?.containsKey('vehicleType'), isFalse);
    });

    testWidgets('minibüs seçimi uca vehicleType olarak gider', (tester) async {
      // 🔴 Süzme SUNUCUDA: sayfalı listeyi istemcide süzmek "N hat" sayacını
      // ve sonsuz kaydırmayı yalancı yapardı.
      final adapter = await openTransport(tester);

      await tester.tap(find.text('Minibüs'));
      await tester.pumpAndSettle();

      final query = adapter
          .lastOf('/v1/transport/intercity-routes')
          ?.queryParameters;
      expect(query?['vehicleType'], 'minibus');
    });

    testWidgets('araç şeridi MEVCUT ARAMAYI korur', (tester) async {
      // 12.5'te panelde arama ve araç süzgeci bu yüzden tek forma alınmıştı:
      // ayrı tutulsalardı şeride dokunmak aramayı sessizce düşürürdü.
      final adapter = await openTransport(tester);

      await tester.enterText(find.byType(TextField).first, 'adana');
      await tester.pumpAndSettle();
      await tester.tap(find.text('Minibüs'));
      await tester.pumpAndSettle();

      final query = adapter
          .lastOf('/v1/transport/intercity-routes')
          ?.queryParameters;
      expect(query?['searchTerm'], 'adana');
      expect(query?['vehicleType'], 'minibus');
    });

    testWidgets('süzgeç yüzünden boşalan liste SEBEBİNİ söyler', (tester) async {
      await openTransport(
        tester,
        routes: {
          '/v1/transport/intercity-routes': (options) async => jsonResponse(
            pagedBody(
              options.queryParameters.containsKey('vehicleType')
                  ? const []
                  : [intercity()],
            ),
          ),
        },
      );

      await tester.tap(find.text('Minibüs'));
      await tester.pumpAndSettle();

      // "Hiç hat yok" ile "bu tipte hat yok" farklı şeyler.
      expect(find.textContaining('Minibüs tipinde hat bulunmuyor'), findsOneWidget);
      await tester.tap(find.text('Filtreleri temizle'));
      await tester.pumpAndSettle();
      expect(find.text('Kadirli → Adana'), findsOneWidget);
    });

    testWidgets('kartta araç rozeti görünür, tanınmayan tipte GÖRÜNMEZ', (
      tester,
    ) async {
      await openTransport(
        tester,
        intercityItems: [
          intercity(vehicleType: 'minibus'),
          // Sunucu yarın üçüncü bir tip gönderirse kart "Otobüs" yazıp
          // yalan söylememeli.
          intercity(id: 'ic-2', destination: 'Kozan', vehicleType: 'dolmus'),
        ],
      );

      expect(find.text('Minibüs'), findsNWidgets(2)); // şerit + rozet
      expect(find.text('Otobüs'), findsOneWidget); // yalnız şerit
    });
  });

  group('sefer günleri (12.6)', () {
    testWidgets('gün rozeti kalkış saatinin yanında çıkar', (tester) async {
      await openTransport(
        tester,
        intercityItems: [
          intercity(times: const ['06:30'], days: weekdayCodes),
        ],
      );

      await tester.tap(find.text('Kadirli → Adana'));
      await tester.pumpAndSettle();

      expect(find.text('06:30'), findsOneWidget);
      expect(find.text('Hafta içi'), findsOneWidget);
    });

    testWidgets('her gün çalışan hatta rozet şeridi yer kaplamaz', (
      tester,
    ) async {
      await openTransport(
        tester,
        intercityItems: [
          intercity(times: const ['07:00'], days: const [
            'mon', 'tue', 'wed', 'thu', 'fri', 'sat', 'sun',
          ]),
        ],
      );

      await tester.tap(find.text('Kadirli → Adana'));
      await tester.pumpAndSettle();
      expect(find.text('Her gün'), findsNothing);
    });

    testWidgets('🔴 gün alanı HİÇ gelmeyen sefer gizlenmez (eski kayıt)', (
      tester,
    ) async {
      // 12.5 öncesi kayıtlarda `days` yok. "Hiç gün seçilmemiş" sayılsaydı
      // sefer ekrandan sessizce silinirdi.
      await openTransport(
        tester,
        intercityItems: [intercity(times: const ['07:00', '14:00'])],
      );

      await tester.tap(find.text('Kadirli → Adana'));
      await tester.pumpAndSettle();

      expect(find.text('07:00'), findsOneWidget);
      expect(find.text('14:00'), findsOneWidget);
    });

    group('🔴 "kalktı" yalnız BUGÜN çalışan sefer için doğrudur', () {
      // Bu üçlü bir **test boşluğunu** kapatıyor: bozma denemesinde `isPast`
      // kuralından gün kontrolü çıkarıldı ve **hiçbir test kırılmadı** —
      // golden'ın %0.5 toleransı tek bir üstü çizili hapı yutuyor (tolerans
      // anti-aliasing için bilinçli, düzen hataları binlerce piksel değiştirir).
      // Hasar sessiz ve gerçek: Pazar günü bakan vatandaş, o gün **hiç
      // kalkmamış** bir seferi "kalkmış" olarak görürdü.
      // 🔑 12.5'in dersi: yeşil kalan bir bozma denemesi "kural sağlam" demek
      // değil, "test o kuralı tutmuyor" demektir.

      /// Kartı sabit bir anda çizer ve [time] hapının üstünün çizili olup
      /// olmadığını söyler.
      Future<bool> isStruckThrough(
        WidgetTester tester, {
        required DateTime now,
        required List<String> days,
        String time = '07:00',
      }) async {
        tester.view.physicalSize = const Size(1080, 2400);
        tester.view.devicePixelRatio = 3;
        addTearDown(tester.view.reset);

        await tester.pumpWidget(
          MaterialApp(
            home: Scaffold(
              body: SingleChildScrollView(
                child: IntercityRouteCard(
                  now: now,
                  expanded: true,
                  onToggle: () {},
                  route: IntercityRoute.fromJson(
                    intercity(times: [time], days: days),
                  ),
                ),
              ),
            ),
          ),
        );
        await tester.pump();

        final text = tester.widget<Text>(
          find.descendant(
            of: find.byType(IntercityRouteCard),
            matching: find.text(time),
          ),
        );
        return text.style?.decoration == TextDecoration.lineThrough;
      }

      // 9 Ağustos 2026 Pazar, Kadirli 12:00 (UTC+3).
      final sunday = DateTime.utc(2026, 8, 9, 9);
      // 3 Ağustos 2026 Pazartesi, Kadirli 12:00.
      final monday = DateTime.utc(2026, 8, 3, 9);

      testWidgets('bugün çalışmayan seferin üstü ÇİZİLMEZ', (tester) async {
        expect(
          await isStruckThrough(tester, now: sunday, days: weekdayCodes),
          isFalse,
          reason:
              'Pazar günü hafta içi seferi kalkmadı; üstünü çizmek "bugünkü '
              'sefer gitti" demektir ve yalandır.',
        );
      });

      testWidgets('bugün çalışıp saati geçen seferin üstü ÇİZİLİR', (
        tester,
      ) async {
        // Karşı yön: "hiçbir zaman çizme" gerçeklemesi de testi geçmesin.
        expect(
          await isStruckThrough(tester, now: monday, days: weekdayCodes),
          isTrue,
        );
      });

      testWidgets('bugün çalışan ama saati gelmemiş sefer çizilmez', (
        tester,
      ) async {
        expect(
          await isStruckThrough(
            tester,
            now: monday,
            days: weekdayCodes,
            time: '18:00',
          ),
          isFalse,
        );
      });
    });

    group('🐛 "bitti" ile "yok" aynı cümle değil', () {
      // Canlı emülatör denetiminde bulundu: giriş cümlesi `daysAhead`'e değil
      // hattın **bugün çalışıp çalışmadığına** bakmalı.
      Future<void> pumpCard(
        WidgetTester tester, {
        required DateTime now,
        required List<String> days,
        required String time,
      }) async {
        tester.view.physicalSize = const Size(1080, 2400);
        tester.view.devicePixelRatio = 3;
        addTearDown(tester.view.reset);
        await tester.pumpWidget(
          MaterialApp(
            home: Scaffold(
              body: SingleChildScrollView(
                child: IntercityRouteCard(
                  now: now,
                  expanded: false,
                  onToggle: () {},
                  route: IntercityRoute.fromJson(
                    intercity(times: [time], days: days),
                  ),
                ),
              ),
            ),
          ),
        );
        await tester.pump();
      }

      // 3 Ağustos 2026 Pazartesi, Kadirli 20:00.
      final mondayEvening = DateTime.utc(2026, 8, 3, 17);

      testWidgets('bugün çalışıp saatleri geçen hat "bitti" der', (
        tester,
      ) async {
        await pumpCard(
          tester,
          now: mondayEvening,
          days: weekdayCodes,
          time: '07:00',
        );
        expect(find.textContaining('Bugünkü seferler bitti'), findsOneWidget);
        expect(find.textContaining('Yarın 07:00'), findsOneWidget);
      });

      testWidgets('bugün HİÇ çalışmayan hat "bugün sefer yok" der', (
        tester,
      ) async {
        // Pazartesi bakılan hafta sonu hattı: bugün hiç sefer olmadı, "bitti"
        // demek olmamış bir sefer dizisini ima eder.
        await pumpCard(
          tester,
          now: mondayEvening,
          days: const ['sat', 'sun'],
          time: '21:00',
        );
        expect(find.textContaining('Bugün sefer yok'), findsOneWidget);
        expect(find.textContaining('Cmt 21:00'), findsOneWidget);
        expect(find.textContaining('bitti'), findsNothing);
      });
    });

    testWidgets('gün rozeti ekran okuyucuya TAM gün adıyla okunur', (
      tester,
    ) async {
      await openTransport(
        tester,
        intercityItems: [
          intercity(times: const ['06:30'], days: const ['mon', 'wed']),
        ],
      );

      await tester.tap(find.text('Kadirli → Adana'));
      await tester.pumpAndSettle();

      expect(
        find.bySemanticsLabel(RegExp('Pazartesi, Çarşamba')),
        findsOneWidget,
      );
    });
  });

  group('kalkış noktası ve yol tarifi (12.6)', () {
    testWidgets('kalkış noktası kartta ve açık bölümde görünür', (tester) async {
      await openTransport(
        tester,
        intercityItems: [
          intercity(
            departurePointName: 'Kadirli Otogarı',
            departurePointAddress: 'Cumhuriyet Mah. Otogar Cad. No:1',
            departurePointLatitude: 37.3745,
            departurePointLongitude: 36.0972,
          ),
        ],
      );

      expect(find.text('Kadirli Otogarı'), findsOneWidget);

      await tester.tap(find.text('Kadirli → Adana'));
      await tester.pumpAndSettle();

      expect(find.text('Kalkış noktası'), findsOneWidget);
      expect(find.text('Cumhuriyet Mah. Otogar Cad. No:1'), findsOneWidget);
      expect(find.text('Yol tarifi'), findsOneWidget);
    });

    testWidgets('koordinatsız ama adresli noktada da yol tarifi çıkar', (
      tester,
    ) async {
      await openTransport(
        tester,
        intercityItems: [
          intercity(
            departurePointName: 'Minibüs Garajı',
            departurePointAddress: 'Savrun Cad.',
          ),
        ],
      );

      await tester.tap(find.text('Kadirli → Adana'));
      await tester.pumpAndSettle();
      expect(find.text('Yol tarifi'), findsOneWidget);
    });

    testWidgets('🔴 kalkış noktası GİRİLMEMİŞSE bölüm hiç çizilmez', (
      tester,
    ) async {
      // "Otogardan kalkar" tahmini vatandaşı yanlış yere götürür — 12.5'in
      // "geri doldurma YOK" kararının mobil karşılığı. İşlevsiz buton da yok.
      await openTransport(tester, intercityItems: [intercity()]);

      await tester.tap(find.text('Kadirli → Adana'));
      await tester.pumpAndSettle();

      expect(find.text('Kalkış noktası'), findsNothing);
      expect(find.text('Yol tarifi'), findsNothing);
    });

    testWidgets('harita araması Kadirli ile sınırlanır', (tester) async {
      // Yalnız "Otogar" aratmak kullanıcıyı başka şehre götürür (12.4 dersi).
      final route = IntercityRoute.fromJson(
        intercity(departurePointName: 'Otogar', departurePointAddress: null),
      );
      expect(route.departureMapQuery, 'Otogar, Kadirli');

      final named = IntercityRoute.fromJson(
        intercity(departurePointName: 'Kadirli Otogarı'),
      );
      expect(named.departureMapQuery, 'Kadirli Otogarı');
    });
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
