import 'package:flutter/material.dart';
import 'package:freezed_annotation/freezed_annotation.dart';

part 'notification_preferences.freezed.dart';
part 'notification_preferences.g.dart';

/// Kullanıcının bildirim tercihleri (`MyProfileDto.notificationPreferences`).
///
/// Varsayılanlar **sunucudaki `NotificationPreferences` entity'siyle birebir**:
/// duyuru/vefat/eczane/etkinlik açık, ilan/kampanya kapalı gelir. Bu yüzden
/// alan yanıtta hiç gelmezse bile kullanıcıya doğru durum gösterilir.
@freezed
abstract class NotificationPreferences with _$NotificationPreferences {
  const factory NotificationPreferences({
    @Default(true) bool announcements,
    @Default(true) bool deaths,
    @Default(true) bool pharmacy,
    @Default(true) bool events,
    @Default(false) bool ads,
    @Default(false) bool campaigns,
    // 🔴 Faz 12.15b — varsayılan `true` ve sunucudaki `NotificationPreferences.News` ile
    // birebir. Alanı tanımayan bir sunucu yanıtı (ya da 12.15b öncesi bir sürümden gelen
    // önbellek) kullanıcıyı sessizce "haberleri kapatmış" göstermemeli: alanın YOKLUĞU,
    // alan eklenmeden önceki davranışı vermeli (checklist §5).
    @Default(true) bool news,
  }) = _NotificationPreferences;

  const NotificationPreferences._();

  factory NotificationPreferences.fromJson(Map<String, dynamic> json) =>
      _$NotificationPreferencesFromJson(json);

  bool valueOf(NotificationTopic topic) => switch (topic) {
    NotificationTopic.announcements => announcements,
    NotificationTopic.deaths => deaths,
    NotificationTopic.pharmacy => pharmacy,
    NotificationTopic.events => events,
    NotificationTopic.ads => ads,
    NotificationTopic.campaigns => campaigns,
    NotificationTopic.news => news,
  };

  NotificationPreferences withValue(NotificationTopic topic, bool value) =>
      switch (topic) {
        NotificationTopic.announcements => copyWith(announcements: value),
        NotificationTopic.deaths => copyWith(deaths: value),
        NotificationTopic.pharmacy => copyWith(pharmacy: value),
        NotificationTopic.events => copyWith(events: value),
        NotificationTopic.ads => copyWith(ads: value),
        NotificationTopic.campaigns => copyWith(campaigns: value),
        NotificationTopic.news => copyWith(news: value),
      };
}

/// Yedi bildirim anahtarı — `PATCH /v1/users/me/notifications` gövde adlarıyla
/// birebir (`{"announcements": false}` gibi **kısmi** güncelleme).
///
/// Ekran bu listeden üretilir: yeni bir tercih eklenirse tek satır yeter
/// (11.4'teki `kAppModules` deseninin küçüğü).
enum NotificationTopic {
  announcements(
    key: 'announcements',
    label: 'Duyurular',
    description: 'Belediye ve kurum duyuruları',
    icon: Icons.campaign_outlined,
  ),
  // 🔑 Sıra ızgaradakiyle aynı: Haberler, Duyurular'ın hemen ardında — ikisi de
  // "şehirde ne oluyor" sorusunun cevabı (`kAppModules` deseni). Enum SIRASI yalnız
  // ekran düzenini belirler; serileştirme `key` üzerinden gider.
  news(
    key: 'news',
    label: 'Haberler',
    description: 'Gazeteden seçilen önemli haberler',
    icon: Icons.newspaper_outlined,
  ),
  deaths(
    key: 'deaths',
    label: 'Vefat ilanları',
    description: 'Yeni vefat ve cenaze bilgileri',
    icon: Icons.local_florist_outlined,
  ),
  pharmacy(
    key: 'pharmacy',
    label: 'Nöbetçi eczane',
    description: 'Günün nöbetçi eczanesi hatırlatması',
    icon: Icons.local_pharmacy_outlined,
  ),
  events(
    key: 'events',
    label: 'Etkinlikler',
    description: 'Yaklaşan etkinlik ve organizasyonlar',
    icon: Icons.celebration_outlined,
  ),
  ads(
    key: 'ads',
    label: 'İlanlar',
    description: 'İlanlarınızla ilgili gelişmeler',
    icon: Icons.sell_outlined,
  ),
  campaigns(
    key: 'campaigns',
    label: 'Kampanyalar',
    description: 'İşletmelerden indirim ve fırsatlar',
    icon: Icons.confirmation_number_outlined,
  );

  const NotificationTopic({
    required this.key,
    required this.label,
    required this.description,
    required this.icon,
  });

  /// Sunucu gövdesindeki alan adı.
  final String key;
  final String label;
  final String description;
  final IconData icon;
}
