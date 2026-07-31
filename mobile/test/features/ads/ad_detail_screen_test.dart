import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/router/app_router.dart';
import 'package:kadirli_app/features/ads/data/models/ad_detail.dart';
import 'package:kadirli_app/features/ads/presentation/ad_detail_screen.dart';
import 'package:kadirli_app/features/ads/presentation/widgets/ad_gallery.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';
import '../../helpers/profile_fixtures.dart';

/// İlan detayı (11.8): galeri · özellikler · iletişim sayaçları · favori.
void main() {
  const guest = {'auth.guestChoice': true};

  Map<String, dynamic> detailBody({
    String id = 'ad-1',
    num? price = 750000,
    String phone = '05321110001',
    List<Map<String, dynamic>> images = const [],
    List<Map<String, dynamic>> properties = const [],
  }) => {
    'id': id,
    'title': 'Sahibinden Temiz Fiat Egea',
    'description': 'Az kullanılmış, tam bakımlı.',
    'price': price,
    'status': 'approved',
    'categoryId': 'cat-araclar',
    'categoryName': 'Araçlar',
    'userId': 'u1',
    'sellerName': 'Ahmet K.',
    'contactPhone': phone,
    'viewCount': 12,
    'createdAt': '2026-07-30T09:00:00Z',
    'expiresAt': '2026-08-30T09:00:00Z',
    'images': images,
    'properties': properties,
  };

  Map<String, dynamic> pagedBody(List<Map<String, dynamic>> items) =>
      successEnvelope({
        'items': items,
        'totalCount': items.length,
        'pageSize': 20,
        'currentPage': 1,
        'totalPages': items.isEmpty ? 0 : 1,
      });

  Future<FakeHttpAdapter> openDetail(
    WidgetTester tester, {
    required Map<String, Future<ResponseBody> Function(RequestOptions)> routes,
    String location = '/ilanlar/ad-1',
    bool signedIn = false,
    // Görselli ilanlarda `AppNetworkImage`'ın shimmer skeleton'ı **sonsuz**
    // animasyon → `pumpAndSettle` hiç dönmez; o testlerde sabit sayıda kare
    // pump edilir.
    bool settle = true,
  }) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/ads/categories': (_) async => jsonResponse(successEnvelope([])),
      '/v1/ads': (_) async => jsonResponse(pagedBody(const [])),
      if (signedIn)
        '/v1/users/me': (_) async =>
            jsonResponse(successEnvelope(profileBody())),
      if (signedIn)
        '/v1/users/me/favorites': (_) async =>
            jsonResponse(pagedBody(const [])),
      ...routes,
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
    if (settle) {
      await tester.pumpAndSettle();
    } else {
      for (var i = 0; i < 10; i++) {
        await tester.pump(const Duration(milliseconds: 50));
      }
    }
    return adapter;
  }

  testWidgets('detay başlık, fiyat, açıklama ve satıcıyı gösterir', (
    tester,
  ) async {
    await openDetail(
      tester,
      routes: {
        '/v1/ads/ad-1': (_) async =>
            jsonResponse(successEnvelope(detailBody())),
      },
    );

    expect(find.text('Sahibinden Temiz Fiat Egea'), findsOneWidget);
    expect(find.text('750.000 ₺'), findsOneWidget);
    expect(find.text('Araçlar'), findsOneWidget);
    expect(find.text('Az kullanılmış, tam bakımlı.'), findsOneWidget);
    expect(find.text('Ahmet K.'), findsOneWidget);
    expect(find.text('12 görüntülenme'), findsOneWidget);
  });

  testWidgets('görselsiz ilanda kırık görsel değil nötr yer tutucu çıkar', (
    tester,
  ) async {
    await openDetail(
      tester,
      routes: {
        '/v1/ads/ad-1': (_) async =>
            jsonResponse(successEnvelope(detailBody())),
      },
    );

    expect(find.text('Bu ilanda fotoğraf yok'), findsOneWidget);
  });

  testWidgets('görsele dokununca TAM EKRAN görüntüleyici açılır', (
    tester,
  ) async {
    await openDetail(
      tester,
      settle: false,
      routes: {
        '/v1/ads/ad-1': (_) async => jsonResponse(
          successEnvelope(
            detailBody(
              images: const [
                {
                  'id': 'i1',
                  'fileId': 'f1',
                  'url': '/uploads/1.png',
                  'isCover': true,
                  'displayOrder': 0,
                },
                {
                  'id': 'i2',
                  'fileId': 'f2',
                  'url': '/uploads/2.png',
                  'isCover': false,
                  'displayOrder': 1,
                },
              ],
            ),
          ),
        ),
      },
    );

    expect(find.text('1 / 2'), findsOneWidget, reason: 'galeri sayacı');

    await tester.tap(find.byType(AdGallery));
    for (var i = 0; i < 12; i++) {
      await tester.pump(const Duration(milliseconds: 50));
    }

    expect(find.byType(AdGalleryViewer), findsOneWidget);
    // 🐛 Canlıda (iOS) yakalandı: dal Navigator'ına push edilince alt sekme
    // çubuğu görüntüleyicinin altında görünmeye devam ediyordu.
    expect(
      find.byType(NavigationBar),
      findsNothing,
      reason: 'tam ekran görüntüleyici kabuğun ÜSTÜNE açılmalı',
    );
  });

  testWidgets('kategoriye özel alanlar tipine göre biçimlenir', (tester) async {
    await openDetail(
      tester,
      routes: {
        '/v1/ads/ad-1': (_) async => jsonResponse(
          successEnvelope(
            detailBody(
              properties: const [
                {
                  'propertyId': 'p1',
                  'propertyName': 'Yakıt',
                  'propertyType': 'Select',
                  'value': 'Dizel',
                },
                {
                  'propertyId': 'p2',
                  'propertyName': 'Hasar kaydı',
                  'propertyType': 'Boolean',
                  'value': 'false',
                },
                {
                  'propertyId': 'p3',
                  'propertyName': 'Boş alan',
                  'propertyType': 'Text',
                  'value': '   ',
                },
              ],
            ),
          ),
        ),
      },
    );

    expect(find.text('Özellikler'), findsOneWidget);
    expect(find.text('Dizel'), findsOneWidget);
    expect(find.text('Yok'), findsOneWidget, reason: 'Boolean false → Yok');
    expect(
      find.text('Boş alan'),
      findsNothing,
      reason: 'değeri boş özellik satırı çizilmez',
    );
  });

  testWidgets('Ara düğmesi track-phone ucunu tetikler', (tester) async {
    final adapter = await openDetail(
      tester,
      routes: {
        '/v1/ads/ad-1': (_) async =>
            jsonResponse(successEnvelope(detailBody())),
        '/v1/ads/ad-1/track-phone': (_) async =>
            jsonResponse(successEnvelope(true)),
      },
    );

    await tester.tap(find.text('Ara'));
    await tester.pumpAndSettle();

    expect(adapter.countOf('/v1/ads/ad-1/track-phone'), 1);
  });

  testWidgets('WhatsApp düğmesi track-whatsapp ucunu tetikler', (tester) async {
    final adapter = await openDetail(
      tester,
      routes: {
        '/v1/ads/ad-1': (_) async =>
            jsonResponse(successEnvelope(detailBody())),
        '/v1/ads/ad-1/track-whatsapp': (_) async =>
            jsonResponse(successEnvelope(true)),
      },
    );

    await tester.tap(find.text('WhatsApp'));
    await tester.pumpAndSettle();

    expect(adapter.countOf('/v1/ads/ad-1/track-whatsapp'), 1);
  });

  testWidgets('sayaç ucu patlasa da kullanıcıya hata gösterilmez', (
    tester,
  ) async {
    await openDetail(
      tester,
      routes: {
        '/v1/ads/ad-1': (_) async =>
            jsonResponse(successEnvelope(detailBody())),
        '/v1/ads/ad-1/track-phone': (_) async => jsonResponse(
          errorEnvelope('NOT_FOUND', 'İlan bulunamadı.'),
          statusCode: 404,
        ),
      },
    );

    await tester.tap(find.text('Ara'));
    await tester.pumpAndSettle();

    expect(find.text('İlan bulunamadı.'), findsNothing);
  });

  testWidgets('telefonsuz ilanda iletişim çubuğu hiç çizilmez', (tester) async {
    await openDetail(
      tester,
      routes: {
        '/v1/ads/ad-1': (_) async =>
            jsonResponse(successEnvelope(detailBody(phone: ''))),
      },
    );

    expect(find.text('Ara'), findsNothing);
    expect(find.text('WhatsApp'), findsNothing);
  });

  testWidgets('detayda favori kalbi anonimken giriş daveti açar', (
    tester,
  ) async {
    final adapter = await openDetail(
      tester,
      routes: {
        '/v1/ads/ad-1': (_) async =>
            jsonResponse(successEnvelope(detailBody())),
      },
    );

    await tester.tap(find.byIcon(Icons.favorite_border_rounded));
    await tester.pumpAndSettle();

    expect(find.text('Bunun için giriş gerekiyor'), findsOneWidget);
    expect(adapter.countOf('/v1/ads/ad-1/favorite'), 0);
  });

  testWidgets('favorideki ilan detayda dolu kalple açılır', (tester) async {
    await openDetail(
      tester,
      signedIn: true,
      routes: {
        '/v1/ads/ad-1': (_) async =>
            jsonResponse(successEnvelope(detailBody())),
        '/v1/users/me/favorites': (_) async => jsonResponse(
          pagedBody([
            {
              'adId': 'ad-1',
              'title': 'Sahibinden Temiz Fiat Egea',
              'price': 750000,
              'status': 'approved',
              'isAvailable': true,
              'viewCount': 12,
              'favoritedAt': '2026-07-30T10:00:00Z',
              'imageUrls': <String>[],
            },
          ]),
        ),
      },
    );

    expect(find.byIcon(Icons.favorite_rounded), findsOneWidget);
  });

  testWidgets('bulunamayan ilan nazik mesaj gösterir, "tekrar dene" çıkmaz', (
    tester,
  ) async {
    await openDetail(
      tester,
      location: '/ilanlar/yok',
      routes: {
        '/v1/ads/yok': (_) async => jsonResponse(
          errorEnvelope('NOT_FOUND', 'İlan bulunamadı.'),
          statusCode: 404,
        ),
      },
    );

    expect(find.text('İlan bulunamadı'), findsOneWidget);
    expect(find.text('Tekrar dene'), findsNothing);
  });

  testWidgets('sunucu hatasında tekrar dene sunulur', (tester) async {
    await openDetail(
      tester,
      routes: {
        '/v1/ads/ad-1': (_) async => jsonResponse(
          errorEnvelope('INTERNAL_ERROR', 'Sunucu hatası.'),
          statusCode: 500,
        ),
      },
    );

    expect(find.text('Tekrar dene'), findsOneWidget);
  });

  group('metinler', () {
    AdDetail sample({num? price = 750000, String phone = '05321110001'}) =>
        AdDetail(
          id: 'ad-1',
          title: 'Sahibinden Temiz Fiat Egea',
          price: price?.toDouble(),
          categoryId: 'c1',
          categoryName: 'Araçlar',
          contactPhone: phone,
          createdAt: DateTime.utc(2026, 7, 30),
          expiresAt: DateTime.utc(2026, 8, 30),
        );

    test('paylaşım metni başlık, fiyat, kategori ve telefonu taşır', () {
      final text = adShareText(sample());
      expect(text, contains('Sahibinden Temiz Fiat Egea'));
      expect(text, contains('750.000 ₺'));
      expect(text, contains('Araçlar'));
      expect(text, contains('05321110001'));
    });

    test('fiyatsız ilanın paylaşımında "0 ₺" yazmaz', () {
      expect(adShareText(sample(price: null)), contains('Fiyat belirtilmemiş'));
    });

    test('WhatsApp mesajı hangi ilan olduğunu söyler', () {
      expect(
        whatsappMessage(sample()),
        contains('"Sahibinden Temiz Fiat Egea" ilanınız'),
      );
    });
  });
}
