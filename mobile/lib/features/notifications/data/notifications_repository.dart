import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';

/// Bildirim uçları (`[A]` — hepsi oturum ister).
///
/// ⚠️ **Kontrat özelliği:** `GET /v1/notifications` sayfalı gövdenin **içine**
/// `unreadCount` koyar (`{unreadCount, items, totalCount, …}`) — zarf `meta`'sı
/// filtreyle sabitlendiği için backend bilinçli olarak böyle yaptı (Faz 10.10).
/// Bu sayı **filtreden bağımsız** toplam okunmamış bildirimdir.
///
/// 11.4 yalnız rozet sayısını kullanır; 11.13 liste + okundu işaretlemeyi
/// buraya ekleyecek.
class NotificationsRepository {
  NotificationsRepository(this._api);

  final ApiClient _api;

  /// Alt sekme rozeti için okunmamış bildirim sayısı.
  ///
  /// Öğelere ihtiyaç yok → `limit=1` ile en küçük yanıt istenir.
  Future<int> unreadCount() async {
    final data = await _api.get(
      '/v1/notifications',
      query: {'page': 1, 'limit': 1},
    );
    if (data is! Map) throw ApiException.unexpectedResponse(cause: data);
    final count = data['unreadCount'];
    return count is num ? count.toInt() : 0;
  }
}

final notificationsRepositoryProvider = Provider<NotificationsRepository>(
  (ref) => NotificationsRepository(ref.watch(apiClientProvider)),
);
