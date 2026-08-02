import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/utils/utils.dart';
import '../../../../core/widgets/widgets.dart';
import '../../application/departure_times.dart';
import '../../data/models/intercity_route.dart';

/// Şehirlerarası hat kartı — kapalıyken "sıradaki kalkış", açıkken günün
/// **tüm** kalkış saatleri.
///
/// ⭐ **Sıradaki kalkış** plan dışıdır ve modülün asıl sorusunu cevaplar:
/// kullanıcı bu ekrana "otobüs kaçta?" diye geliyor; saat listesini gözle
/// tarayıp "şu an 13:40, demek ki 14:00" hesabını kullanıcıya yaptırmak
/// gereksiz. Saatler tarihsiz "duvar saati" olduğu için hesap Kadirli gün
/// içi dakikası üzerinden yapılır.
class IntercityRouteCard extends StatelessWidget {
  const IntercityRouteCard({
    super.key,
    required this.route,
    required this.expanded,
    required this.onToggle,
    this.onShare,
    this.now,
  });

  final IntercityRoute route;
  final bool expanded;
  final VoidCallback onToggle;
  final VoidCallback? onShare;

  /// Testlerde sabitlenebilsin diye dışarıdan verilebilir (11.6 deseni).
  final DateTime? now;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final next = route.next(now: now);

    return AppCard(
      onTap: onToggle,
      padding: const EdgeInsets.all(AppSpacing.lg),
      accentStripe: next != null && next.isImminent ? palette.accent : null,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 40,
                height: 40,
                decoration: BoxDecoration(
                  color: theme.colorScheme.primaryContainer,
                  borderRadius: AppRadius.rSm,
                ),
                child: Icon(
                  Icons.directions_bus_rounded,
                  size: 20,
                  color: theme.colorScheme.onPrimaryContainer,
                ),
              ),
              AppSpacing.wGapMd,
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Kadirli → ${route.destination}',
                      style: theme.textTheme.titleSmall,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    if (route.companyLabel != null) ...[
                      AppSpacing.gapXs,
                      Text(
                        route.companyLabel!,
                        style: theme.textTheme.bodySmall?.copyWith(
                          color: palette.muted,
                        ),
                        overflow: TextOverflow.ellipsis,
                      ),
                    ],
                  ],
                ),
              ),
              AppSpacing.wGapSm,
              Icon(
                expanded
                    ? Icons.keyboard_arrow_up_rounded
                    : Icons.keyboard_arrow_down_rounded,
                color: palette.muted,
              ),
            ],
          ),

          AppSpacing.gapMd,
          _NextDepartureLine(next: next),

          if (route.durationLabel != null ||
              (route.price != null && route.price! > 0)) ...[
            AppSpacing.gapMd,
            // ⚠️ Dar sütunda `Row` içindeki çıplak `Text` bu projenin tekrar
            // eden taşma tuzağı (11.7→11.11) → meta satırı `Wrap`.
            Wrap(
              spacing: AppSpacing.lg,
              runSpacing: AppSpacing.xs,
              children: [
                if (route.durationLabel != null)
                  _MetaChip(
                    icon: Icons.schedule_rounded,
                    label: route.durationLabel!,
                  ),
                if (route.price != null && route.price! > 0)
                  _MetaChip(
                    icon: Icons.payments_rounded,
                    label: AppMoney.amount(route.price!),
                  ),
              ],
            ),
          ],

          if (expanded) ...[
            AppSpacing.gapLg,
            Divider(color: palette.border, height: 1),
            AppSpacing.gapLg,
            _DepartureGrid(route: route, now: now),
            if (onShare != null) ...[
              AppSpacing.gapLg,
              AppButton.ghost(
                label: 'Saatleri paylaş',
                icon: Icons.share_rounded,
                size: AppButtonSize.small,
                expand: true,
                onPressed: onShare,
              ),
            ],
          ],
        ],
      ),
    );
  }
}

