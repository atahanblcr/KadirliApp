import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../../auth/application/auth_controller.dart';
import '../../auth/presentation/widgets/sign_in_prompt.dart';

/// Profil sekmesi — oturum özeti + hesaba giden kısayollar.
///
/// Veriyi kendi çekmez: tek kaynak `currentUserProvider` (oturum açılışında ve
/// her `PATCH /v1/users/me` yanıtında tazelenir). Aşağı çekince `users/me`
/// yeniden okunur — başka cihazdan yapılan değişiklik böyle görünür.
class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final user = ref.watch(currentUserProvider);

    if (user == null) {
      return const AppScaffold(
        title: 'Profil',
        showBackButton: false,
        body: SignInPrompt(
          icon: Icons.person_outline_rounded,
          title: 'Hesabınıza giriş yapın',
          message:
              'İlan vermek, favorilerinizi saklamak ve bildirim almak için '
              'telefon numaranızla saniyeler içinde giriş yapabilirsiniz.',
        ),
      );
    }

    return AppScaffold(
      title: 'Profil',
      showBackButton: false,
      onRefresh: () => ref.read(authControllerProvider.notifier).refreshCurrentUser(),
      actions: [
        IconButton(
          onPressed: () => context.push(AppRoutes.settings),
          icon: const Icon(Icons.settings_rounded),
          tooltip: 'Ayarlar',
        ),
        AppSpacing.wGapSm,
      ],
      body: ListView(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.lg,
          AppSpacing.xl,
          AppSpacing.lg,
          AppSpacing.xxl,
        ),
        children: [
          Center(
            child: UserAvatar(
              initial: user.initial,
              photoUrl: user.profilePhotoUrl,
              radius: 40,
            ),
          ),
          AppSpacing.gapLg,
          Text(
            user.displayName,
            style: theme.textTheme.titleLarge,
            textAlign: TextAlign.center,
          ),
          AppSpacing.gapXs,
          Text(
            AppPhone.display(user.phone),
            style: theme.textTheme.bodyMedium?.copyWith(color: palette.muted),
            textAlign: TextAlign.center,
          ),
          if (user.primaryNeighborhoodName != null) ...[
            AppSpacing.gapXs,
            // Ad olduğu gibi yazılır: liste "Yenimahalle" gibi adlar ve
            // ileride köyler de içeriyor → "Mahallesi" eki eklenmez.
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(Icons.place_outlined, size: 16, color: palette.muted),
                AppSpacing.wGapXs,
                Text(
                  user.primaryNeighborhoodName!,
                  style: theme.textTheme.bodyMedium?.copyWith(color: palette.muted),
                ),
              ],
            ),
          ],
          if (user.createdAt != null) ...[
            AppSpacing.gapXs,
            Text(
              '${AppDate.date(user.createdAt!)} tarihinden beri aramızda',
              style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
              textAlign: TextAlign.center,
            ),
          ],
          AppSpacing.gapLg,
          Center(
            child: AppButton.ghost(
              label: 'Profili düzenle',
              icon: Icons.edit_outlined,
              size: AppButtonSize.small,
              onPressed: () => context.push(AppRoutes.profileEdit),
            ),
          ),
          AppSpacing.gapXl,

          const SectionHeader(title: 'Hesabım'),
          AppCard(
            padding: EdgeInsets.zero,
            child: Column(
              children: [
                _ProfileRow(
                  icon: Icons.settings_rounded,
                  label: 'Ayarlar ve kontrol',
                  onTap: () => context.push(AppRoutes.settings),
                ),
                _ProfileDivider(color: palette.border),
                _ProfileRow(
                  icon: Icons.notifications_outlined,
                  label: 'Bildirim tercihleri',
                  onTap: () => context.push(AppRoutes.settings),
                ),
                _ProfileDivider(color: palette.border),
                _ProfileRow(
                  icon: Icons.sell_outlined,
                  label: 'İlanlarım',
                  trailing: 'Yakında',
                  onTap: () => context.go(AppRoutes.ads),
                ),
                _ProfileDivider(color: palette.border),
                _ProfileRow(
                  icon: Icons.favorite_outline_rounded,
                  label: 'Favorilerim',
                  trailing: 'Yakında',
                  onTap: () => context.go(AppRoutes.ads),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ProfileRow extends StatelessWidget {
  const _ProfileRow({
    required this.icon,
    required this.label,
    required this.onTap,
    this.trailing,
  });

  final IconData icon;
  final String label;
  final String? trailing;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.lg,
          vertical: AppSpacing.md,
        ),
        child: Row(
          children: [
            Icon(icon, size: 20, color: theme.colorScheme.primary),
            AppSpacing.wGapMd,
            Expanded(child: Text(label, style: theme.textTheme.bodyLarge)),
            if (trailing != null)
              Text(trailing!, style: theme.textTheme.labelSmall),
            AppSpacing.wGapSm,
            Icon(Icons.chevron_right_rounded, size: 20, color: theme.palette.muted),
          ],
        ),
      ),
    );
  }
}

class _ProfileDivider extends StatelessWidget {
  const _ProfileDivider({required this.color});

  final Color color;

  @override
  Widget build(BuildContext context) =>
      Divider(height: 1, thickness: 1, color: color);
}
