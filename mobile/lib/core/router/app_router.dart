import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/ads/presentation/ads_screen.dart';
import '../../features/announcements/presentation/announcement_detail_screen.dart';
import '../../features/announcements/presentation/announcements_screen.dart';
import '../../features/auth/application/auth_controller.dart';
import '../../features/auth/application/otp_flow_controller.dart';
import '../../features/auth/presentation/otp_verify_screen.dart';
import '../../features/auth/presentation/phone_login_screen.dart';
import '../../features/auth/presentation/register_screen.dart';
import '../../features/auth/presentation/splash_screen.dart';
import '../../features/common/presentation/module_placeholder_screen.dart';
import '../../features/dev/presentation/design_preview_screen.dart';
import '../../features/dev/presentation/network_probe_screen.dart';
import '../../features/guide/presentation/guide_item_detail_screen.dart';
import '../../features/guide/presentation/guide_screen.dart';
import '../../features/home/presentation/home_screen.dart';
import '../../features/notifications/presentation/notifications_screen.dart';
import '../../features/pharmacies/presentation/pharmacies_screen.dart';
import '../../features/pharmacies/presentation/pharmacy_detail_screen.dart';
import '../../features/power_outages/presentation/power_outage_detail_screen.dart';
import '../../features/power_outages/presentation/power_outages_screen.dart';
import '../../features/profile/presentation/account_delete_screen.dart';
import '../../features/profile/presentation/profile_edit_screen.dart';
import '../../features/profile/presentation/profile_screen.dart';
import '../../features/settings/presentation/settings_screen.dart';
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

      // --- Henüz yazılmamış modüller: kayıttan otomatik üretilir ---
      // Bir modül gerçeklenince `AppModule.ready` true olur, kendi rotası
      // yukarıya yazılır ve buradaki "yakında" ekranı devreden çıkar.
      for (final module in kAppModules)
        if (module.route != AppRoutes.ads && !module.ready)
          GoRoute(
            path: module.route,
            name: 'module-${module.id}',
            builder: (context, state) => ModulePlaceholderScreen(module: module),
          ),

      // --- Geliştirici (yalnız debug) ---
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
  if (auth.isRegistering) {
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
