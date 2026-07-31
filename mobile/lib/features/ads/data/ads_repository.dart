import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/ad_category.dart';
import 'models/ad_detail.dart';
import 'models/ad_summary.dart';
import 'models/favorite_ad.dart';

/// İlan uçları (API_CONTRACT §10 — Ads).
///
/// 11.8 okuma + iletişim/favori aksiyonlarını kapsar; ilan verme/düzenleme
/// (`POST/PUT/DELETE /v1/ads`) 11.9'da buraya eklenecek.
class AdsRepository {
  AdsRepository(this._api);

  final ApiClient _api;

  /// Sayfalı ilan listesi.
  ///
  /// [sort] whitelist: `newest | oldest | price_asc | price_desc` — dışındaki
  /// değer sunucuda **400** üretir, bu yüzden ekran `AdSort` enum'undan geçer.
  Future<PagedResult<AdSummary>> list({
    int page = 1,
    int limit = 20,
    String? categoryId,
    String? search,
    String? sort,
    num? minPrice,
    num? maxPrice,
  }) => _api.getPaged(
    '/v1/ads',
    AdSummary.fromJson,
    page: page,
    limit: limit,
    query: {
      'categoryId': ?_blankToNull(categoryId),
      'search': ?_blankToNull(search),
      'sort': ?_blankToNull(sort),
      'minPrice': ?minPrice,
      'maxPrice': ?maxPrice,
    },
  );

  /// Kategori ağacı: [parentId] boşsa kök kategoriler, doluysa alt kategoriler.
  ///
  /// Uç **sayfasız düz liste** döndürür (rehber kategorilerinden farklı).
  Future<List<AdCategory>> categories({String? parentId}) async {
    final items = await _api.getList(
      '/v1/ads/categories',
      AdCategory.fromJson,
      query: {'parentId': ?_blankToNull(parentId)},
    );
    return [...items]..sort((a, b) => a.displayOrder.compareTo(b.displayOrder));
  }

  /// İlan detayı. ⚠️ Her çağrı sunucuda `view_count`'u artırır.
  Future<AdDetail> detail(String id) =>
      _api.getObject('/v1/ads/$id', AdDetail.fromJson);

  /// Favoriye ekler. İdempotent: zaten favorideyse de 200 döner (`data:false`).
  Future<void> addFavorite(String id) => _api.post('/v1/ads/$id/favorite');

  /// Favoriden çıkarır. İdempotent.
  Future<void> removeFavorite(String id) => _api.delete('/v1/ads/$id/favorite');

  /// Kullanıcının favorileri (11.8: kimlik kümesi, 11.9: "Favorilerim" ekranı).
  Future<PagedResult<FavoriteAd>> favorites({int page = 1, int limit = 50}) =>
      _api.getPaged(
        '/v1/users/me/favorites',
        FavoriteAd.fromJson,
        page: page,
        limit: limit,
      );

  /// Telefon tıklama sayacı (anonim). Aramayı **bekletmemek** için çağıran
  /// ekran `await` etmez; hata yutulur (sayaç kullanıcının işini engellemez).
  Future<void> trackPhone(String id) => _api.post('/v1/ads/$id/track-phone');

  Future<void> trackWhatsapp(String id) =>
      _api.post('/v1/ads/$id/track-whatsapp');

  static String? _blankToNull(String? value) {
    final trimmed = value?.trim();
    return (trimmed == null || trimmed.isEmpty) ? null : trimmed;
  }
}

final adsRepositoryProvider = Provider<AdsRepository>(
  (ref) => AdsRepository(ref.watch(apiClientProvider)),
);
