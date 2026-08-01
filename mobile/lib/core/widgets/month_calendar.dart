import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';
import '../utils/utils.dart';

/// Aylık takvim ızgarası — **kayıt tipinden bağımsız**.
///
/// 11.7'de nöbetçi eczane takvimi olarak yazılmıştı; 11.10'da etkinlik takvimi
/// aynı davranışı isteyince ortak bileşene çıkarıldı (`DutyCalendar` artık
/// bunun ince bir sarmalayıcısı). "Bu ayın hangi gününde ne var" sorusu tarama
/// yaparak değil **bakarak** cevaplanmalı: dolu günler vurgulu, bugün çerçeveli,
/// seçili gün dolgulu; **kaydı olmayan gün dokunulabilir değil** (işlevsiz
/// buton yok).
///
/// Hafta Pazartesi'den başlar (TR konvansiyonu).
class MonthCalendar extends StatelessWidget {
  const MonthCalendar({
    super.key,
    required this.year,
    required this.month,
    required this.markedDays,
    required this.selectedDayKey,
    required this.onDaySelected,
    this.markedSemantics,
  });

  final int year;
  final int month;

  /// `yyyy-MM-dd` → o günkü kayıt sayısı. Boş küme = o ay hiç kayıt yok.
  final Map<String, int> markedDays;

  /// `yyyy-MM-dd`; null = seçim yok.
  final String? selectedDayKey;

  final void Function(String dayKey) onDaySelected;

  /// Ekran okuyucu etiketi ("2 etkinlik var" gibi). Verilmezse genel ifade.
  final String Function(int count)? markedSemantics;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final firstDay = DateTime(year, month);
    final daysInMonth = DateTime(year, month + 1, 0).day;
    // DateTime.weekday: Pazartesi=1 … Pazar=7 → ızgarada kaç boş hücre var.
    final leadingBlanks = firstDay.weekday - 1;

    final today = AppDate.nowInTurkey;
    final todayKey = dayKey(today.year, today.month, today.day);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            for (final label in const ['Pt', 'Sa', 'Ça', 'Pe', 'Cu', 'Ct', 'Pz'])
              Expanded(
                child: Center(
                  child: Text(
                    label,
                    style: theme.textTheme.labelSmall?.copyWith(
                      color: palette.muted,
                    ),
                  ),
                ),
              ),
          ],
        ),
        AppSpacing.gapSm,
        GridView.builder(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
            crossAxisCount: 7,
            mainAxisSpacing: AppSpacing.xs,
            crossAxisSpacing: AppSpacing.xs,
          ),
          itemCount: leadingBlanks + daysInMonth,
          itemBuilder: (context, index) {
            if (index < leadingBlanks) return const SizedBox.shrink();

            final day = index - leadingBlanks + 1;
            final key = dayKey(year, month, day);
            final count = markedDays[key] ?? 0;

            return _DayCell(
              day: day,
              count: count,
              semanticsLabel: count == 0
                  ? '$day'
                  : '$day, ${markedSemantics?.call(count) ?? 'kayıt var'}',
              isToday: key == todayKey,
              isSelected: key == selectedDayKey,
              onTap: count == 0 ? null : () => onDaySelected(key),
            );
          },
        ),
      ],
    );
  }

  /// `yyyy-MM-dd` gün anahtarı.
  static String dayKey(int year, int month, int day) =>
      '$year-${month.toString().padLeft(2, '0')}-${day.toString().padLeft(2, '0')}';

  /// Sunucudan gelen "gün" alanının anahtarı.
  ///
  /// ⚠️ **Saat dilimi kaydırılmaz:** hem nöbet (`dutyDate`) hem etkinlik
  /// (`eventDate`) alanlarını sunucu "Türkiye günü, 00:00 UTC" olarak yazıyor;
  /// +3 eklemek 1 Ağustos'u 1 Ağustos 03:00'a taşır ama gün adı değişmez —
  /// yine de ham UTC okumak niyeti açık kılar (11.7 dersi).
  static String dayKeyOf(DateTime serverDay) {
    final utc = serverDay.toUtc();
    return dayKey(utc.year, utc.month, utc.day);
  }
}

class _DayCell extends StatelessWidget {
  const _DayCell({
    required this.day,
    required this.count,
    required this.semanticsLabel,
    required this.isToday,
    required this.isSelected,
    this.onTap,
  });

  final int day;
  final int count;
  final String semanticsLabel;
  final bool isToday;
  final bool isSelected;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final hasEntry = count > 0;

    final background = isSelected
        ? theme.colorScheme.primary
        : (hasEntry ? theme.colorScheme.primaryContainer : Colors.transparent);
    final foreground = isSelected
        ? theme.colorScheme.onPrimary
        : (hasEntry ? theme.colorScheme.onPrimaryContainer : palette.muted);

    return Semantics(
      button: hasEntry,
      selected: isSelected,
      label: semanticsLabel,
      child: Material(
        color: background,
        shape: RoundedRectangleBorder(
          borderRadius: AppRadius.rSm,
          side: isToday
              ? BorderSide(color: theme.colorScheme.primary, width: 1.6)
              : BorderSide.none,
        ),
        child: InkWell(
          onTap: onTap,
          borderRadius: AppRadius.rSm,
          child: Center(
            child: Text(
              '$day',
              style: theme.textTheme.bodyMedium?.copyWith(
                color: foreground,
                fontWeight: hasEntry ? FontWeight.w700 : FontWeight.w400,
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// "Ağustos 2026" başlığı + ay ileri/geri okları.
class MonthSwitcher extends StatelessWidget {
  const MonthSwitcher({
    super.key,
    required this.year,
    required this.month,
    required this.onPrevious,
    required this.onNext,
  });

  final int year;
  final int month;
  final VoidCallback onPrevious;
  final VoidCallback onNext;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final label = DateFormat('MMMM y', 'tr_TR').format(DateTime(year, month));

    return Row(
      children: [
        IconButton(
          onPressed: onPrevious,
          icon: const Icon(Icons.chevron_left_rounded),
          tooltip: 'Önceki ay',
        ),
        Expanded(
          child: Text(
            label,
            textAlign: TextAlign.center,
            style: theme.textTheme.titleMedium,
          ),
        ),
        IconButton(
          onPressed: onNext,
          icon: const Icon(Icons.chevron_right_rounded),
          tooltip: 'Sonraki ay',
        ),
      ],
    );
  }
}

/// Takvimde gösterilen ay (record → yapısal eşitlik, `family` anahtarı olabilir).
typedef CalendarMonth = ({int year, int month});

CalendarMonth calendarMonthOf(DateTime date) => (year: date.year, month: date.month);

/// Bir ay ileri/geri (Aralık→Ocak sarması dahil).
CalendarMonth shiftCalendarMonth(CalendarMonth month, int delta) {
  final shifted = DateTime(month.year, month.month + delta);
  return (year: shifted.year, month: shifted.month);
}
