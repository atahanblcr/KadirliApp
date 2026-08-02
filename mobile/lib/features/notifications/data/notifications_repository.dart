import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/app_notification.dart';

/// `GET /v1/notifications` yanıtı: bir sayfa + **filtreden bağımsız** okunmamış sayısı.
class NotificationPage {
  const NotificationPage({required this.page, required this.unreadCount});

  final PagedResult<AppNotification> page;

  /// Rozetin sayısı. `unreadOnly` filtresi uygulansa bile **toplam** okunmamış.
  final int unreadCount;
}

/// Bildirim uçları (`[A]` — hepsi oturum ister).
///
/// ⚠️ **Kontrat özelliği (Faz 10.10):** `GET /v1/notifications` sayfalı gövdenin
/// **içine** `unreadCount` koyar (`{unreadCount, items, totalCount, …}`) — zarf
/// `meta`'sı filtreyle sabitlendiği için backend bilinçli olarak böyle yaptı.
/// Bu yüzden liste `ApiClient.getPaged` ile değil ham `get` ile okunuyor: hem
/// sayfa hem sayaç **tek yanıttan** çıkıyor, rozet için ikinci istek gerekmiyor.
class NotificationsRepository {
  NotificationsRepository(this._api);

  final ApiClient _api;

  /// Bildirim listesi. [unreadOnly] sunucu tarafında filtreler.
  Future<NotificationPage> list({
    required int page,
    required int limit,
    bool unreadOnly = false,
  }) async {
    final data = await _api.get(
      '/v1/notifications',
      query: {
        'page': page,
        'limit': limit,
        if (unreadOnly) 'unreadOnly': true,
      },
    );
    if (data is! Map) throw ApiException.unexpectedResponse(cause: data);

    final map = Map<String, dynamic>.from(data);
    return NotificationPage(
      page: PagedResult<AppNotification>.fromJson(
        map,
        (item) =>
            AppNotification.fromJson(Map<String, dynamic>.from(item as Map)),
      ),
      unreadCount: _asInt(map['unreadCount']),
    );
  }

  /// Alt sekme rozeti için okunmamış bildirim sayısı.
  ///
  /// Öğelere ihtiyaç yok → `limit=1` ile en küçük yanıt istenir.
  Future<int> unreadCount() async {
    final data = await _api.get(
      '/v1/notifications',
      query: {'page': 1, 'limit': 1},
    );
    if (data is! Map) throw ApiException.unexpectedResponse(cause: data);
    return _asInt(data['unreadCount']);
  }

  /// Tek bildirimi okundu yapar. Sunucuda **idempotent**; başkasının bildirimi
  /// 404 döner (varlık sızdırılmaz).
  Future<void> markRead(String id) => _api.patch('/v1/notifications/$id/read');

  /// Tüm okunmamışları okundu yapar; sunucunun işaretlediği sayıyı döndürür.
  Future<int> markAllRead() async {
    final data = await _api.post('/v1/notifications/read-all');
    if (data is! Map) return 0;
    return _asInt(data['markedCount']);
  }

  static int _asInt(Object? value) => value is num ? value.toInt() : 0;
}

final notificationsRepositoryProvider = Provider<NotificationsRepository>(
  (ref) => NotificationsRepository(ref.watch(apiClientProvider)),
);
