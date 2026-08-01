import 'package:flutter/material.dart';

import '../../../../core/widgets/widgets.dart';
import '../../data/models/duty_schedule.dart';

/// Aylık **nöbet** takvimi — ortak [MonthCalendar]'ın eczaneye özel sarmalayıcısı.
///
/// 11.10'da etkinlik takvimi de aynı ızgarayı isteyince çizim işi
/// `core/widgets/month_calendar.dart`'a taşındı; burada yalnızca "hangi günde
/// kaç nöbet var" eşlemesi ve nöbete özel ekran okuyucu metni kalıyor.
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
    final counts = <String, int>{};
    for (final entry in schedule) {
      counts.update(entry.dayKey, (value) => value + 1, ifAbsent: () => 1);
    }

    return MonthCalendar(
      year: year,
      month: month,
      markedDays: counts,
      selectedDayKey: selectedDayKey,
      onDaySelected: onDaySelected,
      markedSemantics: (count) =>
          count == 1 ? 'nöbetçi eczane var' : '$count nöbetçi eczane var',
    );
  }
}
