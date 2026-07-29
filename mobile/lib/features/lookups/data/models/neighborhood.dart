import 'package:freezed_annotation/freezed_annotation.dart';

part 'neighborhood.freezed.dart';
part 'neighborhood.g.dart';

/// `GET /v1/neighborhoods` öğesi (`NeighborhoodLookupDto`).
///
/// Sunucu yalnız **aktif** mahalleleri, `displayOrder`+ad sırasıyla döner ve
/// sonucu 15 dk cache'ler — istemcinin ayrıca sıralaması/filtrelemesi gerekmez.
@freezed
abstract class Neighborhood with _$Neighborhood {
  const factory Neighborhood({
    required String id,
    required String name,
    @Default('') String slug,

    /// `mahalle` / `köy` gibi tür etiketi — boş olabilir.
    String? type,
    double? latitude,
    double? longitude,
    @Default(0) int displayOrder,
  }) = _Neighborhood;

  const Neighborhood._();

  factory Neighborhood.fromJson(Map<String, dynamic> json) => _$NeighborhoodFromJson(json);

  /// Seçim listelerinde gösterilecek etiket ("Savrun (köy)" gibi).
  String get label {
    final kind = type?.trim();
    return (kind == null || kind.isEmpty) ? name : '$name ($kind)';
  }
}
