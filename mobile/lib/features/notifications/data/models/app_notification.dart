import 'package:flutter/material.dart';
import 'package:freezed_annotation/freezed_annotation.dart';

import '../../../../core/navigation/app_modules.dart';
import '../../../../core/router/app_routes.dart';

part 'app_notification.freezed.dart';
part 'app_notification.g.dart';

/// `GET /v1/notifications` öğesi (`NotificationDto`).
///
/// ⚠️ Sınıf adı bilinçli olarak **`AppNotification`**: Flutter'ın kendi
/// `Notification` sınıfı (`NotificationListener` ağacı) `material.dart` ile
/// birlikte geliyor, aynı adı kullanmak her ekranda `hide` gerektirirdi.
@freezed
abstract class AppNotification with _$AppNotification {
  const factory AppNotification({
    required String id,
    @Default('') String title,
    @Default('') String body,

    /// Bildirimi üreten olayın türü (bugün yalnız `announcement` üretiliyor).
    String? type,

    /// Deep-link hedefinin kimliği — [relatedType] ile birlikte anlamlı.
    String? relatedId,

    /// Deep-link hedefinin modülü (`announcement`, `ad`, `event`…).
    String? relatedType,
    @Default(false) bool isRead,
    DateTime? readAt,
    DateTime? createdAt,
  }) = _AppNotification;

  const AppNotification._();

  factory AppNotification.fromJson(Map<String, dynamic> json) =>
      _$AppNotificationFromJson(json);

  /// Dokununca gidilecek rota — yoksa `null` (bildirim yalnız okunur).
  String? get targetRoute =>
      notificationRouteFor(relatedType: relatedType, relatedId: relatedId);

  bool get hasTarget => targetRoute != null;
}

/// Bildirimin `relatedType`'ından türeyen görsel kimlik (ikon + Türkçe etiket).
///
/// Sunucu serbest metin gönderiyor; **tanınmayan tür sessizce kaybolmamalı** →
/// genel zil ikonu + modülün kendi adı (bulunabiliyorsa) kullanılır. Aynı
/// yaklaşım 11.12'de şikayet türlerinde de uygulandı.
@immutable
class NotificationKind {
  const NotificationKind({required this.icon, required this.label});

  final IconData icon;
  final String label;

  static const _fallback = NotificationKind(
    icon: Icons.notifications_rounded,
    label: 'Bildirim',
  );

  static NotificationKind of(String? relatedType) {
    if (relatedType == null || relatedType.trim().isEmpty) return _fallback;

    return switch (relatedType.trim().toLowerCase()) {
      'announcement' => const NotificationKind(
        icon: Icons.campaign_rounded,
        label: 'Duyuru',
      ),
      'ad' => const NotificationKind(icon: Icons.sell_rounded, label: 'İlan'),
      'event' => const NotificationKind(
        icon: Icons.celebration_rounded,
        label: 'Etkinlik',
      ),
      'campaign' => const NotificationKind(
        icon: Icons.confirmation_number_rounded,
        label: 'Kampanya',
      ),
      'death' => const NotificationKind(
        icon: Icons.filter_vintage_rounded,
        label: 'Vefat',
      ),
      'power_outage' || 'poweroutage' => const NotificationKind(
        icon: Icons.bolt_rounded,
        label: 'Kesinti',
      ),
      'pharmacy' => const NotificationKind(
        icon: Icons.local_pharmacy_rounded,
        label: 'Eczane',
      ),
      'complaint' => const NotificationKind(
        icon: Icons.support_agent_rounded,
        label: 'Şikayet',
      ),
      'place' => const NotificationKind(
        icon: Icons.place_rounded,
        label: 'Mekan',
      ),
      'taxi' || 'taxi_driver' => const NotificationKind(
        icon: Icons.local_taxi_rounded,
        label: 'Taksi',
      ),
      _ => _fallback,
    };
  }
}

/// 🔑 **Push ve liste dokunuşunun ORTAK deep-link eşlemesi.**
///
/// Sunucunun `data.relatedType` + `data.relatedId` çifti → uygulama rotası.
/// Hedef rotaların tamamı 11.6-11.11'de zaten yazıldı; burada yalnız eşleme var.
///
/// **Kurallar:**
/// - Tanınmayan tür ya da eksik/bozuk kimlik → `null` (uydurma rotaya gidilmez;
///   bildirim yine listede okunabilir). Sessizce yanlış ekrana götürmek,
///   hiç götürmemekten kötüdür.
/// - Kimlik ham `String` olarak geliyor; **GUID biçimi doğrulanır** (sunucu
///   `Guid?` gönderiyor ama push `data` sözlüğü metin taşır).
/// - Modül kaydı [kAppModules] ile tutarlılık `notification_link_test.dart`'ta
///   denetleniyor: eşlenen her tür gerçek ve `ready` bir modüle gitmeli.
String? notificationRouteFor({
  required String? relatedType,
  required String? relatedId,
}) {
  final type = relatedType?.trim().toLowerCase();
  final id = relatedId?.trim();
  if (type == null || type.isEmpty || id == null || !_looksLikeGuid(id)) {
    return null;
  }

  return switch (type) {
    'announcement' => AppRoutes.announcementDetail(id),
    'ad' => AppRoutes.adDetail(id),
    'event' => AppRoutes.eventDetail(id),
    'campaign' => AppRoutes.campaignDetail(id),
    'death' => AppRoutes.deathDetail(id),
    'power_outage' || 'poweroutage' => AppRoutes.powerOutageDetail(id),
    'pharmacy' => AppRoutes.pharmacyDetail(id),
    'place' => AppRoutes.placeDetail(id),
    'taxi' || 'taxi_driver' => AppRoutes.taxiDriverDetail(id),
    'guide' || 'guide_item' => AppRoutes.guideItemDetail(id),
    _ => null,
  };
}

/// `00000000-0000-0000-0000-000000000000` biçimi (tire dahil, 36 karakter).
bool _looksLikeGuid(String value) => RegExp(
  r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$',
).hasMatch(value);
