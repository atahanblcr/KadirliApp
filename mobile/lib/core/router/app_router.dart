import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/dev/presentation/design_preview_screen.dart';
import '../../features/home/presentation/home_screen.dart';
import '../config/env.dart';
import '../widgets/widgets.dart';
import 'app_routes.dart';

/// Uygulama yönlendiricisi.
///
/// 11.3'te auth redirect (korumalı rotalar), 11.4'te `StatefulShellRoute` ile
/// alt sekme kabuğu buraya eklenecek.
final routerProvider = Provider<GoRouter>((ref) {
  return GoRouter(
    initialLocation: AppRoutes.home,
    debugLogDiagnostics: Env.showDevTools,
    routes: [
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
});
