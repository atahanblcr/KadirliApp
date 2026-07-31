import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/theme/app_theme.dart';
import 'package:kadirli_app/features/ads/data/models/ad_summary.dart';
import 'package:kadirli_app/features/ads/presentation/widgets/ad_card.dart';

/// İlan kartı — **taşma dayanıklılığı** ve içerik kuralları.
///
/// **Neden ayrı dosya:** 11.7'de `PharmacyTile`'ın uzun metinle 222 piksel
/// `RenderFlex` taşması verdiği ancak testte görüldü (canlıda kısa metinle fark
/// edilmiyordu). İlan kartı aynı riski taşıyor: başlık kullanıcı yazıyor
/// (200 karakteye kadar), fiyat 7 haneli olabiliyor ve yazı ölçeği 1.4'e
/// çıkabiliyor. Bu testler o kombinasyonu her koşuda deniyor.
void main() {
  AdSummary ad({
    String title = 'Sahibinden Temiz Fiat Egea',
    double? price = 750000,
    int viewCount = 12,
    List<String> images = const [],
  }) => AdSummary(
    id: 'ad-1',
    title: title,
    price: price,
    viewCount: viewCount,
    createdAt: DateTime.now().toUtc().subtract(const Duration(hours: 3)),
    imageUrls: images,
  );

  Future<void> pumpCard(
    WidgetTester tester,
    AdSummary summary, {
    double textScale = 1,
    bool favorite = false,
    VoidCallback? onFavoriteTap,
    Size surface = const Size(1080, 2400),
  }) async {
    tester.view.physicalSize = surface;
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: MediaQuery(
          data: MediaQueryData(textScaler: TextScaler.linear(textScale)),
          child: Scaffold(
            body: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                AdCard(
                  ad: summary,
                  isFavorite: favorite,
                  onFavoriteTap: onFavoriteTap ?? () {},
                  onTap: () {},
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  testWidgets('normal ilan başlık, fiyat, tarih ve görüntülenmeyi gösterir', (
    tester,
  ) async {
    await pumpCard(tester, ad());

    expect(find.text('Sahibinden Temiz Fiat Egea'), findsOneWidget);
    expect(find.text('750.000 ₺'), findsOneWidget);
    expect(find.text('3 saat önce'), findsOneWidget);
    expect(find.text('12'), findsOneWidget);
  });

  testWidgets('görüntülenmesi 0 olan ilanda göz ikonu hiç çizilmez', (
    tester,
  ) async {
    await pumpCard(tester, ad(viewCount: 0));

    expect(find.byIcon(Icons.visibility_outlined), findsNothing);
  });

  testWidgets('çok uzun başlık + 7 haneli fiyat taşmaz', (tester) async {
    await pumpCard(
      tester,
      ad(
        title:
            'Sahibinden Az Kullanılmış Tek Elden Hatasız Boyasız Bakımlı '
            'Servis Kayıtlı 2019 Model Otomobil Takas Olmaz Pazarlık Payı Var',
        price: 1234567.89,
      ),
    );

    expect(tester.takeException(), isNull);
  });

  testWidgets('en büyük yazı ölçeğinde (1.4) taşma olmaz', (tester) async {
    await pumpCard(
      tester,
      ad(title: 'Merkez Konumda 3+1 Kiralık Daire Doğalgazlı', price: 9500000),
      textScale: 1.4,
    );

    expect(tester.takeException(), isNull);
  });

  testWidgets('dar ekranda (küçük telefon) taşma olmaz', (tester) async {
    await pumpCard(
      tester,
      ad(title: 'Az Kullanılmış Çamaşır Makinesi Arçelik 9 kg', price: 1234567),
      textScale: 1.4,
      surface: const Size(720, 1440),
    );

    expect(tester.takeException(), isNull);
  });

  testWidgets('fiyatsız ilanda "0 ₺" değil nötr metin yazar', (tester) async {
    await pumpCard(tester, ad(price: null));

    expect(find.text('Fiyat belirtilmemiş'), findsOneWidget);
    expect(find.text('0 ₺'), findsNothing);
  });

  testWidgets('favori kalbi dolu/boş durumu ikonla ayırt edilir', (
    tester,
  ) async {
    await pumpCard(tester, ad());
    expect(find.byIcon(Icons.favorite_border_rounded), findsOneWidget);

    await pumpCard(tester, ad(), favorite: true);
    expect(find.byIcon(Icons.favorite_rounded), findsOneWidget);
  });

  testWidgets('kalbe dokunmak kartın kendisini açmaz', (tester) async {
    var favoriteTaps = 0;
    var cardTaps = 0;

    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: Scaffold(
          body: AdCard(
            ad: ad(),
            onFavoriteTap: () => favoriteTaps++,
            onTap: () => cardTaps++,
          ),
        ),
      ),
    );

    await tester.tap(find.byIcon(Icons.favorite_border_rounded));
    await tester.pumpAndSettle();

    expect(favoriteTaps, 1);
    expect(cardTaps, 0, reason: 'kalp dokunuşu karta sızmamalı');
  });

  testWidgets('favori geri çağrısı yoksa kalp hiç çizilmez', (tester) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: Scaffold(body: AdCard(ad: ad(), onTap: () {})),
      ),
    );

    expect(find.byIcon(Icons.favorite_border_rounded), findsNothing);
  });
}
