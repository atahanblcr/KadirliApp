import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/router/app_routes.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/widgets/widgets.dart';
import '../../application/auth_controller.dart';

/// "Yetki gerekli" durumu (MOBILE_UX_PLAN §7): anonim kullanıcı korumalı bir
/// aksiyona basınca **hata değil davet** gösterilir.
///
/// 11.4'ten itibaren favori/ilan ver/kod gör/taksi ara gibi `[A]` aksiyonların
/// hepsi [ensureSignedIn] ile başlar.
Future<bool> showLoginRequiredSheet(
  BuildContext context, {
  String? reason,
}) async {
  final accepted = await showModalBottomSheet<bool>(
    context: context,
    showDragHandle: true,
    isScrollControlled: true,
    builder: (context) {
      final theme = Theme.of(context);
      return Padding(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.xl,
          0,
          AppSpacing.xl,
          AppSpacing.xl,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Center(
              child: Container(
                padding: const EdgeInsets.all(AppSpacing.lg),
                decoration: BoxDecoration(
                  color: theme.colorScheme.primaryContainer,
                  shape: BoxShape.circle,
                ),
                child: Icon(
                  Icons.lock_open_rounded,
                  size: 28,
                  color: theme.colorScheme.onPrimaryContainer,
                ),
              ),
            ),
            AppSpacing.gapLg,
            Text(
              'Bunun için giriş gerekiyor',
              style: theme.textTheme.titleMedium,
              textAlign: TextAlign.center,
            ),
            AppSpacing.gapSm,
            Text(
              reason ??
                  'Telefon numaranızla saniyeler içinde giriş yapabilirsiniz. '
                      'Şifre yok, tek kullanımlık kod yeterli.',
              style: theme.textTheme.bodyMedium?.copyWith(color: theme.palette.muted),
              textAlign: TextAlign.center,
            ),
            AppSpacing.gapXl,
            AppButton(
              label: 'Giriş yap',
              icon: Icons.login_rounded,
              expand: true,
              onPressed: () => Navigator.of(context).pop(true),
            ),
            AppSpacing.gapSm,
            AppButton.ghost(
              label: 'Şimdi değil',
              expand: true,
              onPressed: () => Navigator.of(context).pop(false),
            ),
          ],
        ),
      );
    },
  );

  return accepted ?? false;
}

/// Korumalı aksiyonların kapısı: oturum varsa `true`, yoksa nazik davet gösterip
/// (kabul edilirse) giriş ekranına götürür ve `false` döner.
///
/// Kullanım: `if (!await ensureSignedIn(context, ref)) return;`
Future<bool> ensureSignedIn(
  BuildContext context,
  WidgetRef ref, {
  String? reason,
}) async {
  if (ref.read(authControllerProvider).isAuthenticated) return true;

  final wantsLogin = await showLoginRequiredSheet(context, reason: reason);
  if (wantsLogin && context.mounted) context.push(AppRoutes.login);
  return false;
}
