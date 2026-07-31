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

  /// Bitmiş mi?
  bool isPast({DateTime? now}) =>
      !endTime.toUtc().isAfter((now ?? DateTime.now()).toUtc());

  /// Üç durumdan biri — liste gruplaması ve rozet buradan okunur.
  PowerOutageStatus status({DateTime? now}) {
    if (isActive(now: now)) return PowerOutageStatus.active;
    if (isUpcoming(now: now)) return PowerOutageStatus.upcoming;
    return PowerOutageStatus.past;
  }

  /// Planlanan toplam kesinti süresi.
  Duration get duration => endTime.difference(startTime);

  /// Süren kesintide bitişe, planlıda başlangıca kalan süre; geçmişte null.
  Duration? remaining({DateTime? now}) {
    final reference = (now ?? DateTime.now()).toUtc();
    return switch (status(now: reference)) {
      PowerOutageStatus.active => endTime.toUtc().difference(reference),
      PowerOutageStatus.upcoming => startTime.toUtc().difference(reference),
      PowerOutageStatus.past => null,
    };
  }

  /// Mahalle adı — boş/whitespace gelirse şehir geneli sayılır.
  String get placeLabel {
    final name = neighborhood?.trim();
    return (name == null || name.isEmpty) ? 'Kadirli geneli' : name;
  }

  /// Kullanıcının mahallesiyle eşleşiyor mu (ad üzerinden — uç mahalle **id**'si
  /// döndürmüyor, yalnız ad; büyük/küçük harf ve boşluk farkı yok sayılır).
  bool matchesNeighborhood(String? userNeighborhood) {
    final mine = userNeighborhood?.trim().toLowerCase();
    final here = neighborhood?.trim().toLowerCase();
    if (mine == null || mine.isEmpty || here == null || here.isEmpty) {
      return false;
    }
    return mine == here;
  }
}

/// Kesintinin zamana göre durumu.
enum PowerOutageStatus { active, upcoming, past }
