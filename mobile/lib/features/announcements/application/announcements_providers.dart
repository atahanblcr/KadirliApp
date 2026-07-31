import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/paging/paged_feed.dart';
import '../data/announcements_repository.dart';
import '../data/models/announcement.dart';
import '../data/models/announcement_type.dart';

/// Duyuru türleri (filtre chip'leri). Nadiren değişir → `autoDispose` yok.
final announcementTypesProvider = FutureProvider<List<AnnouncementType>>(
  (ref) => ref.watch(announcementsRepositoryProvider).types(),
  retry: apiRetry,
);

/// Tek duyuru (detay ekranı ve 11.13 push deep-link'i).
///
/// `autoDispose`: detaydan çıkınca bellekte tutmanın anlamı yok, geri dönüldüğünde
/// taze veri istenir (görüntülenme sayacı da yeniden tetiklenir).
final announcementDetailProvider = FutureProvider.autoDispose
    .family<Announcement, String>(
      (ref, id) => ref.watch(announcementsRepositoryProvider).detail(id),
      retry: apiRetry,
    );

/// Duyuru akışının durumu — filtre olarak seçili tür kimliği (`null` = tümü).
typedef AnnouncementFeedState = PagedFeedState<Announcement, String?>;

extension AnnouncementFeedFilter on AnnouncementFeedState {
  /// Seçili tür filtresi (`null` = tümü).
  String? get typeId => filter;

  /// Boş sonuç filtre yüzünden mi (mesajı ona göre yazmak için)?
  bool get isFiltered => filter != null;
}

/// Duyuru listesinin denetleyicisi — sayfalama/yarış/mükerrer eleme ortak
/// çekirdekte ([PagedFeedController]), burada yalnız uca özgü kısım var.
class AnnouncementFeedController
    extends PagedFeedController<Announcement, String?> {
  @override
  String? get initialFilter => null;

  @override
  Future<PagedResult<Announcement>> fetchPage({
    required int page,
    required int limit,
    required String? filter,
  }) => ref
      .read(announcementsRepositoryProvider)
      .list(page: page, limit: limit, typeId: filter);

  @override
  String idOf(Announcement item) => item.id;

  /// Tür seç/kaldır — aynı türe tekrar dokunmak filtreyi kaldırır.
  void selectType(String? typeId) =>
      applyFilter(state.typeId == typeId ? null : typeId);
}

final announcementFeedProvider =
    NotifierProvider<AnnouncementFeedController, AnnouncementFeedState>(
      AnnouncementFeedController.new,
    );
