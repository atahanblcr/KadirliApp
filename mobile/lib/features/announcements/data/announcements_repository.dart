import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/announcement.dart';

/// Duyuru uçları. 11.4 yalnız Ana Sayfa'nın "Öne çıkan" kartlarını çeker;
/// 11.6 tür filtresi, sayfalama, detay ve view/click sayaçlarını ekleyecek.
class AnnouncementsRepository {
  AnnouncementsRepository(this._api);

  final ApiClient _api;

  /// Sayfalı liste (API_CONTRACT §5). Sunucu en yenisini başa koyar.
  Future<PagedResult<Announcement>> list({
    int page = 1,
    int limit = 20,
    String? typeId,
  }) => _api.getPaged(
    '/v1/announcements',
    Announcement.fromJson,
    page: page,
    limit: limit,
    query: {'typeId': ?typeId},
  );

  /// Ana Sayfa vitrini — ilk sayfadan [limit] kadar duyuru.
  Future<List<Announcement>> latest({int limit = 3}) async {
    final page = await list(limit: limit);
    return page.items;
  }
}

final announcementsRepositoryProvider = Provider<AnnouncementsRepository>(
  (ref) => AnnouncementsRepository(ref.watch(apiClientProvider)),
);
