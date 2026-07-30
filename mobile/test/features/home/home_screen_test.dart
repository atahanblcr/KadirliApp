import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// Ana Sayfa (Hub) — 11.4.
///
/// Testler gerçek uygulamayı (router + kabuk + interceptor zinciri) sahte HTTP
/// adaptörüyle koşturur; yanıt gövdeleri **canlı API'den alınan gerçek
/// şekillerdir** (bkz. `Memory_Bank/API_CONTRACT.md`).
void main() {
  const guest = {'auth.guestChoice': true};

  String iso(Duration offsetFromNow) =>
      DateTime.now().toUtc().add(offsetFromNow).toIso8601String();

  Map<String, dynamic> pharmacy({String name = 'Şifa Eczanesi'}) => {
    'scheduleId': 'aaaaaaaa-0000-0000-0000-000000000001',
    'dutyDate': iso(Duration.zero),
    'startTime': '08:30',
    'endTime': '08:30',
    'pharmacyId': 'bbbbbbbb-0000-0000-0000-000000000001',
    'name': name,
    'address': 'Cumhuriyet Cad. No:12',
    'phone': '+903287141001',
  };

  Map<String, dynamic> outage({
    required Duration start,
    required Duration end,
    String neighborhood = 'Yenimahalle',
  }) => {
    'id': 'cccccccc-0000-0000-0000-000000000001',
    'neighborhood': neighborhood,
    'startTime': iso(start),
    'endTime': iso(end),
    'reason': 'Trafo bakımı',
  };

  Map<String, dynamic> announcement({
    required String title,
    int priority = 0,
    String type = 'Belediye Duyurusu',
  }) => {
    'id': 'dddddddd-0000-0000-0000-00000000000$priority',
    'title': title,
    'body': 'Duyuru içeriği.',
    'typeName': type,
    'priority': priority,
    'status': 'active',
    'sentAt': iso(const Duration(hours: -2)),
    'createdAt': iso(const Duration(hours: -2)),
  };

  testWidgets('acil şerit bugünün nöbetçi eczanesini gösterir', (tester) async {
    await pumpApp(
      tester,
      prefs: guest,
      adapter: routedAdapter(homeStubs(onDuty: [pharmacy()])),
    );

    expect(find.textContaining('Şifa Eczanesi'), findsOneWidget);
  });

  testWidgets('nöbet atanmamışsa şerit bunu açıkça söyler (boş ≠ hata)', (tester) async {
    await pumpApp(tester, prefs: guest, adapter: routedAdapter(homeStubs()));

    expect(find.text('Bugünün nöbetçi eczanesi henüz girilmedi'), findsOneWidget);
    expect(find.text('Planlı elektrik kesintisi yok'), findsOneWidget);
  });

  testWidgets('süren kesinti uyarı olarak görünür', (tester) async {
    await pumpApp(
      tester,
      prefs: guest,
      adapter: routedAdapter(
        homeStubs(
          outages: [
            outage(start: const Duration(hours: -1), end: const Duration(hours: 2)),
          ],
        ),
      ),
    );

    expect(find.textContaining('kesinti sürüyor'), findsOneWidget);
    expect(find.textContaining('Yenimahalle'), findsOneWidget);
  });

  testWidgets('geçmiş kesintiler şeride girmez (uç tarih filtrelemiyor)', (tester) async {
    await pumpApp(
      tester,
      prefs: guest,
      adapter: routedAdapter(
        homeStubs(
          outages: [
            outage(start: const Duration(days: -20), end: const Duration(days: -20)),
          ],
        ),
      ),
    );

    expect(find.text('Planlı elektrik kesintisi yok'), findsOneWidget);
  });

  testWidgets('son duyurular vitrinde listelenir', (tester) async {
    await pumpApp(
      tester,
      prefs: guest,
      adapter: routedAdapter(
        homeStubs(
          announcements: [
            announcement(title: 'Pazar Yeri Taşınıyor'),
            announcement(title: 'Su Kesintisi', priority: 2, type: 'Su Kesintisi'),
          ],
        ),
      ),
    );

    // Vitrin ızgaranın altında → görünür alana kaydırılır. (İç GridView de bir
    // Scrollable olduğu için dış ListView açıkça seçilir.)
    await tester.scrollUntilVisible(
      find.text('Pazar Yeri Taşınıyor'),
      200,
      scrollable: find.byType(Scrollable).first,
    );
    expect(find.text('Pazar Yeri Taşınıyor'), findsOneWidget);
    expect(find.text('Su Kesintisi'), findsWidgets);
    // Öncelik yalnız renkle değil metinle de belirtilir (erişilebilirlik).
    expect(find.text('Acil'), findsOneWidget);
  });

  testWidgets('bir uç patlarsa yalnız o kart hata gösterir, diğerleri çalışır', (
    tester,
  ) async {
    await pumpApp(
      tester,
      prefs: guest,
      adapter: routedAdapter({
        ...homeStubs(onDuty: [pharmacy()]),
        '/v1/announcements': (_) async => jsonResponse(
          errorEnvelope('INTERNAL_ERROR', 'Sunucu hatası.'),
          statusCode: 500,
        ),
      }),
    );

    // Acil şerit dolu (duyuru hatası onu etkilemedi)…
    expect(find.textContaining('Şifa Eczanesi'), findsOneWidget);

    // …vitrinde ise hata + tekrar dene. (Kaydırınca şerit ListView'dan
    // düşeceği için sıralama önemli.)
    await tester.scrollUntilVisible(
      find.text('Tekrar dene'),
      200,
      scrollable: find.byType(Scrollable).first,
    );
    expect(find.text('Tekrar dene'), findsOneWidget);
  });

  testWidgets('modül kartına dokununca ilgili ekran açılır (ölü buton yok)', (
    tester,
  ) async {
    await pumpApp(tester, prefs: guest, adapter: routedAdapter(homeStubs()));

    await tester.tap(find.text('Duyurular'));
    await tester.pumpAndSettle();

    expect(find.text('Duyurular yakında'), findsOneWidget);
  });

  testWidgets('şeritteki eczane satırı Eczane modülüne götürür', (tester) async {
    await pumpApp(
      tester,
      prefs: guest,
      adapter: routedAdapter(homeStubs(onDuty: [pharmacy()])),
    );

    await tester.tap(find.textContaining('Şifa Eczanesi'));
    await tester.pumpAndSettle();

    expect(find.text('Eczane yakında'), findsOneWidget);
  });

  testWidgets('aşağı çekince üç veri kaynağı da tazelenir', (tester) async {
    final adapter = routedAdapter(homeStubs(onDuty: [pharmacy()]));
    await pumpApp(tester, prefs: guest, adapter: adapter);

    expect(adapter.countOf('/v1/announcements'), 1);
    expect(adapter.countOf('/v1/pharmacies/on-duty'), 1);
    expect(adapter.countOf('/v1/power-outages'), 1);

    await tester.fling(find.text('Modüller'), const Offset(0, 400), 1000);
    await tester.pumpAndSettle();

    expect(adapter.countOf('/v1/announcements'), 2);
    expect(adapter.countOf('/v1/pharmacies/on-duty'), 2);
    expect(adapter.countOf('/v1/power-outages'), 2);
  });

  testWidgets('sekmeler arasında geçiş veriyi yeniden çekmez', (tester) async {
    final adapter = routedAdapter(homeStubs(onDuty: [pharmacy()]));
    await pumpApp(tester, prefs: guest, adapter: adapter);

    Finder tab(String label) => find.descendant(
      of: find.byType(NavigationBar),
      matching: find.text(label),
    );

    await tester.tap(tab('İlanlar'));
    await tester.pumpAndSettle();
    await tester.tap(tab('Ana Sayfa'));
    await tester.pumpAndSettle();

    expect(adapter.countOf('/v1/pharmacies/on-duty'), 1);
    expect(find.textContaining('Şifa Eczanesi'), findsOneWidget);
  });
}
