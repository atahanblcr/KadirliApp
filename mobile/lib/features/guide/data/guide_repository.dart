import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/guide_category.dart';
import 'models/guide_item.dart';

/// Şehir rehberi uçları (API_CONTRACT §10).
class GuideRepository {
  GuideRepository(this._api);

  final ApiClient _api;

  /// Kategoriler. ⚠️ Uç sayfalı ve varsayılan `pageSize` **10**; kategori
  /// listesi filtre şeridi olduğu için tek seferde alınıyor (limit 50 =
  /// public uç tavanı).
  Future<List<GuideCategory>> categories({int limit = 50}) async {
    final page = await _api.getPaged(
      '/v1/guide/categories',
      GuideCategory.fromJson,
      page: 1,
      limit: limit,
    );
    final items = [...page.items]
      ..sort((a, b) => a.displayOrder.compareTo(b.displayOrder));
    return items;
  }

  /// Rehber kayıtları (sayfalı; `search` ada/açıklamaya bakar).
  Future<PagedResult<GuideItem>> items({
    int page = 1,
    int limit = 20,
    String? categoryId,
    String? search,
  }) => _api.getPaged(
    '/v1/guide/items',
    GuideItem.fromJson,
    page: page,
    limit: limit,
    query: {
      'categoryId': ?_blankToNull(categoryId),
      'search': ?_blankToNull(search),
    },
  );

  Future<GuideItem> item(String id) =>
      _api.getObject('/v1/guide/items/$id', GuideItem.fromJson);

  static String? _blankToNull(String? value) {
    final trimmed = value?.trim();
    return (trimmed == null || trimmed.isEmpty) ? null : trimmed;
  }
}

final guideRepositoryProvider = Provider<GuideRepository>(
  (ref) => GuideRepository(ref.watch(apiClientProvider)),
);
