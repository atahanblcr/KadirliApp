import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/router/app_router.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// Duyurular listesi + detayı (11.6), gerçek router ve interceptor zinciriyle.
void main() {
  const guest = {'auth.guestChoice': true};

  Map<String, dynamic> item(
    String id, {
    String title = 'Pazar Yeri Taşınıyor',
    int priority = 0,
    String? typeId = 'type-belediye',
    String typeName = 'Belediye Duyurusu',
    bool hasLink = false,
    String? externalLink,
  }) => {
    'id': id,
    'title': title,
    'body': 'Cumartesi pazarı kapalı pazar alanında kurulacaktır.',
    'typeId': typeId,
    'typeName': typeName,
    'priority': priority,
    'status': 'active',
    'sentAt': '2026-07-30T09:00:00Z',
    'createdAt': '2026-07-30T09:00:00Z',
    'hasLink': hasLink,
    'externalLink': externalLink,
    'hasLocation': false,
  };

  Map<String, dynamic> pagedBody(
    List<Map<String, dynamic>> items, {
    int currentPage = 1,
    int totalPages = 1,
  }) => successEnvelope({
    'items': items,
    'totalCount': items.length,
    'pageSize': 20,
    'currentPage': currentPage,
    'totalPages': totalPages,
  });

  const types = [
    {
      'id': 'type-belediye',
      'name': 'Belediye Duyurusu',
      'slug': 'belediye-duyurusu',
      'icon': 'fa-landmark',
      'color': '#10B981',
      'displayOrder': 3,
    },
    {
      'id': 'type-elektrik',
      'name': 'Elektrik Kesintisi',
      'slug': 'elektrik-kesintisi',
      'icon': 'fa-bolt',
      'color': '#F59E0B',
      'displayOrder': 1,
    },
  ];

  /// Duyuru ekranlarını doğrudan açar (hub'dan geçmeden).
  Future<FakeHttpAdapter> openAnnouncements(
    WidgetTester tester, {
    required Map<String, Future<ResponseBody> Function(RequestOptions)> routes,
    String location = '/duyurular',
  }) async {
    final adapter = routedAdapter({...homeStubs(), ...routes});
    final container = await pumpApp(tester, prefs: guest, adapter: adapter);
    container.read(routerProvider).go(location);
    await tester.pumpAndSettle();
    return adapter;
  }

  testWidgets('liste duyuruları ve tür chip\'lerini gösterir', (tester) async {
    await openAnnouncements(
      tester,
      routes: {
        '/v1/announcements': (_) async => jsonResponse(pagedBody([item('1')])),
        '/v1/announcements/types': (_) async =>
            jsonResponse(successEnvelope(types)),
      },
    );

    expect(find.text('Pazar Yeri Taşınıyor'), findsOneWidget);
    expect(find.text('Tümü'), findsOneWidget);
    expect(find.text('Elektrik Kesintisi'), findsOneWidget);
  });

  testWidgets('tür chip\'ine dokununca liste o türle yeniden istenir', (
    tester,
  ) async {
    final adapter = await openAnnouncements(
      tester,
      routes: {
        '/v1/announcements': (_) async => jsonResponse(pagedBody([item('1')])),
        '/v1/announcements/types': (_) async =>
            jsonResponse(successEnvelope(types)),
      },
    );

    await tester.tap(find.text('Elektrik Kesintisi'));
    await tester.pumpAndSettle();

    expect(
      adapter.lastOf('/v1/announcements')?.queryParameters['typeId'],
      'type-elektrik',
    );
  });

  testWidgets('tür listesi patlarsa filtre çizilmez ama duyurular görünür', (
    tester,
  ) async {
    await openAnnouncements(
      tester,
      routes: {
        '/v1/announcements': (_) async => jsonResponse(pagedBody([item('1')])),
        '/v1/announcements/types': (_) async => jsonResponse(
          errorEnvelope('INTERNAL_ERROR', 'Sunucu hatası.'),
          statusCode: 500,
        ),
      },
    );

    expect(find.text('Tümü'), findsNothing, reason: 'çalışmayan filtre gösterilmez');
    expect(find.text('Pazar Yeri Taşınıyor'), findsOneWidget);
  });

  testWidgets('filtreliyken boş sonuç, filtreyi kaldırma önerir', (
    tester,
  ) async {
    final adapter = await openAnnouncements(
      tester,
      routes: {
        '/v1/announcements': (options) async => jsonResponse(
          options.queryParameters.containsKey('typeId')
              ? pagedBody(const [])
              : pagedBody([item('1')]),
        ),
        '/v1/announcements/types': (_) async =>
            jsonResponse(successEnvelope(types)),
      },
    );

    await tester.tap(find.text('Elektrik Kesintisi'));
    await tester.pumpAndSettle();

    expect(find.text('Bu türde duyuru yok'), findsOneWidget);
    await tester.tap(find.text('Tüm duyurular'));
    await tester.pumpAndSettle();

    expect(find.text('Pazar Yeri Taşınıyor'), findsOneWidget);
    expect(
      adapter.lastOf('/v1/announcements')?.queryParameters.containsKey('typeId'),
      isFalse,
    );
  });

  testWidgets('karta dokununca detay açılır ve görüntülenme sayacı tetiklenir', (
    tester,
  ) async {
    final adapter = await openAnnouncements(
      tester,
      routes: {
        '/v1/announcements': (_) async => jsonResponse(pagedBody([item('abc')])),
        '/v1/announcements/types': (_) async =>
            jsonResponse(successEnvelope(types)),
        '/v1/announcements/abc': (_) async =>
            jsonResponse(successEnvelope(item('abc'))),
        '/v1/announcements/abc/view': (_) async =>
            jsonResponse(successEnvelope(true)),
      },
    );

    await tester.tap(find.text('Pazar Yeri Taşınıyor'));
    await tester.pumpAndSettle();

    expect(find.text('Duyuru'), findsOneWidget, reason: 'detay başlığı');
    expect(adapter.countOf('/v1/announcements/abc/view'), 1);
    expect(
      adapter.lastOf('/v1/announcements/abc/view')?.method,
      'POST',
    );
  });

  testWidgets('görüntülenme sayacı yeniden çizimde tekrar gönderilmez', (
    tester,
  ) async {
    final adapter = await openAnnouncements(
      tester,
      location: '/duyurular/abc',
      routes: {
        '/v1/announcements': (_) async => jsonResponse(pagedBody([item('abc')])),
        '/v1/announcements/abc': (_) async =>
            jsonResponse(successEnvelope(item('abc'))),
        '/v1/announcements/abc/view': (_) async =>
            jsonResponse(successEnvelope(true)),
      },
    );

    await tester.pump(const Duration(milliseconds: 300));
    await tester.pumpAndSettle();

    expect(adapter.countOf('/v1/announcements/abc/view'), 1);
  });

  testWidgets('silinmiş duyuru (200 + success:false) "bulunamadı" gösterir', (
    tester,
  ) async {
    await openAnnouncements(
      tester,
      location: '/duyurular/yok',
      routes: {
        // ⚠️ Kontrat istisnası: HTTP 200 ama success:false.
        '/v1/announcements/yok': (_) async =>
            jsonResponse(errorEnvelope('NOT_FOUND', 'Duyuru bulunamadı.')),
      },
    );

    expect(find.text('Duyuru bulunamadı'), findsOneWidget);
    expect(
      find.text('Tekrar dene'),
      findsNothing,
      reason: 'bulunamayan kayıt için tekrar denemek anlamsız',
    );
  });

  testWidgets('dış bağlantıya dokununca tıklama sayacı gönderilir', (
    tester,
  ) async {
    final adapter = await openAnnouncements(
      tester,
      location: '/duyurular/abc',
      routes: {
        '/v1/announcements/abc': (_) async => jsonResponse(
          successEnvelope(
            item('abc', hasLink: true, externalLink: 'https://kadirli.bel.tr'),
          ),
        ),
        '/v1/announcements/abc/view': (_) async =>
            jsonResponse(successEnvelope(true)),
        '/v1/announcements/abc/click': (_) async =>
            jsonResponse(successEnvelope(true)),
      },
    );

    await tester.tap(find.text('Bağlantıyı aç'));
    // Sayaç isteği "ateşle ve unut": bağlantıyı bekletmiyor, bu yüzden
    // mikro-görev sırasının boşalması için birkaç kare gerekiyor.
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(adapter.countOf('/v1/announcements/abc/click'), 1);
  });
}
