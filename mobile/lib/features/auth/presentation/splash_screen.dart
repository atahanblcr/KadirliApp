import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../application/auth_controller.dart';
import 'widgets/brand_header.dart';

/// Açılış ekranı: oturum kararı verilene kadar görünür.
///
/// Karar `AuthController.bootstrap()`'ta alınır (token var mı → profil taze
/// mi); yönlendirmeyi router yapar, bu ekran `context.go` çağırmaz.
class SplashScreen extends ConsumerStatefulWidget {
  const SplashScreen({super.key});

  @override
  ConsumerState<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends ConsumerState<SplashScreen> {
  @override
  void initState() {
    super.initState();
    // İlk kareden sonra: provider'lar hazır, `ref.read` güvenli.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) ref.read(authControllerProvider.notifier).bootstrap();
    });
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.xl),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const BrandHeader(),
              AppSpacing.gapXl,
              SizedBox(
                height: 22,
                width: 22,
                child: CircularProgressIndicator(
                  strokeWidth: 2.4,
                  color: theme.colorScheme.primary,
                ),
              ),
              AppSpacing.gapLg,
              Text(
                'Bağlanılıyor…',
                style: theme.textTheme.bodyMedium?.copyWith(color: theme.palette.muted),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
