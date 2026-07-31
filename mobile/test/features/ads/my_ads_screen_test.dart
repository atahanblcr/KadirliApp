import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/router/app_router.dart';
import 'package:kadirli_app/core/widgets/widgets.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';
import '../../helpers/profile_fixtures.dart';

/// "İlanlarım" (11.9): statü filtresi · red gerekçesi · performans sayaçları ·
/// uzat / sil · süre uyarısı.
void main() {
  Map<String, dynamic> myAd({
    String id = 'ad-1',
    String title = 'Az kullanılmış bisiklet',
    String status = 'approved',
    num? price = 4500,
    String? rejectedReason,
    int extensionCount = 0,
    int maxExtensions = 3,
    Duration expiresIn = const Duration(days: 20),
    int viewCount = 41,
  }) => {
    'id': id,
    'title': title,
    'description': 'Temiz.',
    'price': price,
    'status': status,
    'categoryId': 'cat-1',
    'categoryName': 'Spor',
    'contactPhone': '+905321110001',
    'viewCount': viewCount,
    'phoneClickCount': 3,
    'whatsappClickCount': 2,
    'favoriteCount': 5,
    'extensionCount': extensionCount,
    'maxExtensions': maxExtensions,
    'rejectedReason': rejectedReason,
    'createdAt': '2026-07-01T09:00:00.0000000Z',
    'expiresAt': DateTime.now().toUtc().add(expiresIn).toIso8601String(),
    'imageUrls': const <String>[],
  };

  Map<String, dynamic> paged(
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

  Future<FakeHttpAdapter> openMyAds(
    WidgetTester tester, {
    List<Map<String, dynamic>>? ads,
    Map<String, Future<ResponseBody> Function(RequestOptions)> extraRoutes =
        const {},
  }) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/users/me': (_) async => jsonResponse(successEnvelope(profileBody())),
      '/v1/users/me/favorites': (_) async => jsonResponse(paged(const [])),
      '/v1/users/me/ads': (_) async => jsonResponse(paged(ads ?? [myAd()])),
      ...extraRoutes,
    });

    final container = await pumpApp(
      tester,
      tokenStore: InMemoryTokenStore(accessToken: 'A', refreshToken: 'R'),
      adapter: adapter,
    );
    container.read(routerProvider).go('/profil/ilanlarim');
    await tester.pumpAndSettle();
    return adapter;
  }

  testWidgets('ilanlar durumu, fiyatı ve performans sayaçlarıyla listelenir', (
    tester,
  ) async {
    await openMyAds(tester);

    expect(find.text('Az kullanılmış bisiklet'), findsOneWidget);
    expect(find.text('4.500 ₺'), findsOneWidget);
    expect(find.text('Yayında'), findsWidgets);
    // Sayaçlar: 41 görüntülenme / 3 arama / 2 whatsapp / 5 favori
    expect(find.text('41'), findsOneWidget);
    expect(find.text('5'), findsOneWidget);
    expect(find.text('Toplam 1 ilan'), findsOneWidget);
  });

  testWidgets('reddedilen ilanda gerekçe kırmızı kartta görünür', (
    tester,
  ) async {
    await openMyAds(
      tester,
      ads: [
        myAd(
          status: 'rejected',
          rejectedReason: 'İletişim bilgisi açıklamaya yazılamaz.',
        ),
      ],
    );

    // "Reddedildi" hem filtre chip'inde hem durum rozetinde yazıyor.
    expect(find.text('Reddedildi'), findsNWidgets(2));
    expect(find.text('Yayınlanmama gerekçesi'), findsOneWidget);
    expect(
      find.text('İletişim bilgisi açıklamaya yazılamaz.'),
      findsOneWidget,
    );
  });

  testWidgets('onay bekleyen ilanda ne olacağı yazar', (tester) async {
    await openMyAds(tester, ads: [myAd(status: 'pending')]);

    expect(find.text('Onay bekliyor'), findsWidgets);
    expect(find.textContaining('yönetici onayında'), findsOneWidget);
  });

  testWidgets('statü filtresi uca status parametresi gönderir', (tester) async {
    final adapter = await openMyAds(tester);

    await tester.ensureVisible(find.text('Reddedildi').first);
    await tester.tap(find.text('Reddedildi').first);
    await tester.pumpAndSettle();

    expect(
      adapter.lastOf('/v1/users/me/ads')?.queryParameters['status'],
      'rejected',
    );
  });

  testWidgets('aynı statüye tekrar dokunmak filtreyi kaldırır', (tester) async {
    final adapter = await openMyAds(tester);

    Future<void> tapChip(String label) async {
      await tester.ensureVisible(find.text(label).first);
      await tester.tap(find.text(label).first);
      await tester.pumpAndSettle();
    }

    await tapChip('Onay bekliyor');
    await tapChip('Onay bekliyor');

    expect(
      adapter.lastOf('/v1/users/me/ads')?.queryParameters['status'],
      isNull,
    );
  });

  testWidgets('süresi dolmuş ilanda uyarı çıkar, uzatma açıktır', (
    tester,
  ) async {
    await openMyAds(
      tester,
      ads: [myAd(status: 'expired', expiresIn: const Duration(days: -2))],
    );

    expect(find.textContaining('Yayın süresi'), findsOneWidget);
    expect(find.text('Uzat (3)'), findsOneWidget);
  });

  testWidgets('yayın süresi yaklaşan ilanda kalan gün yazar', (tester) async {
    await openMyAds(tester, ads: [myAd(expiresIn: const Duration(days: 3))]);

    expect(find.textContaining('3 gün kaldı'), findsOneWidget);
  });

  testWidgets('uzatma hakkı bitmiş ilanda buton devre dışı (ölü buton yok)', (
    tester,
  ) async {
    await openMyAds(
      tester,
      ads: [myAd(extensionCount: 3, maxExtensions: 3)],
    );

    // Hak yoksa sayı gösterilmez ve butona basılamaz.
    expect(find.text('Uzat'), findsOneWidget);
    final extend = tester
        .widgetList<AppButton>(find.byType(AppButton))
        .firstWhere((button) => button.label == 'Uzat');
    expect(extend.onPressed, isNull);
  });

  testWidgets('uzatma ucu çağrılır ve yeni bitiş tarihi listeye yazılır', (
    tester,
  ) async {
    final adapter = await openMyAds(
      tester,
      ads: [myAd(status: 'expired', expiresIn: const Duration(days: -1))],
      extraRoutes: {
        '/v1/ads/ad-1/extend': (_) async => jsonResponse(
          successEnvelope({
            'adId': 'ad-1',
            'status': 'approved',
            'expiresAt': DateTime.now()
                .toUtc()
                .add(const Duration(days: 30))
                .toIso8601String(),
            'extensionCount': 1,
            'maxExtensions': 3,
            'remainingExtensions': 2,
          }),
        ),
      },
    );

    await tester.tap(find.text('Uzat (3)'));
    await tester.pumpAndSettle();

    expect(adapter.countOf('/v1/ads/ad-1/extend'), 1);
    expect(find.textContaining('Kalan uzatma hakkı: 2'), findsOneWidget);
    // Statü güncellendi → kart artık "Yayında" ve hak 2'ye düştü.
    expect(find.text('Uzat (2)'), findsOneWidget);
  });

  testWidgets('uzatma hakkı dolduysa sunucu mesajı gösterilir (409)', (
    tester,
  ) async {
    await openMyAds(
      tester,
      extraRoutes: {
        '/v1/ads/ad-1/extend': (_) async => jsonResponse(
          errorEnvelope('CONFLICT', 'Uzatma hakkınız doldu (en fazla 3 uzatma).'),
          statusCode: 409,
        ),
      },
    );

    await tester.tap(find.text('Uzat (3)'));
    await tester.pumpAndSettle();

    expect(
      find.text('Uzatma hakkınız doldu (en fazla 3 uzatma).'),
      findsOneWidget,
    );
  });

  testWidgets('silme onay ister ve onaylanınca kayıt listeden düşer', (
    tester,
  ) async {
    final adapter = await openMyAds(
      tester,
      extraRoutes: {
        '/v1/ads/ad-1': (_) async => jsonResponse(successEnvelope(true)),
      },
    );

    await tester.tap(find.byTooltip('İlanı sil'));
    await tester.pumpAndSettle();
    expect(find.text('İlan silinsin mi?'), findsOneWidget);

    await tester.tap(find.text('Sil'));
    await tester.pumpAndSettle();

    expect(adapter.countOf('/v1/ads/ad-1'), 1);
    expect(find.text('Az kullanılmış bisiklet'), findsNothing);
    expect(find.text('İlan silindi.'), findsOneWidget);
  });

  testWidgets('silmeden vazgeçilirse uca istek gitmez', (tester) async {
    final adapter = await openMyAds(tester);

    await tester.tap(find.byTooltip('İlanı sil'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Vazgeç'));
    await tester.pumpAndSettle();

    expect(adapter.countOf('/v1/ads/ad-1'), 0);
    expect(find.text('Az kullanılmış bisiklet'), findsOneWidget);
  });

  testWidgets('hiç ilan yoksa ilan vermeye davet eder', (tester) async {
    await openMyAds(tester, ads: const []);

    expect(find.text('Henüz ilanınız yok'), findsOneWidget);
    expect(find.text('İlan ver'), findsWidgets);
  });

  testWidgets('filtreliyken boş sonuç filtreyi kaldırmayı önerir', (
    tester,
  ) async {
    var call = 0;
    await openMyAds(
      tester,
      extraRoutes: {
        '/v1/users/me/ads': (options) async {
          call++;
          // İlk çağrı (filtresiz) dolu, statü filtresi seçilince boş.
          return jsonResponse(
            paged(options.queryParameters['status'] == null ? [myAd()] : const []),
          );
        },
      },
    );

    await tester.ensureVisible(find.text('Reddedildi').first);
    await tester.tap(find.text('Reddedildi').first);
    await tester.pumpAndSettle();

    expect(call, greaterThan(1));
    expect(find.text('Reddedildi ilanınız yok'), findsOneWidget);
    expect(find.text('Filtreyi kaldır'), findsOneWidget);
  });

  testWidgets('misafir kullanıcı ilanlarım rotasına giremez (giriş gerekli)', (
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
    container.read(routerProvider).go('/profil/ilanlarim');
    await tester.pumpAndSettle();

    // `protectedPrefixes` → router giriş ekranına yönlendirir.
    expect(find.text('İlanlarım'), findsNothing);
    expect(find.textContaining('Telefon'), findsWidgets);
  });
}
