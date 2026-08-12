import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/features/news/data/models/news_article.dart';
import 'package:kadirli_app/features/news/data/models/news_category.dart';

/// Haber modeli: ayrıştırma + kartın/detayın dayandığı türetmeler (12.14).
void main() {
  Map<String, dynamic> json({
    String? contentHtml,
    String? publishedAt = '2026-08-11T14:40:59Z',
    String? modifiedAt = '2026-08-11T14:41:00Z',
    List<Map<String, dynamic>> categories = const [
      {'id': 'c1', 'name': 'Yerel Haberler', 'slug': 'yerel-haberler'},
    ],
  }) => {
    'id': 'n1',
    'title': 'Kadirli’de yaz akşamları sinema keyfiyle renkleniyor',
    'excerpt': 'Kadirli Belediyesi açık hava sineması etkinliklerine devam ediyor.',
    'contentHtml': contentHtml,
    'imageUrl': '/uploads/abc_kapak.jpg',
    'imageWidth': 650,
    'imageHeight': 368,
    'sourceUrl': 'https://www.silagazetesi.com.tr/kadirlide-yaz-aksamlari/',
    'publishedAt': publishedAt,
    'modifiedAt': modifiedAt,
    'readingMinutes': 3,
    'isFeatured': true,
    'categories': categories,
  };

  test('uç gövdesi eksiksiz ayrıştırılır', () {
    final article = NewsArticle.fromJson(json(contentHtml: '<p>Merhaba</p>'));

    expect(article.id, 'n1');
    expect(article.readingMinutes, 3);
    expect(article.isFeatured, isTrue);
    expect(article.imageUrl, '/uploads/abc_kapak.jpg');
    expect(article.categories.single.name, 'Yerel Haberler');
    expect(article.contentHtml, '<p>Merhaba</p>');
  });

  test('listede gövde null gelir — kart bunu bir hata saymaz', () {
    // 🔴 Kontrat: `contentHtml` YALNIZ detayda dolu (API_CONTRACT "Haberler").
    // Liste kartı `excerpt` kullanıyor; gövdenin yokluğu kaydı gizlememeli.
    final article = NewsArticle.fromJson(json());

    expect(article.contentHtml, isNull);
    expect(article.excerpt, isNotEmpty);
  });

  test('eksik alanlar varsayılana düşer (kayıt gizlenmez)', () {
    // Sunucu yarın bir alanı göndermezse ya da mağazadaki eski sürüm yeni bir
    // alan görmezse kayıt yine çizilebilmeli — §7 madde 49'un sınıfı.
    final article = NewsArticle.fromJson({'id': 'n2'});

    expect(article.title, '');
    expect(article.readingMinutes, 1);
    expect(article.isFeatured, isFalse);
    expect(article.categories, isEmpty);
    expect(article.publishedLabel(), isNull);
  });

  test('okuma süresi en az 1 dk gösterilir', () {
    final article = NewsArticle.fromJson({'id': 'n3', 'readingMinutes': 0});

    expect(article.readingLabel, '1 dk okuma');
  });

  group('primaryCategory', () {
    test('çoklu kategoride ilkini verir', () {
      // Kaynakta bir haber birden çok kategoride olabiliyor (`[49,51,52]`
      // ölçüldü, 12.12) — kart hepsini basmaz, detay basar.
      final article = NewsArticle.fromJson(
        json(
          categories: const [
            {'id': 'c1', 'name': 'Gündem', 'slug': 'gundem'},
            {'id': 'c2', 'name': 'Siyaset', 'slug': 'siyaset'},
          ],
        ),
      );

      expect(article.primaryCategory, 'Gündem');
    });

    test('adı boş olan kategori atlanır', () {
      final article = NewsArticle.fromJson(
        json(
          categories: const [
            {'id': 'c1', 'name': '  ', 'slug': 'bos'},
            {'id': 'c2', 'name': 'Spor', 'slug': 'spor'},
          ],
        ),
      );

      expect(article.primaryCategory, 'Spor');
    });

    test('kategori yoksa null — rozet hiç çizilmez', () {
      final article = NewsArticle.fromJson(json(categories: const []));

      expect(article.primaryCategory, isNull);
    });
  });

  group('wasUpdated', () {
    test('senkronun saniyelik farkı "güncellendi" saymaz', () {
      // 🐛 Canlıda ölçüldü: publishedAt 14:40:59 ↔ modifiedAt 14:41:00.
      // Eşik olmasaydı **her haber** "güncellendi" rozeti alırdı ve rozet
      // hiçbir şey anlatmaz olurdu.
      final article = NewsArticle.fromJson(json());

      expect(article.wasUpdated, isFalse);
    });

    test('gerçek bir düzeltme "güncellendi" sayılır', () {
      final article = NewsArticle.fromJson(
        json(modifiedAt: '2026-08-11T16:10:00Z'),
      );

      expect(article.wasUpdated, isTrue);
    });

    test('tarihlerden biri eksikse rozet çizilmez', () {
      expect(NewsArticle.fromJson(json(modifiedAt: null)).wasUpdated, isFalse);
      expect(NewsArticle.fromJson(json(publishedAt: null)).wasUpdated, isFalse);
    });
  });

  test('publishedLabel enjekte edilen "şimdi"ye göre hesaplanır', () {
    // ⚠️ Enjekte edilemeseydi golden referansı **her gün** kırılırdı ve insan
    // `--update-goldens`'ı refleks hâline getirirdi (bu projede 4 kez yaşandı).
    final article = NewsArticle.fromJson(json());
    final label = article.publishedLabel(
      now: DateTime.utc(2026, 8, 11, 17, 40, 59),
    );

    expect(label, isNotNull);
    expect(label, isNot(contains('null')));
  });

  test('paylaşım metni başlık, özet ve kaynak adresini taşır', () {
    final text = NewsArticle.fromJson(json()).shareText();

    expect(text, contains('Kadirli’de yaz akşamları'));
    expect(text, contains('https://www.silagazetesi.com.tr/'));
    expect(text, contains('Kadirli uygulaması'));
  });

  group('NewsCategory', () {
    test('sayaç "bizde görünen" sayıdır ve 0 olabilir', () {
      final category = NewsCategory.fromJson(const {
        'id': 'c1',
        'name': 'E-Gazete',
        'slug': 'e-gazete',
        'articleCount': 0,
        'showInFilterStrip': true,
        'displayOrder': 0,
      });

      expect(category.hasArticles, isFalse);
      expect(category.label, 'E-Gazete');
    });

    test('adı boş kategori ham boşluk basmaz', () {
      final category = NewsCategory.fromJson(const {'id': 'c2', 'name': ' '});

      expect(category.label, 'Kategori');
    });
  });
}
