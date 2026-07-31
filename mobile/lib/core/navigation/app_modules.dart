import 'package:flutter/material.dart';

import '../router/app_routes.dart';

/// Uygulamanın **modül kaydı** — Ana Sayfa ızgarasının, "yakında" ekranlarının
/// ve (11.13'te) push deep-link eşlemesinin tek doğruluk kaynağı.
///
/// Neden tek liste: MOBILE_UX_PLAN'ın "işlevsiz buton yok" şartı ancak her
/// modülün **nereye gittiği** ve **hangi uçlara bağlanacağı** tek yerde
/// yazılıysa denetlenebilir. 11.6+ bir modülü gerçekleyince yalnız
/// [AppModule.ready] `true` olur; ızgara, rota ve ekran kendiliğinden uyar.
@immutable
class AppModule {
  const AppModule({
    required this.id,
    required this.label,
    required this.icon,
    required this.route,
    required this.phase,
    required this.summary,
    required this.endpoints,
    this.ready = false,
  });

  /// Backend izin/modül adlarıyla uyumlu kısa kimlik.
  final String id;

  /// Izgarada ve ekran başlığında görünen Türkçe ad.
  final String label;

  final IconData icon;

  final String route;

  /// Bu modülü getiren alt-faz ("11.6" gibi) — "yakında" ekranında gösterilir.
  final String phase;

  /// Modülün bir cümlelik açıklaması.
  final String summary;

  /// Modülün kullanacağı public uçlar (API_CONTRACT §10).
  final List<String> endpoints;

  /// Gerçek ekran yazıldı mı? `false` iken rota "yakında" ekranına düşer.
  final bool ready;
}

/// Ana Sayfa ızgarasının sırası = bu listenin sırası (en çok kullanılan üstte).
const kAppModules = <AppModule>[
  AppModule(
    id: 'ads',
    label: 'İlanlar',
    icon: Icons.sell_rounded,
    route: AppRoutes.ads,
    phase: '11.8',
    summary: 'Alım-satım ilanları: ara, filtrele, favorile, ilan ver.',
    endpoints: ['GET /v1/ads', 'GET /v1/ads/{id}', 'POST /v1/ads'],
  ),
  AppModule(
    id: 'announcements',
    label: 'Duyurular',
    icon: Icons.campaign_rounded,
    route: AppRoutes.announcements,
    phase: '11.6',
    summary: 'Belediye ve kurum duyuruları, su kesintisi bildirimleri.',
    endpoints: [
      'GET /v1/announcements',
      'GET /v1/announcements/types',
      'GET /v1/announcements/{id}',
      'POST /v1/announcements/{id}/view',
      'POST /v1/announcements/{id}/click',
    ],
    ready: true,
  ),
  AppModule(
    id: 'pharmacies',
    label: 'Eczane',
    icon: Icons.local_pharmacy_rounded,
    route: AppRoutes.pharmacies,
    phase: '11.7',
    summary: 'Bugünün nöbetçi eczanesi ve aylık nöbet takvimi.',
    endpoints: [
      'GET /v1/pharmacies/on-duty',
      'GET /v1/pharmacies/schedule',
      'GET /v1/pharmacies',
      'GET /v1/pharmacies/{id}',
    ],
    ready: true,
  ),
  AppModule(
    id: 'deaths',
    label: 'Vefat',
    icon: Icons.filter_vintage_rounded,
    route: AppRoutes.deaths,
    phase: '11.11',
    summary: 'Vefat ilanları, cenaze namazı ve taziye bilgileri.',
    endpoints: ['GET /v1/deaths', 'POST /v1/deaths'],
  ),
  AppModule(
    id: 'events',
    label: 'Etkinlik',
    icon: Icons.celebration_rounded,
    route: AppRoutes.events,
    phase: '11.10',
    summary: 'Şehirdeki etkinlikler, takvim görünümü ve detaylar.',
    endpoints: ['GET /v1/events', 'GET /v1/events/calendar'],
  ),
  AppModule(
    id: 'campaigns',
    label: 'Kampanya',
    // ⚠️ `local_offer` İlanlar'ın `sell` ikonuyla neredeyse aynı görünüyordu
    // (canlı emülatör kontrolünde yakalandı) → bilet ikonu (mockup'taki 🎟️).
    icon: Icons.confirmation_number_rounded,
    route: AppRoutes.campaigns,
    phase: '11.10',
    summary: 'Esnaf kampanyaları ve indirim kodları.',
    endpoints: ['GET /v1/campaigns', 'POST /v1/campaigns/{id}/view-code'],
  ),
  AppModule(
    id: 'places',
    label: 'Mekanlar',
    icon: Icons.place_rounded,
    route: AppRoutes.places,
    phase: '11.11',
    summary: 'Gezilecek yerler, park ve mekan bilgileri.',
    endpoints: ['GET /v1/places', 'GET /v1/places/{id}'],
  ),
  AppModule(
    id: 'taxis',
    label: 'Taksi',
    icon: Icons.local_taxi_rounded,
    route: AppRoutes.taxis,
    phase: '11.11',
    summary: 'Doğrulanmış taksi durakları ve sürücüleri — tek dokunuşla ara.',
    endpoints: ['GET /v1/taxis/drivers', 'POST /v1/taxis/drivers/{id}/call'],
  ),
  AppModule(
    id: 'guide',
    label: 'Rehber',
    icon: Icons.menu_book_rounded,
    route: AppRoutes.guide,
    phase: '11.7',
    summary: 'Şehir rehberi: kurumlar, sağlık, eğitim ve iletişim bilgileri.',
    endpoints: [
      'GET /v1/guide/categories',
      'GET /v1/guide/items',
      'GET /v1/guide/items/{id}',
    ],
    ready: true,
  ),
  AppModule(
    id: 'transport',
    label: 'Ulaşım',
    icon: Icons.directions_bus_rounded,
    route: AppRoutes.transport,
    phase: '11.12',
    summary: 'Şehirlerarası kalkış saatleri ve şehir içi hat durakları.',
    endpoints: [
      'GET /v1/transport/intercity-routes',
      'GET /v1/transport/intracity-routes',
    ],
  ),
  AppModule(
    id: 'power-outages',
    label: 'Kesinti',
    icon: Icons.bolt_rounded,
    route: AppRoutes.powerOutages,
    phase: '11.6',
    summary: 'Planlı elektrik kesintileri: mahalle, tarih ve saat.',
    endpoints: ['GET /v1/power-outages', 'GET /v1/power-outages/{id}'],
    ready: true,
  ),
  AppModule(
    id: 'complaints',
    label: 'Şikayet',
    icon: Icons.support_agent_rounded,
    route: AppRoutes.complaints,
    phase: '11.12',
    summary: 'Şikayet ve istek bildir, durumunu takip et.',
    endpoints: ['POST /v1/complaints', 'GET /v1/complaints/my'],
  ),
];

/// Rota → modül (yakında ekranı başlığı ve 11.13 deep-link için).
AppModule? moduleForRoute(String route) {
  for (final module in kAppModules) {
    if (module.route == route) return module;
  }
  return null;
}

/// Kimlik → modül.
AppModule moduleById(String id) =>
    kAppModules.firstWhere((module) => module.id == id);
