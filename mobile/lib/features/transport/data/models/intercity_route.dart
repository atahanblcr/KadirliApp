import 'package:freezed_annotation/freezed_annotation.dart';

import '../../../../core/utils/utils.dart';
import '../../application/departure_times.dart';
import '../../application/operating_days.dart';
import '../../application/transport_vehicle.dart';

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
///
/// ⚠️ **Sefer günleri sunucuda ELENMEZ, yalnız bildirilir** (görünmez sözleşme
/// #46): uç Pazar günü de hafta içi seferini gönderir. Ayıklama istemcide de
/// yapılmaz — saatler *gösterilir*, hangi gün çalıştıkları **rozetle söylenir**
/// ve "sıradaki sefer" hesabı günü dikkate alır. Listeyi süzmek, bir hafta içi
/// hattının kartını Pazar günü **boş** bırakırdı.
@freezed
abstract class IntercityRoute with _$IntercityRoute {
  const factory IntercityRoute({
    required String id,
    required String destination,
    double? price,
    int? durationMinutes,
    String? company,
    @Default(true) bool isActive,

    /// Faz 12.5/12.6 — `"bus"` | `"minibus"`. Varsayılan `bus`: 12.5 öncesi
    /// kayıtlar migration'da böyle göç etti, alan gelmezse de anlamı budur.
    @Default('bus') String vehicleType,

    /// Faz 12.6 — kalkış noktası; `null` ise panelde **girilmemiş** demektir.
    /// Uydurulmaz: "otogardan kalkar" tahmini vatandaşı yanlış yere götürür
    /// (12.5'in "geri doldurma YOK" kararının mobil karşılığı).
    String? departurePointName,
    String? departurePointAddress,
    double? departurePointLatitude,
    double? departurePointLongitude,
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

  /// Faz 12.6 — araç tipi; **tanınmayan değerde `null`** (rozet çizilmez,
  /// uydurma etiket basılmaz).
  TransportVehicle? get vehicle => TransportVehicle.parse(vehicleType);

  /// Saate göre sıralı seferler — saat **ve** hangi günler çalıştığı.
  /// Bozuk saat taşıyan kayıt burada eleniyor (o satır hiç çizilmez).
  List<DepartureOption> get departures {
    final entries = <DepartureOption>[];
    for (final schedule in schedules) {
      final minutes = DepartureTimes.minutesOfDay(schedule.departureTime);
      if (minutes != null) {
        entries.add(
          DepartureOption(minutesOfDay: minutes, days: schedule.operatingDays),
        );
      }
    }
    entries.sort((a, b) => a.minutesOfDay.compareTo(b.minutesOfDay));
    return entries;
  }

  /// Saate göre sıralı `"HH:mm"` listesi (paylaşım metni ve kısa özetler için).
  List<String> get departureTimes => [
    for (final departure in departures) departure.label,
  ];

  bool get hasDepartures => departures.isNotEmpty;

  /// Hattın **tüm** seferleri her gün mü çalışıyor? Hepsi her günse kartta gün
  /// rozeti gösterilmez — 12.5 öncesi kayıtların tamamı bu durumda ve rozet
  /// şeridi hiçbir şey söylemeden yer kaplardı.
  bool get runsDaily =>
      departures.isEmpty || departures.every((d) => d.days.runsDaily);

  /// 🔴 Gün maskesini hesaba katan sıradaki sefer (12.6'nın çekirdeği).
  NextDeparture? next({DateTime? now}) =>
      DepartureTimes.nextAmong(departures, now: now);

  /// Hattın **bugün** (Kadirli günü) hiç seferi var mı?
  ///
  /// "Bugünkü seferler bitti" ile "Bugün sefer yok" farklı cümlelerdir: hafta
  /// içi çalışan bir hatta Pazar günü "bitti" demek, o gün **hiç olmayan** bir
  /// sefer dizisini ima eder. Küçük bir metin farkı ama vatandaşın kafasındaki
  /// modeli doğru kuran şey bu.
  bool runsToday({DateTime? now}) {
    final today = DepartureTimes.nowWeekday(now: now);
    return departures.any((d) => d.days.runsOnWeekday(today));
  }

  String? get departurePointLabel {
    final value = departurePointName?.trim();
    return (value == null || value.isEmpty) ? null : value;
  }

  String? get departurePointAddressLabel {
    final value = departurePointAddress?.trim();
    return (value == null || value.isEmpty) ? null : value;
  }

  bool get hasDeparturePointCoordinates =>
      departurePointLatitude != null && departurePointLongitude != null;

  /// "Yol tarifi" ancak koordinat **veya** adres varsa anlamlıdır
  /// (`ContactActions` deseni — işlevsiz buton yok).
  bool get canShowDirections =>
      hasDeparturePointCoordinates || departurePointAddressLabel != null;

  /// Koordinat yoksa harita araması için metin: "Kadirli Otogarı, Kadirli".
  /// ⚠️ Yalnız "Otogar" aratmak kullanıcıyı **başka bir şehre** götürebilir
  /// (12.4'te etkinlik `mapQuery`'sinde birebir bu yaşandı).
  String? get departureMapQuery {
    final address = departurePointAddressLabel;
    final name = departurePointLabel;
    final parts = <String>[
      ?name,
      if (address != null && address != name) address,
    ];
    if (parts.isEmpty) return null;
    final query = parts.join(', ');
    return query.toLowerCase().contains('kadirli') ? query : '$query, Kadirli';
  }

  String get shareText {
    final lines = <String>[
      '🚌 Kadirli → $destination',
      ?companyLabel,
      ?vehicle?.label,
      if (departurePointLabel != null) 'Kalkış: ${departurePointLabel!}',
      for (final departure in departures)
        departure.days.runsDaily
            ? departure.label
            : '${departure.label} (${departure.days.label})',
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

    /// Faz 12.5: `["mon","tue",…]` — **kontrat kodları**, Pazartesi'den sıralı.
    /// Alan gelmezse (eski sunucu / eski kayıt) anlamı "her gün"dür.
    @Default(<String>[]) List<String> days,
    @Default(true) bool runsDaily,
  }) = _IntercityDeparture;

  const IntercityDeparture._();

  factory IntercityDeparture.fromJson(Map<String, dynamic> json) =>
      _$IntercityDepartureFromJson(json);

  /// 🔴 Gün dönüşümünün tek sahibi [OperatingDays] — burada ikinci bir eşleme
  /// yazılmaz (görünmez sözleşme #46'nın istemci tarafı).
  ///
  /// `days` boşsa `runsDaily` bayrağına bakılmaz: [OperatingDays.fromCodes]
  /// zaten "boş → her gün" diyor ve iki kaynağın çeliştiği durumda **göstermek**
  /// doğru yön (seferi sessizce gizlemek yerine).
  OperatingDays get operatingDays => OperatingDays.fromCodes(days);
}
