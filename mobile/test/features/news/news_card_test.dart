import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/theme/app_theme.dart';
import 'package:kadirli_app/features/news/data/models/news_article.dart';
import 'package:kadirli_app/features/news/data/models/news_category.dart';
import 'package:kadirli_app/features/news/presentation/widgets/news_card.dart';
import 'package:kadirli_app/features/news/presentation/widgets/news_featured_card.dart';

/// Haber kartları — **taşma dayanıklılığı** ve içerik kuralları (12.14).
///
/// **Neden ayrı dosya:** dar sütunda `Row` içindeki çıplak `Text` bu projede
/// **yedi kez** `RenderFlex` taşması üretti. Haber kartı aynı riski taşıyor:
/// başlık gazeteden geliyor (uzunluğu bizim denetimimizde değil), kategori adı
/// "Bilim ve Teknoloji" kadar uzun olabiliyor ve yazı ölçeği 1.4'e çıkabiliyor.
void main() {
  // Sabit "şimdi" — göreli tarih testleri makinenin saatine bağlı olmamalı.
  final now = DateTime.utc(2026, 8, 12, 12);

  NewsArticle article({
    String title = 'Kadirli’de yaz akşamları sinema keyfiyle renkleniyor',
    String excerpt = 'Kadirli Belediyesi açık hava sineması etkinliklerine devam ediyor.',
    String? imageUrl,
    String categoryName = 'Yerel Haberler',
    int readingMinutes = 3,
  }) => NewsArticle(
    id: 'n1',
    title: title,
    excerpt: excerpt,
    imageUrl: imageUrl,
    sourceUrl: 'https://www.silagazetesi.com.tr/haber/',
    publishedAt: now.subtract(const Duration(hours: 3)),
    modifiedAt: now.subtract(const Duration(hours: 3)),
    readingMinutes: readingMinutes,
    categories: [
      NewsCategory(id: 'c1', name: categoryName, slug: 'slug'),
    ],
  );

  Future<void> pumpCard(
    WidgetTester tester,
    Widget card, {
    double textScale = 1,
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
            body: SingleChildScrollView(child: card),
          ),
        ),
      ),
    );
    // ⚠️ `pumpAndSettle` DEĞİL: görselli senaryoda `CachedNetworkImage`'in
    // yer tutucusu sonsuz shimmer çalıştırıyor ve settle hiç gelmiyor.
    await tester.pump();
  }

  testWidgets('kart başlığı, kategoriyi ve okuma süresini gösterir', (
    tester,
  ) async {
    await pumpCard(tester, NewsCard(article: article(), now: now));

    expect(
      find.text('Kadirli’de yaz akşamları sinema keyfiyle renkleniyor'),
      findsOneWidget,
    );
    expect(find.text('Yerel Haberler'), findsOneWidget);
    expect(find.text('3 dk okuma'), findsOneWidget);
  });

  testWidgets('görsel yoksa kart bozulmadan çizilir', (tester) async {
    // Haberlerin bir kısmında öne çıkan görsel yok; kart o zaman metni tüm
    // genişlikte gösterir, boş bir kutu ayırmaz.
    await pumpCard(tester, NewsCard(article: article(), now: now));

    expect(tester.takeException(), isNull);
  });

  testWidgets('görsel varsa kart yine taşmaz', (tester) async {
    await pumpCard(
      tester,
      NewsCard(article: article(imageUrl: '/uploads/kapak.jpg'), now: now),
    );

    expect(tester.takeException(), isNull);
  });

  testWidgets('başlık ve özet KIRPILIR — kart sınırsız büyümez', (
    tester,
  ) async {
    // 🐛 Taşma testi bu kartta tek başına yetmiyor: `_Meta` metinleri gerçek
    // veride hiçbir zaman satırı taşıracak kadar uzun olmuyor (bozma turunda
    // ölçüldü — `Flexible` kaldırıldığında test **yeşil kaldı**). Kartın gerçek
    // regresyon riski taşma değil **sınırsız büyüme**: `maxLines` düşerse
    // gazeteden gelen uzun bir başlık kartı ekran boyuna çıkarır ve listede
    // tek haber görünür. Kural bu yüzden doğrudan kilitleniyor.
    const title =
        'Osmaniye’de kamyonette 89 kilo 550 gram uyuşturucu madde ele '
        'geçirildi, olayla ilgili bir kişi tutuklandı';
    await pumpCard(tester, NewsCard(article: article(title: title), now: now));

    final titleWidget = tester.widget<Text>(find.text(title));
    expect(titleWidget.maxLines, 3);
    expect(titleWidget.overflow, TextOverflow.ellipsis);

    final excerptWidget = tester.widget<Text>(
      find.textContaining('Kadirli Belediyesi'),
    );
    expect(excerptWidget.maxLines, 2);
    expect(excerptWidget.overflow, TextOverflow.ellipsis);
  });

  testWidgets('uzun başlık + uzun kategori + 1.4 ölçekte taşma yok', (
    tester,
  ) async {
    await pumpCard(
      tester,
      NewsCard(
        article: article(
          title:
              'Osmaniye’de kamyonette 89 kilo 550 gram uyuşturucu madde ele '
              'geçirildi, olayla ilgili bir kişi tutuklandı',
          excerpt:
              'Osmaniye’de polis ekiplerinin Gaziantep Emniyet Müdürlüğü '
              'ekipleriyle düzenlediği ortak çalışmada, durdurulan kamyonette '
              'narkotik köpeği ile arama yapıldı ve çok miktarda uyuşturucu '
              'madde ele geçirildi.',
          imageUrl: '/uploads/kapak.jpg',
          categoryName: 'Bilim ve Teknoloji',
        ),
        now: now,
      ),
      textScale: 1.4,
      // 360 dp — piyasadaki en dar telefon (MOBILE_UX_PLAN §0.6).
      surface: const Size(1080, 3600),
    );

    expect(tester.takeException(), isNull);
  });

  testWidgets('kaydedilmiş haber kartta METİNLE de söylenir', (tester) async {
    // ⚠️ Renk/ikon **tek başına** anlam taşımaz: ekran okuyucu kullanan biri
    // için "kaydedildi" bilgisi yazıya dökülmeli.
    await pumpCard(
      tester,
      NewsCard(article: article(), now: now, isSaved: true),
    );

    expect(find.text('Kaydedildi'), findsOneWidget);
  });

  testWidgets('manşet kartı 1.4 ölçekte ve uzun kategori adıyla taşmaz', (
    tester,
  ) async {
    await pumpCard(
      tester,
      NewsFeaturedCard(
        article: article(
          title:
              'Kadirli Belediyesi mahallelerde açık hava sineması '
              'etkinliklerine devam ediyor',
          categoryName: 'Bilim ve Teknoloji',
          imageUrl: '/uploads/kapak.jpg',
        ),
        width: 280,
        now: now,
      ),
      textScale: 1.4,
    );

    expect(tester.takeException(), isNull);
  });

  testWidgets('manşet kartı görselsiz de çizilir', (tester) async {
    await pumpCard(
      tester,
      NewsFeaturedCard(article: article(), width: 280, now: now),
    );

    expect(tester.takeException(), isNull);
    expect(find.text('Yerel Haberler'), findsOneWidget);
  });
}
