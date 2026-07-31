import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../auth/application/auth_controller.dart';
import '../data/ads_repository.dart';

/// Kullanıcının favori ilan **kimlik kümesi**.
///
/// **Neden ayrı bir küme:** `AdDetailDto`'da `isFavorited` alanı yok (backend
/// kontratı Faz 10'da donduruldu) ve `POST/DELETE .../favorite` yalnız
/// "değişiklik oldu mu" bilgisini döndürüyor. Kalbin dolu mu boş mu çizileceği
/// bu yüzden `GET /v1/users/me/favorites` üzerinden **bir kez** öğrenilip
/// bellekte tutuluyor; hem liste kartı hem detay ekranı aynı kümeye bakıyor.
///
/// Anonim kullanıcıda **hiç istek atılmaz** (uç `[A]`) — küme boş kalır,
/// kalbe dokunmak `ensureSignedIn` davetini açar.
@immutable
class FavoriteAdsState {
  const FavoriteAdsState({
    this.ids = const <String>{},
    this.isLoading = false,
    this.busyId,
  });

  final Set<String> ids;

  /// İlk yükleme (kalp ikonu bu sırada nötr çizilir, "boş" değil).
  final bool isLoading;

  /// Şu an sunucuya yazılan ilan — aynı kalbe çift dokunuşu engeller.
  final String? busyId;

  bool contains(String adId) => ids.contains(adId);

  FavoriteAdsState copyWith({
    Set<String>? ids,
    bool? isLoading,
    String? busyId,
    bool clearBusy = false,
  }) => FavoriteAdsState(
    ids: ids ?? this.ids,
    isLoading: isLoading ?? this.isLoading,
    busyId: clearBusy ? null : (busyId ?? this.busyId),
  );
}

class FavoriteAdsController extends Notifier<FavoriteAdsState> {
  /// Kaç sayfa okunacağı — 50'lik sayfa × 4 = 2000 favori. Bunun ötesi
  /// gerçekçi değil; aşan kullanıcıda yalnız kalp ikonu eksik görünür,
  /// aksiyonun kendisi (idempotent uçlar) yine doğru çalışır.
  static const _maxPages = 4;
  static const _pageSize = 50;

  @override
  FavoriteAdsState build() {
    // Oturum açılınca/kapanınca küme yeniden kurulur.
    final isAuthenticated = ref.watch(
      authControllerProvider.select((auth) => auth.isAuthenticated),
    );
    if (!isAuthenticated) return const FavoriteAdsState();

    Future.microtask(load);
    return const FavoriteAdsState(isLoading: true);
  }

  Future<void> load() async {
    if (!ref.read(authControllerProvider).isAuthenticated) return;
    state = state.copyWith(isLoading: true);

    final repository = ref.read(adsRepositoryProvider);
    final ids = <String>{};
    try {
      for (var page = 1; page <= _maxPages; page++) {
        final result = await repository.favorites(page: page, limit: _pageSize);
        ids.addAll(result.items.map((favorite) => favorite.adId));
        if (!result.hasNextPage) break;
      }
      state = FavoriteAdsState(ids: ids);
    } on ApiException {
      // Favori listesi alınamazsa ekran çalışmaya devam eder; kalpler boş
      // görünür ve dokunulduğunda uç yine idempotent davranır.
      state = state.copyWith(isLoading: false);
    }
  }

  /// Favoriyi çevirir. **İyimser:** kalp anında dolar/boşalır, istek başarısız
  /// olursa geri alınır ve hata çağırana döner (ekran şerit gösterir).
  ///
  /// Dönen değer: işlemden sonraki favori durumu.
  Future<bool> toggle(String adId) async {
    if (state.busyId == adId) return state.contains(adId);

    final wasFavorite = state.contains(adId);
    final next = {...state.ids};
    wasFavorite ? next.remove(adId) : next.add(adId);
    state = state.copyWith(ids: next, busyId: adId);

    final repository = ref.read(adsRepositoryProvider);
    try {
      if (wasFavorite) {
        await repository.removeFavorite(adId);
      } else {
        await repository.addFavorite(adId);
      }
      state = state.copyWith(clearBusy: true);
      return !wasFavorite;
    } on ApiException {
      final reverted = {...state.ids};
      wasFavorite ? reverted.add(adId) : reverted.remove(adId);
      state = state.copyWith(ids: reverted, clearBusy: true);
      rethrow;
    }
  }
}

final favoriteAdsProvider =
    NotifierProvider<FavoriteAdsController, FavoriteAdsState>(
      FavoriteAdsController.new,
    );

/// Tek ilanın favori durumu — kart/detay yalnız kendi id'sini izler
/// (bir kalbe dokunmak tüm listeyi yeniden çizmez).
final isFavoriteProvider = Provider.family<bool, String>(
  (ref, adId) =>
      ref.watch(favoriteAdsProvider.select((state) => state.contains(adId))),
);
