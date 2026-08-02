import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/utils/utils.dart';
import '../../../../core/widgets/widgets.dart';
import '../../data/models/complaint.dart';

/// "Bildirimlerim" listesindeki kart.
///
/// Kartın asıl işi **durum takibi**: rozet hem renk hem metin taşır (renk körü
/// kullanıcı için renk tek başına yetmez — 11.9 `AdStatus` kararı) ve varsa
/// yönetici notu ayrı bir kutuda öne çıkar; kullanıcının beklediği cevap odur.
class ComplaintCard extends StatelessWidget {
  const ComplaintCard({super.key, required this.complaint});

  final Complaint complaint;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final status = complaint.statusValue;

    final statusColor = switch (status) {
      ComplaintStatus.pending => palette.warning,
      ComplaintStatus.inProgress => palette.info,
      ComplaintStatus.resolved => palette.success,
      ComplaintStatus.rejected => palette.danger,
      ComplaintStatus.unknown => palette.muted,
    };

    return AppCard(
      accentStripe: status.isClosed ? null : statusColor,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ⚠️ Dar sütunda `Row` içindeki çıplak `Text` bu projenin tekrar eden
          // taşma tuzağı (11.7→11.11) → rozet + tarih `Wrap` içinde.
          Wrap(
            spacing: AppSpacing.sm,
            runSpacing: AppSpacing.xs,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              _StatusBadge(status: status, color: statusColor),
              if (complaint.typeLabel != null)
                Text(
                  complaint.typeLabel!,
                  style: theme.textTheme.labelSmall?.copyWith(
                    color: palette.muted,
                  ),
                ),
              Text(
                AppDate.relative(complaint.createdAt),
                style: theme.textTheme.labelSmall?.copyWith(
                  color: palette.muted,
                ),
              ),
            ],
          ),
          AppSpacing.gapMd,

          Text(
            complaint.subject,
            style: theme.textTheme.titleSmall,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
          ),
          if (complaint.message.trim().isNotEmpty) ...[
            AppSpacing.gapXs,
            Text(
              complaint.message.trim(),
              style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
              maxLines: 3,
              overflow: TextOverflow.ellipsis,
            ),
          ],

          if (complaint.hasAnswer) ...[
            AppSpacing.gapMd,
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(AppSpacing.md),
              decoration: BoxDecoration(
                color: theme.colorScheme.primaryContainer.withValues(alpha: .4),
                borderRadius: AppRadius.rSm,
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(
                        Icons.mark_email_read_outlined,
                        size: 14,
                        color: theme.colorScheme.primary,
                      ),
                      AppSpacing.wGapXs,
                      Flexible(
                        child: Text(
                          'Yetkili yanıtı',
                          style: theme.textTheme.labelSmall?.copyWith(
                            color: theme.colorScheme.primary,
                          ),
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ),
                  AppSpacing.gapXs,
                  Text(complaint.answer!, style: theme.textTheme.bodyMedium),
                ],
              ),
            ),
          ] else if (!status.isClosed) ...[
            AppSpacing.gapMd,
            Text(
              status.description,
              style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
            ),
          ],

          if (complaint.resolvedAt != null) ...[
            AppSpacing.gapSm,
            Text(
              'Sonuçlandırıldı: ${AppDate.dateTime(complaint.resolvedAt!)}',
              style: theme.textTheme.labelSmall?.copyWith(color: palette.muted),
            ),
          ],
        ],
      ),
    );
  }
}

class _StatusBadge extends StatelessWidget {
  const _StatusBadge({required this.status, required this.color});

  final ComplaintStatus status;
  final Color color;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: AppSpacing.xxs,
      ),
      decoration: BoxDecoration(
        color: color.withValues(alpha: .12),
        borderRadius: AppRadius.rPill,
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(status.icon, size: 12, color: color),
          AppSpacing.wGapXs,
          Text(
            status.label,
            style: theme.textTheme.labelSmall?.copyWith(color: color),
          ),
        ],
      ),
    );
  }
}
