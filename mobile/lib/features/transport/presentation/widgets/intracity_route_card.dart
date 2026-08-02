import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/widgets/widgets.dart';
import '../../application/departure_times.dart';
import '../../data/models/intracity_route.dart';

/// Şehir içi hat kartı — kapalıyken servis durumu, açıkken **durak zaman
/// çizelgesi**.
///
/// ⭐ Plan dışı: hattın **şu an çalışıp çalışmadığı** kartın en üstünde yazıyor.
/// Sunucu yalnız ilk/son saat ve sıklık veriyor; "06:30 – 22:00" satırını
/// okuyup saatine bakmak yerine "Şu an çalışıyor · yaklaşık 20 dakikada bir"
/// cümlesi doğrudan cevabı veriyor.
class IntracityRouteCard extends StatelessWidget {
  const IntracityRouteCard({
    super.key,
    required this.route,
    required this.expanded,
    required this.onToggle,
    this.onShare,
    this.now,
  });

  final IntracityRoute route;
  final bool expanded;
  final VoidCallback onToggle;
  final VoidCallback? onShare;
  final DateTime? now;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final status = route.status(now: now);

    return AppCard(
      onTap: onToggle,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Hat numarası şehir içi ulaşımın günlük dilinde ana kimlik
              // ("2 numaraya bineceksin") → rozet olarak öne çıkarılıyor.
              Container(
                constraints: const BoxConstraints(minWidth: 40),
                height: 40,
                alignment: Alignment.center,
                padding: const EdgeInsets.symmetric(horizontal: AppSpacing.sm),
                decoration: BoxDecoration(
                  color: theme.colorScheme.primaryContainer,
                  borderRadius: AppRadius.rSm,
                ),
                child: Text(
                  route.routeNumber,
                  style: theme.textTheme.titleSmall?.copyWith(
                    color: theme.colorScheme.onPrimaryContainer,
                  ),
                ),
              ),
              AppSpacing.wGapMd,
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      route.routeName,
                      style: theme.textTheme.titleSmall,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    if (route.serviceHoursLabel != null) ...[
                      AppSpacing.gapXs,
                      Text(
                        route.serviceHoursLabel!,
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
          _ServiceStatusLine(route: route, status: status),

          if (route.hasStops && !expanded) ...[
            AppSpacing.gapSm,
            Row(
              children: [
                Icon(
                  Icons.route_rounded,
                  size: 14,
                  color: palette.muted,
                ),
                AppSpacing.wGapXs,
                Flexible(
                  child: Text(
                    '${route.stops.length} durak · dokununca güzergâh açılır',
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: palette.muted,
                    ),
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              ],
            ),
          ],

          if (expanded) ...[
            AppSpacing.gapLg,
            Divider(color: palette.border, height: 1),
            AppSpacing.gapLg,
            _StopTimeline(route: route),
            if (onShare != null) ...[
              AppSpacing.gapLg,
              AppButton.ghost(
                label: 'Hattı paylaş',
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

class _ServiceStatusLine extends StatelessWidget {
  const _ServiceStatusLine({required this.route, required this.status});

  final IntracityRoute route;
  final IntracityStatus status;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    // Durum hem renk hem metinle (11.6 kararı).
    final (icon, color, text) = switch (status.state) {
      IntracityServiceState.running => (
        Icons.directions_bus_filled_rounded,
        palette.success,
        _runningText(),
      ),
      IntracityServiceState.beforeFirst => (
        Icons.schedule_rounded,
        palette.warning,
        'İlk sefer ${status.nextLabel}'
            '${status.minutesUntil == null ? '' : ' · ${DepartureTimes.untilLabel(status.minutesUntil!)}'}',
      ),
      IntracityServiceState.finished => (
        Icons.nightlight_round,
        palette.muted,
        'Bugünkü seferler bitti · Yarın ${status.nextLabel}',
      ),
      IntracityServiceState.unknown => (
        Icons.info_outline_rounded,
        palette.muted,
        'Servis saatleri girilmemiş',
      ),
    };

    return Row(
      children: [
        Icon(icon, size: 16, color: color),
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

  String _runningText() {
    final buffer = StringBuffer('Şu an çalışıyor');
    final frequency = route.frequencyMinutes;
    if (frequency != null && frequency > 0) {
      buffer.write(' · $frequency dk arayla');
    }
    final next = status.nextLabel;
    if (next != null) buffer.write(' · yaklaşık $next');
    return buffer.toString();
  }
}

/// Durak zaman çizelgesi — sıra + ilk duraktan itibaren dakika.
class _StopTimeline extends StatelessWidget {
  const _StopTimeline({required this.route});

  final IntracityRoute route;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final stops = route.orderedStops;

    if (stops.isEmpty) {
      return const InfoBanner(
        tone: InfoBannerTone.info,
        message: 'Bu hattın durakları henüz girilmemiş.',
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Güzergâh',
          style: theme.textTheme.labelMedium?.copyWith(color: palette.muted),
        ),
        AppSpacing.gapMd,
        for (var index = 0; index < stops.length; index++)
          _StopRow(
            stop: stops[index],
            isFirst: index == 0,
            isLast: index == stops.length - 1,
          ),
        if (route.frequencyLabel != null) ...[
          AppSpacing.gapSm,
          Text(
            '${route.frequencyLabel!}. Durak süreleri ilk duraktan itibaren '
            'yaklaşık değerlerdir.',
            style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
          ),
        ],
      ],
    );
  }
}

class _StopRow extends StatelessWidget {
  const _StopRow({
    required this.stop,
    required this.isFirst,
    required this.isLast,
  });

  final IntracityStop stop;
  final bool isFirst;
  final bool isLast;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final primary = theme.colorScheme.primary;

    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Çizgi + nokta: hattın yönü bakışta okunsun.
          SizedBox(
            width: 20,
            child: Column(
              children: [
                Container(
                  width: 2,
                  height: AppSpacing.sm,
                  color: isFirst ? Colors.transparent : palette.border,
                ),
                Container(
                  width: isFirst || isLast ? 12 : 8,
                  height: isFirst || isLast ? 12 : 8,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: isFirst || isLast ? primary : palette.border,
                  ),
                ),
                Expanded(
                  child: Container(
                    width: 2,
                    color: isLast ? Colors.transparent : palette.border,
                  ),
                ),
              ],
            ),
          ),
          AppSpacing.wGapMd,
          Expanded(
            child: Padding(
              padding: const EdgeInsets.only(bottom: AppSpacing.md),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: Text(
                      stop.stopName,
                      style: theme.textTheme.bodyMedium?.copyWith(
                        fontWeight: isFirst || isLast ? FontWeight.w600 : null,
                      ),
                    ),
                  ),
                  if (stop.offsetLabel != null) ...[
                    AppSpacing.wGapSm,
                    Text(
                      stop.offsetLabel!,
                      style: theme.textTheme.labelSmall?.copyWith(
                        color: palette.muted,
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
