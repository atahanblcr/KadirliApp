import 'package:freezed_annotation/freezed_annotation.dart';

part 'paged_result.freezed.dart';
part 'paged_result.g.dart';

/// Sayfalı uçların `data` gövdesi (API_CONTRACT §5).
///
/// ```json
/// { "items": [...], "totalCount": 137, "pageSize": 20,
///   "currentPage": 1, "totalPages": 7 }
/// ```
///
/// ⚠️ Public uçlarda `limit` en fazla **50**'dir (aşan değer sessizce kırpılır),
/// bu yüzden `pageSize` istemcinin istediği değil, sunucunun uyguladığı değerdir.
@Freezed(genericArgumentFactories: true)
abstract class PagedResult<T> with _$PagedResult<T> {
  const factory PagedResult({
    @Default(<Never>[]) List<T> items,
    @Default(0) int totalCount,
    @Default(0) int pageSize,
    @Default(1) int currentPage,
    @Default(0) int totalPages,
  }) = _PagedResult<T>;

  const PagedResult._();

  factory PagedResult.fromJson(
    Map<String, dynamic> json,
    T Function(Object?) fromJsonT,
  ) => _$PagedResultFromJson(json, fromJsonT);

  /// Sonsuz kaydırmada "daha var mı" kontrolü (11.6'dan itibaren listeler).
  bool get hasNextPage => currentPage < totalPages;

  bool get isEmpty => items.isEmpty;
}
