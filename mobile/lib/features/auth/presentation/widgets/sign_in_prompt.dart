import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/router/app_routes.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/widgets/widgets.dart';

/// Oturum gerektiren **sekmelerin** anonim hali (Bildirimler, Profil).
///
/// **KARAR (11.4):** bu sekmeler router'da korumalı önek yapılmadı. Misafiri
/// sert yönlendirmeyle kabuktan atmak yerine sekmenin içinde davet gösteriyoruz:
/// alt sekme çubuğu yerinde kalır, kullanıcı "vazgeç" diyebilir ve 11.3'ün
/// "misafir gezinme birinci sınıf" kararı bozulmaz (MOBILE_UX_PLAN §7).
class SignInPrompt extends StatelessWidget {
  const SignInPrompt({
    super.key,
    required this.title,
    required this.message,
    this.icon = Icons.lock_open_rounded,
  });

  final String title;
  final String message;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return ListView(
      // Kaydırılabilir olmalı: pull-to-refresh ve küçük ekran taşması için.
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.xl,
        vertical: AppSpacing.huge,
      ),
      children: [
        Center(
          child: Container(
            padding: const EdgeInsets.all(AppSpacing.lg),
            decoration: BoxDecoration(
              color: theme.colorScheme.primaryContainer,
              shape: BoxShape.circle,
            ),
            child: Icon(icon, size: 32, color: theme.colorScheme.onPrimaryContainer),
          ),
        ),
        AppSpacing.gapLg,
        Text(title, style: theme.textTheme.titleMedium, textAlign: TextAlign.center),
        AppSpacing.gapSm,
        Text(
          message,
          style: theme.textTheme.bodyMedium?.copyWith(color: theme.palette.muted),
          textAlign: TextAlign.center,
        ),
        AppSpacing.gapXl,
        AppButton(
          label: 'Giriş yap',
          icon: Icons.login_rounded,
          expand: true,
          onPressed: () => context.push(AppRoutes.login),
        ),
      ],
    );
  }
}
