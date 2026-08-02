import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/paging/paged_feed.dart';
import '../data/models/app_notification.dart';
import '../data/notifications_repository.dart';
import 'unread_count_provider.dart';

/// Bildirim listesinin filtresi — bugün tek boyut: "yalnız okunmamışlar".
///
/// Sunucu `?unreadOnly=` destekliyor (Faz 10.10) → filtre **istemcide değil
/// uçta** uygulanır; istemcide süzmek sayfalamayı tutarsız yapardı (11.11
/// mekan sıralaması / 11.12 şikayet durumu kararlarının aynısı).
@immutable
class NotificationFilter {
  const NotificationFilter({this.unreadOnly = false});

  final bool unreadOnly;

  NotificationFilter copyWith({bool? unreadOnly}) =>
      NotificationFilter(unreadOnly: unreadOnly ?? this.unreadOnly);

  @override
  bool operator ==(Object other) =>
      other is NotificationFilter && other.unreadOnly == unreadOnly;

  @override
  int get hashCode => unreadOnly.hashCode;
}

/// Bildirim listesi — ortak [PagedFeedController] üstünde.
///
/// Okundu işaretleme **iyimser**: satır anında sönükleşir, uç hata verirse
/// eski hâline döner (11.8 favori kalbinin deseni). Rozet sunucu-otoriter
/// kalsın diye mutasyondan sonra [unreadNotificationCountProvider] tazelenir.
class NotificationsFeedController
    extends PagedFeedController<AppNotification, NotificationFilter> {
  NotificationsRepository get _repo => ref.read(notificationsRepositoryProvider);

  @override
  NotificationFilter get initialFilter => const NotificationFilter();

  @override
  String idOf(AppNotification item) => item.id;

  @override
  Future<PagedResult<AppNotification>> fetchPage({
    required int page,
    required int limit,
    required NotificationFilter filter,
  }) async {
    final result = await _repo.list(
      page: page,
      limit: limit,
      unreadOnly: filter.unreadOnly,
    );
    return result.page;
  }

  void toggleUnreadOnly() =>
      applyFilter(state.filter.copyWith(unreadOnly: !state.filter.unreadOnly));

  /// Tek bildirimi okundu yapar. Zaten okunmuşsa uca **istek gitmez**.
  ///
  /// Dönüş: işlem gerçekten yapıldı mı (test ve çağıran için).
  Future<bool> markRead(String id) async {
    final index = state.items.indexWhere((item) => item.id == id);
    if (index < 0) return false;

    final previous = state.items[index];
    if (previous.isRead) return false;

    _replace(index, previous.copyWith(isRead: true, readAt: DateTime.now()));

    try {
      await _repo.markRead(id);
      if (!ref.mounted) return true;
      _refreshBadge();
      // "Yalnız okunmamışlar" görünümündeyken okunan satır listede kalmalı:
      // gözünün önünde kaybolan satır kullanıcıya "yanlış şeye mi dokundum?"
      // dedirtiyor. Liste bir sonraki tazelemede kendiliğinden düzelir.
      return true;
    } on ApiException catch (error) {
      if (!ref.mounted) return false;
      // 404 = bildirim silinmiş/başkasının → geri almanın anlamı yok.
      if (!error.isNotFound) _replace(index, previous);
      return false;
    }
  }

  /// Tümünü okundu yapar. İyimser: liste anında sönükleşir.
  Future<bool> markAllRead() async {
    final previous = state.items;
    if (previous.every((item) => item.isRead)) return false;

    final now = DateTime.now();
    state = state.copyWith(
      items: [
        for (final item in previous)
          item.isRead ? item : item.copyWith(isRead: true, readAt: now),
      ],
    );

    try {
      await _repo.markAllRead();
      if (!ref.mounted) return true;
      _refreshBadge();
      return true;
    } on ApiException {
      if (!ref.mounted) return false;
      state = state.copyWith(items: previous);
      return false;
    }
  }

  void _replace(int index, AppNotification item) {
    final items = [...state.items];
    items[index] = item;
    state = state.copyWith(items: items);
  }

  void _refreshBadge() => ref.invalidate(unreadNotificationCountProvider);
}

final notificationsFeedProvider =
    NotifierProvider<
      NotificationsFeedController,
      PagedFeedState<AppNotification, NotificationFilter>
    >(NotificationsFeedController.new);
