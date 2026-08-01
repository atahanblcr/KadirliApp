import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/widgets/widgets.dart';
import '../../data/models/death_notice.dart';

/// Vefat ilanı kartı.
///
/// **Sade ve saygılı** (MOBILE_UX_PLAN + 11.11 kararı): renkli rozet, sayaç ya
/// da "yeni" etiketi yok. Tek vurgu, kullanıcının gerçekten aradığı bilgi olan
/// **cenaze namazının zamanı**; bugünkü cenazede kartın solunda ince bir şerit
/// belirir, o kadar.
class DeathNoticeTile extends StatelessWidget {
  const DeathNoticeTile({
    super.key,
    required this.notice,
    this.onTap,
    this.now,
  });

  final DeathNotice notice;
  final VoidCallback? onTap;

  /// Testlerde "bugün"ü sabitlemek için.
  final DateTime? now;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final isToday = notice.isToday(now: now);

    final places = [
      if (notice.mosqueName != null) notice.mosqueName!,
      if (notice.cemeteryName != null) notice.cemeteryName!,
    ].join(' · ');

    return AppCard(
      onTap: onTap,
      accentStripe: isToday ? theme.colorScheme.primary : null,
      semanticLabel:
          '${notice.deceasedName}, cenaze namazı ${notice.funeralLabel(now: now)}',
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _Portrait(url: notice.photoUrl),
          AppSpacing.wGapMd,
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  notice.deceasedName,
                  style: theme.textTheme.titleMedium,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
                AppSpacing.gapXs,
                // ⚠️ Dar sütunda `Row` içindeki çıplak `Text` bu projede dört
                // fazda taştı → ikon + `Flexible` + ellipsis.
                Row(
                  children: [
                    Icon(
                      Icons.schedule_rounded,
                      size: 15,
                      color: isToday ? theme.colorScheme.primary : palette.muted,
                    ),
                    AppSpacing.wGapXs,
                    Flexible(
                      child: Text(
                        notice.funeralLabel(now: now),
                        style: theme.textTheme.bodyMedium?.copyWith(
                          color: isToday
                              ? theme.colorScheme.primary
                              : theme.colorScheme.onSurface,
                          fontWeight: isToday
                              ? FontWeight.w700
                              : FontWeight.w600,
                        ),
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ],
                ),
                if (places.isNotEmpty) ...[
                  AppSpacing.gapXs,
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Icon(
                        Icons.location_on_outlined,
                        size: 15,
                        color: palette.muted,
                      ),
                      AppSpacing.wGapXs,
                      Flexible(
                        child: Text(
                          places,
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: palette.muted,
                          ),
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// Merhumun fotoğrafı; yoksa nötr bir sembol (boş kutu yerine).
class _Portrait extends StatelessWidget {
  const _Portrait({required this.url});

  final String? url;

  static const double _size = 54;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    if (url != null && url!.trim().isNotEmpty) {
      return ClipRRect(
        borderRadius: AppRadius.rSm,
        child: SizedBox(
          width: _size,
          height: _size,
          child: AppNetworkImage(url: url, fit: BoxFit.cover),
        ),
      );
    }

    return Container(
      width: _size,
      height: _size,
      decoration: BoxDecoration(
        color: palette.skeletonBase,
        borderRadius: AppRadius.rSm,
      ),
      child: Icon(
        Icons.filter_vintage_rounded,
        color: palette.muted,
        size: 24,
      ),
    );
  }
}