/// "Sıradaki 14:00 · 2 sa 12 dk sonra" satırı.
class _NextDepartureLine extends StatelessWidget {
  const _NextDepartureLine({required this.next});

  final NextDeparture? next;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    if (next == null) {
      return Text(
        'Kalkış saati girilmemiş',
        style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
      );
    }

    // Yarına kalan sefer sakin tonda: bugün için bir aciliyet yok.
    final color = next!.isTomorrow
        ? palette.muted
        : (next!.isImminent ? palette.accent : theme.colorScheme.primary);

    final text = next!.isTomorrow
        ? 'Bugünkü seferler bitti · Yarın ${next!.label}'
        : 'Sıradaki ${next!.label} · ${DepartureTimes.untilLabel(next!.minutesUntil)}';

    return Row(
      children: [
        Icon(
          next!.isTomorrow
              ? Icons.nightlight_round
              : Icons.departure_board_rounded,
          size: 16,
          color: color,
        ),
        AppSpacing.wGapSm,
        Flexible(
          child: Text(
            text,
            style: theme.textTheme.labelLarge?.copyWith(color: color),
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ],
    );
  }
}

/// Günün tüm kalkışları — geçenler soluk, sıradaki vurgulu.
class _DepartureGrid extends StatelessWidget {
  const _DepartureGrid({required this.route, this.now});

  final IntercityRoute route;
  final DateTime? now;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final times = route.departureTimes;

    if (times.isEmpty) {
      return const InfoBanner(
        tone: InfoBannerTone.info,
        message: 'Bu hat için kalkış saati henüz girilmemiş.',
      );
    }

    final current = DepartureTimes.nowMinutes(now: now);
    final next = route.next(now: now);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Kalkış saatleri',
          style: theme.textTheme.labelMedium?.copyWith(color: palette.muted),
        ),
        AppSpacing.gapSm,
        Wrap(
          spacing: AppSpacing.sm,
          runSpacing: AppSpacing.sm,
          children: [
            for (final time in times)
              _TimePill(
                time: time,
                isNext: next != null && !next.isTomorrow && next.label == time,
                isPast: (DepartureTimes.minutesOfDay(time) ?? 0) < current,
              ),
          ],
        ),
        AppSpacing.gapSm,
        Text(
          'Geçen seferler soluk gösterilir. Saatler firmadan alınan bilgiye '
          'göredir, yolculuk öncesi teyit edin.',
          style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
        ),
      ],
    );
  }
}

class _TimePill extends StatelessWidget {
  const _TimePill({
    required this.time,
    required this.isNext,
    required this.isPast,
  });

  final String time;
  final bool isNext;
  final bool isPast;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final background = isNext
        ? theme.colorScheme.primary
        : theme.colorScheme.surface;
    final foreground = isNext
        ? theme.colorScheme.onPrimary
        : (isPast ? palette.muted : theme.colorScheme.onSurface);

    return Semantics(
      // Renk tek başına yetmez (11.6 kararı) → durum ekran okuyucuya yazılır.
      label: isNext
          ? '$time, sıradaki kalkış'
          : (isPast ? '$time, kalktı' : time),
      excludeSemantics: true,
      child: Container(
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.md,
          vertical: AppSpacing.sm,
        ),
        decoration: BoxDecoration(
          color: background,
          borderRadius: AppRadius.rPill,
          border: Border.all(color: isNext ? background : palette.border),
        ),
        child: Text(
          time,
          style: theme.textTheme.labelLarge?.copyWith(
            color: foreground,
            decoration: isPast && !isNext ? TextDecoration.lineThrough : null,
            decorationColor: palette.muted,
          ),
        ),
      ),
    );
  }
}

class _MetaChip extends StatelessWidget {
  const _MetaChip({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 14, color: palette.muted),
        AppSpacing.wGapXs,
        Flexible(
          child: Text(
            label,
            style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ],
    );
  }
}
