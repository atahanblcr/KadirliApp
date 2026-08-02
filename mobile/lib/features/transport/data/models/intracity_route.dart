import 'package:freezed_annotation/freezed_annotation.dart';

import '../../application/departure_times.dart';

part 'intracity_route.freezed.dart';
part 'intracity_route.g.dart';

/// `GET /v1/transport/intracity-routes` satırı (`IntracityRouteResponseDto`).
///
/// ⚠️ `firstDeparture` / `lastDeparture` sunucuda `TimeSpan` → JSON'da
/// **`"06:30:00"`** biçiminde gelir (şehirlerarasındaki `"07:00"`den farklı);
/// ikisini de [DepartureTimes.minutesOfDay] çözüyor.
///
/// ⚠️ Ayrı bir detay ucu yok; duraklar liste gövdesinde geliyor.
@freezed
abstract class IntracityRoute with _$IntracityRoute {
  const factory IntracityRoute({
    required String id,
    @Default('') String routeNumber,
    @Default('') String routeName,
    String? firstDeparture,
    String? lastDeparture,
    int? frequencyMinutes,
    @Default(true) bool isActive,
    @Default(<IntracityStop>[]) List<IntracityStop> stops,
  }) = _IntracityRoute;

  const IntracityRoute._();

  factory IntracityRoute.fromJson(Map<String, dynamic> json) =>
      _$IntracityRouteFromJson(json);

  /// "06:30 – 22:00"; saatlerden biri girilmemişse satır hiç yazılmaz.
  String? get serviceHoursLabel {
    final start = DepartureTimes.minutesOfDay(firstDeparture);
    final end = DepartureTimes.minutesOfDay(lastDeparture);
    if (start == null || end == null) return null;
    return '${DepartureTimes.label(start)} – ${DepartureTimes.label(end)}';
  }

  /// "Yaklaşık 20 dakikada bir".
  String? get frequencyLabel {
    final minutes = frequencyMinutes;
    if (minutes == null || minutes <= 0) return null;
    return 'Yaklaşık $minutes dakikada bir';
  }

  /// `stopOrder`a göre sıralı duraklar (sunucu sıralı gönderiyor; yine de
  /// istemcide sabitleniyor — zaman çizelgesi sırası yanlışsa hat okunmaz).
  List<IntracityStop> get orderedStops {
    final sorted = [...stops]..sort((a, b) => a.stopOrder.compareTo(b.stopOrder));
    return sorted;
  }

  bool get hasStops => stops.isNotEmpty;

  IntracityStatus status({DateTime? now}) => DepartureTimes.intracity(
    first: firstDeparture,
    last: lastDeparture,
    frequencyMinutes: frequencyMinutes,
    now: now,
  );

  String get shareText {
    final lines = <String>[
      '🚌 $routeNumber numaralı hat — $routeName',
      if (serviceHoursLabel != null) 'Servis saatleri: $serviceHoursLabel',
      ?frequencyLabel,
      if (hasStops)
        'Duraklar: ${orderedStops.map((stop) => stop.stopName).join(' → ')}',
      '',
      '— Kadirli uygulaması',
    ];
    return lines.join('\n');
  }
}

/// Güzergâh durağı (`StopDto`). [timeFromStart] ilk duraktan itibaren dakika.
@freezed
abstract class IntracityStop with _$IntracityStop {
  const factory IntracityStop({
    required String id,
    @Default('') String stopName,
    @Default(0) int stopOrder,
    int? timeFromStart,
  }) = _IntracityStop;

  const IntracityStop._();

  factory IntracityStop.fromJson(Map<String, dynamic> json) =>
      _$IntracityStopFromJson(json);

  /// "+7 dk" — ilk durak için (0) yazılmaz, "başlangıç" zaten belli.
  String? get offsetLabel {
    final minutes = timeFromStart;
    if (minutes == null || minutes <= 0) return null;
    return '+$minutes dk';
  }
}
