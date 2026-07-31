import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/utils/utils.dart';
import '../../../../core/widgets/widgets.dart';
import '../../data/models/power_outage.dart';

/// Kesinti kartı — listede ve (özet olarak) detayda aynı görsel dil.
///
/// Durum **hem renk hem metinle** verilir ("Sürüyor" / "Planlı" / "Sona erdi"):
/// renk körü kullanıcı için renk tek başına yeterli değil.
class PowerOutageTile extends StatelessWidget {
  const PowerOutageTile({
    super.key,
    required this.outage,
    this.onTap,
    this.now,
    this.highlightNeighborhood = false,
  });

  final PowerOutage outage;
  final VoidCallback? onTap;

  /// Testlerde sabitlenebilsin diye dışarıdan verilebilir.
  final DateTime? now;

  /// Kullanıcının kendi mahallesi mi (listede öne çıkarılır).
  final bool highlightNeighborhood;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final status = outage.status(now: now);
    // Rozet kısa tutuluyor: liste başlıkları zaten "Şu an sürüyor" /
    // "Planlanan" diyor, aynı cümleyi kartta tekrarlamak gürültü olurdu.
    final (statusLabel, statusColor) = switch (status) {
      PowerOutageStatus.active => ('Sürüyor', palette.danger),
      PowerOutageStatus.upcoming => ('Planlı', palette.warning),
      PowerOutageStatus.past => ('Sona erdi', palette.muted),
    };

    final remaining = outage.remaining(now: now);
    final countdown = switch (status) {
      PowerOutageStatus.active when remaining != null =>
        'Bitmesine ${AppDate.duration(remaining)}',
      PowerOutageStatus.upcoming when remaining != null =>
        '${AppDate.duration(remaining)} sonra başlıyor',
      _ => null,
    };

    return AppCard(
      onTap: onTap,
      accentStripe: status == PowerOutageStatus.past ? null : statusColor,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              _StatusBadge(label: statusLabel, color: statusColor),
              if (highlightNeighborhood) ...[
                AppSpacing.wGapSm,
                _StatusBadge(
                  label: 'Mahalleniz',
                  color: theme.colorScheme.primary,
                  icon: Icons.home_rounded,
                ),
              ],
            ],
          ),
          AppSpacing.gapSm,
          Row(
            children: [
              Icon(Icons.place_rounded, size: 18, color: palette.muted),
              AppSpacing.wGapSm,
              Expanded(
                child: Text(
                  outage.placeLabel,
                  style: theme.textTheme.titleSmall,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
            ],
          ),
          AppSpacing.gapSm,
          Row(
            children: [
              Icon(Icons.schedule_rounded, size: 18, color: palette.muted),
              AppSpacing.wGapSm,
              Expanded(
                child: Text(
                  AppDate.range(outage.startTime, outage.endTime),
                  style: theme.textTheme.bodyMedium,
                ),
              ),
            ],
          ),
          if (countdown != null) ...[
            AppSpacing.gapSm,
            Text(
              countdown,
              style: theme.textTheme.labelMedium?.copyWith(
                color: statusColor,
                fontWeight: FontWeight.w700,
              ),
            ),
          ],
          if ((outage.reason ?? '').trim().isNotEmpty) ...[
            AppSpacing.gapSm,
            Text(
              outage.reason!.trim(),
              style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
            ),
          ],
        ],
      ),
    );
  }
}

class _StatusBadge extends StatelessWidget {
  const _StatusBadge({required this.label, required this.color, this.icon});

  final String label;
  final Color color;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: AppSpacing.xxs,
      ),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.14),
        borderRadius: AppRadius.rPill,
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 12, color: color),
            AppSpacing.wGapXs,
          ],
          Text(
            label,
            style: theme.textTheme.labelSmall?.copyWith(
              color: color,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}
