import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/paging/paged_feed.dart';
import '../data/ads_repository.dart';
import '../data/models/ad_category.dart';
import '../data/models/ad_detail.dart';
import '../data/models/ad_summary.dart';

/// İlan sıralaması — sunucunun whitelist'i (`GetAdsQueryHandler`); dışındaki
/// değer 400 üretir, bu yüzden ekran serbest metin göndermez.
enum AdSort {
  newest('newest', 'En yeni'),
  oldest('oldest', 'En eski'),
  priceAsc('price_asc', 'Önce ucuz'),
  priceDesc('price_desc', 'Önce pahalı');

  const AdSort(this.apiValue, this.label);

  final String apiValue;
  final String label;
}

/// Kök kategoriler (filtre şeridinin 1. katmanı). Nadiren değişir → sunucuda
/// 15 dk cache'li, istemcide `autoDispose` yok.
final adRootCategoriesProvider = FutureProvider<List<AdCategory>>(
  (ref) => ref.watch(adsRepositoryProvider).categories(),
  retry: apiRetry,
);

/// Bir kökün alt kategorileri (filtre şeridinin 2. katmanı).
///
/// Yalnız **alt kategorisi olan** kök seçilince istenir (`subCategoryCount`
/// zaten kök yanıtında geliyor) → boşuna istek atılmaz.
final adSubCategoriesProvider = FutureProvider.family<List<AdCategory>, String>(
  (ref, parentId) =>
      ref.watch(adsRepositoryProvider).categories(parentId: parentId),
  retry: apiRetry,
);

/// İlan detayı — 11.13 push deep-link hedefi de bunu okuyacak.
///
/// ⚠️ `autoDispose`: ekrandan çıkınca bırakılır. Bilinçli, çünkü sunucu her
/// çağrıda `view_count`'u artırıyor; uzun ömürlü önbellek "kaç kişi baktı"
/// sayısını kullanıcının kendi geri/ileri gezinmesiyle şişirirdi.
final adDetailProvider = FutureProvider.autoDispose.family<AdDetail, String>(
  (ref, id) => ref.watch(adsRepositoryProvider).detail(id),
  retry: apiRetry,
);

/// İlan listesinin filtresi: kategori + arama + sıralama + fiyat aralığı
/// **birlikte** uygulanır (hepsi tek uca gider).
@immutable
class AdsFilter {
  const AdsFilter({
    this.categoryId,
    this.rootCategoryId,
    this.search = '',
    this.sort = AdSort.newest,
    this.minPrice,
    this.maxPrice,
  });

  /// Uca gönderilen kategori (alt kategori seçiliyse onun kimliği).
  final String? categoryId;

  /// Şeridin 2. katmanını hangi kökün açacağı — [categoryId] bir alt kategori
  /// olduğunda kök chip'i de seçili görünmeli.
  final String? rootCategoryId;

  final String search;
  final AdSort sort;
  final num? minPrice;
  final num? maxPrice;

  bool get hasPriceRange => minPrice != null || maxPrice != null;

  /// "Filtreleri temizle" önerilmeli mi? (Sıralama filtre sayılmaz — her zaman
  /// bir değeri var.)
  bool get isActive =>
      categoryId != null || search.trim().isNotEmpty || hasPriceRange;

  AdsFilter copyWith({
    String? categoryId,
    String? rootCategoryId,
    bool clearCategory = false,
    String? search,
    AdSort? sort,
    num? minPrice,
    num? maxPrice,
    bool clearPrice = false,
  }) => AdsFilter(
    categoryId: clearCategory ? null : (categoryId ?? this.categoryId),
    rootCategoryId: clearCategory
        ? null
        : (rootCategoryId ?? this.rootCategoryId),
    search: search ?? this.search,
    sort: sort ?? this.sort,
    minPrice: clearPrice ? null : (minPrice ?? this.minPrice),
    maxPrice: clearPrice ? null : (maxPrice ?? this.maxPrice),
  );

