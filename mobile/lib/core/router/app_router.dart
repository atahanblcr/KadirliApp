import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/ads/presentation/ad_detail_screen.dart';
import '../../features/ads/presentation/ad_form_screen.dart';
import '../../features/ads/presentation/ads_screen.dart';
import '../../features/ads/presentation/favorites_screen.dart';
import '../../features/ads/presentation/my_ads_screen.dart';
import '../../features/announcements/presentation/announcement_detail_screen.dart';
import '../../features/announcements/presentation/announcements_screen.dart';
import '../../features/auth/application/auth_controller.dart';
import '../../features/campaigns/presentation/campaign_detail_screen.dart';
import '../../features/campaigns/presentation/campaigns_screen.dart';
import '../../features/complaints/presentation/complaint_form_screen.dart';
import '../../features/complaints/presentation/complaints_screen.dart';
import '../../features/auth/application/otp_flow_controller.dart';
import '../../features/auth/presentation/otp_verify_screen.dart';
import '../../features/auth/presentation/phone_login_screen.dart';
import '../../features/auth/presentation/register_screen.dart';
import '../../features/auth/presentation/splash_screen.dart';
import '../../features/common/presentation/module_placeholder_screen.dart';
import '../../features/deaths/presentation/death_detail_screen.dart';
import '../../features/deaths/presentation/death_report_screen.dart';
import '../../features/deaths/presentation/deaths_screen.dart';
import '../../features/dev/presentation/design_preview_screen.dart';
import '../../features/events/presentation/event_detail_screen.dart';
import '../../features/events/presentation/events_screen.dart';
import '../../features/dev/presentation/network_probe_screen.dart';
import '../../features/guide/presentation/guide_item_detail_screen.dart';
import '../../features/guide/presentation/guide_screen.dart';
import '../../features/home/presentation/home_screen.dart';
import '../../features/legal/presentation/legal_document_screen.dart';
import '../../features/legal/presentation/legal_documents_screen.dart';
import '../../features/legal/presentation/legal_version_screen.dart';
import '../../features/legal/presentation/reconsent_screen.dart';
import '../../features/news/presentation/news_detail_screen.dart';
import '../../features/news/presentation/news_screen.dart';
import '../../features/news/presentation/saved_news_screen.dart';
import '../../features/notifications/presentation/notifications_screen.dart';
import '../../features/pharmacies/presentation/pharmacies_screen.dart';
import '../../features/pharmacies/presentation/pharmacy_detail_screen.dart';
import '../../features/places/presentation/place_detail_screen.dart';
import '../../features/places/presentation/places_screen.dart';
import '../../features/power_outages/presentation/power_outage_detail_screen.dart';
import '../../features/power_outages/presentation/power_outages_screen.dart';
import '../../features/profile/presentation/account_delete_screen.dart';
import '../../features/profile/presentation/profile_edit_screen.dart';
import '../../features/profile/presentation/profile_screen.dart';
import '../../features/settings/presentation/settings_screen.dart';
import '../../features/taxis/presentation/taxi_driver_detail_screen.dart';
import '../../features/taxis/presentation/taxis_screen.dart';
import '../../features/transport/presentation/transport_screen.dart';
import '../config/env.dart';
import '../navigation/app_modules.dart';
import '../widgets/widgets.dart';
import 'app_routes.dart';
import 'app_shell.dart';

