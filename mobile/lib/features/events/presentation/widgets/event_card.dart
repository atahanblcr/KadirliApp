import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/widgets/widgets.dart';
import '../../data/models/event.dart';

/// Etkinlik listesi kartı.
///
/// **Solda takvim yaprağı** (gün + ay): etkinlikte aranan ilk bilgi "ne zaman";
/// göz aşağı kaydırırken tarihleri tek sütunda tarayabilsin diye tarih sabit
/// konumda duruyor, görsel ise sağda küçük bir kapak.
///
/// ⚠️ Meta satırı `Wrap` + `Flexible`: dar sütunda `Row` içindeki çıplak `Text`
/// bu projede üç fazda üst üste taşma üretti (11.7/11.8/11.9).
class EventCard extends StatelessWidget {
  const EventCard({super.key, required this.event, this.onTap, this.now});

  final Event event;
  final VoidCallback? onTap;

  /// Testlerde "bugün"ü sabitlemek için.
  final DateTime? now;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final day = event.eventDate.toUtc();
    final countdown = event.countdownLabel(now: now);
    final isPast = event.isPast(now: now);
    final price = event.priceLabel;
    final location = event.locationBadge;

    return AppCard(
      onTap: onTap,
      padding: const EdgeInsets.all(AppSpacing.md),
      // Konum ekran okuyucuya da söylenir: rozet görsel bir ipucu olarak kalırsa
      // "bu etkinlik nerede" sorusu erişilebilirlik tarafında cevapsız kalır.
      semanticLabel:
          '${event.title}, ${DateFormat('d MMMM', 'tr_TR').format(day)}'
          '${location == null ? '' : ', $location'}',
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _DateBadge(date: day, dimmed: isPast),
          AppSpacing.wGapMd,
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (event.categoryName != null)
                  Text(
                    event.categoryName!,
                    style: theme.textTheme.labelSmall?.copyWith(
                      color: theme.colorScheme.primary,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                Text(
                  event.title,
                  style: theme.textTheme.titleSmall,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
                AppSpacing.gapXs,
                Wrap(
                  spacing: AppSpacing.md,
                  runSpacing: AppSpacing.xs,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    _Meta(icon: Icons.schedule_rounded, text: event.timeLabel),
                    if ((event.venueName ?? '').trim().isNotEmpty)
                      _Meta(
                        icon: Icons.place_rounded,
                        text: event.venueName!.trim(),
                      ),
                    // Faz 12.4 — konum. Mekan adıyla aynı `Wrap` içinde: ikisi de
                    // "nerede" sorusunun parçası ve dar sütunda alt satıra iniyor.
                    // ⚠️ `_Meta` içi `Flexible`+ellipsis — bu projede `Row` içine
                    // giren çıplak `Text` yedi kez taşma üretti.
                    if (location != null)
                      _Meta(
                        icon: Icons.map_rounded,
                        text: location,
                        emphasized: event.isLocal,
                      ),
                  ],
                ),
                if (countdown != null || price != null) ...[
                  AppSpacing.gapSm,
                  Wrap(
                    spacing: AppSpacing.sm,
                    runSpacing: AppSpacing.xs,
                    children: [
                      if (countdown != null)
                        _Pill(
                          label: countdown,
                          color: theme.colorScheme.primary,
                        ),
                      if (price != null)
                        _Pill(
                          label: price,
                          color: event.isFree ? palette.success : palette.muted,
                        ),
                    ],
                  ),
                ],
              ],
            ),
          ),
          if (event.coverImageUrl != null) ...[
            AppSpacing.wGapMd,
            AppNetworkImage(
              url: event.coverImageUrl,
              width: 64,
              height: 64,
              fallbackIcon: Icons.celebration_outlined,
            ),
          ],
        ],
      ),
    );
  }
}

/// Takvim yaprağı: "12 / Ağu".
class _DateBadge extends StatelessWidget {
  const _DateBadge({required this.date, required this.dimmed});

  final DateTime date;
  final bool dimmed;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final background = dimmed
        ? theme.colorScheme.surface
        : theme.colorScheme.primaryContainer;
    final foreground = dimmed
        ? palette.muted
        : theme.colorScheme.onPrimaryContainer;

    return Container(
      width: 52,
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
      decoration: BoxDecoration(
        color: background,
        borderRadius: AppRadius.rSm,
        border: Border.all(color: palette.border),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            DateFormat('d', 'tr_TR').format(date),
            style: theme.textTheme.titleLarge?.copyWith(
              color: foreground,
              fontWeight: FontWeight.w800,
              height: 1.1,
            ),
          ),
          Text(
            DateFormat('MMM', 'tr_TR').format(date),
            style: theme.textTheme.labelSmall?.copyWith(color: foreground),
          ),
        ],
      ),
    );
  }
}

class _Meta extends StatelessWidget {
  const _Meta({
    required this.icon,
    required this.text,
    this.emphasized = false,
  });

  final IconData icon;
  final String text;

  /// Kendi ilçemizdeki etkinlik: renk vurgusu alır. ⚠️ Renk **tek başına** anlam
  /// taşımaz — metin ("Kadirli") zaten orada, vurgu yalnız göz için.
  final bool emphasized;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final color = emphasized ? theme.colorScheme.primary : palette.muted;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 14, color: color),
        AppSpacing.wGapXs,
        // ⚠️ Wrap çocuğuna sınırlı genişlik verir → çıplak Text taşar.
        Flexible(
          child: Text(
            text,
            style: theme.textTheme.bodySmall?.copyWith(
              color: color,
              fontWeight: emphasized ? FontWeight.w600 : null,
            ),
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ],
    );
  }
}

/// Küçük durum rozeti — renk tek başına anlam taşımaz, **metin her zaman var**.
class _Pill extends StatelessWidget {
  const _Pill({required this.label, required this.color});

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: 2,
      ),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: AppRadius.rPill,
        border: Border.all(color: color.withValues(alpha: 0.35)),
      ),
      child: Text(
        label,
        style: theme.textTheme.labelSmall?.copyWith(
          color: color,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}
