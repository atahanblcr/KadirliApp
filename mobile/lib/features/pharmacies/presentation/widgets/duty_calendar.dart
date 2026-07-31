import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/utils/utils.dart';
import '../../data/models/duty_schedule.dart';

/// Aylık nöbet takvimi ızgarası.
///
/// **Neden ızgara, liste değil:** kullanıcı "bu ayın hangi günü hangi eczane"
/// sorusunu tarama yaparak değil bakarak cevaplamak istiyor; nöbetli günler
/// noktalı, bugün çerçeveli, seçili gün dolu gösteriliyor. Nöbeti olmayan gün
/// **soluk ama dokunulabilir değil** (işlevsiz buton yok).
///
/// Hafta Pazartesi'den başlar (TR konvansiyonu).
class DutyCalendar extends StatelessWidget {
  const DutyCalendar({
    super.key,
    required this.year,
    required this.month,
    required this.schedule,
    required this.selectedDayKey,
    required this.onDaySelected,
  });

  final int year;
  final int month;
  final List<DutySchedule> schedule;

  /// `yyyy-MM-dd`; null = seçim yok.
  final String? selectedDayKey;
  final void Function(String dayKey) onDaySelected;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final byDay = <String, List<DutySchedule>>{};
    for (final entry in schedule) {
      byDay.putIfAbsent(entry.dayKey, () => []).add(entry);
    }

    final firstDay = DateTime(year, month);
    final daysInMonth = DateTime(year, month + 1, 0).day;
    // DateTime.weekday: Pazartesi=1 … Pazar=7 → ızgarada kaç boş hücre var.
    final leadingBlanks = firstDay.weekday - 1;

    final today = AppDate.nowInTurkey;
    final todayKey = _dayKey(today.year, today.month, today.day);

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
            final key = _dayKey(year, month, day);
            final entries = byDay[key];

            return _DayCell(
              day: day,
              hasDuty: entries != null,
              isToday: key == todayKey,
              isSelected: key == selectedDayKey,
              onTap: entries == null ? null : () => onDaySelected(key),
            );
          },
        ),
      ],
    );
  }

  static String _dayKey(int year, int month, int day) =>
      '$year-${month.toString().padLeft(2, '0')}-${day.toString().padLeft(2, '0')}';
}

class _DayCell extends StatelessWidget {
  const _DayCell({
    required this.day,
    required this.hasDuty,
    required this.isToday,
    required this.isSelected,
    this.onTap,
  });

  final int day;
  final bool hasDuty;
  final bool isToday;
  final bool isSelected;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final background = isSelected
        ? theme.colorScheme.primary
        : (hasDuty ? theme.colorScheme.primaryContainer : Colors.transparent);
    final foreground = isSelected
        ? theme.colorScheme.onPrimary
        : (hasDuty ? theme.colorScheme.onPrimaryContainer : palette.muted);

    return Semantics(
      button: hasDuty,
      selected: isSelected,
      label: hasDuty ? '$day, nöbetçi eczane var' : '$day',
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
                fontWeight: hasDuty ? FontWeight.w700 : FontWeight.w400,
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