/// Uygulama yönlendiricisi.
///
/// **Oturum yönlendirmesi tek yerde:** ekranlar "giriş yapılmadı" diye
/// `context.go` çağırmaz; durum değişir, [GoRouter.redirect] karar verir.
///
/// **Yapı (11.4):** 4 dallı `StatefulShellRoute` (alt sekmeler) + kabuğun
/// ÜSTÜNE açılan tam ekran rotalar (giriş akışı, Ayarlar, modül ekranları).
/// Modül ekranları bilinçli olarak kabuğun dışında: hub'dan bir modüle
/// girmek "içeri girmek"tir, geri tuşu hub'a döner (mockup deseni).
final routerProvider = Provider<GoRouter>((ref) {
  // Oturum durumu değişince redirect yeniden değerlendirilsin
  // (`sessionExpiredProvider` → AuthController → burası).
  final refreshSignal = ValueNotifier<int>(0);
  ref.listen(authControllerProvider, (_, _) => refreshSignal.value++);
  ref.onDispose(refreshSignal.dispose);

  final router = GoRouter(
    initialLocation: AppRoutes.splash,
    debugLogDiagnostics: Env.showDevTools,
    refreshListenable: refreshSignal,
    redirect: (context, state) => _redirect(ref, state.matchedLocation),
    routes: [
      // --- Alt sekme kabuğu ---
      StatefulShellRoute.indexedStack(
        builder: (context, state, navigationShell) =>
            AppShell(navigationShell: navigationShell),
        branches: [
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.home,
                name: 'home',
                builder: (context, state) => const HomeScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.ads,
                name: 'ads',
                builder: (context, state) => const AdsScreen(),
                routes: [
                  // 11.8: detay sekmenin KENDİ Navigator'ında açılır — alt
                  // sekme çubuğu kaybolmaz, geri liste konumunu korur.
                  GoRoute(
                    path: ':id',
                    name: 'adDetail',
                    builder: (context, state) =>
                        AdDetailScreen(id: state.pathParameters['id']!),
                  ),
                ],
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.notifications,
                name: 'notifications',
                builder: (context, state) => const NotificationsScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.profile,
                name: 'profile',
                builder: (context, state) => const ProfileScreen(),
                routes: [
                  // 11.9: me-scoped listeler sekmenin İÇİNDE (alt sekme
                  // çubuğu kalır, geri tuşu profile döner).
                  GoRoute(
                    path: 'ilanlarim',
                    name: 'myAds',
                    builder: (context, state) => const MyAdsScreen(),
                  ),
                  GoRoute(
                    path: 'favorilerim',
                    name: 'myFavorites',
                    builder: (context, state) => const FavoritesScreen(),
                  ),
                ],
              ),
            ],
          ),
        ],
      ),

      // --- Kimlik doğrulama (11.3) ---
      GoRoute(
        path: AppRoutes.splash,
        name: 'splash',
        builder: (context, state) => const SplashScreen(),
      ),
      GoRoute(
        path: AppRoutes.login,
        name: 'login',
        builder: (context, state) => const PhoneLoginScreen(),
        routes: [
          // Alt rota: /giris/kod — kod ekranı telefon adımının devamı.
          GoRoute(
            path: 'kod',
            name: 'otpVerify',
            builder: (context, state) => const OtpVerifyScreen(),
          ),
        ],
      ),
      GoRoute(
        path: AppRoutes.register,
        name: 'register',
        builder: (context, state) => const RegisterScreen(),
      ),

      // --- Ayarlar / Kontrol + Profil (11.5) ---
      GoRoute(
        path: AppRoutes.settings,
        name: 'settings',
        builder: (context, state) => const SettingsScreen(),
      ),
      GoRoute(
        path: AppRoutes.profileEdit,
        name: 'profileEdit',
        builder: (context, state) => const ProfileEditScreen(),
      ),
      GoRoute(
        path: AppRoutes.accountDelete,
        name: 'accountDelete',
        builder: (context, state) => const AccountDeleteScreen(),
      ),

      // --- Hukuki metinler / KVKK (12.17) ---
      // ⚠️ Üçü de **kardeş** rota: `/yasal/:type` iç içe yazılsaydı go_router
      // üstteki liste ekranını da kurar ve kayıt akışındaki bir kullanıcı
      // metni açtığında arkada gereksiz bir istek atardı (11.7 tuzağı).
      GoRoute(
        path: AppRoutes.legal,
        name: 'legalDocuments',
        builder: (context, state) => const LegalDocumentsScreen(),
      ),
      GoRoute(
        path: '${AppRoutes.legal}/:type',
        name: 'legalDocument',
        builder: (context, state) =>
            LegalDocumentScreen(type: state.pathParameters['type']!),
      ),
      GoRoute(
        path: '${AppRoutes.legalVersionPrefix}/:id',
        name: 'legalVersion',
        builder: (context, state) =>
            LegalVersionScreen(versionId: state.pathParameters['id']!),
      ),
      GoRoute(
        path: AppRoutes.reconsent,
        name: 'reconsent',
        builder: (context, state) => const ReconsentScreen(),
      ),

      // --- İlan verme / düzenleme (11.9) ---
      // Kabuğun dışında tam ekran. ⚠️ Düzenleme **kardeş** rota: alt rota
      // yapılırsa go_router üstteki "yeni ilan" ekranını da kurar (11.7'de
      // eczane detayında görülen tuzak) ve arkada boşuna istek/diyalog çıkar.
      GoRoute(
        path: AppRoutes.adCreate,
        name: 'adCreate',
        builder: (context, state) => const AdFormScreen(),
      ),
      GoRoute(
        path: '${AppRoutes.adEditPrefix}/:id',
        name: 'adEdit',
        builder: (context, state) =>
            AdFormScreen(adId: state.pathParameters['id']!),
      ),

      // --- Gerçeklenmiş modüller (11.6) ---
      // Detaylar **alt rota** olarak tanımlı: `/duyurular/<id>` hem geri tuşunda
      // listeye döner hem de 11.13 push deep-link'i için hazır bir hedef.
      GoRoute(
        path: AppRoutes.announcements,
        name: 'module-announcements',
        builder: (context, state) => const AnnouncementsScreen(),
        routes: [
          GoRoute(
            path: ':id',
            name: 'announcementDetail',
            builder: (context, state) =>
                AnnouncementDetailScreen(id: state.pathParameters['id']!),
          ),
        ],
      ),
      GoRoute(
        path: AppRoutes.powerOutages,
        name: 'module-power-outages',
        builder: (context, state) => const PowerOutagesScreen(),
        routes: [
          GoRoute(
            path: ':id',
            name: 'powerOutageDetail',
            builder: (context, state) =>
                PowerOutageDetailScreen(id: state.pathParameters['id']!),
          ),
        ],
      ),

      // --- Gerçeklenmiş modüller (11.7) ---
      GoRoute(
        path: AppRoutes.pharmacies,
        name: 'module-pharmacies',
        builder: (context, state) => const PharmaciesScreen(),
        routes: [
          GoRoute(
            path: ':id',
            name: 'pharmacyDetail',
            builder: (context, state) =>
                PharmacyDetailScreen(id: state.pathParameters['id']!),
          ),
        ],
      ),
      GoRoute(
        path: AppRoutes.guide,
        name: 'module-guide',
        builder: (context, state) => const GuideScreen(),
        routes: [
          GoRoute(
            path: ':id',
            name: 'guideItemDetail',
            builder: (context, state) =>
                GuideItemDetailScreen(id: state.pathParameters['id']!),
          ),
        ],
      ),

      // --- Gerçeklenmiş modüller (11.10) ---
      GoRoute(
        path: AppRoutes.events,
        name: 'module-events',
        builder: (context, state) => const EventsScreen(),
        routes: [
          GoRoute(
            path: ':id',
            name: 'eventDetail',
            builder: (context, state) =>
                EventDetailScreen(id: state.pathParameters['id']!),
          ),
        ],
      ),
      GoRoute(
        path: AppRoutes.campaigns,
        name: 'module-campaigns',
        builder: (context, state) => const CampaignsScreen(),
        routes: [
          GoRoute(
            path: ':id',
            name: 'campaignDetail',
            builder: (context, state) =>
                CampaignDetailScreen(id: state.pathParameters['id']!),
          ),
        ],
      ),

      // --- Gerçeklenmiş modüller (11.11) ---
      GoRoute(
        path: AppRoutes.deaths,
        name: 'module-deaths',
        builder: (context, state) => const DeathsScreen(),
        routes: [
          GoRoute(
            path: ':id',
            name: 'deathDetail',
            builder: (context, state) =>
                DeathDetailScreen(id: state.pathParameters['id']!),
          ),
        ],
      ),
      // ⚠️ Vefat bildirimi `/vefat`ın alt rotası DEĞİL (bkz. AppRoutes.deathReport).
      GoRoute(
        path: AppRoutes.deathReport,
        name: 'deathReport',
        builder: (context, state) => const DeathReportScreen(),
      ),
      GoRoute(
        path: AppRoutes.taxis,
        name: 'module-taxis',
        builder: (context, state) => const TaxisScreen(),
        routes: [
          GoRoute(
            path: ':id',
            name: 'taxiDriverDetail',
            builder: (context, state) =>
                TaxiDriverDetailScreen(id: state.pathParameters['id']!),
          ),
        ],
      ),
      GoRoute(
        path: AppRoutes.places,
        name: 'module-places',
        builder: (context, state) => const PlacesScreen(),
        routes: [
          GoRoute(
            path: ':id',
            name: 'placeDetail',
            builder: (context, state) =>
                PlaceDetailScreen(id: state.pathParameters['id']!),
          ),
        ],
      ),

      // --- Gerçeklenmiş modüller (12.14) ---
      GoRoute(
        path: AppRoutes.news,
        name: 'module-news',
        builder: (context, state) => const NewsScreen(),
        routes: [
          GoRoute(
            path: ':id',
            name: 'newsDetail',
            builder: (context, state) =>
                NewsDetailScreen(id: state.pathParameters['id']!),
          ),
        ],
      ),
      // ⚠️ "Kaydedilenler" `/haberler`in **kardeşi** (bkz. AppRoutes.savedNews):
      // alt rota olsaydı hem üstteki liste ekranı arka planda kurulur hem de
      // `:id` deseni bu yolu bir haber kimliği sanardı.
      GoRoute(
        path: AppRoutes.savedNews,
        name: 'savedNews',
        builder: (context, state) => const SavedNewsScreen(),
      ),

      // --- Gerçeklenmiş modüller (11.12) ---
      // ⚠️ Ulaşımda detay rotası YOK: sunucuda `{id}` ucu yok, saatler ve
      // duraklar liste gövdesinde geliyor → kart yerinde açılıyor.
      GoRoute(
        path: AppRoutes.transport,
        name: 'module-transport',
        builder: (context, state) => const TransportScreen(),
      ),
      GoRoute(
        path: AppRoutes.complaints,
        name: 'module-complaints',
        builder: (context, state) => const ComplaintsScreen(),
      ),
      // ⚠️ Form `/sikayet`in kardeşi (bkz. AppRoutes.complaintCreate).
      GoRoute(
        path: AppRoutes.complaintCreate,
        name: 'complaintCreate',
        builder: (context, state) {
          final query = state.uri.queryParameters;
          return ComplaintFormScreen(
            initialType: query['tur'],
            relatedModule: query['modul'],
            relatedId: query['kayit'],
            relatedTitle: query['baslik'],
          );
        },
      ),

      // --- Henüz yazılmamış modüller: kayıttan otomatik üretilir ---
      // Bir modül gerçeklenince `AppModule.ready` true olur, kendi rotası
      // yukarıya yazılır ve buradaki "yakında" ekranı devreden çıkar.
      for (final module in kAppModules)
        if (!module.ready)
          GoRoute(
            path: module.route,
            name: 'module-${module.id}',
            builder: (context, state) => ModulePlaceholderScreen(module: module),
          ),

      // --- Geliştirici (yalnız debug) ---
      // ⚠️ Faz 11.16: bu rotalar eskiden KOŞULSUZ kayıtlıydı. Menü girişleri
      // `Env.showDevTools` ile gizlendiği için "yalnız debug" sanılıyordu, ama
      // rota tablosunda durdukları sürece yayın yapısında da **açılabiliyorlardı**
      // (deep-link ya da elle yazılan adres). `/gelistirici/ag` yedi gerçek uca
      // istek atıp traceId basan bir tanılama ekranı — vatandaşın elindeki
      // uygulamada bulunmamalı. Artık kayıt da koşullu.
      if (Env.showDevTools) ...[
        GoRoute(
          path: AppRoutes.designPreview,
          name: 'designPreview',
          builder: (context, state) => const DesignPreviewScreen(),
        ),
        GoRoute(
          path: AppRoutes.networkProbe,
          name: 'networkProbe',
          builder: (context, state) => const NetworkProbeScreen(),
        ),
      ],
    ],
    errorBuilder: (context, state) => AppScaffold(
      title: 'Sayfa bulunamadı',
      body: ErrorView(
        icon: Icons.explore_off_rounded,
        title: 'Sayfa bulunamadı',
        message: 'Aradığınız ekran mevcut değil.',
        retryLabel: 'Ana sayfaya dön',
        onRetry: () => context.go(AppRoutes.home),
      ),
    ),
  );

  ref.onDispose(router.dispose);
  return router;
});

