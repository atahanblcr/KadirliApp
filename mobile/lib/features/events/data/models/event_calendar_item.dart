import 'package:freezed_annotation/freezed_annotation.dart';

import '../../../../core/utils/utils.dart';
import '../../../../core/widgets/widgets.dart';

part 'event_calendar_item.freezed.dart';
part 'event_calendar_item.g.dart';

/// `GET /v1/events/calendar?year=&month=` satırı (`EventCalendarItemDto`).
///
/// Takvim ızgarası için **ince** bir DTO: liste kartının ihtiyaç duyduğu
/// açıklama/fiyat/görsel alanları yok. Güne dokunulunca gösterilen özet bu
/// veriyle çizilir; kullanıcı detaya girerse tam kayıt ayrıca çekilir.
@freezed
abstract class EventCalendarItem with _$EventCalendarItem {
  const factory EventCalendarItem({
    required String id,
    required String title,
    required DateTime eventDate,
    @Default('00:00:00') String eventTime,
    String? venueName,
    String? categoryName,
    @Default('approved') String status,
  }) = _EventCalendarItem;

  const EventCalendarItem._();

  factory EventCalendarItem.fromJson(Map<String, dynamic> json) =>
      _$EventCalendarItemFromJson(json);

  /// `yyyy-MM-dd` — ⚠️ saat dilimi kaydırılmaz (bkz. `Event.dayKey`).
  String get dayKey => MonthCalendar.dayKeyOf(eventDate);

  String get timeLabel => AppDate.clockLabel(eventTime);
}
