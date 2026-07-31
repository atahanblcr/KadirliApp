import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/features/ads/application/ads_providers.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// İlan akışı denetleyicisinin **filtre semantiği** (`AdsFeedController`).
///
/// Ekran testleri bu kuralları dokunma üzerinden doğruluyor; burada doğrudan
/// denetleyiciye bakılıyor çünkü bazı davranışlar (ters fiyat aralığının
/// takas edilmesi, seçili köke tekrar dokunmak) canlıda doğrulanmıştı ama
/// testle kilitlenmemişti.
void main() {
  Map<String, dynamic> emptyPage() => successEnvelope({
    'items': <Object>[],
    'totalCount': 0,
    'pageSize': 20,
    'currentPage': 1,
    'totalPages': 0,
  });

  Future<(AdsFeedController, FakeHttpAdapter)> feed() async {
    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/ads': (_) async => jsonResponse(emptyPage()),
      '/v1/ads/categories': (_) async => jsonResponse(successEnvelope([])),
    });
    final container = await testContainer(adapter: adapter);
    container.read(adsFeedProvider);
    await waitUntil(
      () =>
          adapter.countOf('/v1/ads') > 0 &&
          !container.read(adsFeedProvider).isLoadingFirstPage,
      reason: 'ilk sayfa',
    );
    return (container.read(adsFeedProvider.notifier), adapter);
  }

  /// Filtreyi değiştirir ve **yeni isteğin gitmesini** bekler (sabit süre
  /// beklemek tüm süit paralel koşarken flaky oluyordu).
  Future<void> act(FakeHttpAdapter adapter, void Function() action) async {
    final before = adapter.countOf('/v1/ads');
    action();
    await waitUntil(
      () => adapter.countOf('/v1/ads') > before,
      reason: 'filtre değişimi yeni istek atmalı',
    );
  }

  Map<String, dynamic> lastQuery(FakeHttpAdapter adapter) =>
      adapter.lastOf('/v1/ads')!.queryParameters;

  test('başlangıçta filtre yok, sıralama newest', () async {
    final (controller, adapter) = await feed();

    expect(controller.state.filter.isActive, isFalse);
    expect(lastQuery(adapter)['sort'], 'newest');
    expect(lastQuery(adapter).containsKey('categoryId'), isFalse);
  });

  group('kategori', () {
    test('seçili köke tekrar dokunmak filtreyi kaldırır', () async {
      final (controller, adapter) = await feed();

      await act(adapter, () => controller.selectRootCategory('c-araclar'));
      expect(lastQuery(adapter)['categoryId'], 'c-araclar');

      await act(adapter, () => controller.selectRootCategory('c-araclar'));
      expect(lastQuery(adapter).containsKey('categoryId'), isFalse);
      expect(controller.state.filter.rootCategoryId, isNull);
    });

    test('başka köke geçmek alt kategori seçimini sıfırlar', () async {
      final (controller, adapter) = await feed();

      await act(adapter, () => controller.selectRootCategory('c-araclar'));
      await act(adapter, () => controller.selectSubCategory('c-otomobil'));
      expect(lastQuery(adapter)['categoryId'], 'c-otomobil');

      await act(adapter, () => controller.selectRootCategory('c-emlak'));
      expect(lastQuery(adapter)['categoryId'], 'c-emlak');
      expect(controller.state.filter.rootCategoryId, 'c-emlak');
    });

    test('seçili alt kategoriye tekrar dokunmak köke geri döner', () async {
      final (controller, adapter) = await feed();

      await act(adapter, () => controller.selectRootCategory('c-araclar'));
      await act(adapter, () => controller.selectSubCategory('c-otomobil'));

      await act(adapter, () => controller.selectSubCategory('c-otomobil'));

      expect(lastQuery(adapter)['categoryId'], 'c-araclar');
      expect(controller.state.filter.rootCategoryId, 'c-araclar');
    });

    test('kök seçilmemişken alt kategori çağrısı yok sayılır', () async {
      final (controller, adapter) = await feed();
      final before = adapter.countOf('/v1/ads');

      controller.selectSubCategory('c-otomobil');
      // Negatif iddia: istek gitmemesi bekleniyor → sınırlı bekleme yeterli.
      await Future<void>.delayed(const Duration(milliseconds: 150));

      expect(adapter.countOf('/v1/ads'), before);
      expect(controller.state.filter.categoryId, isNull);
    });
  });

  group('fiyat aralığı', () {
    test('ters girilen aralık sessizce takas edilir', () async {
      final (controller, adapter) = await feed();

      await act(
        adapter,
        () => controller.applyPriceRange(min: 500000, max: 100000),
      );

      expect(lastQuery(adapter)['minPrice'], 100000);
      expect(lastQuery(adapter)['maxPrice'], 500000);
    });

    test('tek taraflı sınır tek parametre gönderir', () async {
      final (controller, adapter) = await feed();

      await act(adapter, () => controller.applyPriceRange(max: 5000));

      expect(lastQuery(adapter).containsKey('minPrice'), isFalse);
      expect(lastQuery(adapter)['maxPrice'], 5000);
    });

    test('aralık kaldırılınca parametreler tamamen düşer', () async {
      final (controller, adapter) = await feed();

      await act(
        adapter,
        () => controller.applyPriceRange(min: 1000, max: 2000),
      );
      await act(adapter, () => controller.clearPriceRange());

      expect(lastQuery(adapter).containsKey('minPrice'), isFalse);
      expect(lastQuery(adapter).containsKey('maxPrice'), isFalse);
      expect(controller.state.filter.hasPriceRange, isFalse);
    });
  });

  group('filtreler birlikte', () {
    test('sıralama değişimi kategori ve aramayı korur', () async {
      final (controller, adapter) = await feed();

      await act(adapter, () => controller.selectRootCategory('c-emlak'));
      await act(adapter, () => controller.search('daire'));
      await act(adapter, () => controller.changeSort(AdSort.priceDesc));

      final query = lastQuery(adapter);
      expect(query['categoryId'], 'c-emlak');
      expect(query['search'], 'daire');
      expect(query['sort'], 'price_desc');
    });

    test('"Filtreleri temizle" sıralamayı KORUR, diğerlerini siler', () async {
      final (controller, adapter) = await feed();

      await act(adapter, () => controller.changeSort(AdSort.priceAsc));
      await act(adapter, () => controller.selectRootCategory('c-emlak'));
      await act(adapter, () => controller.search('daire'));
      await act(adapter, () => controller.applyPriceRange(min: 1000));

      await act(adapter, () => controller.clearFilters());

      final query = lastQuery(adapter);
      expect(query['sort'], 'price_asc', reason: 'sıralama kullanıcı tercihi');
      expect(query.containsKey('categoryId'), isFalse);
      expect(query.containsKey('search'), isFalse);
      expect(query.containsKey('minPrice'), isFalse);
      expect(controller.state.filter.isActive, isFalse);
    });

    test('aramadaki baştaki/sondaki boşluk kırpılır', () async {
      final (controller, adapter) = await feed();

      await act(adapter, () => controller.search('  egea  '));

      expect(lastQuery(adapter)['search'], 'egea');
    });

    test('yalnız boşluktan ibaret arama istek bile atmaz', () async {
      final (controller, adapter) = await feed();
      final before = adapter.countOf('/v1/ads');

      // Kırpılınca boş filtreye eşit → `applyFilter` no-op olmalı.
      controller.search('   ');
      await Future<void>.delayed(const Duration(milliseconds: 150));

      expect(adapter.countOf('/v1/ads'), before);
      expect(controller.state.filter.isActive, isFalse);
    });

    test('arama temizlenince liste yeniden yüklenir', () async {
      final (controller, adapter) = await feed();

      await act(adapter, () => controller.search('egea'));
      await act(adapter, () => controller.search(''));

      expect(lastQuery(adapter).containsKey('search'), isFalse);
    });
  });
}
