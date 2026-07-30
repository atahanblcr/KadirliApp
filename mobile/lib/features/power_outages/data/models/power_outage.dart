import 'package:freezed_annotation/freezed_annotation.dart';

part 'power_outage.freezed.dart';
part 'power_outage.g.dart';

/// `GET /v1/power-outages` öğesi (`PowerOutageDto`).
///
/// ⚠️ Uç **tüm** kayıtları döner (tarih filtresi yok, sayfalama yok) — geçmiş
/// kesintileri ayıklamak istemcinin işi ([isActive]/[isUpcoming]).
@freezed
abstract class PowerOutage with _$PowerOutage {
  const factory PowerOutage({
    required String id,
    String? neighborhood,
    required DateTime startTime,
    required DateTime endTime,
    String? reason,
  }) = _PowerOutage;

  const PowerOutage._();

  factory PowerOutage.fromJson(Map<String, dynamic> json) =>
      _$PowerOutageFromJson(json);

  /// Şu an sürüyor mu?
  bool isActive({DateTime? now}) {
    final reference = (now ?? DateTime.now()).toUtc();
    return !startTime.toUtc().isAfter(reference) && endTime.toUtc().isAfter(reference);
  }

  /// Henüz başlamadı mı (planlı)?
  bool isUpcoming({DateTime? now}) =>
      startTime.toUtc().isAfter((now ?? DateTime.now()).toUtc());

  /// Ana Sayfa şeridinde gösterilmeye değer mi (süren ya da gelecek)?
  bool isRelevant({DateTime? now}) => isActive(now: now) || isUpcoming(now: now);
}
