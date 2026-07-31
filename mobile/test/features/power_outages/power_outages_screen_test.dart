import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/router/app_router.dart';
import 'package:kadirli_app/features/power_outages/data/models/power_outage.dart';
import 'package:kadirli_app/features/power_outages/presentation/power_outage_detail_screen.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';
import '../../helpers/profile_fixtures.dart';

/// Elektrik kesintileri listesi + detayı (11.6).
void main() {
  const guest = {'auth.guestChoice': true};

  String iso(Duration offset) =>
      DateTime.now().toUtc().add(offset).toIso8601String();

  Map<String, dynamic> outage({
    required String id,
    required Duration start,
    required Duration end,
    String? neighborhood = 'Yenimahalle',
    String? reason = 'Trafo bakımı',
  }) => {
    'id': id,
    'neighborhood': neighborhood,
    'startTime': iso(start),
    'endTime': iso(end),
    'reason': reason,
  };

  Future<FakeHttpAdapter> openOutages(
    WidgetTester tester, {
    required Map<String, Future<ResponseBody> Function(RequestOptions)> routes,
    String location = '/kesintiler',
    bool signedIn = false,
  }) async {
    final adapter = routedAdapter({...homeStubs(), ...routes});
    final container = await pumpApp(
      tester,
      prefs: signedIn ? const {} : guest,
      tokenStore: signedIn
          ? InMemoryTokenStore(accessToken: 'A', refreshToken: 'R')
          : InMemoryTokenStore(),
      adapter: adapter,
    );
    container.read(routerProvider).go(location);
    await tester.pumpAndSettle();
    return adapter;
  }

  testWidgets('süren ve planlanan kesintiler ayrı başlıklar altında', (
    tester,
  ) async {
    await openOutages(
      tester,
      routes: {
        '/v1/power-outages': (_) async => jsonResponse(
          successEnvelope([
            outage(
              id: 'suren',
              start: const Duration(hours: -1),
              end: const Duration(hours: 2),
            ),
            outage(
              id: 'planli',
              neighborhood: 'Karataş',
              start: const Duration(hours: 5),
              end: const Duration(hours: 7),
            ),
          ]),
        ),
      },
    );

    expect(find.text('Şu an sürüyor'), findsOneWidget);
    expect(find.text('Planlanan'), findsOneWidget);
    expect(find.text('Yenimahalle'), findsOneWidget);
    expect(find.text('Karataş'), findsOneWidget);
    expect(find.textContaining('Bitmesine'), findsOneWidget);
    expect(find.textContaining('sonra başlıyor'), findsOneWidget);
  });

  testWidgets('geçmiş kesinti güncel sekmede yok, Geçmiş sekmesinde var', (
    tester,
  ) async {
    await openOutages(
      tester,
      routes: {
        '/v1/power-outages': (_) async => jsonResponse(
          successEnvelope([
            outage(
              id: 'gecmis',
              neighborhood: 'Savrun',
              start: const Duration(hours: -9),
              end: const Duration(hours: -6),
            ),
          ]),
        ),
      },
    );

    expect(find.text('Planlı kesinti yok'), findsOneWidget);
    expect(find.text('Güncel (0)'), findsOneWidget);
    expect(find.text('Geçmiş (1)'), findsOneWidget);

    await tester.tap(find.text('Geçmiş (1)'));
    await tester.pumpAndSettle();

    expect(find.text('Savrun'), findsOneWidget);
    expect(find.text('Sona erdi'), findsOneWidget);
  });

  testWidgets('misafirde "sadece mahallem" anahtarı hiç çizilmez', (
    tester,
  ) async {
    await openOutages(
      tester,
      routes: {
        '/v1/power-outages': (_) async => jsonResponse(
          successEnvelope([
            outage(
              id: '1',
              start: const Duration(hours: 2),
              end: const Duration(hours: 4),
            ),
          ]),
        ),
      },
    );

    expect(find.textContaining('Sadece'), findsNothing);
  });

  testWidgets(
    'oturum açık kullanıcıda mahalle filtresi başka mahalleyi gizler, '
    'şehir genelini bırakır',
    (tester) async {
      await openOutages(
        tester,
        signedIn: true,
        routes: {
          '/v1/users/me': (_) async => jsonResponse(
            successEnvelope(profileBody(neighborhoodName: 'Yenimahalle')),
          ),
          '/v1/power-outages': (_) async => jsonResponse(
            successEnvelope([
              outage(
                id: 'benim',
                start: const Duration(hours: 2),
                end: const Duration(hours: 3),
              ),
              outage(
                id: 'baska',
                neighborhood: 'Karataş',
                start: const Duration(hours: 4),
                end: const Duration(hours: 5),
              ),
              outage(
                id: 'sehir',
                neighborhood: null,
                start: const Duration(hours: 6),
                end: const Duration(hours: 7),
              ),
            ]),
          ),
        },
      );

      expect(find.text('Mahalleniz'), findsOneWidget, reason: 'filtre kapalıyken rozet');
      expect(find.text('Karataş'), findsOneWidget);

      await tester.tap(find.text('Sadece Yenimahalle'));
      await tester.pumpAndSettle();

      expect(find.text('Karataş'), findsNothing);
      expect(find.text('Kadirli geneli'), findsOneWidget);
      expect(find.textContaining('1 kesinti mahalle filtresi'), findsOneWidget);
    },
  );

  testWidgets('karta dokununca detay açılır', (tester) async {
    await openOutages(
      tester,
      routes: {
        '/v1/power-outages': (_) async => jsonResponse(
          successEnvelope([
            outage(
              id: 'abc',
              start: const Duration(hours: 3),
              end: const Duration(hours: 6),
            ),
          ]),
        ),
        '/v1/power-outages/abc': (_) async => jsonResponse(
          successEnvelope(
            outage(
              id: 'abc',
              start: const Duration(hours: 3),
              end: const Duration(hours: 6),
            ),
          ),
        ),
      },
    );

    await tester.tap(find.text('Yenimahalle'));
    await tester.pumpAndSettle();

    expect(find.text('Kesinti'), findsOneWidget, reason: 'detay başlığı');
    expect(find.text('Planlanan kesinti'), findsOneWidget);
    expect(find.text('Başlangıç'), findsOneWidget);
    expect(find.text('Süre'), findsOneWidget);
    expect(find.text('Kesintiyi paylaş'), findsOneWidget);
  });

  testWidgets('bulunamayan kesinti (200 + success:false) nazik mesaj gösterir', (
    tester,
  ) async {
    await openOutages(
      tester,
      location: '/kesintiler/yok',
      routes: {
        '/v1/power-outages/yok': (_) async => jsonResponse(
          errorEnvelope('NOT_FOUND', 'Elektrik kesintisi bulunamadı.'),
        ),
      },
    );

    expect(find.text('Kesinti kaydı bulunamadı'), findsOneWidget);
    expect(find.text('Tekrar dene'), findsNothing);
  });

  test('paylaşım metni mahalle, aralık, süre ve sebebi taşır', () {
    final text = shareTextOf(
      PowerOutage(
        id: '1',
        neighborhood: 'Yenimahalle',
        startTime: DateTime.utc(2026, 8, 12, 6),
        endTime: DateTime.utc(2026, 8, 12, 12),
        reason: 'Trafo bakımı',
      ),
    );

    expect(text, contains('Yenimahalle'));
    expect(text, contains('12 Ağustos 2026'));
    expect(text, contains('09:00'), reason: 'Kadirli saati (UTC+3)');
    expect(text, contains('15:00'));
    expect(text, contains('6 saat'));
    expect(text, contains('Trafo bakımı'));
  });
}
