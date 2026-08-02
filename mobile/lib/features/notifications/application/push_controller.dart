import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/push/push_messaging.dart';
import '../../../core/router/app_router.dart';
import '../data/fcm_token_service.dart';
import '../data/models/app_notification.dart';
import '../data/notifications_repository.dart';
import 'notifications_feed.dart';
import 'unread_count_provider.dart';

/// Push sağlayıcısı. **Varsayılan no-op** — gerçek Firebase sağlayıcısı
/// `main.dart`'ta override edilir (`sharedPreferencesProvider` ile aynı desen).
///
/// Böylece widget testleri Firebase kanalı olmadan koşar ve yapılandırması
/// olmayan bir derleme sessizce push'suz çalışır.
final pushMessagingProvider = Provider<PushMessaging>(
  (ref) => const NoopPushMessaging(),
);

/// Ön planda yakalanan son push — uygulama içi şerit bunu gösterir.
/// Gösterildikten sonra [PushCoordinator.clearForegroundMessage] ile temizlenir.
final foregroundPushProvider = NotifierProvider<_ForegroundPush, PushPayload?>(
  _ForegroundPush.new,
);

class _ForegroundPush extends Notifier<PushPayload?> {
  @override
  PushPayload? build() => null;

  void set(PushPayload? value) => state = value;
}

/// Push izin durumu — Bildirimler ekranındaki uyarı şeridi bunu okur.
final pushPermissionProvider = NotifierProvider<_PushPermission, PushPermission>(
  _PushPermission.new,
);

class _PushPermission extends Notifier<PushPermission> {
  @override
  PushPermission build() => PushPermission.unavailable;

  void set(PushPermission value) => state = value;
}

/// 🔑 Push borusunun **tek** bağlantı noktası: izin → token kaydı →
/// ön plan/arka plan/kapalı-uygulama mesajlarının yönlendirilmesi.
///
/// Uygulama kökünde bir kez kurulur (`app.dart`). Tüm gezinme kararları
/// [openNotification] üzerinden geçer → liste dokunuşu ve push dokunuşu
/// **aynı kodu** çalıştırır (davranış ayrışması olamaz).
class PushCoordinator {
  PushCoordinator(this._ref);

  final Ref _ref;
  final List<StreamSubscription<Object?>> _subscriptions = [];
  bool _started = false;

  PushMessaging get _messaging => _ref.read(pushMessagingProvider);

  /// Uygulama açılışında bir kez çağrılır.
  Future<void> start() async {
    if (_started) return;
    _started = true;

    _subscriptions
      ..add(_messaging.onForegroundMessage.listen(_handleForeground))
      ..add(_messaging.onMessageOpenedApp.listen(openFromPush))
      ..add(_messaging.onTokenRefresh.listen(_handleTokenRefresh));

    _ref.read(pushPermissionProvider.notifier).set(
      await _messaging.currentPermission(),
    );

    // Uygulama bildirime dokunularak KAPALIYKEN açıldıysa o mesaj burada.
    final initial = await _messaging.initialMessage();
    if (initial != null) unawaited(openFromPush(initial));
  }

  void dispose() {
    for (final subscription in _subscriptions) {
      subscription.cancel();
    }
    _subscriptions.clear();
  }

  /// Kullanıcıdan bildirim izni ister ve sonucu yayınlar.
  ///
  /// Android 13+ ve iOS'ta izin **kullanıcı bir şey beklerken** istenmeli;
  /// bu yüzden açılışta değil, oturum açıldığında ya da kullanıcı Bildirimler
  /// ekranındaki şeride dokunduğunda çağrılır.
  Future<PushPermission> requestPermission() async {
    final result = await _messaging.requestPermission();
    if (!_ref.mounted) return result;
    _ref.read(pushPermissionProvider.notifier).set(result);
    return result;
  }

  /// FCM token'ı kendiliğinden yenilendi → sunucudaki kayıt tazelenir.
  /// Yenisi kaydedilmezse **push sessizce kesilir** (hiçbir hata görünmez).
  /// Kayıt yolu tek: 11.3'te yazılan [FcmTokenService].
  Future<void> _handleTokenRefresh(String token) =>
      _ref.read(fcmTokenServiceProvider).registerToken(token);

  /// Ön planda gelen mesaj: sistem bildirimi gösterilmez → rozeti tazele,
  /// listeyi tazele ve uygulama içi şeridi kaldır.
  void _handleForeground(PushPayload payload) {
    _ref.invalidate(unreadNotificationCountProvider);
    // Bildirimler sekmesi açıksa liste de anında güncellensin.
    final feed = _ref.read(notificationsFeedProvider.notifier);
    unawaited(feed.refresh());
    _ref.read(foregroundPushProvider.notifier).set(payload);
  }

  void clearForegroundMessage() =>
      _ref.read(foregroundPushProvider.notifier).set(null);

  /// Push'a dokunuldu → okundu işaretle + ilgili ekrana git.
  Future<void> openFromPush(PushPayload payload) => openNotification(
    notificationId: payload.notificationId,
    relatedType: payload.relatedType,
    relatedId: payload.relatedId,
  );

  /// 🔑 **Liste dokunuşu ve push dokunuşunun ortak yolu.**
  ///
  /// - Bildirim okundu işaretlenir (varsa kimliği). Uç hatası **yutulur**:
  ///   kullanıcı içeriği görmek istiyor, "okundu yapılamadı" uyarısı yolunu
  ///   kesmemeli.
  /// - Hedef rota çözülemezse (tanınmayan tür / bozuk kimlik) **gezinilmez**
  ///   ama okundu işaretlemesi yine yapılır — uydurma rotaya gitmek, hiç
  ///   gitmemekten kötüdür.
  Future<void> openNotification({
    String? notificationId,
    String? relatedType,
    String? relatedId,
  }) async {
    if (notificationId != null && notificationId.isNotEmpty) {
      unawaited(_markReadQuietly(notificationId));
    }

    final route = notificationRouteFor(
      relatedType: relatedType,
      relatedId: relatedId,
    );
    if (route == null || !_ref.mounted) return;

    _ref.read(routerProvider).push(route);
  }

  Future<void> _markReadQuietly(String notificationId) async {
    try {
      await _ref.read(notificationsRepositoryProvider).markRead(notificationId);
      if (!_ref.mounted) return;
      _ref.invalidate(unreadNotificationCountProvider);
      final feed = _ref.read(notificationsFeedProvider.notifier);
      unawaited(feed.refresh());
    } on ApiException catch (error) {
      debugPrint('Bildirim okundu işaretlenemedi: ${error.code}');
    }
  }
}

final pushCoordinatorProvider = Provider<PushCoordinator>((ref) {
  final coordinator = PushCoordinator(ref);
  ref.onDispose(coordinator.dispose);
  return coordinator;
});
