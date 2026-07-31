import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/paging/paged_feed.dart';
import '../../auth/application/auth_controller.dart';
import '../data/ads_repository.dart';
import '../data/models/favorite_ad.dart';

/// "Favorilerim" listesi — filtresiz (uç filtre parametresi almıyor).
///
/// Filtre tipi yine de `String` bırakıldı: ileride arama eklenirse
/// [PagedFeedController] deseni bozulmadan doldurulur.
typedef FavoritesFeedState = PagedFeedState<FavoriteAd, String>;

/// `GET /v1/users/me/favorites` — favoriye eklenme sırasına göre (yeni → eski).
///
/// ⚠️ Bu ekran, kalp ikonlarını besleyen `favoriteAdsProvider` **kimlik
/// kümesinden ayrıdır**: küme yalnız id tutar, burada satırın kendisi
/// (başlık/fiyat/görsel/`isAvailable`) gerekiyor. Favori kaldırıldığında ikisi
/// birlikte güncellenir.
class FavoritesFeedController
    extends PagedFeedController<FavoriteAd, String> {
  @override
  String get initialFilter => '';

  @override
  Future<PagedResult<FavoriteAd>> fetchPage({
    required int page,
    required int limit,
    required String filter,
  }) {
    if (!ref.read(authControllerProvider).isAuthenticated) {
      return Future.value(const PagedResult<FavoriteAd>());
    }
    return ref.read(adsRepositoryProvider).favorites(page: page, limit: limit);
  }

  @override
  String idOf(FavoriteAd item) => item.adId;

  /// Favoriden çıkarılan ilanı listeden anında düşürür (kalp zaten iyimser
  /// çalışıyor — liste de aynı anda tepki vermeli).
  void removeLocally(String adId) {
    final items = state.items.where((favorite) => favorite.adId != adId).toList();
    if (items.length == state.items.length) return;
    state = state.copyWith(
      items: items,
      totalCount: state.totalCount > 0 ? state.totalCount - 1 : 0,
    );
  }

  /// Geri alma (kaldırma isteği başarısız olursa satır geri konur).
  void restoreLocally(FavoriteAd favorite, int index) {
    if (state.items.any((item) => item.adId == favorite.adId)) return;
    final items = [...state.items];
    items.insert(index.clamp(0, items.length), favorite);
    state = state.copyWith(items: items, totalCount: state.totalCount + 1);
  }
}

final favoritesFeedProvider =
    NotifierProvider<FavoritesFeedController, FavoritesFeedState>(
      FavoritesFeedController.new,
    );
