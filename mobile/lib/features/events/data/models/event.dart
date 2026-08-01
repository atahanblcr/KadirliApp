import 'package:freezed_annotation/freezed_annotation.dart';

import '../../../../core/utils/utils.dart';
import '../../../../core/widgets/widgets.dart';

part 'event.freezed.dart';
part 'event.g.dart';

/// `GET /v1/events` ve `/v1/events/{id}` gövdesi (`EventResponseDto`).
///
/// ⚠️ **Tarih ve saat iki ayrı alan:** `eventDate` "Türkiye günü, 00:00 UTC"
/// (nöbet takvimindeki `dutyDate` ile aynı sunucu konvansiyonu, 11.7 dersi →
/// **saat dilimi kaydırılmaz**), `eventTime` ise `"19:00:00"` biçiminde duvar
/// saati. İkisini birleştiren tek bir "an" [startsAt] içinde üretiliyor.
@freezed
abstract class Event with _$Event {
  const factory Event({
    required String id,
    required String title,
    @Default('') String description,
    String? categoryId,
    String? categoryName,
    required DateTime eventDate,
    @Default('00:00:00') String eventTime,
    String? venueName,
    String? address,
    double? latitude,
    double? longitude,
    @Default(false) bool hasLocation,
    String? organizer,
    double? ticketPrice,
    @Default(false) bool isFree,
    @Default(true) bool isLocal,
    String? coverImageId,
    String? coverImageUrl,
    @Default('approved') String status,
    DateTime? createdAt,
  }) = _Event;

  const Event._();

  factory Event.fromJson(Map<String, dynamic> json) => _$EventFromJson(json);

  /// Takvim ızgarası anahtarı (`yyyy-MM-dd`).
  String get dayKey => MonthCalendar.dayKeyOf(eventDate);

  /// "19:00" — saniye kısmı kullanıcıya gösterilmez.
  String get timeLabel => AppDate.clockLabel(eventTime);

  /// Etkinliğin başlangıç anı, **Türkiye duvar saatiyle** (karşılaştırma için).
  ///
  /// `eventDate`in gün bilgisi + `eventTime`in saati; ikisi de zaten TR
  /// takviminde yazıldığı için ek çevrim yapılmaz.
  DateTime get startsAt {
    final day = eventDate.toUtc();
    final parts = eventTime.split(':');
    final hour = parts.isNotEmpty ? int.tryParse(parts[0]) ?? 0 : 0;
    final minute = parts.length > 1 ? int.tryParse(parts[1]) ?? 0 : 0;
    return DateTime(day.year, day.month, day.day, hour, minute);
  }

  /// Bugüne göre kaç gün sonra (bugün 0, dün -1). Saat dikkate alınmaz.
  int daysFromToday({DateTime? now}) {
    final today = AppDate.toTurkey(now ?? DateTime.now());
    final reference = DateTime(today.year, today.month, today.day);
    final day = eventDate.toUtc();
    return DateTime(day.year, day.month, day.day).difference(reference).inDays;
  }

  bool isToday({DateTime? now}) => daysFromToday(now: now) == 0;

  /// Gün olarak geçmişte mi — **gün bazında**: bugün akşam 19:00'daki etkinlik
  /// sabah 09:00'da "geçmiş" sayılmamalı (liste "Yaklaşan"da kalır).
  bool isPast({DateTime? now}) => daysFromToday(now: now) < 0;

  /// Kartta ve detayda gösterilen zaman rozeti: "Bugün", "Yarın", "3 gün sonra".
  String? countdownLabel({DateTime? now}) {
    final days = daysFromToday(now: now);
    return switch (days) {
      0 => 'Bugün',
      1 => 'Yarın',
      > 1 && < 8 => '$days gün sonra',
      _ => null,
    };
  }

  /// Bilet bilgisi: ücretsizse net söylenir, fiyat yoksa boş bırakılır
  /// ("0 ₺" yazmak yanlış bilgi olurdu — `AppMoney` kararının aynısı).
  String? get priceLabel {
    if (isFree) return 'Ücretsiz';
    final price = ticketPrice;
    if (price == null || price <= 0) return null;
    return AppMoney.amount(price);
  }

  bool get canOpenMap => latitude != null && longitude != null;

  /// Haritada aranabilir bir ipucu var mı (koordinat ya da adres/mekan adı).
  String? get mapQuery {
    final parts = [venueName, address]
        .map((value) => value?.trim())
        .where((value) => value != null && value.isNotEmpty)
        .join(', ');
    return parts.isEmpty ? null : parts;
  }
}
