import 'package:freezed_annotation/freezed_annotation.dart';

part 'taxi_driver.freezed.dart';
part 'taxi_driver.g.dart';

/// `GET /v1/taxis/drivers` ve `/drivers/{id}` gövdesi (`TaxiDriverResponseDto`).
///
/// ⚠️ Public uç **yalnız doğrulanmış + aktif** sürücüleri döndürür
/// (`OnlyPublic=true` controller'da sabit; istemcinin `?isVerified=` /
/// `?isActive=` parametreleri yok sayılır) → istemci ayrıca süzmez, "doğrulandı"
/// bilgisi kullanıcıya **güven işareti** olarak gösterilir.
///
/// ⚠️ [phone] listede de geliyor; yine de arama `POST /drivers/{id}/call`
/// üzerinden yapılır — çağrı kaydı (`taxi_calls`) ve sürücünün `total_calls`
/// sayacı esnafın ölçümü (10.12). Uç aranacak telefonu döndürür, çeviriciyi
/// istemci açar.
@freezed
abstract class TaxiDriver with _$TaxiDriver {
  const factory TaxiDriver({
    required String id,
    String? userId,
    required String name,
    @Default('') String phone,
    String? plaka,
    String? vehicleInfo,
    @Default(true) bool isVerified,
    @Default(true) bool isActive,
  }) = _TaxiDriver;

  const TaxiDriver._();

  factory TaxiDriver.fromJson(Map<String, dynamic> json) =>
      _$TaxiDriverFromJson(json);

  String? get plateLabel {
    final value = plaka?.trim();
    return (value == null || value.isEmpty) ? null : value;
  }

  String? get vehicleLabel {
    final value = vehicleInfo?.trim();
    return (value == null || value.isEmpty) ? null : value;
  }

  bool get hasPhone => phone.trim().isNotEmpty;

  String get shareText {
    final lines = <String>[
      '🚕 $name',
      if (plateLabel != null) 'Plaka: $plateLabel',
      ?vehicleLabel,
      if (hasPhone) 'Telefon: ${phone.trim()}',
      '',
      '— Kadirli uygulaması',
    ];
    return lines.join('\n');
  }
}
