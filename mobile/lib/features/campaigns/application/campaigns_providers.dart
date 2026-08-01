import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/paging/paged_feed.dart';
import '../data/campaigns_repository.dart';
import '../data/models/campaign.dart';

/// Tek kampanya (detay + 11.13 deep-link).
final campaignDetailProvider = FutureProvider.autoDispose
    .family<Campaign, String>(
      (ref, id) => ref.watch(campaignsRepositoryProvider).detail(id),
      retry: apiRetry,
    );

/// Kampanya listesi — filtre = arama metni ('' = filtresiz).
///
/// Kategori filtresi bilinçli olarak yok: uç `?categoryId=` ile **işletme
/// kategorisine** göre süzüyor ama public bir işletme-kategori lookup ucu
/// bulunmuyor; olmayan bir listeden chip üretmek "işlevsiz buton" olurdu.
typedef CampaignsFeedState = PagedFeedState<Campaign, String>;

class CampaignsFeedController extends PagedFeedController<Campaign, String> {
  @override
  String get initialFilter => '';

  @override
  Future<PagedResult<Campaign>> fetchPage({
    required int page,
    required int limit,
    required String filter,
  }) => ref
      .read(campaignsRepositoryProvider)
      .list(page: page, limit: limit, search: filter);

  @override
  String idOf(Campaign item) => item.id;

  void search(String term) => applyFilter(term.trim());

  void clearFilters() => applyFilter('');
}

final campaignsFeedProvider =
    NotifierProvider<CampaignsFeedController, CampaignsFeedState>(
      CampaignsFeedController.new,
    );
