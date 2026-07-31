import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/utils/utils.dart';
import 'package:kadirli_app/features/ads/application/ads_providers.dart';
import 'package:kadirli_app/features/ads/data/models/ad_category.dart';
import 'package:kadirli_app/features/ads/data/models/ad_detail.dart';
import 'package:kadirli_app/features/ads/data/models/ad_summary.dart';

/// İlan modelleri + para biçimleme (11.8).
void main() {
  group('AppMoney', () {
    test('tam sayı fiyat kuruşsuz, kuruşlu fiyat kuruşlu yazılır', () {
      expect(AppMoney.amount(750000), '750.000 ₺');
      expect(AppMoney.amount(8500.50), '8.500,50 ₺');
      expect(AppMoney.amount(0), '0 ₺');
    });

    test('fiyatsız ilan "0 ₺" değil nötr metin gösterir', () {
      expect(AppMoney.price(null), 'Fiyat belirtilmemiş');
      expect(AppMoney.price(null, empty: 'Pazarlık'), 'Pazarlık');
      expect(AppMoney.price(0), '0 ₺');
    });

    test('aralık etiketi tek taraflı sınırı da anlatır', () {
      expect(AppMoney.rangeLabel(null, null), isNull);
      expect(AppMoney.rangeLabel(1000, null), '1.000 ₺ ve üzeri');
      expect(AppMoney.rangeLabel(null, 5000), '5.000 ₺ ve altı');
      expect(AppMoney.rangeLabel(1000, 5000), '1.000 ₺ – 5.000 ₺');
    });

    test('kullanıcı girdisi hem Türkçe hem düz biçimde okunur', () {
      expect(AppMoney.parse('1.250,50'), 1250.5);
      expect(AppMoney.parse('1250.50'), 1250.5);
      expect(AppMoney.parse('  50000 '), 50000);
      // Türkçe klavyede binlik ayracı nokta: "50.000" 50 değil 50.000'dir.
      expect(AppMoney.parse('50.000 ₺'), 50000);
      expect(AppMoney.parse('1.250.000'), 1250000);
      expect(AppMoney.parse(''), isNull);
      expect(AppMoney.parse('abc'), isNull);
      expect(AppMoney.parse(null), isNull);
    });
  });

  group('AdSummary', () {
    test(
      'kapak görseli listenin ilk elemanıdır (sunucu kapağı başa koyar)',
      () {
        final ad = AdSummary(
          id: 'a1',
          title: 'Egea',
          createdAt: DateTime.utc(2026, 7, 1),
          imageUrls: const ['/uploads/kapak.png', '/uploads/ikinci.png'],
        );
        expect(ad.coverImageUrl, '/uploads/kapak.png');
        expect(ad.hasImage, isTrue);
      },
    );

    test('görselsiz ilanda kapak null (nötr ikon çizilir)', () {
      final ad = AdSummary(
        id: 'a1',
        title: 'Egea',
        createdAt: DateTime.utc(2026, 7, 1),
      );
      expect(ad.coverImageUrl, isNull);
      expect(ad.hasImage, isFalse);
    });

    test('fiyat alanı sunucudan tam sayı gelse de okunur', () {
      final ad = AdSummary.fromJson({
        'id': 'a1',
        'title': 'Egea',
        'price': 750000,
        'createdAt': '2026-07-01T09:00:00Z',
        'imageUrls': <String>[],
      });
      expect(ad.price, 750000.0);
    });
  });

  group('AdPropertyValue', () {
    AdPropertyValue property(String type, String value) => AdPropertyValue(
      propertyId: 'p1',
      propertyName: 'Alan',
      propertyType: type,
      value: value,
    );

    test('Boolean değeri Var/Yok olarak yazılır', () {
      expect(property('Boolean', 'true').displayValue, 'Var');
      expect(property('Boolean', 'false').displayValue, 'Yok');
      expect(property('Boolean', '1').displayValue, 'Var');
    });

    test('MultiSelect virgülleri düzeltilir', () {
      expect(
        property('MultiSelect', 'ABS,Klima , Sunroof').displayValue,
        'ABS, Klima, Sunroof',
      );
    });

    test('boş değerli özellik satırı hiç çizilmez', () {
      expect(property('Text', '   ').displayValue, isNull);
      expect(property('MultiSelect', ' , ').displayValue, isNull);
    });
  });

  group('AdDetail', () {
    AdDetail detail({List<AdImage> images = const []}) => AdDetail(
      id: 'a1',
      title: 'Egea',
      categoryId: 'c1',
      createdAt: DateTime.utc(2026, 7, 1),
      expiresAt: DateTime.utc(2026, 8, 1),
      images: images,
    );

    test('url\'siz görsel kayıtları galeriden elenir', () {
      final ad = detail(
        images: const [
          AdImage(id: 'i1', url: '/uploads/1.png'),
          AdImage(id: 'i2', url: '  '),
          AdImage(id: 'i3'),
        ],
      );
      expect(ad.imageUrls, ['/uploads/1.png']);
    });

    test('telefonsuz ilanda iletişim çubuğu koşulu kapanır', () {
      expect(detail().hasPhone, isFalse);
    });
  });

  group('AdCategory', () {
    test('slug Material ikonuna eşlenir, bilinmeyen nötre düşer', () {
      const araclar = AdCategory(id: 'c', name: 'Araçlar', slug: 'araclar');
      expect(araclar.materialIcon, Icons.directions_car_rounded);

      const unknown = AdCategory(id: 'c', name: 'Yeni', slug: 'yeni-kategori');
      expect(unknown.materialIcon, Icons.label_rounded);
    });

    test('alt kategori sayısı ek istek atmadan bilinir', () {
      const withSubs = AdCategory(
        id: 'c',
        name: 'Araçlar',
        subCategoryCount: 3,
      );
      const leaf = AdCategory(id: 'c', name: 'Giyim');
      expect(withSubs.hasSubCategories, isTrue);
      expect(leaf.hasSubCategories, isFalse);
    });
  });

  group('AdsFilter', () {
    test('sıralama tek başına "aktif filtre" saymaz', () {
      expect(const AdsFilter().isActive, isFalse);
      expect(const AdsFilter(sort: AdSort.priceAsc).isActive, isFalse);
      expect(const AdsFilter(search: 'egea').isActive, isTrue);
      expect(const AdsFilter(search: '  ').isActive, isFalse);
      expect(const AdsFilter(minPrice: 100).isActive, isTrue);
      expect(const AdsFilter(categoryId: 'c1').isActive, isTrue);
    });

    test('eşitlik tüm alanları kapsar (filtre değişimi listeyi sıfırlar)', () {
      expect(
        const AdsFilter(categoryId: 'c1', search: 'x', minPrice: 5),
        const AdsFilter(categoryId: 'c1', search: 'x', minPrice: 5),
      );
      expect(
        const AdsFilter(sort: AdSort.newest),
        isNot(const AdsFilter(sort: AdSort.oldest)),
      );
    });

    test('sunucu whitelist değerleri birebir gönderilir', () {
      expect(AdSort.values.map((sort) => sort.apiValue), [
        'newest',
        'oldest',
        'price_asc',
        'price_desc',
      ]);
    });
  });

  group('Debouncer', () {
    testWidgets('art arda çağrılar tek sefere iner', (tester) async {
      final debouncer = Debouncer(delay: const Duration(milliseconds: 100));
      addTearDown(debouncer.dispose);

      var calls = 0;
      debouncer.run(() => calls++);
      debouncer.run(() => calls++);
      debouncer.run(() => calls++);

      expect(calls, 0, reason: 'gecikme dolmadan çalışmamalı');
      await tester.pump(const Duration(milliseconds: 150));
      expect(calls, 1);
    });

    testWidgets('flush beklemeyi atlar ve bekleyeni iptal eder', (
      tester,
    ) async {
      final debouncer = Debouncer(delay: const Duration(milliseconds: 100));
      addTearDown(debouncer.dispose);

      var calls = 0;
      debouncer.run(() => calls++);
      debouncer.flush(() => calls++);

      expect(calls, 1);
      await tester.pump(const Duration(milliseconds: 150));
      expect(calls, 1, reason: 'bekleyen çağrı iptal edilmeliydi');
    });
  });
}