/// Oturum durumuna göre hedef rota; `null` = olduğun yerde kal.
String? _redirect(Ref ref, String location) {
  final auth = ref.read(authControllerProvider);

  // Açılış kararı verilmeden hiçbir yere gitmeyiz (splash bootstrap'ı çalıştırır).
  if (auth.isUnknown) return location == AppRoutes.splash ? null : AppRoutes.splash;

  // Kayıt yarım kaldıysa (tempToken elde) tek çıkış yolu kayıt ekranıdır.
  //
  // 🔴 **Hukuki metin ekranları istisnadır (12.17).** Kural olmasaydı onay
  // kutusunun yanındaki "oku" bağlantısı kullanıcıyı kayıt ekranına geri
  // fırlatırdı ve geriye **okumadan onaylamaktan başka seçenek kalmazdı** —
  // yani KVKK bloğunun tamamı boşa giderdi. Belge uçları zaten anonim.
  if (auth.isRegistering) {
    if (AppRoutes.isLegalReading(location)) return null;
    return location == AppRoutes.register ? null : AppRoutes.register;
  }

  if (auth.isAuthenticated) {
    // Oturum açıkken giriş akışında durulmaz.
    if (location == AppRoutes.splash || AppRoutes.authFlow.contains(location)) {
      return AppRoutes.home;
    }
    return null;
  }

  // --- Anonim ---
  // Kayıt ekranı yalnız kayıt akışında anlamlı.
  if (location == AppRoutes.register) return AppRoutes.login;
  // Kod ekranına doğrudan gelinemez (kod gönderilmemişse telefon adımına dön).
  if (location == AppRoutes.otpVerify && !ref.read(otpFlowProvider).codeSent) {
    return AppRoutes.login;
  }
  if (AppRoutes.requiresAuth(location)) return AppRoutes.login;

  // Giriş önerisi: "misafir olarak devam" demeyen kullanıcı Ana Sayfa'da
  // tutulmaz. Bu kural iki durumu birlikte çözer: **ilk açılış** ve **çıkış
  // sonrası** (çıkışta misafir tercihi de sıfırlanır) → ikisinde de Giriş gelir.
  //
  // ⚠️ Yalnız Ana Sayfa'ya bakılır: misafir tercihini yapmamış kullanıcı zaten
  // sekmelere ulaşamadan Giriş'e düşer; diğer sekmeler kendi içlerinde davet
  // gösterir (bkz. `AppRoutes.protectedPrefixes` notu).
  if (!ref.read(authControllerProvider.notifier).hasChosenGuest &&
      (location == AppRoutes.splash || location == AppRoutes.home)) {
    return AppRoutes.login;
  }

  if (location == AppRoutes.splash) return AppRoutes.home;
  return null;
}
