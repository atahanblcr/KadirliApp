import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/paging/paged_feed.dart';
import '../data/guide_repository.dart';
import '../data/models/guide_category.dart';
import '../data/models/guide_item.dart';

/// Rehber kategorileri (filtre şeridi). Nadiren değişir → `autoDispose` yok.
final guideCategoriesProvider = FutureProvider<List<GuideCategory>>(
  (ref) => ref.watch(guideRepositoryProvider).categories(),
  retry: apiRetry,
);

/// Acil numaralar kategorisi — ekranın üstündeki kısayol şeridi için.
final emergencyCategoryProvider = Provider<GuideCategory?>((ref) {
  final categories = ref.watch(guideCategoriesProvider).value;
  if (categories == null) return null;
  for (final category in categories) {
    if (category.isEmergency) return category;
  }
  return null;
});

/// Tek rehber kaydı (detay + 11.13 deep-link).
final guideItemProvider = FutureProvider.autoDispose.family<GuideItem, String>(
  (ref, id) => ref.watch(guideRepositoryProvider).item(id),
  retry: apiRetry,
);

/// Rehber listesinin filtresi: kategori **ve** arama birlikte uygulanabilir.
@immutable
class GuideFilter {
  const GuideFilter({this.categoryId, this.search = ''});

  final String? categoryId;
  final String search;

  bool get isActive => categoryId != null || search.trim().isNotEmpty;

  GuideFilter copyWith({
    String? categoryId,
    bool clearCategory = false,
    String? search,
  }) => GuideFilter(
    categoryId: clearCategory ? null : (categoryId ?? this.categoryId),
    search: search ?? this.search,
  );

  @override
  bool operator ==(Object other) =>
      other is GuideFilter &&
      other.categoryId == categoryId &&
      other.search == search;

  @override
  int get hashCode => Object.hash(categoryId, search);
}

typedef GuideFeedState = PagedFeedState<GuideItem, GuideFilter>;

class GuideFeedController extends PagedFeedController<GuideItem, GuideFilter> {
  @override
  GuideFilter get initialFilter => const GuideFilter();

  @override
  Future<PagedResult<GuideItem>> fetchPage({
    required int page,
    required int limit,
    required GuideFilter filter,
  }) => ref.read(guideRepositoryProvider).items(
    page: page,
    limit: limit,
    categoryId: filter.categoryId,
    search: filter.search,
  );

  @override
  String idOf(GuideItem item) => item.id;

  /// Kategori seç/kaldır (aynı kategoriye tekrar dokunmak filtreyi kaldırır).
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
}

final guideFeedProvider =
    NotifierProvider<GuideFeedController, GuideFeedState>(
      GuideFeedController.new,
    );
