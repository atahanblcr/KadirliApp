import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/config/env.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/phone.dart';
import '../../../core/widgets/widgets.dart';
import '../../auth/application/auth_controller.dart';
import '../../auth/presentation/widgets/login_required_sheet.dart';
import '../../settings/presentation/widgets/theme_mode_selector.dart';

/// Geçici karşılama ekranı.
///
/// ⚠️ 11.4'te bu ekranın yerini gerçek **Ana Sayfa (Hub)** alacak:
/// selamlama + acil şerit (nöbetçi eczane / kesinti) + modül ızgarası +
/// öne çıkan duyurular. Şu an tema doğrulaması, oturum durumu ve geliştirici
/// kısayolları için var.
class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final user = ref.watch(currentUserProvider);

    return AppScaffold(
      titleWidget: const Text('Kadirli'),
      showBackButton: false,
      actions: [
        IconButton(
          onPressed: () {},
          icon: const Icon(Icons.settings_rounded),
          tooltip: 'Ayarlar',
        ),
        AppSpacing.wGapSm,
      ],
      body: ListView(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.lg,
          AppSpacing.sm,
          AppSpacing.lg,
          AppSpacing.xxl,
        ),
        children: [
          Text(
            user == null ? 'Merhaba 👋' : 'Merhaba ${user.displayName} 👋',
            style: theme.textTheme.displaySmall,
          ),
          AppSpacing.gapSm,
          Text(
            'Kadirli uygulamasına hoş geldiniz. Şehrin duyuruları, nöbetçi '
            'eczanesi, ilanları ve daha fazlası burada olacak.',
            style: theme.textTheme.bodyLarge?.copyWith(color: palette.muted),
          ),
          AppSpacing.gapXl,

          const SectionHeader(title: 'Hesap'),
          const _AccountCard(),
          AppSpacing.gapXl,

          AppCard(
            accentStripe: palette.accent,
            child: Row(
              children: [
                Icon(Icons.construction_rounded, color: palette.accent),
                AppSpacing.wGapMd,
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Yapım aşamasında', style: theme.textTheme.titleSmall),
                      AppSpacing.gapXs,
                      Text(
                        'Faz 11.3 tamam: giriş/kayıt akışı çalışıyor. '
                        'Modül ekranları sırayla eklenecek.',
                        style: theme.textTheme.bodySmall,
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          AppSpacing.gapXl,

          const SectionHeader(title: 'Görünüm'),
          const ThemeModeSelector(),
          AppSpacing.gapXl,

          if (Env.showDevTools) ...[
            const SectionHeader(title: 'Geliştirici'),
            AppCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _InfoRow(label: 'Ortam', value: Env.flavor.name),
                  AppSpacing.gapSm,
                  _InfoRow(label: 'API', value: Env.apiBaseUrl),
                  AppSpacing.gapLg,
                  AppButton.ghost(
                    label: 'Tasarım sistemi önizlemesi',
                    icon: Icons.palette_rounded,
                    expand: true,
                    onPressed: () => context.push(AppRoutes.designPreview),
                  ),
                  AppSpacing.gapSm,
                  AppButton.ghost(
                    label: 'Ağ tanılama',
                    icon: Icons.lan_rounded,
                    expand: true,
                    onPressed: () => context.push(AppRoutes.networkProbe),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}

/// Oturum kartı: giriş yapıldıysa özet + çıkış, yapılmadıysa giriş daveti.
///
/// ⚠️ Gerçek profil/ayarlar ekranı 11.5'in işi; bu kart 11.3'ün oturum akışını
/// uçtan uca denenebilir kılmak için var (giriş → çıkış → tekrar giriş).
class _AccountCard extends ConsumerStatefulWidget {
  const _AccountCard();

  @override
  ConsumerState<_AccountCard> createState() => _AccountCardState();
}

class _AccountCardState extends ConsumerState<_AccountCard> {
  bool _loggingOut = false;

  Future<void> _logout() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Çıkış yapılsın mı?'),
        content: const Text(
          'Hesabınızdan çıkacaksınız. Uygulamayı misafir olarak kullanmaya '
          'devam edebilirsiniz.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Vazgeç'),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Çıkış yap'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    setState(() => _loggingOut = true);
    await ref.read(authControllerProvider.notifier).logout();
    if (mounted) setState(() => _loggingOut = false);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final user = ref.watch(currentUserProvider);

    if (user == null) {
      return AppCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.person_outline_rounded, color: palette.muted),
                AppSpacing.wGapMd,
                Expanded(
                  child: Text(
                    'Misafir olarak geziyorsunuz',
                    style: theme.textTheme.titleSmall,
                  ),
                ),
              ],
            ),
            AppSpacing.gapSm,
            Text(
              'İlan vermek, favorilere eklemek ve bildirim almak için giriş yapın.',
              style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
            ),
            AppSpacing.gapLg,
            AppButton(
              label: 'Giriş yap',
              icon: Icons.login_rounded,
              expand: true,
              onPressed: () => context.push(AppRoutes.login),
            ),
            AppSpacing.gapSm,
            // Korumalı aksiyon denemesi: nazik davet akışı (11.4+ deseni).
            AppButton.ghost(
              label: 'Profilim',
              icon: Icons.badge_outlined,
              expand: true,
              onPressed: () => ensureSignedIn(
                context,
                ref,
                reason: 'Profilinizi görmek için giriş yapmanız gerekiyor.',
              ),
            ),
          ],
        ),
      );
    }

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                backgroundColor: theme.colorScheme.primaryContainer,
                child: Text(
                  user.displayName.substring(0, 1).toUpperCase(),
                  style: theme.textTheme.titleMedium?.copyWith(
                    color: theme.colorScheme.onPrimaryContainer,
                  ),
                ),
              ),
              AppSpacing.wGapMd,
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(user.displayName, style: theme.textTheme.titleSmall),
                    Text(
                      AppPhone.display(user.phone),
                      style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
                    ),
                  ],
                ),
              ),
            ],
          ),
          if (user.primaryNeighborhoodName != null) ...[
            AppSpacing.gapMd,
            Row(
              children: [
                Icon(Icons.place_outlined, size: 18, color: palette.muted),
                AppSpacing.wGapSm,
                Text(user.primaryNeighborhoodName!, style: theme.textTheme.bodyMedium),
              ],
            ),
          ],
          AppSpacing.gapLg,
          AppButton.ghost(
            label: 'Çıkış yap',
            icon: Icons.logout_rounded,
            expand: true,
            loading: _loggingOut,
            onPressed: _loggingOut ? null : _logout,
          ),
        ],
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          width: 72,
          child: Text(label, style: theme.textTheme.labelMedium?.copyWith(
            color: theme.palette.muted,
          )),
        ),
        Expanded(child: Text(value, style: theme.textTheme.bodyMedium)),
      ],
    );
  }
}
