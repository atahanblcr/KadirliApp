import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/utils/app_date.dart';
import '../../data/models/app_notification.dart';

/// Okunmamış göstergesinin anahtarı — ekran testleri bunu sayıyor.
const unreadDotKey = ValueKey('notification-unread-dot');

/// Bildirim listesi satırı.
///
/// **Okunmamış bildirim görsel olarak baskın**: dolu ikon zemini, kalın başlık
/// ve solda ince bir nokta. Okunmuş satır sönük ama **silik değil** — geçmişe
/// bakmak da bu ekranın işi.
class NotificationTile extends StatelessWidget {
  const NotificationTile({super.key, required this.notification, this.onTap});

  final AppNotification notification;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final kind = NotificationKind.of(notification.relatedType);
    final unread = !notification.isRead;

    return Material(
      color: unread
          ? theme.colorScheme.primaryContainer.withValues(alpha: 0.35)
          : theme.colorScheme.surface,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppRadius.lg),
        side: BorderSide(color: palette.border),
      ),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.md),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 40,
                height: 40,
                decoration: BoxDecoration(
                  color: unread
                      ? theme.colorScheme.primary
                      : theme.colorScheme.primaryContainer,
                  shape: BoxShape.circle,
                ),
                child: Icon(
                  kind.icon,
                  size: 20,
                  color: unread
                      ? theme.colorScheme.onPrimary
                      : theme.colorScheme.onPrimaryContainer,
                ),
              ),
              AppSpacing.wGapMd,
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // ⚠️ Dar sütunda `Row` içindeki çıplak `Text` bu projenin
                    // tekrar eden taşma tuzağı (11.7-11.11) → `Wrap` + kısaltma.
                    Wrap(
                      spacing: AppSpacing.sm,
                      crossAxisAlignment: WrapCrossAlignment.center,
                      children: [
                        Text(
                          kind.label,
                          style: theme.textTheme.labelSmall?.copyWith(
                            color: palette.muted,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        Text(
                          notification.createdAt == null
                              ? ''
                              : AppDate.relative(notification.createdAt!),
                          style: theme.textTheme.labelSmall?.copyWith(
                            color: palette.muted,
                          ),
                        ),
                      ],
                    ),
                    AppSpacing.gapXs,
                    Text(
                      notification.title,
                      style: theme.textTheme.titleSmall?.copyWith(
                        fontWeight: unread ? FontWeight.w700 : FontWeight.w600,
                      ),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    if (notification.body.trim().isNotEmpty) ...[
                      AppSpacing.gapXs,
                      Text(
                        notification.body,
                        style: theme.textTheme.bodySmall?.copyWith(
                          color: palette.muted,
                        ),
                        maxLines: 3,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ],
                  ],
                ),
              ),
              if (unread) ...[
                AppSpacing.wGapSm,
                Padding(
                  padding: const EdgeInsets.only(top: AppSpacing.sm),
                  child: Semantics(
                    label: 'Okunmadı',
                    child: Container(
                      // ⚠️ `find.bySemanticsLabel` semantik ağaç kapalıyken 0
                      // döner (11.8 tuzağı) → testler bu anahtarı kullanıyor.
                      key: unreadDotKey,
                      width: 10,
                      height: 10,
                      decoration: BoxDecoration(
                        color: theme.colorScheme.primary,
                        shape: BoxShape.circle,
                      ),
                    ),
                  ),
                ),
              ] else if (notification.hasTarget) ...[
                AppSpacing.wGapSm,
                Icon(
                  Icons.chevron_right_rounded,
                  size: 20,
                  color: palette.muted,
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
