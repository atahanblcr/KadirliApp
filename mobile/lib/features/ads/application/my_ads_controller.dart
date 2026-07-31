import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/paging/paged_feed.dart';
import '../../auth/application/auth_controller.dart';
import '../data/ads_repository.dart';
import '../data/models/ad_extend_result.dart';
import '../data/models/my_ad.dart';

/// "İlanlarım" listesinin durumu — filtre, durum (statü) filtresidir;
/// `null` = tümü.
typedef MyAdsState = PagedFeedState<MyAd, AdStatus?>;

/// Kullanıcının kendi ilanları (`GET /v1/users/me/ads`).
///
/// Sayfalama/yarış/mükerrer eleme ortak çekirdekten (`PagedFeedController`)
/// geliyor; buradaki tek özel davranış **statü filtresi** ve listeyi
/// bozmadan tek satırı güncelleyen/silen yardımcılar.
///
/// ⚠️ Uç `[A]`: anonim kullanıcıda hiç istek atılmaz (ekran zaten korumalı,
/// ama oturum bu ekrandayken düşerse boşuna 401 üretmeyelim).
class MyAdsController extends PagedFeedController<MyAd, AdStatus?> {
  @override
  AdStatus? get initialFilter => null;

  @override
  Future<PagedResult<MyAd>> fetchPage({
    required int page,
    required int limit,
    required AdStatus? filter,
  }) {
    if (!ref.read(authControllerProvider).isAuthenticated) {
      return Future.value(const PagedResult<MyAd>());
    }
    return ref
        .read(adsRepositoryProvider)
        .myAds(page: page, limit: limit, status: filter?.apiValue);
  }

  @override
  String idOf(MyAd item) => item.id;

  /// Statü chip'i: aynı statüye tekrar dokunmak filtreyi kaldırır.
  void selectStatus(AdStatus? status) =>
      applyFilter(state.filter == status ? null : status);

  /// Silinen ilanı listeden **anında** düşürür (tam tazeleme beklenmez —
  /// kullanıcı "sil"e bastıktan sonra kaydın durmasını istemez).
  ///
  /// `totalCount` de düşürülür ki liste sonundaki "Toplam N ilan" tutarlı kalsın.
  void removeLocally(String adId) {
    final items = state.items.where((ad) => ad.id != adId).toList();
    if (items.length == state.items.length) return;
    state = state.copyWith(
      items: items,
      totalCount: state.totalCount > 0 ? state.totalCount - 1 : 0,
    );
  }

  /// Uzatma sonrası tek satırı yerinde günceller (sunucu yeni bitiş tarihini
  /// ve kalan hakkı zaten döndürüyor → yeniden liste çekmeye gerek yok).
  ///
  /// Statü filtresi "süresi doldu" iken uzatılan ilan artık o filtreye
  /// uymadığı için listeden düşürülür.
  void applyExtension(AdExtendResult result) {
    final filter = state.filter;
    final updated = <MyAd>[];
    for (final ad in state.items) {
      if (ad.id != result.adId) {
        updated.add(ad);
        continue;
      }
      final next = ad.copyWith(
        status: result.status,
        expiresAt: result.expiresAt,
        extensionCount: result.extensionCount,
        maxExtensions: result.maxExtensions,
      );
      if (filter == null || filter.apiValue == next.status) updated.add(next);
    }
    state = state.copyWith(
      items: updated,
      totalCount: updated.length < state.items.length && state.totalCount > 0
          ? state.totalCount - 1
          : state.totalCount,
    );
  }
}

final myAdsProvider = NotifierProvider<MyAdsController, MyAdsState>(
  MyAdsController.new,
);

/// Kullanıcının toplam ilan sayısı — Profil sekmesindeki satırda gösterilir.
///
/// Listeyi zaten çekmiş olan denetleyiciden **türetilir**: ayrı bir istek
/// atmaz, liste açılmamışsa `null` (rozet çizilmez).
final myAdsCountProvider = Provider<int?>((ref) {
  final state = ref.watch(myAdsProvider);
  if (state.isLoadingFirstPage || state.error != null) return null;
  // Filtreliyken toplam, filtrenin toplamıdır → rozet yanıltmasın.
  return state.filter == null ? state.totalCount : null;
});
