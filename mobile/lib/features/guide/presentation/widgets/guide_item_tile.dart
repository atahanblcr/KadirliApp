import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/widgets/widgets.dart';
import '../../data/models/guide_item.dart';

/// Rehber kaydı kartı.
///
/// Sağdaki **yeşil arama düğmesi** bilinçli: rehberin asıl işi "numarayı bul,
/// ara" — kullanıcıyı detaya girmeye zorlamak fazladan iki dokunuş olurdu.
/// Telefon yoksa düğme hiç çizilmez.
class GuideItemTile extends StatelessWidget {
  const GuideItemTile({
    super.key,
    required this.item,
    this.onTap,
    this.onCall,
  });

  final GuideItem item;
  final VoidCallback? onTap;
  final VoidCallback? onCall;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return AppCard(
      onTap: onTap,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if ((item.categoryName ?? '').trim().isNotEmpty) ...[
                  Text(
                    item.categoryName!.trim(),
                    style: theme.textTheme.labelMedium?.copyWith(
                      color: theme.colorScheme.primary,
                    ),
                  ),
                  AppSpacing.gapXs,
                ],
                Text(
                  item.name,
                  style: theme.textTheme.titleSmall,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
                if (item.hasPhone) ...[
                  AppSpacing.gapXs,
                  Row(
                    children: [
                      Icon(Icons.call_rounded, size: 14, color: palette.muted),
                      AppSpacing.wGapXs,
                      Text(
                        item.phone!.trim(),
                        style: theme.textTheme.bodySmall,
                      ),
                    ],
                  ),
                ],
                if ((item.address ?? '').trim().isNotEmpty) ...[
                  AppSpacing.gapXs,
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Icon(Icons.place_rounded, size: 14, color: palette.muted),
                      AppSpacing.wGapXs,
                      Expanded(
                        child: Text(
                          item.address!.trim(),
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: palette.muted,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ),
                ],
                if ((item.workingHours ?? '').trim().isNotEmpty) ...[
                  AppSpacing.gapXs,
                  Row(
                    children: [
                      Icon(Icons.schedule_rounded, size: 14, color: palette.muted),
                      AppSpacing.wGapXs,
                      Text(
                        item.workingHours!.trim(),
                        style: theme.textTheme.labelSmall?.copyWith(
                          color: palette.muted,
                        ),
                      ),
                    ],
                  ),
                ],
              ],
            ),
          ),
          if (onCall != null) ...[
            AppSpacing.wGapMd,
            Semantics(
              button: true,
              label: '${item.name} numarasını ara',
              child: Material(
                color: theme.colorScheme.primaryContainer,
                shape: const CircleBorder(),
                child: InkWell(
                  onTap: onCall,
                  customBorder: const CircleBorder(),
                  child: SizedBox(
                    width: AppA11y.minTapSize,
                    height: AppA11y.minTapSize,
                    child: Icon(
                      Icons.call_rounded,
                      color: theme.colorScheme.onPrimaryContainer,
                    ),
                  ),
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }
}
