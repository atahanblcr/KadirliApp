import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/paging/paged_feed.dart';
import '../data/models/news_article.dart';
import '../data/models/news_category.dart';
import '../data/news_repository.dart';

/// Kategori sözlüğü (filtre şeridi). Nadiren değişir → `autoDispose` yok.
final newsCategoriesProvider = FutureProvider<List<NewsCategory>>(
  (ref) => ref.watch(newsRepositoryProvider).categories(),
  retry: apiRetry,
);

/// Tek haber (detay + 12.15 push deep-link).
final newsDetailProvider = FutureProvider.autoDispose.family<NewsArticle, String>(
  (ref, id) => ref.watch(newsRepositoryProvider).detail(id),
  retry: apiRetry,
);

/// **Manşet** — öne çıkarılmış haberler (plan dışı ek, 12.14).
///
/// Panelde "öne çıkar" anahtarı 12.13'te yazıldı ve uç `?featured=true` ile
/// süzüyor; mobil tarafta karşılığı olmasaydı yöneticinin bastığı anahtar
/// **hiçbir işe yaramazdı** (§7 madde 37'nin *"panelin en sinsi yalan biçimi"*).
///
/// ⚠️ Şerit yalnız **süzgeçsiz** listede gösterilir: kullanıcı "Spor" seçmişken
/// başka kategoriden bir manşet basmak, süzgecin çalışmadığı izlenimi verir.
final newsFeaturedProvider = FutureProvider<List<NewsArticle>>((ref) async {
  final result = await ref
      .watch(newsRepositoryProvider)
      .list(page: 1, limit: 5, featured: true);
  return result.items;
}, retry: apiRetry);

/// **İlgili haberler** — detayın altındaki "Bu kategoriden" şeridi (plan dışı ek).
///
/// Yeni bir uç gerektirmiyor: var olan `?categoryId=` süzgeci kullanılıyor.
/// Okunan haberin kendisi listeden **elenir** (aksi hâlde kullanıcı zaten
/// açık olan habere geri dönen bir kart görürdü).
final relatedNewsProvider =
    FutureProvider.autoDispose.family<List<NewsArticle>, RelatedNewsRequest>((
      ref,
      request,
    ) async {
      final categoryId = request.categoryId;
      if (categoryId == null) return const [];
      // Bir fazlası istenir: okunan haber elendiğinde liste eksik kalmasın.
      final result = await ref
          .watch(newsRepositoryProvider)
          .list(page: 1, limit: RelatedNewsRequest.maxItems + 1, categoryId: categoryId);
      return result.items
          .where((item) => item.id != request.excludeId)
          .take(RelatedNewsRequest.maxItems)
          .toList();
    }, retry: apiRetry);

/// [relatedNewsProvider] anahtarı — `family` değeri **değer eşitliği** taşımak
/// zorunda, yoksa her `build` yeni bir provider açar ve istek döngüye girer.
@immutable
class RelatedNewsRequest {
  const RelatedNewsRequest({required this.categoryId, required this.excludeId});

  static const int maxItems = 5;

  final String? categoryId;
  final String excludeId;

  @override
  bool operator ==(Object other) =>
      other is RelatedNewsRequest &&
      other.categoryId == categoryId &&
      other.excludeId == excludeId;

  @override
  int get hashCode => Object.hash(categoryId, excludeId);
}

// ------------------------------------------------------------------- Liste

/// Liste süzgeci — **kategori ve arama tek nesnede**.
///
/// ⚠️ Ayrı tutulsalardı şeride dokunmak aramayı sessizce düşürürdü
/// (`CODE_REVIEW_CHECKLIST` §5; panelde 12.5'te aynı sebeple tek forma alındı).
@immutable
class NewsFilter {
  const NewsFilter({this.categoryId, this.search = ''});

  /// Sunucunun aramada istediği en az uzunluk (API_CONTRACT "Haberler").
  ///
  /// 🔑 Sunucu bunun altındaki değeri **süzgeç uygulamadan** yok sayıyor
  /// (400 değil — §5: bir yazım hatası listeyi boşaltmaz). İstemci de aynı
  /// kuralı bilerek taşıyor: tek harfte istek atıp "sonuç yok" göstermek,
  /// aslında **tüm listeyi** almışken kullanıcıya süzülmüş sanmak olurdu.
  static const int minSearchLength = 2;

  final String? categoryId;
  final String search;

  /// Uca gerçekten gidecek arama değeri; kısa girdi **hiç gönderilmez**.
  String? get effectiveSearch {
    final trimmed = search.trim();
    return trimmed.length < minSearchLength ? null : trimmed;
  }

  /// Kullanıcı yazmaya başladı ama sunucu henüz süzmüyor — liste "sonuç yok"
  /// demek yerine bunu **söylemeli**.
  bool get isSearchTooShort {
    final trimmed = search.trim();
    return trimmed.isNotEmpty && trimmed.length < minSearchLength;
  }

  bool get isActive => categoryId != null || search.trim().isNotEmpty;

  NewsFilter copyWith({
    String? categoryId,
    bool clearCategory = false,
    String? search,
  }) => NewsFilter(
    categoryId: clearCategory ? null : (categoryId ?? this.categoryId),
    search: search ?? this.search,
  );

  @override
  bool operator ==(Object other) =>
      other is NewsFilter &&
      other.categoryId == categoryId &&
      other.search == search;

  @override
  int get hashCode => Object.hash(categoryId, search);
}

typedef NewsFeedState = PagedFeedState<NewsArticle, NewsFilter>;

class NewsFeedController extends PagedFeedController<NewsArticle, NewsFilter> {
  @override
  NewsFilter get initialFilter => const NewsFilter();

  @override
  Future<PagedResult<NewsArticle>> fetchPage({
    required int page,
    required int limit,
    required NewsFilter filter,
  }) => ref
      .read(newsRepositoryProvider)
      .list(
        page: page,
        limit: limit,
        // 🔴 Süzme **sunucuda**: 20'lik sayfadan kayıt eleyip "17 haber" demek
        // `totalCount`'u ve sonsuz kaydırmayı yalancı yapar (checklist §5).
        search: filter.effectiveSearch,
        categoryId: filter.categoryId,
      );

  @override
  String idOf(NewsArticle item) => item.id;

  /// Aynı kategoriye tekrar dokunmak süzgeci kaldırır (etkinlik/rehber şeridiyle
  /// aynı el alışkanlığı).
  void selectCategory(String? categoryId) {
    final current = state.filter;
    final next = current.categoryId == categoryId ? null : categoryId;
    applyFilter(
      next == null
          ? current.copyWith(clearCategory: true)
          : current.copyWith(categoryId: next),
    );
  }

  void search(String term) =>
      applyFilter(state.filter.copyWith(search: term.trim()));

  void clearFilters() => applyFilter(const NewsFilter());
}

final newsFeedProvider = NotifierProvider<NewsFeedController, NewsFeedState>(
  NewsFeedController.new,
);
