import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/utils/utils.dart';
import '../../../../core/widgets/widgets.dart';
import '../../application/departure_times.dart';
import '../../application/operating_days.dart';
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
                  // 12.6: minibüsün ikonu da farklı — "Adana minibüsü" ile
                  // "Adana otobüsü" listede **ilk bakışta** ayrılmalı.
                  route.vehicle?.icon ?? Icons.directions_bus_rounded,
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
                    if (route.companyLabel != null ||
                        route.vehicle != null) ...[
                      AppSpacing.gapXs,
                      // ⚠️ Firma adı + araç tipi yan yana: dar sütunda çıplak
                      // `Row` bu projenin yedi kez tekrarlayan taşma tuzağı →
                      // `Wrap`.
                      Wrap(
                        spacing: AppSpacing.sm,
                        runSpacing: AppSpacing.xs,
                        crossAxisAlignment: WrapCrossAlignment.center,
                        children: [
                          if (route.companyLabel != null)
                            Text(
                              route.companyLabel!,
                              style: theme.textTheme.bodySmall?.copyWith(
                                color: palette.muted,
                              ),
                            ),
                          // Tanınmayan araç tipinde rozet **hiç çizilmez**:
                          // uydurma bir etiket basmak yalan söylemektir.
                          if (route.vehicle != null)
                            _VehicleBadge(label: route.vehicle!.label),
                        ],
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
          _NextDepartureLine(next: next, runsToday: route.runsToday(now: now)),

          if (route.durationLabel != null ||
              (route.price != null && route.price! > 0) ||
              route.departurePointLabel != null) ...[
            AppSpacing.gapMd,
            // ⚠️ Dar sütunda `Row` içindeki çıplak `Text` bu projenin tekrar
            // eden taşma tuzağı (11.7→11.11) → meta satırı `Wrap`.
            Wrap(
              spacing: AppSpacing.lg,
              runSpacing: AppSpacing.xs,
              children: [
                if (route.departurePointLabel != null)
                  _MetaChip(
                    icon: Icons.place_rounded,
                    label: route.departurePointLabel!,
                  ),
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
            _DeparturePointSection(route: route),
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
  const _NextDepartureLine({required this.next, required this.runsToday});

  final NextDeparture? next;

  /// Hattın bugün hiç seferi var mı — "bitti" ile "yok" ayrımı için.
  final bool runsToday;

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

    // Bugün olmayan sefer sakin tonda: bugün için bir aciliyet yok.
    final color = next!.isToday
        ? (next!.isImminent ? palette.accent : theme.colorScheme.primary)
        : palette.muted;

    // 🔴 12.6 — üç dal: bugün / yarın / ilerideki bir gün. Üçüncüsü olmadan
    // yalnız hafta içi çalışan bir hat Cumartesi "Yarın 06:30" derdi ve
    // vatandaş **Pazar günü durakta beklerdi** — hata vermeyen yanlış cevap.
    //
    // 🐛 Canlı emülatör denetiminde bulundu: giriş cümlesi `daysAhead`'e değil
    // **hattın bugün çalışıp çalışmadığına** bakmalı. "Bugünkü seferler bitti"
    // cümlesi, o gün hiç seferi olmayan bir hatta *olmamış* bir sefer dizisini
    // ima ediyordu.
    final prefix = runsToday ? 'Bugünkü seferler bitti' : 'Bugün sefer yok';
    final text = next!.isToday
        ? 'Sıradaki ${next!.label} · '
              '${DepartureTimes.untilLabel(next!.minutesUntil)}'
        : '$prefix · ${next!.dayLabel} ${next!.label}';

    return Row(
      children: [
        Icon(
          next!.isToday
              ? Icons.departure_board_rounded
              : (next!.isTomorrow
                    ? Icons.nightlight_round
                    : Icons.event_repeat_rounded),
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
    final departures = route.departures;

    if (departures.isEmpty) {
      return const InfoBanner(
        tone: InfoBannerTone.info,
        message: 'Bu hat için kalkış saati henüz girilmemiş.',
      );
    }

    final current = DepartureTimes.nowMinutes(now: now);
    final today = DepartureTimes.nowWeekday(now: now);
    final next = route.next(now: now);
    // Hattın tamamı her gün çalışıyorsa rozet hiçbir şey söylemez, yalnız yer
    // kaplar — 12.5 öncesi kayıtların **tamamı** bu durumda.
    final showDayBadges = !route.runsDaily;

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
            for (final departure in departures)
              _TimePill(
                time: departure.label,
                days: departure.days,
                showDays: showDayBadges,
                isNext:
                    next != null &&
                    next.isToday &&
                    next.minutesOfDay == departure.minutesOfDay,
                // 🔴 "Geçti" yalnız **bugün çalışan** sefer için doğru: hafta
                // içi 07:00 seferi Pazar öğlen "kalktı" diye üstü çizilseydi
                // ekran, aslında hiç kalkmamış bir seferi kalkmış gösterirdi.
                isPast:
                    departure.days.runsOnWeekday(today) &&
                    departure.minutesOfDay < current,
                isOffDay: !departure.days.runsOnWeekday(today),
              ),
          ],
        ),
        AppSpacing.gapSm,
        Text(
          showDayBadges
              ? 'Bugün çalışmayan seferler soluk, geçenlerin üstü çizilidir. '
                    'Saatler firmadan alınan bilgiye göredir, yolculuk öncesi '
                    'teyit edin.'
              : 'Geçen seferler soluk gösterilir. Saatler firmadan alınan '
                    'bilgiye göredir, yolculuk öncesi teyit edin.',
          style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
        ),
      ],
    );
  }
}

class _TimePill extends StatelessWidget {
  const _TimePill({
    required this.time,
    required this.days,
    required this.showDays,
    required this.isNext,
    required this.isPast,
    required this.isOffDay,
  });

  final String time;
  final OperatingDays days;

  /// Hattın tamamı her gün çalışıyorsa gün satırı çizilmez.
  final bool showDays;
  final bool isNext;
  final bool isPast;

  /// Sefer **bugün** çalışmıyor (ör. Pazar günü bakılan hafta içi seferi).
  final bool isOffDay;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final background = isNext
        ? theme.colorScheme.primary
        : theme.colorScheme.surface;
    final foreground = isNext
        ? theme.colorScheme.onPrimary
        : ((isPast || isOffDay) ? palette.muted : theme.colorScheme.onSurface);

    // Renk tek başına yetmez (11.6 kararı) → durum ekran okuyucuya **yazılır**.
    final dayPart = showDays ? ', ${days.semanticsLabel}' : '';
    final statePart = isNext
        ? ', sıradaki kalkış'
        : (isOffDay ? ', bugün sefer yok' : (isPast ? ', kalktı' : ''));

    return Semantics(
      label: '$time$dayPart$statePart',
      excludeSemantics: true,
      child: Container(
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.md,
          vertical: AppSpacing.sm,
        ),
        decoration: BoxDecoration(
          color: background,
          borderRadius: AppRadius.rMd,
          border: Border.all(color: isNext ? background : palette.border),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            Text(
              time,
              style: theme.textTheme.labelLarge?.copyWith(
                color: foreground,
                decoration: isPast && !isNext
                    ? TextDecoration.lineThrough
                    : null,
                decorationColor: palette.muted,
              ),
            ),
            if (showDays) ...[
              const SizedBox(height: 2),
              Text(
                days.label,
                style: theme.textTheme.labelSmall?.copyWith(
                  color: isNext
                      ? theme.colorScheme.onPrimary
                      : (isOffDay ? palette.muted : theme.colorScheme.primary),
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

/// Kalkış noktası + **Yol tarifi** (12.5'in koordinat sözlüğünün varlık sebebi).
///
/// Kalkış noktası girilmemişse bölüm **hiç çizilmez** — "otogardan kalkar"
/// tahmini vatandaşı yanlış yere götürür (12.5'in "geri doldurma YOK" kararı).
class _DeparturePointSection extends StatelessWidget {
  const _DeparturePointSection({required this.route});

  final IntercityRoute route;

  @override
  Widget build(BuildContext context) {
    final name = route.departurePointLabel;
    if (name == null) return const SizedBox.shrink();

    final theme = Theme.of(context);
    final palette = theme.palette;
    final address = route.departurePointAddressLabel;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Kalkış noktası',
          style: theme.textTheme.labelMedium?.copyWith(color: palette.muted),
        ),
        AppSpacing.gapSm,
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(Icons.place_rounded, size: 18, color: theme.colorScheme.primary),
            AppSpacing.wGapSm,
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(name, style: theme.textTheme.bodyMedium),
                  if (address != null && address != name) ...[
                    AppSpacing.gapXs,
                    Text(
                      address,
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: palette.muted,
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ],
        ),
        if (route.canShowDirections) ...[
          AppSpacing.gapMd,
          // Ortak bileşen: koordinat varsa `geo:`, yoksa adres araması —
          // "harita açılamadı" bilgi şeridi de orada tek yerde.
          ContactActions(
            latitude: route.departurePointLatitude,
            longitude: route.departurePointLongitude,
            mapLabel: name,
            address: route.departureMapQuery,
          ),
        ],
        AppSpacing.gapLg,
      ],
    );
  }
}

/// "Otobüs" / "Minibüs" rozeti.
class _VehicleBadge extends StatelessWidget {
  const _VehicleBadge({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: 2,
      ),
      decoration: BoxDecoration(
        color: theme.colorScheme.secondaryContainer,
        borderRadius: AppRadius.rPill,
      ),
      child: Text(
        label,
        style: theme.textTheme.labelSmall?.copyWith(
          color: theme.colorScheme.onSecondaryContainer,
          fontWeight: FontWeight.w700,
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
