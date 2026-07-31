import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/router/app_router.dart';
import 'package:kadirli_app/features/pharmacies/data/models/pharmacy.dart';
import 'package:kadirli_app/features/pharmacies/presentation/pharmacy_detail_screen.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// Nöbetçi Eczane ekranı + eczane detayı (11.7).
void main() {
  const guest = {'auth.guestChoice': true};

  Map<String, dynamic> onDuty({String name = 'Şifa Eczanesi'}) => {
    'scheduleId': 'aaaa0000-0000-0000-0000-000000000001',
    'dutyDate': '2026-07-31T00:00:00Z',
    'startTime': '19:00',
    'endTime': '09:00',
    'pharmacyId': 'bbbb0000-0000-0000-0000-000000000001',
    'name': name,
    'address': 'Fatih Mah. Hastane Cad. No:8',
    'phone': '+903287141003',
  };

  Map<String, dynamic> scheduleEntry({
    required String date,
    String id = 'cccc0000-0000-0000-0000-000000000001',
    String pharmacyId = 'bbbb0000-0000-0000-0000-000000000001',
    String pharmacyName = 'Merkez Eczanesi',
  }) => {
    'id': id,
    'dutyDate': date,
    'startTime': '19:00',
    'endTime': '09:00',
    'pharmacyId': pharmacyId,
    'pharmacyName': pharmacyName,
    'source': 'mock',
  };

  Map<String, dynamic> pharmacy({
    String id = 'p1',
    String name = 'Merkez Eczanesi',
  }) => {
    'id': id,
    'name': name,
    'address': 'Cumhuriyet Cad. No:12',
    'phone': '+903287141001',
    'latitude': null,
    'longitude': null,
    'workingHours': '08:30 - 19:00',
    'pharmacistName': 'Ecz. Zeynep Aslan',
    'isActive': true,
  };

  Map<String, dynamic> pagedBody(List<Map<String, dynamic>> items) =>
      successEnvelope({
        'items': items,
        'totalCount': items.length,
        'pageSize': 20,
        'currentPage': 1,
        'totalPages': 1,
      });

  Future<FakeHttpAdapter> openPharmacies(
    WidgetTester tester, {
    Map<String, Future<ResponseBody> Function(RequestOptions)> routes = const {},
    String location = '/eczaneler',
  }) async {
    // ⚠️ Varsayılan test yüzeyi 800x600 — takvim ızgarası + altındaki bölümler
    // görüş alanının dışında kalıyor ve `tap` "off-screen" diye reddediliyor.
    // Gerçek bir telefon yüzeyi vermek, testleri kaydırma jimnastiğinden
    // kurtarıyor (ekranın kendisi zaten kaydırılabilir).
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final adapter = routedAdapter({...homeStubs(), ...routes});
    final container = await pumpApp(tester, prefs: guest, adapter: adapter);
    container.read(routerProvider).go(location);
    await tester.pumpAndSettle();
    // İç içe rota: `/eczaneler/:id`'ye gitmek ALT ekranı (liste) da kurar →
    // takvim isteği arkada başlar; bir kare daha pompalanmazsa "pending timer".
    await tester.pump(const Duration(milliseconds: 100));
    await tester.pumpAndSettle();
    return adapter;
  }

  testWidgets('bugünün nöbetçisi kartta gösterilir ve arama butonu çıkar', (
    tester,
  ) async {
    await openPharmacies(
      tester,
      routes: {
        '/v1/pharmacies/on-duty': (_) async =>
            jsonResponse(successEnvelope([onDuty()])),
        '/v1/pharmacies/schedule': (_) async =>
            jsonResponse(successEnvelope(<Object>[])),
      },
    );

    expect(find.text('BUGÜN NÖBETÇİ'), findsOneWidget);
    expect(find.text('Şifa Eczanesi'), findsOneWidget);
    expect(find.text('19:00 - 09:00'), findsOneWidget);
    expect(find.text('Eczaneyi ara'), findsOneWidget);
  });

  testWidgets('nöbetçi yoksa takvime yönlendiren nazik mesaj çıkar', (
    tester,
  ) async {
    await openPharmacies(
      tester,
      routes: {
        '/v1/pharmacies/on-duty': (_) async =>
            jsonResponse(successEnvelope(<Object>[])),
        '/v1/pharmacies/schedule': (_) async =>
            jsonResponse(successEnvelope(<Object>[])),
      },
    );

    expect(find.textContaining('henüz girilmedi'), findsOneWidget);
    expect(find.text('Eczaneyi ara'), findsNothing);
  });

  testWidgets('takvimde nöbetli güne dokununca o günün nöbetçisi listelenir', (
    tester,
  ) async {
    final now = DateTime.now();
    final day = DateTime.utc(now.year, now.month, 15);

    await openPharmacies(
      tester,
      routes: {
        '/v1/pharmacies/on-duty': (_) async =>
            jsonResponse(successEnvelope(<Object>[])),
        '/v1/pharmacies/schedule': (_) async => jsonResponse(
          successEnvelope([
            scheduleEntry(date: day.toIso8601String(), pharmacyName: 'Savrun Eczanesi'),
          ]),
        ),
      },
    );

    expect(find.textContaining('işaretli bir güne dokunun'), findsOneWidget);

    await tester.tap(find.text('15'));
    await tester.pumpAndSettle();

    expect(find.text('Savrun Eczanesi'), findsOneWidget);
    expect(
      find.textContaining('ertesi gün'),
      findsOneWidget,
      reason: 'gece yarısını aşan nöbette uyarı yazılmalı',
    );
  });

  testWidgets('nöbetsiz güne dokunmak hiçbir şey yapmaz (ölü buton yok)', (
    tester,
  ) async {
    final now = DateTime.now();
    final day = DateTime.utc(now.year, now.month, 15);

    await openPharmacies(
      tester,
      routes: {
        '/v1/pharmacies/on-duty': (_) async =>
            jsonResponse(successEnvelope(<Object>[])),
        '/v1/pharmacies/schedule': (_) async => jsonResponse(
          successEnvelope([scheduleEntry(date: day.toIso8601String())]),
        ),
      },
    );

    await tester.tap(find.text('16'));
    await tester.pumpAndSettle();

    expect(find.textContaining('işaretli bir güne dokunun'), findsOneWidget);
  });

  testWidgets('ay ileri gidince yeni ayın nöbet listesi istenir', (
    tester,
  ) async {
    final adapter = await openPharmacies(
      tester,
      routes: {
        '/v1/pharmacies/on-duty': (_) async =>
            jsonResponse(successEnvelope(<Object>[])),
        '/v1/pharmacies/schedule': (_) async =>
            jsonResponse(successEnvelope(<Object>[])),
      },
    );

    final firstMonth =
        adapter.lastOf('/v1/pharmacies/schedule')?.queryParameters['month'];

    await tester.tap(find.byTooltip('Sonraki ay'));
    await tester.pumpAndSettle();

    final secondMonth =
        adapter.lastOf('/v1/pharmacies/schedule')?.queryParameters['month'];

    expect(adapter.countOf('/v1/pharmacies/schedule'), 2);
    expect(secondMonth, isNot(firstMonth));
  });

  testWidgets('Eczaneler sekmesinde arama uca gider ve detay açılır', (
    tester,
  ) async {
    final adapter = await openPharmacies(
      tester,
      routes: {
        '/v1/pharmacies/on-duty': (_) async =>
            jsonResponse(successEnvelope(<Object>[])),
        '/v1/pharmacies/schedule': (_) async =>
            jsonResponse(successEnvelope(<Object>[])),
        '/v1/pharmacies': (_) async => jsonResponse(pagedBody([pharmacy()])),
        '/v1/pharmacies/p1': (_) async =>
            jsonResponse(successEnvelope(pharmacy())),
      },
    );

    await tester.tap(find.text('Eczaneler'));
    await tester.pumpAndSettle();

    expect(find.text('Merkez Eczanesi'), findsOneWidget);

    await tester.enterText(find.byType(TextField), 'merkez');
    await tester.pumpAndSettle();
    expect(
      adapter.lastOf('/v1/pharmacies')?.queryParameters['search'],
      'merkez',
    );

    await tester.tap(find.text('Merkez Eczanesi'));
    await tester.pumpAndSettle();

    expect(find.text('Eczane'), findsOneWidget, reason: 'detay başlığı');
    expect(find.text('Ecz. Zeynep Aslan'), findsOneWidget);
    expect(find.text('Çalışma saatleri'), findsOneWidget);
  });

  testWidgets('bulunamayan eczane nazik mesaj gösterir', (tester) async {
    await openPharmacies(
      tester,
      location: '/eczaneler/yok',
      routes: {
        '/v1/pharmacies/yok': (_) async => jsonResponse(
          errorEnvelope('NOT_FOUND', 'Eczane bulunamadı.'),
          statusCode: 404,
        ),
        // Detay ekranı "bu ayki nöbet günleri" için takvimi de okuyor.
        '/v1/pharmacies/schedule': (_) async =>
            jsonResponse(successEnvelope(<Object>[])),
      },
    );

    expect(find.text('Eczane bulunamadı'), findsOneWidget);
    expect(find.text('Tekrar dene'), findsNothing);
  });

  test('paylaşım metni ad, adres, telefon ve saatleri taşır', () {
    final text = pharmacyShareText(
      const Pharmacy(
        id: 'p1',
        name: 'Merkez Eczanesi',
        address: 'Cumhuriyet Cad. No:12',
        phone: '+903287141001',
        workingHours: '08:30 - 19:00',
      ),
    );

    expect(text, contains('Merkez Eczanesi'));
    expect(text, contains('Cumhuriyet Cad. No:12'));
    expect(text, contains('+903287141001'));
    expect(text, contains('08:30 - 19:00'));
  });
}
