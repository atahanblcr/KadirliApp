import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/utils/utils.dart';
import '../../../../core/widgets/widgets.dart';
import '../../data/models/announcement.dart';

/// Duyuru kartı — Ana Sayfa vitrininde ve (11.6) duyuru listesinde aynı bileşen.
///
/// Öncelik rengi metinle birlikte verilir ("Acil" etiketi): renk körü kullanıcı
/// için tek başına renk yeterli değil (MOBILE_UX_PLAN §0 erişilebilirlik).
class AnnouncementTile extends StatelessWidget {
  const AnnouncementTile({super.key, required this.announcement, this.onTap, this.now});

  final Announcement announcement;
  final VoidCallback? onTap;

  /// Yalnız test için: "3 saat önce" hesabının dayandığı an. Üretimde null → gerçek saat.
  /// (`EventCard` deseni. Golden testinde sabitlenmezse referans görüntü HER GÜN kırılır.)
  final DateTime? now;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final priority = announcement.priorityLevel;
    final stripe = switch (priority) {
      AnnouncementPriority.urgent => palette.danger,
      AnnouncementPriority.high => palette.warning,
      AnnouncementPriority.normal => null,
    };

    final published = announcement.publishedAt;

    return AppCard(
      onTap: onTap,
      accentStripe: stripe,
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (announcement.imageUrl != null) ...[
            AppNetworkImage(url: announcement.imageUrl, width: 56, height: 56),
            AppSpacing.wGapMd,
          ],
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    if (priority != AnnouncementPriority.normal) ...[
                      _PriorityChip(priority: priority),
                      AppSpacing.wGapSm,
                    ],
                    if (announcement.typeName != null)
                      Flexible(
                        child: Text(
                          announcement.typeName!,
                          style: theme.textTheme.labelMedium?.copyWith(
                            color: theme.colorScheme.primary,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                  ],
                ),
                AppSpacing.gapXs,
                Text(
                  announcement.title,
                  style: theme.textTheme.titleSmall,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
                if (announcement.body.isNotEmpty) ...[
                  AppSpacing.gapXs,
                  Text(
                    announcement.body,
                    style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
                if (published != null) ...[
                  AppSpacing.gapSm,
                  Text(AppDate.relative(published, now: now), style: theme.textTheme.labelSmall),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _PriorityChip extends StatelessWidget {
  const _PriorityChip({required this.priority});

  final AnnouncementPriority priority;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final (label, color) = switch (priority) {
      AnnouncementPriority.urgent => ('Acil', palette.danger),
      AnnouncementPriority.high => ('Önemli', palette.warning),
      AnnouncementPriority.normal => ('', palette.muted),
    };

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.sm, vertical: AppSpacing.xxs),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.14),
        borderRadius: AppRadius.rPill,
      ),
      child: Text(
        label,
        style: theme.textTheme.labelSmall?.copyWith(color: color, fontWeight: FontWeight.w700),
      ),
    );
  }
}
