import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/application/auth_controller.dart';
import '../../features/auth/application/otp_flow_controller.dart';
import '../../features/auth/presentation/otp_verify_screen.dart';
import '../../features/auth/presentation/phone_login_screen.dart';
import '../../features/auth/presentation/register_screen.dart';
import '../../features/auth/presentation/splash_screen.dart';
import '../../features/dev/presentation/design_preview_screen.dart';
import '../../features/dev/presentation/network_probe_screen.dart';
import '../../features/home/presentation/home_screen.dart';
import '../config/env.dart';
import '../widgets/widgets.dart';
import 'app_routes.dart';

/// Uygulama yönlendiricisi.
///
/// **Oturum yönlendirmesi tek yerde:** ekranlar "giriş yapılmadı" diye
/// `context.go` çağırmaz; durum değişir, [GoRouter.redirect] karar verir.
/// 11.4'te `StatefulShellRoute` (alt sekmeler) bu yapının içine girecek.
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
      GoRoute(
        path: AppRoutes.home,
        name: 'home',
        builder: (context, state) => const HomeScreen(),
      ),
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
  if (!ref.read(authControllerProvider.notifier).hasChosenGuest &&
      (location == AppRoutes.splash || location == AppRoutes.home)) {
    return AppRoutes.login;
  }

  if (location == AppRoutes.splash) return AppRoutes.home;
  return null;
}
