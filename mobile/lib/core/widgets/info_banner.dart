import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';

enum InfoBannerTone { info, success, warning, danger }

/// Form üstü / ekran içi bilgi-uyarı şeridi.
///
/// Hata durumunda `ErrorView` tüm ekranı kaplar; bu bileşen ise **kısmi**
/// mesajlar için: "oturumunuz doldu", "kod hatalı", "dev modda kod 123456".
/// Renk rolleri `AppPalette`'ten gelir (palet dışı renk yok).
class InfoBanner extends StatelessWidget {
  const InfoBanner({
    super.key,
    required this.message,
    this.tone = InfoBannerTone.info,
    this.icon,
    this.title,
    this.onClose,
  });

  final String message;
  final InfoBannerTone tone;
  final IconData? icon;
  final String? title;

  /// Verilirse sağda kapatma düğmesi çıkar (tek kullanımlık bildirimler).
  final VoidCallback? onClose;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final color = switch (tone) {
      InfoBannerTone.info => palette.info,
      InfoBannerTone.success => palette.success,
      InfoBannerTone.warning => palette.warning,
      InfoBannerTone.danger => palette.danger,
    };

    final defaultIcon = switch (tone) {
      InfoBannerTone.info => Icons.info_outline_rounded,
      InfoBannerTone.success => Icons.check_circle_outline_rounded,
      InfoBannerTone.warning => Icons.warning_amber_rounded,
      InfoBannerTone.danger => Icons.error_outline_rounded,
    };

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(AppSpacing.md),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.10),
        borderRadius: AppRadius.rMd,
        border: Border.all(color: color.withValues(alpha: 0.35)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon ?? defaultIcon, size: 20, color: color),
          AppSpacing.wGapMd,
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (title != null) ...[
                  Text(title!, style: theme.textTheme.titleSmall),
                  AppSpacing.gapXs,
                ],
                Text(message, style: theme.textTheme.bodyMedium),
              ],
            ),
          ),
          if (onClose != null) ...[
            AppSpacing.wGapSm,
            InkWell(
              onTap: onClose,
              borderRadius: AppRadius.rPill,
              child: Padding(
                padding: const EdgeInsets.all(AppSpacing.xs),
                child: Icon(Icons.close_rounded, size: 18, color: palette.muted),
              ),
            ),
          ],
        ],
      ),
    );
  }
}
