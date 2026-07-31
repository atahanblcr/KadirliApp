import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/widgets/widgets.dart';

/// Eczane/nöbet kartı — listede ve nöbet vitrininde aynı görsel dil.
class PharmacyTile extends StatelessWidget {
  const PharmacyTile({
    super.key,
    required this.name,
    this.address,
    this.pharmacistName,
    this.workingHours,
    this.badge,
    this.badgeColor,
    this.onTap,
  });

  final String name;
  final String? address;
  final String? pharmacistName;
  final String? workingHours;

  /// "Bugün nöbetçi" / "19:00 - 09:00" gibi kısa rozet.
  final String? badge;
  final Color? badgeColor;

  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final color = badgeColor ?? theme.colorScheme.primary;

    return AppCard(
      onTap: onTap,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(AppSpacing.sm),
            decoration: BoxDecoration(
              color: theme.colorScheme.primaryContainer,
              borderRadius: AppRadius.rSm,
            ),
            child: Icon(
              Icons.local_pharmacy_rounded,
              size: 22,
              color: theme.colorScheme.onPrimaryContainer,
            ),
          ),
          AppSpacing.wGapMd,
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (badge != null) ...[
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: AppSpacing.sm,
                      vertical: AppSpacing.xxs,
                    ),
                    decoration: BoxDecoration(
                      color: color.withValues(alpha: 0.14),
                      borderRadius: AppRadius.rPill,
                    ),
                    child: Text(
                      badge!,
                      style: theme.textTheme.labelSmall?.copyWith(
                        color: color,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                  AppSpacing.gapSm,
                ],
                Text(
                  name,
                  style: theme.textTheme.titleSmall,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
                if ((address ?? '').trim().isNotEmpty) ...[
                  AppSpacing.gapXs,
                  Text(
                    address!.trim(),
                    style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
                if ((pharmacistName ?? '').trim().isNotEmpty) ...[
                  AppSpacing.gapXs,
                  Text(
                    pharmacistName!.trim(),
                    style: theme.textTheme.labelSmall?.copyWith(color: palette.muted),
                  ),
                ],
                if ((workingHours ?? '').trim().isNotEmpty) ...[
                  AppSpacing.gapXs,
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Icon(Icons.schedule_rounded, size: 14, color: palette.muted),
                      AppSpacing.wGapXs,
                      // Expanded şart: bu satır "Nöbet ertesi gün 09:00'a kadar
                      // sürer" gibi uzun bir cümle de taşıyabiliyor (widget
                      // testi taşmayı yakaladı).
                      Expanded(
                        child: Text(
                          workingHours!.trim(),
                          style: theme.textTheme.labelSmall?.copyWith(
                            color: palette.muted,
                          ),
                        ),
                      ),
                    ],
                  ),
                ],
              ],
            ),
          ),
          if (onTap != null)
            Icon(Icons.chevron_right_rounded, color: palette.muted),
        ],
      ),
    );
  }
}
