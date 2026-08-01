import 'package:freezed_annotation/freezed_annotation.dart';

import '../../../../core/utils/utils.dart';

part 'death_notice.freezed.dart';
part 'death_notice.g.dart';

/// `GET /v1/deaths` ve `/v1/deaths/{id}` gövdesi (`DeathNoticeResponseDto`).
///
/// ⚠️ **Tarih ve saat iki ayrı alan** (11.7 `dutyDate` / 11.10 `eventDate`
/// dersinin üçüncüsü): `funeralDate` "Türkiye günü, 00:00 UTC" olarak yazılıyor
/// → **saat dilimi kaydırılmaz**; `funeralTime` ise `TimeSpan` serileşmesiyle
/// `"13:30:00"` biçiminde gelen duvar saati.
///
/// ⚠️ Public liste **yalnız `approved`** kayıtları döndürür. Detay ucu bir
/// istisna tanır: kaydı **ekleyen kullanıcı** kendi `pending` ilanını görebilir
/// (`RequesterId`) — bildirim gönderen kullanıcı "kaydım kayboldu" sanmasın.
@freezed
abstract class DeathNotice with _$DeathNotice {
  const factory DeathNotice({
    required String id,
    required String deceasedName,
    String? photoFileId,
    String? photoUrl,
    required DateTime funeralDate,
    @Default('00:00:00') String funeralTime,
    String? cemeteryId,
    String? cemeteryName,
    String? mosqueId,
    String? mosqueName,
    String? neighborhoodId,
    String? condolenceAddress,
    double? condolenceLatitude,
    double? condolenceLongitude,
    @Default(false) bool hasCondolenceLocation,
    String? addedBy,
    @Default('approved') String status,
    DateTime? createdAt,
  }) = _DeathNotice;

  const DeathNotice._();

  factory DeathNotice.fromJson(Map<String, dynamic> json) =>
      _$DeathNoticeFromJson(json);

  /// "13:30" — saniye kısmı kullanıcıya gösterilmez.
  String get timeLabel => AppDate.clockLabel(funeralTime);

  /// Cenaze namazının günü + saati, **Türkiye duvar saatiyle**.
  DateTime get funeralAt {
    final day = funeralDate.toUtc();
    final parts = funeralTime.split(':');
    final hour = parts.isNotEmpty ? int.tryParse(parts[0]) ?? 0 : 0;
    final minute = parts.length > 1 ? int.tryParse(parts[1]) ?? 0 : 0;
    return DateTime(day.year, day.month, day.day, hour, minute);
  }

  /// Bugüne göre kaç gün sonra (bugün 0, dün -1). Saat dikkate alınmaz.
  int daysFromToday({DateTime? now}) {
    final today = AppDate.toTurkey(now ?? DateTime.now());
    final reference = DateTime(today.year, today.month, today.day);
    final day = funeralDate.toUtc();
    return DateTime(day.year, day.month, day.day).difference(reference).inDays;
  }

  bool isToday({DateTime? now}) => daysFromToday(now: now) == 0;

  /// Gün bazında geçmişte mi — bugün 15:00'teki cenaze sabah "geçmiş" olmaz.
  bool isPast({DateTime? now}) => daysFromToday(now: now) < 0;

  /// "Bugün" / "Yarın" / "12 Ağustos Çarşamba".
  ///
  /// Vefat ekranında **tek vurgu noktası** budur: kullanıcının aradığı bilgi
  /// "cenaze bugün mü, saat kaçta". Renk/rozet kalabalığı bilinçli olarak yok.
  String dayLabel({DateTime? now}) => switch (daysFromToday(now: now)) {
    0 => 'Bugün',
    1 => 'Yarın',
    -1 => 'Dün',
    _ => AppDate.dayWithWeekday(funeralDate),
  };

  /// "Bugün, 13:30" — kart ve detaydaki tek satırlık cenaze zamanı.
  String funeralLabel({DateTime? now}) => '${dayLabel(now: now)}, $timeLabel';

  /// Cenaze namazına kalan süre; geçtiyse `null`.
  ///
  /// Yalnız **bugünkü** cenazede gösterilir: "3 gün 4 saat kaldı" bilgisi bir
  /// vefat ilanında geri sayım gibi durur, oysa aynı bilgi tarihte zaten yazılı.
  Duration? timeUntilFuneral({DateTime? now}) {
    if (!isToday(now: now)) return null;
    final remaining = funeralAt.difference(AppDate.toTurkey(now ?? DateTime.now()));
    return remaining.isNegative ? null : remaining;
  }

  bool get isApproved => status == 'approved';
  bool get isPending => status == 'pending';

  /// Taziye adresi ya da koordinat verilmiş mi (harita butonu için).
  bool get hasCondolencePlace =>
      hasCondolenceLocation ||
      (condolenceAddress != null && condolenceAddress!.trim().isNotEmpty);

  /// Paylaşım metni — Kadirli'de vefat haberi WhatsApp gruplarından yayılıyor
  /// (11.6 `AppShare` kararının en doğrudan karşılığı).
  String shareText({DateTime? now}) {
    final lines = <String>[
      '$deceasedName vefat etmiştir.',
      'Cenaze namazı: ${AppDate.date(funeralDate)} $timeLabel',
      if (mosqueName != null) 'Cami: $mosqueName',
      if (cemeteryName != null) 'Defin: $cemeteryName',
      if (condolenceAddress != null && condolenceAddress!.trim().isNotEmpty)
        'Taziye: ${condolenceAddress!.trim()}',
      '',
      'Merhuma Allah\'tan rahmet, ailesine başsağlığı dileriz.',
    ];
    return lines.join('\n');
  }
}
