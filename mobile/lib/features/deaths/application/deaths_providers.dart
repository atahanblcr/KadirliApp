import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/paging/paged_feed.dart';
import '../data/deaths_repository.dart';
import '../data/models/death_notice.dart';

/// Tek vefat ilanı (detay + 11.13 deep-link).
///
/// `autoDispose`: detay ekranından çıkınca bellekte tutulmasına gerek yok,
/// ayrıca bildirim gönderen kullanıcının `pending` kaydı onaylanınca ekran
/// yeniden açıldığında taze veri gelir.
final deathNoticeProvider = FutureProvider.autoDispose
    .family<DeathNotice, String>(
      (ref, id) => ref.watch(deathsRepositoryProvider).detail(id),
      retry: apiRetry,
    );

/// Vefat listesi — filtre yalnız arama metni ('' = filtresiz).
///
/// Tarih filtresi (`?date=`) bilinçli olarak arayüze çıkarılmadı: uç yalnız
/// **tek bir güne** süzüyor, oysa liste zaten kısa (onaylı ilanlar cenazeden 7
/// gün sonra otomatik arşivleniyor — `ArchiveDeathsJob`). Bir takvim açtırmak
/// kullanıcıya bu ekranda yük olurdu.
typedef DeathsFeedState = PagedFeedState<DeathNotice, String>;

class DeathsFeedController extends PagedFeedController<DeathNotice, String> {
  @override
  String get initialFilter => '';

  @override
  Future<PagedResult<DeathNotice>> fetchPage({
    required int page,
    required int limit,
    required String filter,
  }) => ref
      .read(deathsRepositoryProvider)
      .list(page: page, limit: limit, search: filter);

  @override
  String idOf(DeathNotice item) => item.id;

  void search(String term) => applyFilter(term.trim());

  void clearFilters() => applyFilter('');
}

final deathsFeedProvider =
    NotifierProvider<DeathsFeedController, DeathsFeedState>(
      DeathsFeedController.new,
    );

/// **Bugün cenaze namazı olanlar** — liste ekranının üstündeki sakin bilgi
/// şeridi için. Ayrı istek atmaz: yüklenmiş listeden türer (11.6'nın "hub kendi
/// kesinti isteğini atmıyor" kararının aynısı).
final todaysFuneralsProvider = Provider<List<DeathNotice>>((ref) {
  final state = ref.watch(deathsFeedProvider);
  return state.items.where((notice) => notice.isToday()).toList(growable: false);
});
