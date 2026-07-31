import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/router/app_router.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';
import '../../helpers/profile_fixtures.dart';

/// İlan verme / düzenleme formu (11.9): kategori adımı · doğrulama ·
/// kategoriye özel dinamik alanlar · gönderim gövdesi · taslak · düzenleme.
void main() {
  const rootCategories = [
    {
      'id': 'cat-araclar',
      'name': 'Araçlar',
      'slug': 'araclar',
      'parentId': null,
      'icon': null,
      'displayOrder': 0,
      'subCategoryCount': 1,
    },
    {
      'id': 'cat-spor',
      'name': 'Spor',
      'slug': 'spor',
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

  /// Zorunlu bir `Select` + opsiyonel `Number` alanı.
  const properties = [
    {
      'id': 'prop-yakit',
      'propertyName': 'Yakıt',
      'propertyType': 'Select',
      'isRequired': true,
      'defaultValue': null,
      'displayOrder': 0,
      'options': [
        {'id': 'o1', 'optionValue': 'Benzin', 'displayOrder': 0},
        {'id': 'o2', 'optionValue': 'Dizel', 'displayOrder': 1},
      ],
    },
    {
      'id': 'prop-km',
      'propertyName': 'Kilometre',
      'propertyType': 'Number',
      'isRequired': false,
      'defaultValue': null,
      'displayOrder': 1,
      'options': <Map<String, dynamic>>[],
    },
  ];

  Map<String, dynamic> paged(List<Map<String, dynamic>> items) =>
      successEnvelope({
        'items': items,
        'totalCount': items.length,
        'pageSize': 20,
        'currentPage': 1,
        'totalPages': items.isEmpty ? 0 : 1,
      });

  Future<FakeHttpAdapter> openForm(
    WidgetTester tester, {
    String location = '/ilan-ver',
    Map<String, Object> prefs = const {},
    Map<String, Future<ResponseBody> Function(RequestOptions)> extraRoutes =
        const {},
    List<Map<String, dynamic>>? categoryProperties,
  }) async {
    // ⚠️ Form uzun ve `ListView` **tembel**: ekran dışındaki alan hiç
    // kurulmadığı için `find` bulamıyor. Testte yüzey yükseltiliyor
    // (kaydırma jimnastiği yerine — 11.7'de öğrenilen tuzağın devamı).
    tester.view.physicalSize = const Size(1080, 4200);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/users/me': (_) async => jsonResponse(successEnvelope(profileBody())),
      '/v1/users/me/favorites': (_) async => jsonResponse(paged(const [])),
      '/v1/users/me/ads': (_) async => jsonResponse(paged(const [])),
      '/v1/ads/categories': (options) async => jsonResponse(
        successEnvelope(
          options.queryParameters['parentId'] == null
              ? rootCategories
              : subCategories,
        ),
      ),
      '/v1/ads/categories/cat-spor/properties': (_) async =>
          jsonResponse(successEnvelope(categoryProperties ?? const [])),
      '/v1/ads/categories/cat-otomobil/properties': (_) async =>
          jsonResponse(successEnvelope(categoryProperties ?? properties)),
      '/v1/ads': (_) async =>
          jsonResponse(successEnvelope('new-ad-id'), statusCode: 201),
      ...extraRoutes,
    });

    final container = await pumpApp(
      tester,
      prefs: prefs,
      tokenStore: InMemoryTokenStore(accessToken: 'A', refreshToken: 'R'),
      adapter: adapter,
    );
    container.read(routerProvider).go(location);
    await tester.pumpAndSettle();
    return adapter;
  }

  Future<void> tapText(WidgetTester tester, String text) async {
    await tester.ensureVisible(find.text(text).first);
    await tester.pumpAndSettle();
    await tester.tap(find.text(text).first);
    await tester.pumpAndSettle();
  }

  /// Etiketiyle bulunan alanın controller metni.
  String fieldText(WidgetTester tester, String label) => tester
      .widget<TextField>(
        find
            .descendant(
              of: find
                  .ancestor(of: find.text(label), matching: find.byType(Column))
                  .first,
              matching: find.byType(TextField),
            )
            .first,
      )
      .controller!
      .text;

  /// Etiketiyle bir `AppTextField`'a yazar (etiket + alan aynı Column'da).
  Future<void> enterField(
    WidgetTester tester,
    String label,
    String value,
  ) async {
    final field = find.descendant(
      of: find
          .ancestor(of: find.text(label), matching: find.byType(Column))
          .first,
      matching: find.byType(TextField),
    );
    await tester.ensureVisible(field.first);
    await tester.enterText(field.first, value);
    await tester.pumpAndSettle();
  }

  group('kategori adımı', () {
    testWidgets('kök kategoriler listelenir, alt kategorisi olana inilir', (
      tester,
    ) async {
      await openForm(tester);

      expect(find.text('İlanınız hangi kategoride?'), findsOneWidget);
      expect(find.text('Araçlar'), findsOneWidget);

      await tapText(tester, 'Araçlar');

      expect(find.text('Araçlar içinde bir alt kategori seçin'), findsOneWidget);
      expect(find.text('Otomobil'), findsOneWidget);
      // Kök kategoriye doğrudan ilan verme yolu da açık.
      expect(find.text('Araçlar (genel)'), findsOneWidget);
    });

    testWidgets('alt kategori seçilince bilgiler adımına geçilir', (
      tester,
    ) async {
      await openForm(tester);
      await tapText(tester, 'Araçlar');
      await tapText(tester, 'Otomobil');

      expect(find.text('Başlık'), findsOneWidget);
      expect(find.text('Otomobil'), findsWidgets, reason: 'kategori özeti');
    });

    testWidgets('alt kategorisi olmayan kök doğrudan seçilir', (tester) async {
      await openForm(tester);
      await tapText(tester, 'Spor');

      expect(find.text('Başlık'), findsOneWidget);
    });
  });

  group('doğrulama (sunucu kurallarının aynası)', () {
    Future<FakeHttpAdapter> openDetails(WidgetTester tester) async {
      final adapter = await openForm(tester);
      await tapText(tester, 'Spor');
      return adapter;
    }

    testWidgets('kısa başlık uca gitmeden alan altında uyarır', (tester) async {
      final adapter = await openDetails(tester);

      await enterField(tester, 'Başlık', 'ab');
      await enterField(tester, 'Açıklama', 'Temiz ürün.');
      await tapText(tester, 'Devam');

      expect(find.text('Başlık en az 3 karakter olmalı.'), findsOneWidget);
      expect(adapter.countOf('/v1/ads'), 0);
    });

    testWidgets('boş açıklama zorunlu uyarısı verir', (tester) async {
      await openDetails(tester);

      await enterField(tester, 'Başlık', 'Bisiklet');
      await tapText(tester, 'Devam');

      expect(find.text('Açıklama zorunludur.'), findsOneWidget);
    });

    testWidgets('geçersiz telefon uca gitmez', (tester) async {
      final adapter = await openDetails(tester);

      await enterField(tester, 'Başlık', 'Bisiklet');
      await enterField(tester, 'Açıklama', 'Temiz.');
      await enterField(tester, 'İletişim telefonu', '212 111 00 01');
      await tapText(tester, 'Devam');

      expect(
        find.text('Geçerli bir cep telefonu girin (5xx ile başlayan 10 hane).'),
        findsOneWidget,
      );
      expect(adapter.countOf('/v1/ads'), 0);
    });

    testWidgets('telefon profilden ön doldurulur', (tester) async {
      await openDetails(tester);

      // profileBody() → +905321110001. (Aynı metin ipucu olarak da yazıyor,
      // bu yüzden `find.text` değil doğrudan controller okunuyor.)
      expect(fieldText(tester, 'İletişim telefonu'), '532 111 00 01');
      expect(fieldText(tester, 'İlan sahibi'), 'ahmetk');
    });
  });

  group('kategoriye özel alanlar', () {
    testWidgets('zorunlu select alanı boşken devam edilemez', (tester) async {
      final adapter = await openForm(tester);
      await tapText(tester, 'Araçlar');
      await tapText(tester, 'Otomobil');

      await enterField(tester, 'Başlık', 'Fiat Egea');
      await enterField(tester, 'Açıklama', 'Temiz.');
      await tapText(tester, 'Devam');

      expect(find.text('Yakıt'), findsWidgets);
      expect(find.text('Bu alan zorunludur.'), findsOneWidget);
      expect(adapter.countOf('/v1/ads'), 0);
    });

    testWidgets('seçilen değerler gönderim gövdesine metin olarak girer', (
      tester,
    ) async {
      final adapter = await openForm(tester);
      await tapText(tester, 'Araçlar');
      await tapText(tester, 'Otomobil');

      await enterField(tester, 'Başlık', 'Fiat Egea');
      await enterField(tester, 'Açıklama', 'Temiz, bakımlı.');
      await enterField(tester, 'Fiyat', '750.000');
      await enterField(tester, 'Kilometre', '120000');

      // Select alanı dropdown.
      await tester.ensureVisible(find.byType(DropdownButtonFormField<String>));
      await tester.tap(find.byType(DropdownButtonFormField<String>));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Dizel').last);
      await tester.pumpAndSettle();

      await tapText(tester, 'Devam');
      await tapText(tester, 'Yayına gönder');

      final body = Map<String, dynamic>.from(
        adapter.lastOf('/v1/ads')!.data as Map,
      );
      expect(body['categoryId'], 'cat-otomobil');
      expect(body['title'], 'Fiat Egea');
      expect(body['price'], 750000);
      expect(body['contactPhone'], '+905321110001');
      expect(body['propertyValues'], {
        'prop-yakit': 'Dizel',
        'prop-km': '120000',
      });
      expect(body['imageFileIds'], isEmpty);
    });
  });

  group('gönderim', () {
    Future<FakeHttpAdapter> fillAndSubmit(
      WidgetTester tester, {
      Map<String, Future<ResponseBody> Function(RequestOptions)> extraRoutes =
          const {},
    }) async {
      final adapter = await openForm(tester, extraRoutes: extraRoutes);
      await tapText(tester, 'Spor');
      await enterField(tester, 'Başlık', 'Bisiklet');
      await enterField(tester, 'Açıklama', 'Az kullanılmış.');
      await tapText(tester, 'Devam');
      await tapText(tester, 'Yayına gönder');
      return adapter;
    }

    testWidgets('başarılı gönderim onay mesajı gösterir', (tester) async {
      final adapter = await fillAndSubmit(tester);

      expect(adapter.countOf('/v1/ads'), 1);
      expect(find.textContaining('onaya gönderildi'), findsOneWidget);
    });

    testWidgets('fiyatsız ilan gövdede null gider', (tester) async {
      final adapter = await fillAndSubmit(tester);

      final body = Map<String, dynamic>.from(
        adapter.lastOf('/v1/ads')!.data as Map,
      );
      expect(body['price'], isNull);
    });

    testWidgets('sunucu doğrulama hatası ilgili alanın altına düşer', (
      tester,
    ) async {
      await fillAndSubmit(
        tester,
        extraRoutes: {
          '/v1/ads': (_) async => jsonResponse(
            errorEnvelope('VALIDATION_ERROR', 'Başlık 3-200 karakter olmalıdır.'),
            statusCode: 400,
          ),
        },
      );

      // Form 2. adıma geri döner ve mesaj başlığın altına yazılır.
      expect(find.text('Başlık 3-200 karakter olmalıdır.'), findsOneWidget);
      expect(find.text('Başlık'), findsOneWidget);
    });

    testWidgets('özet adımında girilen bilgiler tekrar gösterilir', (
      tester,
    ) async {
      await openForm(tester);
      await tapText(tester, 'Spor');
      await enterField(tester, 'Başlık', 'Bisiklet');
      await enterField(tester, 'Açıklama', 'Az kullanılmış.');
      await enterField(tester, 'Fiyat', '4500');
      await tapText(tester, 'Devam');

      expect(find.text('Özet'), findsOneWidget);
      expect(find.text('4.500 ₺'), findsOneWidget);
      expect(find.text('+90 532 111 00 01'), findsOneWidget);
      expect(find.text('0/10'), findsOneWidget, reason: 'fotoğraf sayacı');
    });
  });

  group('düzenleme', () {
    Map<String, dynamic> adDetail() => successEnvelope({
      'id': 'ad-1',
      'title': 'Fiat Egea',
      'description': 'Temiz.',
      'price': 750000.00,
      'status': 'approved',
      'categoryId': 'cat-otomobil',
      'categoryName': 'Otomobil',
      'userId': 'u1',
      'sellerName': 'Ahmet',
      'contactPhone': '+905321110001',
      'viewCount': 41,
      'createdAt': '2026-07-01T09:00:00.0000000Z',
      'expiresAt': '2026-08-31T09:00:00.0000000Z',
      'images': const <Map<String, dynamic>>[],
      'properties': const [
        {
          'propertyId': 'prop-yakit',
          'propertyName': 'Yakıt',
          'propertyType': 'Select',
          'value': 'Dizel',
        },
      ],
    });

    testWidgets('mevcut ilan forma dolu gelir ve kategori kilitlidir', (
      tester,
    ) async {
      await openForm(
        tester,
        location: '/ilan-duzenle/ad-1',
        extraRoutes: {
          '/v1/ads/ad-1': (_) async => jsonResponse(adDetail()),
        },
      );

      expect(find.text('İlanı düzenle'), findsOneWidget);
      expect(fieldText(tester, 'Başlık'), 'Fiat Egea');
      expect(fieldText(tester, 'Fiyat'), '750.000');
      expect(fieldText(tester, 'İlan sahibi'), 'Ahmet');
      expect(
        find.text('İlanın kategorisi sonradan değiştirilemez.'),
        findsOneWidget,
      );
      // Kategori adımı atlandığı için "Geri" butonu 1. adımda yok.
      expect(find.text('Geri'), findsNothing);
    });

    testWidgets('yeniden onaya düşeceği önceden söylenir', (tester) async {
      await openForm(
        tester,
        location: '/ilan-duzenle/ad-1',
        extraRoutes: {'/v1/ads/ad-1': (_) async => jsonResponse(adDetail())},
      );

      expect(find.textContaining('yeniden yönetici onayına düşer'), findsOneWidget);
    });

    testWidgets('güncelleme PUT ile gider ve mevcut özellikler korunur', (
      tester,
    ) async {
      final adapter = await openForm(
        tester,
        location: '/ilan-duzenle/ad-1',
        extraRoutes: {
          '/v1/ads/ad-1': (options) async => options.method == 'PUT'
              ? jsonResponse(successEnvelope(true))
              : jsonResponse(adDetail()),
        },
      );

      await enterField(tester, 'Başlık', 'Fiat Egea (fiyat düştü)');
      await tapText(tester, 'Devam');
      await tapText(tester, 'Güncelle ve onaya gönder');

      final request = adapter.lastOf('/v1/ads/ad-1');
      expect(request?.method, 'PUT');
      final body = Map<String, dynamic>.from(request!.data as Map);
      expect(body['title'], 'Fiat Egea (fiyat düştü)');
      expect(body['propertyValues'], {'prop-yakit': 'Dizel'});
      expect(body['removeImageIds'], isEmpty);
    });
  });

  group('taslak', () {
    testWidgets('yarım kalan taslak sorularak geri yüklenir', (tester) async {
      await openForm(
        tester,
        prefs: {
          'ads.draft':
              '{"categoryId":"cat-spor","rootCategoryId":"cat-spor",'
              '"categoryName":"Spor","title":"Yarım kalan bisiklet",'
              '"description":"Yazmıştım","price":"1500","sellerName":"",'
              '"contactPhone":"","propertyValues":{},'
              '"savedAt":"${DateTime.now().toUtc().toIso8601String()}"}',
        },
      );

      expect(find.text('Yarım kalan ilanınız var'), findsOneWidget);
      await tapText(tester, 'Geri yükle');

      expect(find.text('Yarım kalan bisiklet'), findsOneWidget);
      expect(find.text('Yazmıştım'), findsOneWidget);
    });

    testWidgets('"Yeni ilan" denirse taslak silinir ve form boş açılır', (
      tester,
    ) async {
      await openForm(
        tester,
        prefs: {
          'ads.draft':
              '{"categoryId":"cat-spor","title":"Eski taslak",'
              '"description":"x","price":"","sellerName":"",'
              '"contactPhone":"","propertyValues":{},'
              '"savedAt":"${DateTime.now().toUtc().toIso8601String()}"}',
        },
      );

      await tapText(tester, 'Yeni ilan');

      expect(find.text('Eski taslak'), findsNothing);
      expect(find.text('İlanınız hangi kategoride?'), findsOneWidget);
    });

    testWidgets('bir haftadan eski taslak hiç teklif edilmez', (tester) async {
      await openForm(
        tester,
        prefs: {
          'ads.draft':
              '{"categoryId":"cat-spor","title":"Çok eski",'
              '"description":"x","price":"","sellerName":"",'
              '"contactPhone":"","propertyValues":{},'
              '"savedAt":"${DateTime.now().toUtc().subtract(const Duration(days: 30)).toIso8601String()}"}',
        },
      );

      expect(find.text('Yarım kalan ilanınız var'), findsNothing);
    });
  });

  testWidgets('misafir "İlan ver" rotasına giremez (giriş gerekli)', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final container = await pumpApp(
      tester,
      prefs: const {'auth.guestChoice': true},
      adapter: routedAdapter(homeStubs()),
    );
    container.read(routerProvider).go('/ilan-ver');
    await tester.pumpAndSettle();

    expect(find.text('İlanınız hangi kategoride?'), findsNothing);
  });
}
