import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/ad_category.dart';
import 'models/ad_detail.dart';
import 'models/ad_extend_result.dart';
import 'models/ad_summary.dart';
import 'models/category_property.dart';
import 'models/favorite_ad.dart';
import 'models/my_ad.dart';

/// İlan uçları (API_CONTRACT §10 — Ads).
///
/// 11.8 okuma + iletişim/favori aksiyonlarını, 11.9 ilan verme/düzenleme/
/// silme/uzatma ve "benim ilanlarım" listesini kapsar.
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

  /// Kategoriye özel form alanları — ilan verme formunun dinamik bölümü.
  ///
  /// Sunucuda 15 dk cache'li; kategori değişince yeniden istenir.
  Future<List<CategoryProperty>> categoryProperties(String categoryId) async {
    final items = await _api.getList(
      '/v1/ads/categories/$categoryId/properties',
      CategoryProperty.fromJson,
    );
    return [...items]..sort((a, b) => a.displayOrder.compareTo(b.displayOrder));
  }

  /// İlan detayı. ⚠️ Her çağrı sunucuda `view_count`'u artırır.
  Future<AdDetail> detail(String id) =>
      _api.getObject('/v1/ads/$id', AdDetail.fromJson);

  // --- 11.9: ilan verme / yönetme ---

  /// Yeni ilan (`POST /v1/ads` `[A]`) — **`pending`** olarak yönetici onayına
  /// düşer. Dönen değer yeni ilanın kimliği.
  ///
  /// [propertyValues] anahtarları `CategoryProperty.id`, değerleri **metin**
  /// (sayı/boolean da metin olarak gider; sunucu tipe göre ayrıştırır).
  Future<String> create({
    required String categoryId,
    required String title,
    required String description,
    required String contactPhone,
    num? price,
    String? sellerName,
    List<String> imageFileIds = const [],
    Map<String, String> propertyValues = const {},
  }) async {
    final data = await _api.post(
      '/v1/ads',
      body: {
        'categoryId': categoryId,
        'title': title,
        'description': description,
        'price': price,
        'contactPhone': contactPhone,
        'sellerName': sellerName,
        'imageFileIds': imageFileIds,
        'propertyValues': propertyValues,
      },
    );
    // Uç `CreatedAtAction(..., id)` döndürüyor → zarfın `data`'sı düz guid.
    if (data is String && data.trim().isNotEmpty) return data;
    throw ApiException.unexpectedResponse(cause: data);
  }

  /// Kendi ilanını günceller (`PUT /v1/ads/{id}` `[A]`).
  ///
  /// ⚠️ **Kategori değiştirilemez** (property tanımları kategoriye bağlı) ve
  /// her düzenleme ilanı **yeniden onaya** (`pending`) düşürür.
  ///
  /// [removeImageIds] `AdImage.id`'leridir (dosya id'si değil — detay
  /// yanıtındaki `images[].id`).
  Future<void> update({
    required String id,
    required String title,
    required String description,
    required String contactPhone,
    num? price,
    String? sellerName,
    List<String> newImageFileIds = const [],
    List<String> removeImageIds = const [],
    Map<String, String>? propertyValues,
  }) => _api.put(
    '/v1/ads/$id',
    body: {
      'title': title,
      'description': description,
      'price': price,
      'contactPhone': contactPhone,
      'sellerName': sellerName,
      'newImageFileIds': newImageFileIds,
      'removeImageIds': removeImageIds,
      // null → sunucu mevcut değerlere DOKUNMAZ; boş map → hepsi silinir.
      'propertyValues': propertyValues,
    },
  );

  /// Kendi ilanını siler (soft delete).
  Future<void> deleteAd(String id) => _api.delete('/v1/ads/$id');

  /// Yayın süresini 30 gün uzatır. Hak dolduysa uç **409** verir.
  Future<AdExtendResult> extend(String id, {int adsWatched = 0}) async {
    final data = await _api.post(
      '/v1/ads/$id/extend',
      body: {'adsWatched': adsWatched},
    );
    if (data is! Map) throw ApiException.unexpectedResponse(cause: data);
    return AdExtendResult.fromJson(Map<String, dynamic>.from(data));
  }

  /// Kullanıcının kendi ilanları — **her statü** görünür (pending/rejected
  /// dahil). [status] whitelist: `pending|approved|rejected|expired`.
  Future<PagedResult<MyAd>> myAds({
    int page = 1,
    int limit = 20,
    String? status,
  }) => _api.getPaged(
    '/v1/users/me/ads',
    MyAd.fromJson,
    page: page,
    limit: limit,
    query: {'status': ?_blankToNull(status)},
  );

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
