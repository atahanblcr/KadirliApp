import 'package:freezed_annotation/freezed_annotation.dart';

import '../../../../core/utils/utils.dart';
import '../../application/departure_times.dart';

part 'intercity_route.freezed.dart';
part 'intercity_route.g.dart';

/// `GET /v1/transport/intercity-routes` satırı (`IntercityRouteResponseDto`).
///
/// ⚠️ **Ayrı bir detay ucu yok** — kalkış saatleri (`schedules`) liste
/// gövdesinde geliyor. Bu yüzden ulaşımda detay ekranı/rotası da yok:
/// kart yerinde açılıyor (bkz. `TransportScreen`).
///
/// ⚠️ Public uç yalnız **aktif** hatları ve hattın **aktif** seferlerini
/// döndürür (`OnlyActive` controller'da sabit) → istemci ayrıca süzmez.
@freezed
abstract class IntercityRoute with _$IntercityRoute {
  const factory IntercityRoute({
    required String id,
    required String destination,
    double? price,
    int? durationMinutes,
    String? company,
    @Default(true) bool isActive,
    @Default(<IntercityDeparture>[]) List<IntercityDeparture> schedules,
  }) = _IntercityRoute;

  const IntercityRoute._();

  factory IntercityRoute.fromJson(Map<String, dynamic> json) =>
      _$IntercityRouteFromJson(json);

  String? get companyLabel {
    final value = company?.trim();
    return (value == null || value.isEmpty) ? null : value;
  }

  /// "1 sa 45 dk" — sunucu dakika veriyor.
  String? get durationLabel {
    final minutes = durationMinutes;
    if (minutes == null || minutes <= 0) return null;
    return AppDate.duration(Duration(minutes: minutes));
  }

  /// Saate göre sıralı `"HH:mm"` listesi (sunucu zaten sıralı gönderiyor;
  /// bozuk kayıt gelirse burada eleniyor).
  List<String> get departureTimes {
    final entries = <(int, String)>[];
    for (final schedule in schedules) {
      final minutes = DepartureTimes.minutesOfDay(schedule.departureTime);
      if (minutes != null) entries.add((minutes, DepartureTimes.label(minutes)));
    }
    entries.sort((a, b) => a.$1.compareTo(b.$1));
    return [for (final entry in entries) entry.$2];
  }

  bool get hasDepartures => departureTimes.isNotEmpty;

  NextDeparture? next({DateTime? now}) =>
      DepartureTimes.next(departureTimes, now: now);

  String get shareText {
    final times = departureTimes;
    final lines = <String>[
      '🚌 Kadirli → $destination',
      ?companyLabel,
      if (times.isNotEmpty) 'Kalkış saatleri: ${times.join(' · ')}',
      if (durationLabel != null) 'Yolculuk: $durationLabel',
      if (price != null && price! > 0) 'Ücret: ${AppMoney.amount(price!)}',
      '',
      '— Kadirli uygulaması',
    ];
    return lines.join('\n');
  }
}

/// Bir seferin kalkış saati (`ScheduleDto`) — `departureTime` `"HH:mm"`.
@freezed
abstract class IntercityDeparture with _$IntercityDeparture {
  const factory IntercityDeparture({
    required String id,
    @Default('') String departureTime,
  }) = _IntercityDeparture;

  factory IntercityDeparture.fromJson(Map<String, dynamic> json) =>
      _$IntercityDepartureFromJson(json);
}
