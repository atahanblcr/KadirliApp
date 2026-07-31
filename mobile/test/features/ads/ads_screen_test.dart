import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/router/app_router.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';
import '../../helpers/profile_fixtures.dart';

/// İlanlar listesi (11.8): arama · kategori (iki katman) · sıralama ·
/// fiyat aralığı · sonsuz kaydırma · favori.
void main() {
  const guest = {'auth.guestChoice': true};

  // Kök kategoriler — chip şeridi yatay ve tembel, testte dokunulan kategori
  // `displayOrder` ile başa alınıyor (11.7'de öğrenilen tuzak).
  const rootCategories = [
    {
      'id': 'cat-araclar',
      'name': 'Araçlar',
      'slug': 'araclar',
      'parentId': null,
      'icon': null,
      'displayOrder': 0,
      'subCategoryCount': 2,
    },
    {
      'id': 'cat-emlak',
      'name': 'Emlak',
      'slug': 'emlak',
      'parentId': null,
      'icon': null,
      'displayOrder': 1,
      'subCategoryCount': 0,
    },
  ];

  const subCategories = [
    {
      'id': 'cat-otomobil',
      'name': 'Otomobil',
      'slug': 'otomobil',
      'parentId': 'cat-araclar',
      'icon': null,
      'displayOrder': 0,
      'subCategoryCount': 0,
    },
  ];

  Map<String, dynamic> ad({
    String id = 'ad-1',
    String title = 'Sahibinden Temiz Fiat Egea',
    num? price = 750000,
    int viewCount = 12,
    List<String> images = const [],
  }) => {
    'id': id,
    'title': title,
    'description': 'Az kullanılmış, bakımlı.',
    'price': price,
    'status': 'approved',
    'contactPhone': '05321110001',
    'viewCount': viewCount,
    'createdAt': DateTime.now()
        .toUtc()
        .subtract(const Duration(hours: 3))
        .toIso8601String(),
    'imageUrls': images,
  };

  Map<String, dynamic> pagedBody(
    List<Map<String, dynamic>> items, {
    int? totalCount,
    int currentPage = 1,
    int totalPages = 1,
  }) => successEnvelope({
    'items': items,
    'totalCount': totalCount ?? items.length,
    'pageSize': 20,
    'currentPage': currentPage,
    'totalPages': totalPages,
  });

  /// Varsayılan rotalar: kategori ağacı + tek ilanlık liste.
  Map<String, Future<ResponseBody> Function(RequestOptions)> defaultRoutes({
    List<Map<String, dynamic>>? ads,
  }) => {
    '/v1/ads/categories': (options) async => jsonResponse(
      successEnvelope(
        options.queryParameters['parentId'] == null
            ? rootCategories
            : subCategories,
      ),
    ),
    '/v1/ads': (_) async => jsonResponse(pagedBody(ads ?? [ad()])),
  };

  Future<FakeHttpAdapter> openAds(
    WidgetTester tester, {
    Map<String, Future<ResponseBody> Function(RequestOptions)>? routes,
    String location = '/ilanlar',
    bool signedIn = false,
  }) async {
    // ⚠️ Varsayılan 800x600 test yüzeyinde şeritler + liste ekrandan taşıp
    // `tap` reddediliyor → gerçek telefon yüzeyi (11.7'de öğrenildi).
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final adapter = routedAdapter({
      ...homeStubs(),
      if (signedIn)
        '/v1/users/me': (_) async =>
            jsonResponse(successEnvelope(profileBody())),
      if (signedIn)
        '/v1/users/me/favorites': (_) async =>
            jsonResponse(pagedBody(const [])),
      ...(routes ?? defaultRoutes()),
    });

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

  /// Yatay chip şeritleri telefon genişliğinde taşıyor: dokunmadan önce
  /// chip'i görünür alana getirmek gerekiyor (yoksa `tap` ekran dışına düşer).
  Future<void> tapChip(WidgetTester tester, String label) async {
    await tester.ensureVisible(find.text(label));
    await tester.pumpAndSettle();
    await tester.tap(find.text(label));
    await tester.pumpAndSettle();
  }

  /// ⚠️ Arama gecikmesi (`Debouncer`) bir `Timer` — `pumpAndSettle` çerçeve
  /// planlanmadığı için beklemez, süreyi **elle** ilerletmek gerekir.
  Future<void> enterSearch(WidgetTester tester, String term) async {
    await tester.enterText(find.byType(TextField).first, term);
    await tester.pump(const Duration(milliseconds: 400));
    await tester.pumpAndSettle();
  }

  testWidgets('ilanlar kartlarda fiyat ve görüntülenmeyle listelenir', (
    tester,
  ) async {
    await openAds(tester);

    expect(find.text('Sahibinden Temiz Fiat Egea'), findsOneWidget);
    expect(find.text('750.000 ₺'), findsOneWidget);
    expect(find.text('12'), findsOneWidget, reason: 'görüntülenme sayısı');
    expect(find.text('Toplam 1 ilan'), findsOneWidget);
  });

  testWidgets('fiyatsız ilanda "0 ₺" değil nötr metin yazar', (tester) async {
    await openAds(
      tester,
      routes: defaultRoutes(ads: [ad(price: null, title: 'Ücretsiz Kedi')]),
    );

    expect(find.text('Fiyat belirtilmemiş'), findsOneWidget);
    expect(find.text('0 ₺'), findsNothing);
  });

  testWidgets('varsayılan sıralama uca newest olarak gider', (tester) async {
    final adapter = await openAds(tester);
    expect(adapter.lastOf('/v1/ads')?.queryParameters['sort'], 'newest');
  });

  testWidgets('sıralama chip\'i uca whitelist değerini gönderir', (
    tester,
  ) async {
    final adapter = await openAds(tester);

    await tapChip(tester, 'Önce ucuz');

    expect(adapter.lastOf('/v1/ads')?.queryParameters['sort'], 'price_asc');
  });

  testWidgets('arama gecikmeyle tek istek atar ve uca search gider', (
    tester,
  ) async {
    final adapter = await openAds(tester);
    final before = adapter.countOf('/v1/ads');

    await tester.enterText(find.byType(TextField).first, 'egea');
    await tester.pump(const Duration(milliseconds: 100));
    expect(
      adapter.countOf('/v1/ads'),
      before,
      reason: 'gecikme dolmadan istek atılmamalı',
    );

    await tester.pump(const Duration(milliseconds: 400));
    await tester.pumpAndSettle();
    expect(adapter.countOf('/v1/ads'), before + 1);
    expect(adapter.lastOf('/v1/ads')?.queryParameters['search'], 'egea');
  });

  testWidgets('kök kategori seçilince alt kategoriler şeritte açılır', (
    tester,
  ) async {
    final adapter = await openAds(tester);

    await tapChip(tester, 'Araçlar');

    expect(
      adapter.lastOf('/v1/ads')?.queryParameters['categoryId'],
      'cat-araclar',
    );
    expect(
      find.text('Otomobil'),
      findsOneWidget,
      reason: 'alt kategori şeridi',
    );
    expect(find.text('Emlak'), findsNothing, reason: 'şerit kökün içine indi');
  });

  testWidgets('alt kategori seçilince filtre alt kategoriye geçer', (
    tester,
  ) async {
    final adapter = await openAds(tester);

    await tapChip(tester, 'Araçlar');
    await tapChip(tester, 'Otomobil');

    expect(
      adapter.lastOf('/v1/ads')?.queryParameters['categoryId'],
      'cat-otomobil',
    );
  });

  testWidgets('alt kategorisi olmayan kökte ikinci katman istenmez', (
    tester,
  ) async {
    final adapter = await openAds(tester);

    await tapChip(tester, 'Emlak');

    expect(
      adapter.lastOf('/v1/ads')?.queryParameters['categoryId'],
      'cat-emlak',
    );
    expect(
      adapter.requests
          .where((r) => r.path == '/v1/ads/categories')
          .where((r) => r.queryParameters['parentId'] != null),
      isEmpty,
      reason: 'subCategoryCount 0 → boşuna alt kategori isteği atılmamalı',
    );
  });

  testWidgets('"Tümü" kategori filtresini kaldırır', (tester) async {
    final adapter = await openAds(tester);

    await tapChip(tester, 'Araçlar');
    await tapChip(tester, 'Tümü');

    expect(
      adapter.lastOf('/v1/ads')?.queryParameters.containsKey('categoryId'),
      isFalse,
    );
  });

  testWidgets('fiyat aralığı uca minPrice/maxPrice olarak gider', (
    tester,
  ) async {
    final adapter = await openAds(tester);

    await tapChip(tester, 'Fiyat');

    await tester.enterText(find.byType(TextField).at(1), '100.000');
    await tester.enterText(find.byType(TextField).at(2), '500.000');
    await tester.tap(find.text('Uygula'));
    await tester.pumpAndSettle();

    final query = adapter.lastOf('/v1/ads')?.queryParameters;
    expect(query?['minPrice'], 100000);
    expect(query?['maxPrice'], 500000);
    expect(find.text('100.000 ₺ – 500.000 ₺'), findsOneWidget);
  });

  testWidgets('fiyat chip\'ine ikinci dokunuş aralığı kaldırır', (
    tester,
  ) async {
    final adapter = await openAds(tester);

    await tapChip(tester, 'Fiyat');
    await tester.enterText(find.byType(TextField).at(2), '5000');
    await tester.tap(find.text('Uygula'));
    await tester.pumpAndSettle();

    await tapChip(tester, '5.000 ₺ ve altı');

    expect(
      adapter.lastOf('/v1/ads')?.queryParameters.containsKey('maxPrice'),
      isFalse,
    );
    expect(find.text('Fiyat'), findsOneWidget);
  });

  testWidgets('sonuç yoksa filtreleri temizleme önerilir', (tester) async {
    await openAds(
      tester,
      routes: {
        ...defaultRoutes(),
        '/v1/ads': (options) async => jsonResponse(
          options.queryParameters.containsKey('search')
              ? pagedBody(const [])
              : pagedBody([ad()]),
        ),
      },
    );

    await enterSearch(tester, 'olmayanurun');

    expect(find.text('Sonuç bulunamadı'), findsOneWidget);
    await tester.tap(find.text('Filtreleri temizle'));
    await tester.pumpAndSettle();

    expect(find.text('Sahibinden Temiz Fiat Egea'), findsOneWidget);
    // 🐛 Canlıda yakalandı: filtre sıfırlanırken arama kutusundaki metin
    // ekranda kalıyordu (liste dolu ama kutuda "olmayanurun" yazıyor).
    expect(
      find.text('olmayanurun'),
      findsNothing,
      reason: 'arama kutusu da temizlenmeli',
    );
  });

  testWidgets('ikinci sayfa hatası okunan ilanları silmez', (tester) async {
    var call = 0;
    final adapter = await openAds(
      tester,
      routes: {
        ...defaultRoutes(),
        '/v1/ads': (options) async {
          call++;
          if (call == 1) {
            return jsonResponse(
              pagedBody(
                [
                  for (var i = 0; i < 20; i++)
                    ad(id: 'ad-$i', title: 'İlan $i'),
                ],
                totalCount: 40,
                totalPages: 2,
              ),
            );
          }
          return jsonResponse(
            errorEnvelope('INTERNAL_ERROR', 'Sunucu hatası.'),
            statusCode: 500,
          );
        },
      },
    );

    await tester.drag(find.byType(ListView).last, const Offset(0, -4000));
    await tester.pumpAndSettle();

    expect(find.text('Devamını yükle'), findsOneWidget);
    expect(adapter.countOf('/v1/ads'), greaterThan(1));
    expect(find.textContaining('İlan '), findsWidgets, reason: 'liste kalmalı');
  });

  testWidgets('kategoriler alınamazsa şerit çizilmez ama liste çalışır', (
    tester,
  ) async {
    await openAds(
      tester,
      routes: {
        '/v1/ads/categories': (_) async => jsonResponse(
          errorEnvelope('INTERNAL_ERROR', 'Sunucu hatası.'),
          statusCode: 500,
        ),
        '/v1/ads': (_) async => jsonResponse(pagedBody([ad()])),
      },
    );

    expect(find.text('Tümü'), findsNothing);
    expect(find.text('Sahibinden Temiz Fiat Egea'), findsOneWidget);
    expect(find.text('En yeni'), findsOneWidget, reason: 'sıralama çalışır');
  });

  testWidgets('misafir favoriye basınca giriş daveti çıkar, istek gitmez', (
    tester,
  ) async {
    final adapter = await openAds(tester);

    await tester.tap(find.byIcon(Icons.favorite_border_rounded));
    await tester.pumpAndSettle();

    expect(find.text('Bunun için giriş gerekiyor'), findsOneWidget);
    expect(adapter.countOf('/v1/ads/ad-1/favorite'), 0);
  });

  testWidgets('oturum açık kullanıcıda kalp iyimser dolar ve uca yazılır', (
    tester,
  ) async {
    final adapter = await openAds(
      tester,
      signedIn: true,
      routes: {
        ...defaultRoutes(),
        '/v1/ads/ad-1/favorite': (_) async =>
            jsonResponse(successEnvelope(true)),
      },
    );

    await tester.tap(find.byIcon(Icons.favorite_border_rounded));
    await tester.pumpAndSettle();

    expect(adapter.countOf('/v1/ads/ad-1/favorite'), 1);
    expect(find.byIcon(Icons.favorite_rounded), findsOneWidget);
    expect(find.text('Favorilere eklendi'), findsOneWidget);
  });

  testWidgets('favori isteği patlarsa kalp geri alınır ve sebep yazılır', (
    tester,
  ) async {
    await openAds(
      tester,
      signedIn: true,
      routes: {
        ...defaultRoutes(),
        '/v1/ads/ad-1/favorite': (_) async => jsonResponse(
          errorEnvelope('NOT_FOUND', 'İlan bulunamadı.'),
          statusCode: 404,
        ),
      },
    );

    await tester.tap(find.byIcon(Icons.favorite_border_rounded));
    await tester.pumpAndSettle();

    expect(find.text('İlan bulunamadı.'), findsOneWidget);
    expect(
      find.byIcon(Icons.favorite_border_rounded),
      findsOneWidget,
      reason: 'iyimser değişiklik geri alınmalı',
    );
  });

  testWidgets('misafirken favori ucuna hiç istek gitmez', (tester) async {
    final adapter = await openAds(tester);
    expect(adapter.countOf('/v1/users/me/favorites'), 0);
  });

  testWidgets('karta dokununca detay açılır ve alt sekme çubuğu kalır', (
    tester,
  ) async {
    await openAds(
      tester,
      routes: {
        ...defaultRoutes(),
        '/v1/ads/ad-1': (_) async => jsonResponse(
          successEnvelope({
            'id': 'ad-1',
            'title': 'Sahibinden Temiz Fiat Egea',
            'description': 'Az kullanılmış.',
            'price': 750000,
            'status': 'approved',
            'categoryId': 'cat-araclar',
            'categoryName': 'Araçlar',
            'userId': 'u1',
            'sellerName': 'Ahmet K.',
            'contactPhone': '05321110001',
            'viewCount': 12,
            'createdAt': '2026-07-30T09:00:00Z',
            'expiresAt': '2026-08-30T09:00:00Z',
            'images': <Object>[],
            'properties': <Object>[],
          }),
        ),
      },
    );

    await tester.tap(find.text('Sahibinden Temiz Fiat Egea'));
    await tester.pumpAndSettle();

    expect(find.text('İlan'), findsOneWidget, reason: 'detay başlığı');
    expect(find.text('Ahmet K.'), findsOneWidget);
    expect(
      find.byType(NavigationBar),
      findsOneWidget,
      reason: 'detay sekmenin içinde açılır → alt çubuk kalır',
    );
  });
}
