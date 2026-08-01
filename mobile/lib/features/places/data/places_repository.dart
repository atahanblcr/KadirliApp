import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/place.dart';
import 'models/place_category.dart';

/// Mekan uçları (API_CONTRACT §10).
class PlacesRepository {
  PlacesRepository(this._api);

  final ApiClient _api;

  /// Sayfalı liste. ⚠️ Sunucu araması **yalnız adda** koşuyor (açıklamada
  /// değil) → boş sonuçta kullanıcıya "isimle arayın" demek daha doğru.
  Future<PagedResult<Place>> list({
    int page = 1,
    int limit = 20,
    String? search,
    String? categoryId,
  }) => _api.getPaged(
    '/v1/places',
    Place.fromJson,
    page: page,
    limit: limit,
    query: {
      'search': ?_blankToNull(search),
      'categoryId': ?_blankToNull(categoryId),
    },
  );

  Future<Place> detail(String id) =>
      _api.getObject('/v1/places/$id', Place.fromJson);

  /// Mekan kategorileri (11.11'de eklenen lookup ucu).
  Future<List<PlaceCategory>> categories() =>
      _api.getList('/v1/places/categories', PlaceCategory.fromJson);

  static String? _blankToNull(String? value) {
    final trimmed = value?.trim();
    return (trimmed == null || trimmed.isEmpty) ? null : trimmed;
  }
}

final placesRepositoryProvider = Provider<PlacesRepository>(
  (ref) => PlacesRepository(ref.watch(apiClientProvider)),
);
