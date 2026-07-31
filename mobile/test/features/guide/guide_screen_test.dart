import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/router/app_router.dart';
import 'package:kadirli_app/features/guide/application/guide_providers.dart';
import 'package:kadirli_app/features/guide/data/models/guide_category.dart';
import 'package:kadirli_app/features/guide/data/models/guide_item.dart';
import 'package:kadirli_app/features/guide/presentation/guide_item_detail_screen.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// Şehir Rehberi listesi + kayıt detayı (11.7).
void main() {
  const guest = {'auth.guestChoice': true};

  // ⚠️ Chip şeridi yatay ve tembel: telefon genişliğinde 2-3 chip sığıyor.
  // Testlerde dokunulan kategori `displayOrder` ile başa alınıyor (repository
  // zaten displayOrder'a göre sıralıyor).
  const categories = [
    {
      'id': 'cat-acil',
      'name': 'Acil Numaralar',
      'slug': 'acil-numaralar',
      'parentId': null,
      'icon': null,
      'color': null,
      'displayOrder': 0,
    },
    {
      'id': 'cat-resmi',
      'name': 'Resmi Kurumlar',
      'slug': 'resmi-kurumlar',
      'parentId': null,
      'icon': null,
      'color': null,
      'displayOrder': 1,
    },
  ];

  Map<String, dynamic> item({
    String id = 'g1',
    String name = 'Kadirli Belediyesi',
    String? phone = '0328 718 10 40',
    String categoryId = 'cat-resmi',
    String categoryName = 'Resmi Kurumlar',
  }) => {
    'id': id,
    'categoryId': categoryId,
    'categoryName': categoryName,
    'name': name,
    'phone': phone,
    'address': 'Cumhuriyet Mah. Merkez',
    'email': null,
    'websiteUrl': null,
    'workingHours': '08:00 - 17:00',
    'latitude': 37.3708,
    'longitude': 36.0961,
    'hasLocation': true,
    'description': null,
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

  Future<FakeHttpAdapter> openGuide(
    WidgetTester tester, {
    Map<String, Future<ResponseBody> Function(RequestOptions)> routes = const {},
    String location = '/rehber',
  }) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final adapter = routedAdapter({...homeStubs(), ...routes});
    final container = await pumpApp(tester, prefs: guest, adapter: adapter);
    container.read(routerProvider).go(location);
    await tester.pumpAndSettle();
    return adapter;
  }

  testWidgets('kategoriler ve kayıtlar listelenir', (tester) async {
    await openGuide(
      tester,
      routes: {
        '/v1/guide/categories': (_) async => jsonResponse(pagedBody(
          categories.map((c) => Map<String, dynamic>.from(c)).toList(),
        )),
        '/v1/guide/items': (_) async => jsonResponse(pagedBody([item()])),
      },
    );

    expect(find.text('Tümü'), findsOneWidget);
    expect(find.text('Acil Numaralar'), findsOneWidget);
    expect(find.text('Kadirli Belediyesi'), findsOneWidget);
    expect(find.text('0328 718 10 40'), findsOneWidget);
  });

  testWidgets('kategori chip\'i seçilince uca categoryId gider', (tester) async {
    final adapter = await openGuide(
      tester,
      routes: {
        '/v1/guide/categories': (_) async => jsonResponse(pagedBody(
          categories.map((c) => Map<String, dynamic>.from(c)).toList(),
        )),
        '/v1/guide/items': (_) async => jsonResponse(pagedBody([item()])),
      },
    );

    await tester.tap(find.text('Acil Numaralar'));
    await tester.pumpAndSettle();

    expect(
      adapter.lastOf('/v1/guide/items')?.queryParameters['categoryId'],
      'cat-acil',
    );
  });

  testWidgets('arama uca search parametresi olarak gider', (tester) async {
    final adapter = await openGuide(
      tester,
      routes: {
        '/v1/guide/categories': (_) async => jsonResponse(pagedBody(
          categories.map((c) => Map<String, dynamic>.from(c)).toList(),
        )),
        '/v1/guide/items': (_) async => jsonResponse(pagedBody([item()])),
      },
    );

    await tester.enterText(find.byType(TextField), 'hastane');
    await tester.pumpAndSettle();

    expect(
      adapter.lastOf('/v1/guide/items')?.queryParameters['search'],
      'hastane',
    );
  });

  testWidgets('kategori + arama BİRLİKTE uygulanır', (tester) async {
    final adapter = await openGuide(
      tester,
      routes: {
        '/v1/guide/categories': (_) async => jsonResponse(pagedBody(
          categories.map((c) => Map<String, dynamic>.from(c)).toList(),
        )),
        '/v1/guide/items': (_) async => jsonResponse(pagedBody([item()])),
      },
    );

    await tester.tap(find.text('Acil Numaralar'));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextField), 'itfaiye');
    await tester.pumpAndSettle();

    final query = adapter.lastOf('/v1/guide/items')?.queryParameters;
    expect(query?['categoryId'], 'cat-acil');
    expect(query?['search'], 'itfaiye');
  });

  testWidgets('sonuç yoksa filtreleri temizleme önerilir', (tester) async {
    await openGuide(
      tester,
      routes: {
        '/v1/guide/categories': (_) async => jsonResponse(pagedBody(
          categories.map((c) => Map<String, dynamic>.from(c)).toList(),
        )),
        '/v1/guide/items': (options) async => jsonResponse(
          options.queryParameters.containsKey('search')
              ? pagedBody(const [])
              : pagedBody([item()]),
        ),
      },
    );

    await tester.enterText(find.byType(TextField), 'olmayan');
    await tester.pumpAndSettle();

    expect(find.text('Sonuç bulunamadı'), findsOneWidget);
    await tester.tap(find.text('Filtreleri temizle'));
    await tester.pumpAndSettle();

    expect(find.text('Kadirli Belediyesi'), findsOneWidget);
  });

  testWidgets('kategoriler alınamazsa şerit çizilmez ama liste çalışır', (
    tester,
  ) async {
    await openGuide(
      tester,
      routes: {
        '/v1/guide/categories': (_) async => jsonResponse(
          errorEnvelope('INTERNAL_ERROR', 'Sunucu hatası.'),
          statusCode: 500,
        ),
        '/v1/guide/items': (_) async => jsonResponse(pagedBody([item()])),
      },
    );

    expect(find.text('Tümü'), findsNothing);
    expect(find.text('Kadirli Belediyesi'), findsOneWidget);
  });

  testWidgets('telefonsuz kayıtta listede arama düğmesi çizilmez', (
    tester,
  ) async {
    await openGuide(
      tester,
      routes: {
        '/v1/guide/categories': (_) async => jsonResponse(pagedBody(
          categories.map((c) => Map<String, dynamic>.from(c)).toList(),
        )),
        '/v1/guide/items': (_) async =>
            jsonResponse(pagedBody([item(name: 'Telefonsuz Kurum', phone: null)])),
      },
    );

    expect(find.text('Telefonsuz Kurum'), findsOneWidget);
    expect(
      find.bySemanticsLabel('Telefonsuz Kurum numarasını ara'),
      findsNothing,
    );
  });

  testWidgets('karta dokununca detay açılır', (tester) async {
    await openGuide(
      tester,
      routes: {
        '/v1/guide/categories': (_) async => jsonResponse(pagedBody(
          categories.map((c) => Map<String, dynamic>.from(c)).toList(),
        )),
        '/v1/guide/items': (_) async => jsonResponse(pagedBody([item()])),
        '/v1/guide/items/g1': (_) async => jsonResponse(successEnvelope(item())),
      },
    );

    await tester.tap(find.text('Kadirli Belediyesi'));
    await tester.pumpAndSettle();

    expect(find.text('Rehber'), findsOneWidget, reason: 'detay başlığı');
    expect(find.text('Telefon'), findsOneWidget);
    expect(find.text('Yol tarifi'), findsOneWidget);
  });

  testWidgets('bulunamayan kayıt nazik mesaj gösterir', (tester) async {
    await openGuide(
      tester,
      location: '/rehber/yok',
      routes: {
        '/v1/guide/categories': (_) async => jsonResponse(pagedBody(const [])),
        '/v1/guide/items': (_) async => jsonResponse(pagedBody(const [])),
        '/v1/guide/items/yok': (_) async => jsonResponse(
          errorEnvelope('NOT_FOUND', 'Kayıt bulunamadı.'),
          statusCode: 404,
        ),
      },
    );

    expect(find.text('Kayıt bulunamadı'), findsOneWidget);
    expect(find.text('Tekrar dene'), findsNothing);
  });

  group('model', () {
    test('kategori slug\'ı Material ikonuna eşlenir', () {
      const emergency = GuideCategory(
        id: 'c',
        name: 'Acil Numaralar',
        slug: 'acil-numaralar',
      );
      expect(emergency.materialIcon, Icons.emergency_rounded);
      expect(emergency.isEmergency, isTrue);

      const unknown = GuideCategory(id: 'c', name: 'Bilinmeyen', slug: 'xyz');
      expect(unknown.materialIcon, Icons.label_rounded);
      expect(unknown.isEmergency, isFalse);
    });

    test('hasLocation true ama koordinat null ise harita açılmaz', () {
      const broken = GuideItem(id: 'g', name: 'Kurum', hasLocation: true);
      expect(broken.canOpenMap, isFalse);
    });

    test('filtre eşitliği kategori ve aramayı birlikte kapsar', () {
      expect(
        const GuideFilter(categoryId: 'a', search: 'x'),
        const GuideFilter(categoryId: 'a', search: 'x'),
      );
      expect(
        const GuideFilter(categoryId: 'a'),
        isNot(const GuideFilter(categoryId: 'b')),
      );
      expect(const GuideFilter().isActive, isFalse);
      expect(const GuideFilter(search: ' ').isActive, isFalse);
      expect(const GuideFilter(search: 'x').isActive, isTrue);
    });

    test('paylaşım metni ad, kategori, telefon ve adresi taşır', () {
      final text = guideShareText(
        const GuideItem(
          id: 'g1',
          name: 'Kadirli Devlet Hastanesi',
          categoryName: 'Sağlık',
          phone: '0328 718 10 26',
          address: 'Şehit Sedat Nezih Özok Cad.',
          workingHours: '7/24 Açık',
        ),
      );

      expect(text, contains('Kadirli Devlet Hastanesi'));
      expect(text, contains('Sağlık'));
      expect(text, contains('0328 718 10 26'));
      expect(text, contains('7/24 Açık'));
    });
  });
}
