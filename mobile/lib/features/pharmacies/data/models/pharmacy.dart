import 'package:freezed_annotation/freezed_annotation.dart';

part 'pharmacy.freezed.dart';
part 'pharmacy.g.dart';

/// `GET /v1/pharmacies` ve `/v1/pharmacies/{id}` gövdesi (`PharmacyDto`).
///
/// Liste ve detay **aynı şekli** döndürüyor (canlıda doğrulandı) — ayrı bir
/// detay modeli tutmaya gerek yok.
@freezed
abstract class Pharmacy with _$Pharmacy {
  const factory Pharmacy({
    required String id,
    required String name,
    String? address,
    String? phone,
    double? latitude,
    double? longitude,

    /// "08:30 - 19:00" gibi serbest metin (saat aritmetiği yapma).
    String? workingHours,
    String? pharmacistName,
    @Default(true) bool isActive,
  }) = _Pharmacy;

  const Pharmacy._();

  factory Pharmacy.fromJson(Map<String, dynamic> json) =>
      _$PharmacyFromJson(json);

  bool get hasLocation => latitude != null && longitude != null;

  bool get hasPhone => (phone ?? '').trim().isNotEmpty;
}
