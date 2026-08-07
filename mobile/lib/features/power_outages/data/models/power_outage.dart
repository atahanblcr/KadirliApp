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

    /// Mahalle **adı**. Faz 12.3'ten beri sunucuda sözlükten türetiliyor (yazım
    /// farkı yok); eski kayıtlarda hâlâ serbest metin olabilir.
    String? neighborhood,

    /// Faz 12.3 (yeni): sözlükteki mahalle kimliği. Eski sürümlerde ve şehir
    /// geneli kesintilerde `null`.
    String? neighborhoodId,

    /// Faz 12.3 (yeni): mahallenin hangi kısmı ("Atatürk Caddesi ve çevresi").
    String? areaDetail,

    required DateTime startTime,
    required DateTime endTime,
    String? reason,

    /// Faz 12.3 (yeni): bu kesinti için üretilmiş duyuru. Dolu olması
    /// "bildirim gönderildi" demektir.
    String? announcementId,
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

  /// Bu kesinti mahalle sözlüğüne bağlı mı (Faz 12.3).
  bool get hasNeighborhoodRef => (neighborhoodId?.trim().isNotEmpty ?? false);

  /// Mahalle bilgisi hiç yok → şehir geneli, herkesi ilgilendirir.
  bool get isCityWide => (neighborhood?.trim().isEmpty ?? true);

  /// Kullanıcının mahallesiyle eşleşiyor mu.
  ///
  /// 🔑 Faz 12.3: **önce kimlik, sonra ad.** Uç 12.3'ten beri `neighborhoodId`
  /// döndürüyor; kimlik varsa ad karşılaştırması hiç yapılmaz — "Cengiz Topel"
  /// ile "Cengiz Topel Mahallesi" yazım farkı yüzünden sessizce eşleşmeyen
  /// kayıtlar tam olarak buradan doğuyordu.
  ///
  /// ⚠️ Ad karşılaştırması **kaldırılmadı**: mağazadaki eski sürümlerin değil,
  /// sunucudaki eski *kayıtların* hatırına — geri doldurmada eşleşmemiş bir
  /// kesintinin `neighborhoodId`'si hâlâ boş gelir ve o kayıt için elimizdeki
  /// tek şey ad.
  bool matchesNeighborhood(String? userNeighborhood, {String? userNeighborhoodId}) {
    final myId = userNeighborhoodId?.trim();
    final hereId = neighborhoodId?.trim();
    if (myId != null && myId.isNotEmpty && hereId != null && hereId.isNotEmpty) {
      return myId == hereId;
    }

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
