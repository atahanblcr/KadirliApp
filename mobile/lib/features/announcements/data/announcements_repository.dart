import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/announcement.dart';
import 'models/announcement_type.dart';

/// Duyuru uçları (API_CONTRACT §10).
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

  /// Filtre chip'leri için tür listesi.
  Future<List<AnnouncementType>> types() =>
      _api.getList('/v1/announcements/types', AnnouncementType.fromJson);

  /// Tek duyuru.
  ///
  /// ⚠️ Bu uç bulunamayınca **HTTP 200 + `success:false`** döner (API_CONTRACT
  /// §3 istisnası). `EnvelopeInterceptor` bunu `ApiException(NOT_FOUND)`'a
  /// çevirdiği için burada ek bir kontrol gerekmez — çağıran `isNotFound`
  /// bakar, gerçek 404'ten ayırt etmek zorunda kalmaz.
  Future<Announcement> detail(String id) =>
      _api.getObject('/v1/announcements/$id', Announcement.fromJson);

  /// Görüntülenme sayacı (anonim de sayılır). Sonuç kullanıcıya gösterilmez.
  Future<void> trackView(String id) => _api.post('/v1/announcements/$id/view');

  /// Duyurudaki dış bağlantıya tıklanma sayacı.
  Future<void> trackClick(String id) => _api.post('/v1/announcements/$id/click');
}

final announcementsRepositoryProvider = Provider<AnnouncementsRepository>(
  (ref) => AnnouncementsRepository(ref.watch(apiClientProvider)),
);
