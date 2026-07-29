import 'package:freezed_annotation/freezed_annotation.dart';

part 'api_meta.freezed.dart';
part 'api_meta.g.dart';

/// Her yanıtın zarfında dönen `meta` bloğu (API_CONTRACT §2).
///
/// `traceId` sunucu loglarıyla (Seq) eşleşir; hata ekranlarında "destek kodu"
/// olarak gösterilir. Faz 10.13'ten sonra TÜM başarılı yanıtlarda dolu gelir,
/// yine de alanlar savunmacı biçimde opsiyonel tutuldu.
@freezed
abstract class ApiMeta with _$ApiMeta {
  const factory ApiMeta({
    DateTime? timestamp,
    String? path,
    String? traceId,
  }) = _ApiMeta;

  factory ApiMeta.fromJson(Map<String, dynamic> json) => _$ApiMetaFromJson(json);
}
