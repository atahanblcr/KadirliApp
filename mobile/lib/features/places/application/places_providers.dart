import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/paging/paged_feed.dart';
import '../data/models/place.dart';
import '../data/models/place_category.dart';
import '../data/places_repository.dart';

/// Mekan kategorileri (filtre şeridi + kart üstündeki kategori adı).
/// Nadiren değişir → `autoDispose` yok.
final placeCategoriesProvider = FutureProvider<List<PlaceCategory>>(
  (ref) => ref.watch(placesRepositoryProvider).categories(),
  retry: apiRetry,
);

/// Kategori kimliği → kategori. Kart ve detay adı buradan okur; liste
/// alınamazsa `null` döner ve **kategori satırı hiç çizilmez**
/// (11.6 "filtre/etiket yoksa gösterme" kararı).
final placeCategoryByIdProvider = Provider.family<PlaceCategory?, String>((
  ref,
  id,
) {
  final categories = ref.watch(placeCategoriesProvider).value;
  if (categories == null) return null;
  for (final category in categories) {
    if (category.id == id) return category;
  }
  return null;
});

/// Tek mekan (detay + 11.13 deep-link).
final placeProvider = FutureProvider.autoDispose.family<Place, String>(
  (ref, id) => ref.watch(placesRepositoryProvider).detail(id),
  retry: apiRetry,
);

/// Mekan listesinin filtresi: kategori **ve** arama birlikte uygulanabilir
/// (11.7 rehber deseni).
@immutable
class PlacesFilter {
  const PlacesFilter({this.categoryId, this.search = ''});

  final String? categoryId;
  final String search;

  bool get isActive => categoryId != null || search.trim().isNotEmpty;

  PlacesFilter copyWith({
    String? categoryId,
    bool clearCategory = false,
    String? search,
  }) => PlacesFilter(
    categoryId: clearCategory ? null : (categoryId ?? this.categoryId),
    search: search ?? this.search,
  );

  @override
  bool operator ==(Object other) =>
      other is PlacesFilter &&
      other.categoryId == categoryId &&
      other.search == search;

  @override
  int get hashCode => Object.hash(categoryId, search);
}

typedef PlacesFeedState = PagedFeedState<Place, PlacesFilter>;

class PlacesFeedController extends PagedFeedController<Place, PlacesFilter> {
  @override
  PlacesFilter get initialFilter => const PlacesFilter();

  @override
  Future<PagedResult<Place>> fetchPage({
    required int page,
    required int limit,
    required PlacesFilter filter,
  }) => ref.read(placesRepositoryProvider).list(
    page: page,
    limit: limit,
    search: filter.search,
    categoryId: filter.categoryId,
  );

  @override
  String idOf(Place item) => item.id;

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

  void clearFilters() => applyFilter(const PlacesFilter());
}

final placesFeedProvider =
    NotifierProvider<PlacesFeedController, PlacesFeedState>(
      PlacesFeedController.new,
    );
