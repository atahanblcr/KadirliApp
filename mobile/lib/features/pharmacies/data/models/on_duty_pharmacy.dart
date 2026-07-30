import 'package:freezed_annotation/freezed_annotation.dart';

part 'on_duty_pharmacy.freezed.dart';
part 'on_duty_pharmacy.g.dart';

/// `GET /v1/pharmacies/on-duty` öğesi (`OnDutyPharmacyDto`).
///
/// Nöbet kaydı + eczane bilgisi tek satırda gelir (backend bilinçli olarak
/// birleştirdi — mobil tek istekle gösterebilsin). Aynı güne birden fazla
/// eczane atanabilir, bu yüzden uç **liste** döner.
///
/// ⚠️ `startTime`/`endTime` sunucudan `"HH:mm"` **metni** olarak gelir
/// (tarih değil) — dönüştürmeye çalışma.
@freezed
abstract class OnDutyPharmacy with _$OnDutyPharmacy {
  const factory OnDutyPharmacy({
    required String scheduleId,
    required DateTime dutyDate,
    @Default('') String startTime,
    @Default('') String endTime,
    required String pharmacyId,
    required String name,
    String? address,
    String? phone,
    double? latitude,
    double? longitude,
    String? pharmacistName,
    String? workingHours,
  }) = _OnDutyPharmacy;

  const OnDutyPharmacy._();

  factory OnDutyPharmacy.fromJson(Map<String, dynamic> json) =>
      _$OnDutyPharmacyFromJson(json);

  /// "08:30 - 08:30" gibi nöbet aralığı; saatler boşsa null.
  String? get dutyHours {
    if (startTime.isEmpty || endTime.isEmpty) return null;
    return '$startTime - $endTime';
  }

  bool get hasLocation => latitude != null && longitude != null;
}
