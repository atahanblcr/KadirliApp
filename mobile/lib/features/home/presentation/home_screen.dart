import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../core/config/env.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/widgets.dart';
import '../../settings/presentation/widgets/theme_mode_selector.dart';

/// Geçici karşılama ekranı.
///
/// ⚠️ 11.4'te bu ekranın yerini gerçek **Ana Sayfa (Hub)** alacak:
/// selamlama + acil şerit (nöbetçi eczane / kesinti) + modül ızgarası +
/// öne çıkan duyurular. Şu an yalnız tema/tipografi doğrulaması ve
/// geliştirici kısayolları için var.
class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

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
          Text('Merhaba 👋', style: theme.textTheme.displaySmall),
          AppSpacing.gapSm,
          Text(
            'Kadirli uygulamasına hoş geldiniz. Şehrin duyuruları, nöbetçi '
            'eczanesi, ilanları ve daha fazlası burada olacak.',
            style: theme.textTheme.bodyLarge?.copyWith(color: palette.muted),
          ),
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
                        'Faz 11.1 tamam: proje iskeleti ve tasarım sistemi hazır. '
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
                ],
              ),
            ),
          ],
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
