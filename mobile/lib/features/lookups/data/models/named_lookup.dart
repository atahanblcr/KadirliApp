import 'package:freezed_annotation/freezed_annotation.dart';

part 'named_lookup.freezed.dart';
part 'named_lookup.g.dart';

/// Sunucunun `NamedLookupDto`'su — mezarlık ve cami listelerinin ortak gövdesi
/// (`GET /v1/deaths/cemeteries`, `GET /v1/deaths/mosques`).
///
/// Adres ve koordinat **opsiyonel** (seed'de çoğu boş): vefat detayında
/// "Yol tarifi" ancak bunlardan biri doluysa çizilir — `ContactActions`'ın
/// "veri yoksa buton yok" kuralı.
@freezed
abstract class NamedLookup with _$NamedLookup {
  const factory NamedLookup({
    required String id,
    required String name,
    String? address,
    double? latitude,
    double? longitude,
  }) = _NamedLookup;

  const NamedLookup._();

  factory NamedLookup.fromJson(Map<String, dynamic> json) =>
      _$NamedLookupFromJson(json);

  bool get hasLocation => latitude != null && longitude != null;
}
