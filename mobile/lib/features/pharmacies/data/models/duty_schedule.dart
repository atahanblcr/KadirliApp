import 'package:freezed_annotation/freezed_annotation.dart';

part 'duty_schedule.freezed.dart';
part 'duty_schedule.g.dart';

/// `GET /v1/pharmacies/schedule?year=&month=` öğesi (`PharmacyScheduleDto`).
///
/// ⚠️ İki kontrat inceliği (canlıda doğrulandı):
/// - `startTime`/`endTime` **"HH:mm" metni**, tarih değil.
/// - `dutyDate` **Türkiye gününün UTC gece yarısı** (`2026-07-03T00:00:00Z`)
///   — bu yüzden gün karşılaştırmasında saat dilimi kaydırması yapılmaz,
///   [dayKey] ham UTC alanlarından üretilir (bkz. notu).
@freezed
abstract class DutySchedule with _$DutySchedule {
  const factory DutySchedule({
    required String id,
    required DateTime dutyDate,
    @Default('') String startTime,
    @Default('') String endTime,
    required String pharmacyId,
    required String pharmacyName,
    String? source,
  }) = _DutySchedule;

  const DutySchedule._();

  factory DutySchedule.fromJson(Map<String, dynamic> json) =>
      _$DutyScheduleFromJson(json);

  /// Takvim ızgarasında eşleştirme anahtarı: `2026-07-03`.
  ///
  /// **Neden `AppDate.toTurkey` kullanılmıyor:** sunucu `duty_date`'i zaten
  /// "Türkiye günü, saat 00:00 UTC" konvansiyonuyla yazıyor (backend
  /// `TurkeyClock`). Üstüne +3 saat eklemek günü kaydırmaz ama gereksizdir;
  /// ham UTC alanları doğrudan doğru günü verir.
  String get dayKey {
    final date = dutyDate.toUtc();
    final month = date.month.toString().padLeft(2, '0');
    final day = date.day.toString().padLeft(2, '0');
    return '${date.year}-$month-$day';
  }

  /// Nöbet aralığı; saatlerden biri boşsa null.
  String? get hours {
    if (startTime.isEmpty || endTime.isEmpty) return null;
    return '$startTime - $endTime';
  }

  /// Nöbet gece yarısını aşıyor mu ("19:00 - 09:00")? Ekranda "ertesi gün"
  /// notu düşmek için — kullanıcı 01:00'de eczane arıyorsa bu bilgi kritik.
  bool get crossesMidnight {
    final start = _minutes(startTime);
    final end = _minutes(endTime);
    if (start == null || end == null) return false;
    return end <= start;
  }

  static int? _minutes(String value) {
    final parts = value.split(':');
    if (parts.length < 2) return null;
    final hour = int.tryParse(parts[0]);
    final minute = int.tryParse(parts[1]);
    if (hour == null || minute == null) return null;
    return hour * 60 + minute;
  }
}
