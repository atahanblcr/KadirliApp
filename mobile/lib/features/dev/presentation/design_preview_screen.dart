import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/widgets.dart';
import '../../settings/presentation/widgets/theme_mode_selector.dart';

/// Yaşayan stil kılavuzu (yalnız dev build'de erişilir).
///
/// Amacı: tema token'larının açık/koyu temada gerçekten doğru göründüğünü
/// tek ekranda doğrulamak ve sonraki fazlarda "hangi bileşen var?" sorusunu
/// cevaplamak. Ürün akışının parçası değildir.
class DesignPreviewScreen extends StatelessWidget {
  const DesignPreviewScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final palette = theme.palette;

    return AppScaffold(
      title: 'Tasarım sistemi',
      body: ListView(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.lg,
          AppSpacing.lg,
          AppSpacing.lg,
          AppSpacing.huge,
        ),
        children: [
          const ThemeModeSelector(),
          AppSpacing.gapXl,

          const SectionHeader(title: 'Renkler'),
          Wrap(
            spacing: AppSpacing.sm,
            runSpacing: AppSpacing.sm,
            children: [
              _Swatch('Primary', scheme.primary, scheme.onPrimary),
              _Swatch('Tint', scheme.primaryContainer, scheme.onPrimaryContainer),
              _Swatch('Accent', palette.accent, Colors.white),
              _Swatch('Success', palette.success, Colors.white),
              _Swatch('Info', palette.info, Colors.white),
              _Swatch('Warning', palette.warning, Colors.black87),
              _Swatch('Danger', palette.danger, Colors.white),
              _Swatch('Surface', scheme.surface, scheme.onSurface),
            ],
          ),
          AppSpacing.gapXl,

          const SectionHeader(title: 'Tipografi'),
          AppCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Display 28 — Çığır', style: theme.textTheme.displaySmall),
                AppSpacing.gapSm,
                Text('H1 22 — Şehrin gündemi', style: theme.textTheme.titleLarge),
                AppSpacing.gapSm,
                Text('H2 18 — Nöbetçi eczane', style: theme.textTheme.titleMedium),
                AppSpacing.gapSm,
                Text('Body 16 — Ağıl Mahallesi’nde yağış bekleniyor.',
                    style: theme.textTheme.bodyLarge),
                AppSpacing.gapSm,
                Text('Body-sm 14 — İlan güncellendi, yeniden incelemede.',
                    style: theme.textTheme.bodyMedium),
                AppSpacing.gapSm,
                Text('Caption 13 — 2 saat önce', style: theme.textTheme.bodySmall),
                AppSpacing.gapSm,
                Text('LABEL 12 — ONAYLANDI', style: theme.textTheme.labelMedium),
              ],
            ),
          ),
          AppSpacing.gapXl,

          const SectionHeader(title: 'Butonlar'),
          Wrap(
            spacing: AppSpacing.sm,
            runSpacing: AppSpacing.sm,
            children: [
              AppButton(label: 'Kaydet', onPressed: () {}),
              AppButton(
                label: 'Ara',
                icon: Icons.phone_rounded,
                variant: AppButtonVariant.accent,
                onPressed: () {},
              ),
              AppButton.ghost(label: 'Vazgeç', onPressed: () {}),
              AppButton.danger(label: 'Sil', icon: Icons.delete_rounded, onPressed: () {}),
              const AppButton(label: 'Pasif', onPressed: null),
              AppButton(label: 'Yükleniyor', loading: true, onPressed: () {}),
              AppButton(label: 'Küçük', size: AppButtonSize.small, onPressed: () {}),
            ],
          ),
          AppSpacing.gapLg,
          AppButton(label: 'Tam genişlik', expand: true, onPressed: () {}),
          AppSpacing.gapXl,

          const SectionHeader(title: 'Kartlar'),
          AppCard(
            onTap: () {},
            child: Row(
              children: [
                Icon(Icons.local_pharmacy_rounded, color: scheme.primary),
                AppSpacing.wGapMd,
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Dokunulabilir kart', style: theme.textTheme.titleSmall),
                      Text('Alt açıklama satırı', style: theme.textTheme.bodySmall),
                    ],
                  ),
                ),
                Icon(Icons.chevron_right_rounded, color: palette.muted),
              ],
            ),
          ),
          AppSpacing.gapMd,
          AppCard(
            accentStripe: palette.danger,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Reddedildi', style: theme.textTheme.titleSmall),
                Text('Şerit renkli kart (ör. ilan red gerekçesi).',
                    style: theme.textTheme.bodySmall),
              ],
            ),
          ),
          AppSpacing.gapXl,

          const SectionHeader(title: 'Durumlar'),
          const OfflineBanner(),
          AppSpacing.gapMd,
          SizedBox(height: 220, child: const LoadingView(itemCount: 2)),
          AppSpacing.gapMd,
          SizedBox(
            height: 260,
            child: EmptyView(
              title: 'Henüz ilan yok',
              message: 'İlk ilanı siz verin.',
              icon: Icons.sell_rounded,
              actionLabel: 'İlan ver',
              onAction: () {},
            ),
          ),
          AppSpacing.gapMd,
          SizedBox(
            height: 300,
            child: ErrorView(traceId: '00-abc123-def456-01', onRetry: () {}),
          ),
        ],
      ),
    );
  }
}

class _Swatch extends StatelessWidget {
  const _Swatch(this.name, this.color, this.onColor);

  final String name;
  final Color color;
  final Color onColor;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 104,
      height: 64,
      padding: const EdgeInsets.all(AppSpacing.sm),
      alignment: Alignment.bottomLeft,
      decoration: BoxDecoration(
        color: color,
        borderRadius: AppRadius.rSm,
        border: Border.all(color: Theme.of(context).palette.border),
      ),
      child: Text(
        name,
        style: Theme.of(context).textTheme.labelMedium?.copyWith(color: onColor),
      ),
    );
  }
}
