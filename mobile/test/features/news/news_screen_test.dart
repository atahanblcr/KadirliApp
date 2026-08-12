import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/router/app_router.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// Haberler listesi (12.14): şerit + arama + boş/hata durumları.
void main() {
  const guest = {'auth.guestChoice': true};

  const categories = [
    {
      'id': 'cat-gundem',
      'name': 'Gündem',
      'slug': 'gundem',
      'articleCount': 9,
      'showInFilterStrip': true,
      'displayOrder': 0,
    },
    {
      'id': 'cat-spor',
      'name': 'Spor',
      'slug': 'spor',
      // Kaydı 0 olan kategori de şeritte durur (sayı bir anlık görüntüdür).
      'articleCount': 0,
      'showInFilterStrip': true,
      'displayOrder': 1,
    },
  ];

  /// ⚠️ Fixture'da **görsel yok** ve bu bilinçli: `CachedNetworkImage`'in
  /// yer tutucusu sonsuz shimmer animasyonu çalıştırıyor, `pumpAndSettle` de
  /// sonsuz animasyonda kilitleniyor (ARCHITECTURE §8 "bilinen test tuzakları").
  /// Kartın görselli düzeni `news_card_test.dart`'ta ayrıca denetleniyor.
  Map<String, dynamic> article({
    String id = 'n1',
    String title = 'Kadirli’de yaz akşamları sinema keyfiyle renkleniyor',
    String? imageUrl,
    bool featured = false,
    String categoryId = 'cat-gundem',
    String categoryName = 'Gündem',
  }) => {
    'id': id,
    'title': title,
    'excerpt': 'Kadirli Belediyesi açık hava sineması etkinliklerine devam ediyor.',
    'contentHtml': null,
    'imageUrl': imageUrl,
    'imageWidth': 650,
    'imageHeight': 368,
    'sourceUrl': 'https://www.silagazetesi.com.tr/haber/',
    'publishedAt': '2026-08-11T14:40:59Z',
    'modifiedAt': '2026-08-11T14:41:00Z',
    'readingMinutes': 2,
    'isFeatured': featured,
    'categories': [
      {'id': categoryId, 'name': categoryName, 'slug': 'slug'},
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

  /// `/v1/news` hem listeyi hem manşeti karşılıyor (`?featured=true`) →
  /// tek işleyici sorgu parametresine bakarak ayırır.
  Future<ResponseBody> Function(RequestOptions) newsRoute({
    required List<Map<String, dynamic>> list,
    List<Map<String, dynamic>> featured = const [],
  }) => (options) async {
    final isFeatured = options.queryParameters['featured'] == true;
    return jsonResponse(pagedBody(isFeatured ? featured : list));
  };

  Future<FakeHttpAdapter> openNews(
    WidgetTester tester, {
    Map<String, Future<ResponseBody> Function(RequestOptions)> routes = const {},
    Map<String, Object> prefs = guest,
    String location = '/haberler',
  }) async {
    // ⚠️ Varsayılan 800x600 yüzeyde uzun ekranlarda `tap` "off-screen" diye
    // reddediliyor (11.7 dersi) → gerçek telefon yüzeyi verilir.
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/news/categories': (_) async => jsonResponse(
        successEnvelope(
          categories.map((c) => Map<String, dynamic>.from(c)).toList(),
        ),
      ),
      '/v1/news': newsRoute(list: [article()]),
      ...routes,
    });
    final container = await pumpApp(tester, prefs: prefs, adapter: adapter);
    container.read(routerProvider).go(location);
    await tester.pumpAndSettle();
    return adapter;
  }

  testWidgets('haberler listelenir', (tester) async {
    await openNews(tester);

    expect(
      find.text('Kadirli’de yaz akşamları sinema keyfiyle renkleniyor'),
      findsOneWidget,
    );
    expect(find.text('2 dk okuma'), findsWidgets);
  });

  testWidgets('kategori şeridi 0 kayıtlı kategoriyi de gösterir', (
    tester,
  ) async {
    // Sunucunun döndürdüğü bir kategoriyi istemcinin gizlemesi "şüphede
    // kalınca gizle" olurdu — §7 madde 49 bunun tersini söylüyor.
    await openNews(tester);

    expect(find.text('Tümü'), findsOneWidget);
    expect(find.text('Gündem'), findsWidgets);
    expect(find.text('Spor'), findsOneWidget);
  });

  testWidgets('kategori seçimi UCA gider (istemcide süzülmez)', (tester) async {
    final adapter = await openNews(tester);

    await tester.tap(find.text('Spor'));
    await tester.pumpAndSettle();

    // 🔴 Süzme sunucuda: 20'lik sayfadan kayıt eleyip "17 haber" demek
    // `totalCount`'u ve sonsuz kaydırmayı yalancı yapardı (checklist §5).
    expect(adapter.lastOf('/v1/news')?.queryParameters['categoryId'], 'cat-spor');
  });

  testWidgets('aynı kategoriye tekrar dokunmak süzgeci kaldırır', (
    tester,
  ) async {
    final adapter = await openNews(tester);

    await tester.tap(find.text('Spor'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Spor'));
    await tester.pumpAndSettle();

    expect(
      adapter.lastOf('/v1/news')?.queryParameters.containsKey('categoryId'),
      isFalse,
    );
  });

  testWidgets('arama kategori süzgecini DÜŞÜRMEZ', (tester) async {
    // ⚠️ Süzgeç ile arama tek filtre nesnesinde taşınmasaydı, şeride dokunmak
    // aramayı (ya da tersi) sessizce düşürürdü (checklist §5).
    final adapter = await openNews(tester);

    await tester.tap(find.text('Spor'));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextField).first, 'sinema');
    await tester.pumpAndSettle();

    final query = adapter.lastOf('/v1/news')?.queryParameters;
    expect(query?['search'], 'sinema');
    expect(query?['categoryId'], 'cat-spor');
  });

  testWidgets('tek harflik arama uca GİTMEZ, ekran sebebini söyler', (
    tester,
  ) async {
    // Sunucu 2 karakterin altında süzgeci hiç uygulamıyor (400 değil) →
    // istek atılsaydı kullanıcı **tüm listeyi** görüp süzülmüş sanırdı.
    final adapter = await openNews(
      tester,
      routes: {'/v1/news': newsRoute(list: const [])},
    );

    await tester.enterText(find.byType(TextField).first, 'k');
    await tester.pumpAndSettle();

    expect(
      adapter.lastOf('/v1/news')?.queryParameters.containsKey('search'),
      isFalse,
    );
    expect(find.text('Aramaya devam edin'), findsOneWidget);
    expect(find.textContaining('en az 2 harf'), findsOneWidget);
  });

  testWidgets('boş liste "hiç haber yok" ile "bu filtrede yok"u ayırır', (
    tester,
  ) async {
    await openNews(tester, routes: {'/v1/news': newsRoute(list: const [])});

    expect(find.text('Henüz haber yok'), findsOneWidget);

    await tester.tap(find.text('Spor'));
    await tester.pumpAndSettle();

    expect(find.text('Bu filtrede haber yok'), findsOneWidget);
    expect(find.text('Filtreleri temizle'), findsOneWidget);
  });

  testWidgets('"Filtreleri temizle" arama kutusunu da temizler', (tester) async {
    await openNews(tester, routes: {'/v1/news': newsRoute(list: const [])});

    await tester.enterText(find.byType(TextField).first, 'bulunmayan');
    await tester.pumpAndSettle();
    await tester.tap(find.text('Filtreleri temizle'));
    await tester.pumpAndSettle();

    expect(find.text('bulunmayan'), findsNothing);
    expect(find.text('Henüz haber yok'), findsOneWidget);
  });

  testWidgets('liste hatasında traceId ile hata ekranı çıkar', (tester) async {
    await openNews(
      tester,
      routes: {
        // ⚠️ Kalıcı hata (404) kullanılıyor: `apiRetry` 5xx'i geçici sayıp
        // yeniden dener ve test "pending timer" ile patlar.
        '/v1/news': (_) async => jsonResponse(
          errorEnvelope('NOT_FOUND', 'Haberler yüklenemedi.'),
          statusCode: 404,
        ),
      },
    );

    expect(find.text('Haberler yüklenemedi.'), findsOneWidget);
    expect(find.text('Tekrar dene'), findsOneWidget);
  });

  testWidgets('kategoriler alınamazsa şerit hiç çizilmez (liste durur)', (
    tester,
  ) async {
    // 11.6'dan beri geçerli kural: çalışmayan filtre gösterilmez.
    await openNews(
      tester,
      routes: {
        '/v1/news/categories': (_) async =>
            jsonResponse(errorEnvelope('NOT_FOUND', 'yok'), statusCode: 404),
      },
    );

    expect(find.text('Tümü'), findsNothing);
    expect(
      find.text('Kadirli’de yaz akşamları sinema keyfiyle renkleniyor'),
      findsOneWidget,
    );
  });

  group('manşet şeridi (plan dışı ek)', () {
    testWidgets('öne çıkan haber ayrı şeritte gösterilir', (tester) async {
      await openNews(
        tester,
        routes: {
          '/v1/news': newsRoute(
            list: [article()],
            featured: [
              article(id: 'f1', title: 'Manşet haberi', featured: true),
            ],
          ),
        },
      );

      expect(find.text('Öne çıkanlar'), findsOneWidget);
      expect(find.text('Manşet haberi'), findsOneWidget);
    });

    testWidgets('süzgeç seçiliyken manşet gizlenir', (tester) async {
      // Kullanıcı "Spor" seçmişken başka kategoriden manşet basmak, süzgecin
      // çalışmadığı izlenimi verirdi.
      await openNews(
        tester,
        routes: {
          '/v1/news': newsRoute(
            list: [article()],
            featured: [
              article(id: 'f1', title: 'Manşet haberi', featured: true),
            ],
          ),
        },
      );

      await tester.tap(find.text('Spor'));
      await tester.pumpAndSettle();

      expect(find.text('Öne çıkanlar'), findsNothing);
      expect(find.text('Manşet haberi'), findsNothing);
    });

    testWidgets('manşet alınamazsa liste yine çalışır', (tester) async {
      await openNews(
        tester,
        routes: {
          '/v1/news': (options) async =>
              options.queryParameters['featured'] == true
              ? jsonResponse(
                  errorEnvelope('NOT_FOUND', 'yok'),
                  statusCode: 404,
                )
              : jsonResponse(pagedBody([article()])),
        },
      );

      expect(find.text('Öne çıkanlar'), findsNothing);
      expect(
        find.text('Kadirli’de yaz akşamları sinema keyfiyle renkleniyor'),
        findsOneWidget,
      );
    });
  });

  group('kaydedilenler (plan dışı ek)', () {
    testWidgets('boş listede yönlendirici bir metin çıkar', (tester) async {
      await openNews(tester, location: '/kaydedilen-haberler');

      expect(find.text('Kaydedilen haber yok'), findsOneWidget);
    });

    testWidgets('kaydedilen haber ağa çıkmadan listelenir', (tester) async {
      // 🔑 Ekranın değeri burada: kayıtlar cihazdaki anlık görüntülerden
      // çiziliyor, **hiçbir istek atılmıyor** (çevrimdışı okunabilir).
      final adapter = await openNews(
        tester,
        prefs: {
          ...guest,
          'news.saved': [
            '{"id":"n9","title":"Kaydedilmiş haber",'
                '"excerpt":"Özet","readingMinutes":4,'
                '"sourceUrl":"https://www.silagazetesi.com.tr/x/"}',
          ],
        },
        location: '/kaydedilen-haberler',
      );

      expect(find.text('Kaydedilmiş haber'), findsOneWidget);
      expect(find.text('4 dk okuma'), findsOneWidget);
      expect(
        find.text('Kaydedilen haberler yalnız bu cihazda saklanır.'),
        findsOneWidget,
      );
      expect(adapter.countOf('/v1/news'), 0);
    });

    testWidgets('bozuk bir kayıt bütün listeyi düşürmez', (tester) async {
      await openNews(
        tester,
        prefs: {
          ...guest,
          'news.saved': [
            'bu JSON değil',
            '{"id":"n9","title":"Sağlam kayıt"}',
          ],
        },
        location: '/kaydedilen-haberler',
      );

      expect(tester.takeException(), isNull);
      expect(find.text('Sağlam kayıt'), findsOneWidget);
    });
  });
}
