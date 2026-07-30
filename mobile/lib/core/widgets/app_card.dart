import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';

/// Uygulamanın tek kart bileşeni: yumuşak köşe, ince kenarlık, gölge yok.
///
/// [onTap] verilirse dokunulabilir olur (ripple + basılı ölçek yok — listelerde
/// çok kart olduğu için ripple yeterli geri bildirim).
class AppCard extends StatelessWidget {
  const AppCard({
    super.key,
    required this.child,
    this.onTap,
    this.padding = const EdgeInsets.all(AppSpacing.lg),
    this.color,
    this.borderColor,
    this.accentStripe,
    this.semanticLabel,
  });

  final Widget child;
  final VoidCallback? onTap;
  final EdgeInsetsGeometry padding;
  final Color? color;
  final Color? borderColor;

  /// Sol kenarda ince renk şeridi (modül/durum kodlaması için — palet dışı renk yok).
  final Color? accentStripe;

  final String? semanticLabel;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    const stripeWidth = 4.0;

    Widget content = Padding(
      padding: accentStripe == null
          ? padding
          : padding.add(const EdgeInsets.only(left: stripeWidth)),
      child: child,
    );

    if (accentStripe != null) {
      // Stack, boyunu içeriğe göre alır; şerit dikeyde onu doldurur.
      content = Stack(
        children: [
          content,
          PositionedDirectional(
            start: 0,
            top: 0,
            bottom: 0,
            width: stripeWidth,
            child: ColoredBox(color: accentStripe!),
          ),
        ],
      );
    }

    return Semantics(
      container: true,
      label: semanticLabel,
      button: onTap != null,
      child: Material(
        color: color ?? theme.colorScheme.surface,
        clipBehavior: Clip.antiAlias,
        shape: RoundedRectangleBorder(
          borderRadius: AppRadius.rMd,
          side: BorderSide(color: borderColor ?? palette.border),
        ),
        child: onTap == null
            ? content
            : InkWell(
                onTap: onTap,
                borderRadius: AppRadius.rMd,
                child: content,
              ),
      ),
    );
  }
}

/// Bölüm başlığı: "Modüller", "Öne çıkanlar" gibi.
class SectionHeader extends StatelessWidget {
  const SectionHeader({
    super.key,
    required this.title,
    this.subtitle,
    this.action,
    this.onActionTap,
  });

  final String title;

  /// Başlığın altındaki tek satırlık açıklama (bölümün ne işe yaradığı).
  final String? subtitle;

  /// Sağdaki metin bağlantısı ("Tümü").
  final String? action;
  final VoidCallback? onActionTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.md),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(title, style: theme.textTheme.titleMedium),
                if (subtitle != null)
                  Text(
                    subtitle!,
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.palette.muted,
                    ),
                  ),
              ],
            ),
          ),
          if (action != null)
            TextButton(
              onPressed: onActionTap,
              style: TextButton.styleFrom(
                foregroundColor: theme.colorScheme.primary,
                minimumSize: const Size(0, AppA11y.minTapSize),
                padding: const EdgeInsets.symmetric(horizontal: AppSpacing.sm),
              ),
              child: Text(action!, style: theme.textTheme.labelLarge?.copyWith(
                color: theme.colorScheme.primary,
              )),
            ),
        ],
      ),
    );
  }
}