  @override
  bool operator ==(Object other) =>
      other is AdsFilter &&
      other.categoryId == categoryId &&
      other.rootCategoryId == rootCategoryId &&
      other.search == search &&
      other.sort == sort &&
      other.minPrice == minPrice &&
      other.maxPrice == maxPrice;

  @override
  int get hashCode =>
      Object.hash(categoryId, rootCategoryId, search, sort, minPrice, maxPrice);
}

typedef AdsFeedState = PagedFeedState<AdSummary, AdsFilter>;

/// İlan listesi — sayfalama/yarış/mükerrer eleme `PagedFeedController`'dan
/// (11.7'de çıkarılan ortak çekirdek) geliyor; burada yalnız filtre semantiği var.
class AdsFeedController extends PagedFeedController<AdSummary, AdsFilter> {
  @override
  AdsFilter get initialFilter => const AdsFilter();

  @override
  Future<PagedResult<AdSummary>> fetchPage({
    required int page,
    required int limit,
    required AdsFilter filter,
  }) => ref
      .read(adsRepositoryProvider)
      .list(
        page: page,
        limit: limit,
        categoryId: filter.categoryId,
        search: filter.search,
        sort: filter.sort.apiValue,
        minPrice: filter.minPrice,
        maxPrice: filter.maxPrice,
      );

  @override
  String idOf(AdSummary item) => item.id;

  /// Kök kategori seç/kaldır. Aynı köke tekrar dokunmak filtreyi kaldırır;
  /// başka bir köke geçmek varsa alt kategori seçimini de sıfırlar.
  void selectRootCategory(String? categoryId) {
    final current = state.filter;
    if (categoryId == null || current.rootCategoryId == categoryId) {
      applyFilter(current.copyWith(clearCategory: true));
      return;
    }
    applyFilter(
      AdsFilter(
        categoryId: categoryId,
        rootCategoryId: categoryId,
        search: current.search,
        sort: current.sort,
        minPrice: current.minPrice,
        maxPrice: current.maxPrice,
      ),
    );
  }

  /// Alt kategori seç/kaldır — kaldırınca filtre köke geri döner
  /// (şeritte hangi chip vurguluysa filtre odur; belirsizlik yok).
  void selectSubCategory(String categoryId) {
    final current = state.filter;
    final root = current.rootCategoryId;
    if (root == null) return;

    applyFilter(
      AdsFilter(
        categoryId: current.categoryId == categoryId ? root : categoryId,
        rootCategoryId: root,
        search: current.search,
        sort: current.sort,
        minPrice: current.minPrice,
        maxPrice: current.maxPrice,
      ),
    );
  }

  void search(String term) =>
      applyFilter(state.filter.copyWith(search: term.trim()));

  void changeSort(AdSort sort) =>
      applyFilter(state.filter.copyWith(sort: sort));

  void applyPriceRange({num? min, num? max}) {
    // Kullanıcı ters yazdıysa (min > max) sessizce düzeltilir — "sonuç yok"
    // ekranı göstermek yerine niyeti uygula.
    final ordered = (min != null && max != null && min > max)
        ? (max, min)
        : (min, max);
    final current = state.filter;
    applyFilter(
      AdsFilter(
        categoryId: current.categoryId,
        rootCategoryId: current.rootCategoryId,
        search: current.search,
        sort: current.sort,
        minPrice: ordered.$1,
        maxPrice: ordered.$2,
      ),
    );
  }

  void clearPriceRange() =>
      applyFilter(state.filter.copyWith(clearPrice: true));

  /// Boş sonuç ekranındaki "Filtreleri temizle" — sıralama korunur.
  void clearFilters() => applyFilter(AdsFilter(sort: state.filter.sort));
}

final adsFeedProvider = NotifierProvider<AdsFeedController, AdsFeedState>(
  AdsFeedController.new,
);
