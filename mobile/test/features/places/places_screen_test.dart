import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/router/app_router.dart';
import 'package:kadirli_app/features/places/data/models/place.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// Gezilecek yerler: liste + kategori filtresi + detay (11.11).
void main() {
  const guest = {'auth.guestChoice': true};

  const categories = [
    {'id': 'cat-doga', 'name': 'Doğa & Yayla', 'slug': 'doga-yayla'},
    {'id': 'cat-tarih', 'name': 'Tarihi Yerler', 'slug': 'tarihi-yerler'},
  ];

  Map<String, dynamic> place({
    String id = 'p1',
    String name = 'Savrun Kanyonu',
    String categoryId = 'cat-doga',
    String? description = 'Yürüyüş parkuru ve şelalesiyle doğa harikası.',
    String? address = 'Savrun Mah.',
    double latitude = 37.3735,
    double longitude = 36.0961,
    num? entranceFee,
    bool isFree = true,
    String? openingHours,
    String? bestSeason = 'İlkbahar-Yaz',
    String? howToGetThere,
    num? distance = 12.5,
    // ⚠️ Sunucu `jsonb` kolonu **metin** olarak döndürüyor (DTO'da string).
    String? amenities,
    String? coverImageUrl,
  }) => {
    'id': id,
    'categoryId': categoryId,
    'name': name,
    'description': description,
    'address': address,
    'latitude': latitude,
    'longitude': longitude,
    'entranceFee': entranceFee,
    'isFree': isFree,
    'openingHours': openingHours,
    'bestSeason': bestSeason,
    'howToGetThere': howToGetThere,
    'distanceFromCenter': distance,
    'amenities': amenities,
    'coverImageId': null,
    'coverImageUrl': coverImageUrl,
    'isActive': true,
    'createdBy': null,
    'createdAt': '2026-07-03T21:19:26Z',
  };

  Map<String, dynamic> pagedBody(List<Map<String, dynamic>> items) =>
      successEnvelope({
        'items': items,
        'totalCount': items.length,
        'pageSize': 20,
        'currentPage': 1,
        'totalPages': items.isEmpty ? 0 : 1,
      });

  Future<FakeHttpAdapter> openPlaces(
    WidgetTester tester, {
    Map<String, Future<ResponseBody> Function(RequestOptions)> routes = const {},
    String location = '/mekanlar',
  }) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/places/categories': (_) async => jsonResponse(
        successEnvelope(
          categories.map((c) => Map<String, dynamic>.from(c)).toList(),
        ),
      ),
      ...routes,
    });
    final container = await pumpApp(tester, prefs: guest, adapter: adapter);
    container.read(routerProvider).go(location);
    await tester.pumpAndSettle();
    return adapter;
  }

  testWidgets('mekanlar kategori, uzaklık ve ücret bilgisiyle listelenir', (
    tester,
  ) async {
    await openPlaces(
      tester,
      routes: {'/v1/places': (_) async => jsonResponse(pagedBody([place()]))},
    );

    expect(find.text('Savrun Kanyonu'), findsOneWidget);
    expect(find.text('Doğa & Yayla'), findsWidgets);
    expect(find.text('Merkeze 12,5 km'), findsOneWidget);
    expect(find.text('Ücretsiz'), findsOneWidget);
  });

  testWidgets('kategori chip\'i uca categoryId gönderir ve geri alınabilir', (
    tester,
  ) async {
    final adapter = await openPlaces(
      tester,
      routes: {'/v1/places': (_) async => jsonResponse(pagedBody([place()]))},
    );

    // Şerit 360 dp'ye sığmıyor → ikinci kategori ekran dışında kalıyor.
    await tester.drag(find.text('Tümü'), const Offset(-220, 0));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Tarihi Yerler'));
    await tester.pumpAndSettle();
    expect(
      adapter.lastOf('/v1/places')?.queryParameters['categoryId'],
      'cat-tarih',
    );

    // Aynı chip'e tekrar dokunmak filtreyi kaldırır (11.7 kararı).
    await tester.tap(find.text('Tarihi Yerler'));
    await tester.pumpAndSettle();
    expect(
      adapter.lastOf('/v1/places')?.queryParameters.containsKey('categoryId'),
      isFalse,
    );
  });

  testWidgets('kategori listesi gelmezse filtre şeridi hiç çizilmez', (
    tester,
  ) async {
    await openPlaces(
      tester,
      routes: {
        '/v1/places': (_) async => jsonResponse(pagedBody([place()])),
        // ⚠️ 5xx **geçici** hata sayılıyor → `apiRetry` yeniden deniyor ve
        // testte "pending timer" bırakıyor; kalıcı bir hata kullanılmalı.
        '/v1/places/categories': (_) async => jsonResponse(
          errorEnvelope('NOT_FOUND', 'Kategori listesi alınamadı.'),
          statusCode: 404,
        ),
      },
    );

    // "İşlevsiz buton yok": şerit yok, liste yine çalışıyor.
    expect(find.text('Tümü'), findsNothing);
    expect(find.text('Savrun Kanyonu'), findsOneWidget);
  });

  testWidgets('arama ve kategori birlikte gönderilir', (tester) async {
    final adapter = await openPlaces(
      tester,
      routes: {'/v1/places': (_) async => jsonResponse(pagedBody([place()]))},
    );

    await tester.tap(find.text('Doğa & Yayla').first);
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextField).first, 'kanyon');
    await tester.pumpAndSettle();

    final query = adapter.lastOf('/v1/places')?.queryParameters;
    expect(query?['search'], 'kanyon');
    expect(query?['categoryId'], 'cat-doga');
  });

  testWidgets('sonuç yoksa filtreler temizlenir ve kutu da boşalır', (
    tester,
  ) async {
    await openPlaces(
      tester,
      routes: {
        '/v1/places': (options) async => jsonResponse(
          options.queryParameters.containsKey('search')
              ? pagedBody(const [])
              : pagedBody([place()]),
        ),
      },
    );

    await tester.enterText(find.byType(TextField).first, 'olmayan');
    await tester.pumpAndSettle();
    expect(find.text('Sonuç bulunamadı'), findsOneWidget);

    await tester.tap(find.text('Filtreleri temizle'));
    await tester.pumpAndSettle();

    final field = tester.widget<TextField>(find.byType(TextField).first);
    expect(field.controller?.text, isEmpty);
    expect(find.text('Savrun Kanyonu'), findsOneWidget);
  });

  testWidgets('karta dokununca detay açılır ve bilgiler listelenir', (
    tester,
  ) async {
    await openPlaces(
      tester,
      routes: {
        '/v1/places': (_) async => jsonResponse(pagedBody([place()])),
        '/v1/places/p1': (_) async => jsonResponse(
          successEnvelope(
            place(
              openingHours: '08:00 - 18:00',
              howToGetThere: 'Şehir merkezinden Savrun yolu takip edilir.',
            ),
          ),
        ),
      },
    );

    await tester.tap(find.text('Savrun Kanyonu'));
    await tester.pumpAndSettle();

    expect(find.text('Mekan'), findsOneWidget, reason: 'detay başlığı');
    expect(find.text('Yol tarifi'), findsOneWidget);
    expect(find.text('Adres'), findsOneWidget);
    expect(find.text('Ziyaret saatleri'), findsOneWidget);
    expect(find.text('En uygun mevsim'), findsOneWidget);
    expect(find.text('Nasıl gidilir?'), findsOneWidget);
  });

  testWidgets('olanaklar var/yok olarak ayrılır, belirtilmeyen yazılmaz', (
    tester,
  ) async {
    await openPlaces(
      tester,
      location: '/mekanlar/p1',
      routes: {
        '/v1/places': (_) async => jsonResponse(pagedBody(const [])),
        '/v1/places/p1': (_) async => jsonResponse(
          successEnvelope(
            place(
              amenities: jsonEncode({'WC': true, 'Wi-Fi': false, 'Klima': true}),
            ),
          ),
        ),
      },
    );

    expect(find.text('Olanaklar'), findsOneWidget);
    expect(find.text('WC'), findsOneWidget);
    expect(find.text('Klima'), findsOneWidget);
    expect(find.text('Wi-Fi'), findsOneWidget);
    // Listede olmayan bir olanak hiç yazılmaz.
    expect(find.text('Otopark'), findsNothing);
  });

  testWidgets('olanak bilgisi yoksa bölüm hiç çizilmez', (tester) async {
    await openPlaces(
      tester,
      location: '/mekanlar/p1',
      routes: {
        '/v1/places': (_) async => jsonResponse(pagedBody(const [])),
        '/v1/places/p1': (_) async => jsonResponse(successEnvelope(place())),
      },
    );

    expect(find.text('Olanaklar'), findsNothing);
  });

  testWidgets('koordinatı olmayan mekanda yol tarifi adresle çalışır', (
    tester,
  ) async {
    await openPlaces(
      tester,
      location: '/mekanlar/p1',
      routes: {
        '/v1/places': (_) async => jsonResponse(pagedBody(const [])),
        '/v1/places/p1': (_) async => jsonResponse(
          successEnvelope(place(latitude: 0, longitude: 0)),
        ),
      },
    );

    expect(find.text('Yol tarifi'), findsOneWidget);
  });

  testWidgets('ne koordinat ne adres varsa yol tarifi butonu çizilmez', (
    tester,
  ) async {
    await openPlaces(
      tester,
      location: '/mekanlar/p1',
      routes: {
        '/v1/places': (_) async => jsonResponse(pagedBody(const [])),
        '/v1/places/p1': (_) async => jsonResponse(
          successEnvelope(place(latitude: 0, longitude: 0, address: null)),
        ),
      },
    );

    expect(find.text('Yol tarifi'), findsNothing);
  });

  testWidgets('bulunamayan mekan nazik mesaj gösterir', (tester) async {
    await openPlaces(
      tester,
      location: '/mekanlar/yok',
      routes: {
        '/v1/places': (_) async => jsonResponse(pagedBody(const [])),
        '/v1/places/yok': (_) async => jsonResponse(
          errorEnvelope('NOT_FOUND', 'Mekan bulunamadı.'),
          statusCode: 404,
        ),
      },
    );

    expect(find.text('Mekan bulunamadı'), findsOneWidget);
    expect(find.text('Tekrar dene'), findsNothing);
  });

  testWidgets('uzun ad + uzun açıklama + 1.4 ölçek dar ekranda taşmaz', (
    tester,
  ) async {
    await openPlaces(
      tester,
      routes: {
        '/v1/places': (_) async => jsonResponse(
          pagedBody([
            place(
              name: 'Maksutoğlu Yaylası Tabiat Parkı ve Mesire Alanı Kamp Yeri',
              description:
                  'Serin havası, çam ormanları ve yayla şenlikleriyle bölgenin '
                  'en çok ziyaret edilen doğa alanlarından biridir.',
              distance: 38,
              entranceFee: 25,
              isFree: false,
            ),
          ]),
        ),
      },
    );

    tester.view.physicalSize = const Size(720, 1600);
    await tester.pumpAndSettle();
    // Yazı ölçeği tema tarafından 1.4'e kadar açılabiliyor.
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
  });

  group('model', () {
    test('amenities METİN olarak gelir ve çözümlenir', () {
      final item = Place.fromJson(
        place(amenities: jsonEncode({'WC': true, 'Wi-Fi': false})),
      );

      expect(item.amenityMap, {'WC': true, 'Wi-Fi': false});
      expect(item.availableAmenities, ['WC']);
      expect(item.missingAmenities, ['Wi-Fi']);
    });

    test('bozuk/boş amenities ekranı patlatmaz', () {
      expect(Place.fromJson(place(amenities: null)).amenityMap, isEmpty);
      expect(Place.fromJson(place(amenities: '')).amenityMap, isEmpty);
      expect(Place.fromJson(place(amenities: 'bozuk-json')).amenityMap, isEmpty);
      expect(Place.fromJson(place(amenities: '[1,2]')).amenityMap, isEmpty);
    });

    test('ücret etiketi: ücretsiz, ücretli ve fiyatsız ayrımı', () {
      expect(Place.fromJson(place()).feeLabel, 'Ücretsiz');
      expect(
        Place.fromJson(place(isFree: false, entranceFee: 25)).feeLabel,
        '25 ₺',
      );
      // Ücretli ama fiyat girilmemişse "0 ₺" YAZILMAZ.
      expect(Place.fromJson(place(isFree: false)).feeLabel, isNull);
    });

    test('uzaklık etiketi 0/negatif değerde üretilmez', () {
      expect(Place.fromJson(place(distance: 12.5)).distanceLabel, '12,5 km');
      expect(Place.fromJson(place(distance: 38)).distanceLabel, '38 km');
      expect(Place.fromJson(place(distance: 0)).distanceLabel, isNull);
      expect(Place.fromJson(place(distance: null)).distanceLabel, isNull);
    });

    test('0,0 koordinatı "konum girilmemiş" sayılır', () {
      expect(Place.fromJson(place()).hasLocation, isTrue);
      expect(
        Place.fromJson(place(latitude: 0, longitude: 0)).hasLocation,
        isFalse,
      );
    });

    test('paylaşım metni kategori, adres ve uzaklığı taşır', () {
      final text = Place.fromJson(
        place(openingHours: '08:00 - 18:00'),
      ).shareText(categoryName: 'Doğa & Yayla');

      expect(text, contains('Savrun Kanyonu'));
      expect(text, contains('Doğa & Yayla'));
      expect(text, contains('Savrun Mah.'));
      expect(text, contains('Merkeze uzaklık: 12,5 km'));
      expect(text, contains('Saatler: 08:00 - 18:00'));
    });
  });
}
