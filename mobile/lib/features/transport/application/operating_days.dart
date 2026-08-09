import 'package:flutter/foundation.dart';

/// Faz 12.6 — bir seferin **hangi günler çalıştığı**; sunucudaki
/// `OperatingDays` değer nesnesinin mobil karşılığı.
///
/// 🔴 **Gün dönüşümünün mobildeki TEK SAHİBİ burasıdır** (görünmez sözleşme
/// #46'nın istemci tarafı). Sunucu maskesi **Pazartesi=1**'den başlar; Dart'ın
/// `DateTime.weekday`'i de **Pazartesi=1 … Pazar=7** olduğu için .NET'teki
/// "Pazar=0" kayması burada *yok* — ama tam bu yüzden tehlikeli: `1 << weekday`
/// ya da `weekday % 7` yazan ikinci bir eşleme **bir gün kaydırır**, ekran
/// açılır, hata vermez ve yalnız gün yanlış olur. Doğru bağıntı tek satırda:
/// `bit = 1 << (weekday - 1)`.
///
/// ⚠️ Kodlar (`"mon"`…`"sun"`) **kontrattır** — sunucu `days` alanında bunları
/// gönderiyor. Türkçe karşılıkları burada üretilir, sunucudan gelmez.
@immutable
class OperatingDays {
  const OperatingDays(this.mask);

  static const int monday = 1;
  static const int tuesday = 2;
  static const int wednesday = 4;
  static const int thursday = 8;
  static const int friday = 16;
  static const int saturday = 32;
  static const int sunday = 64;

  /// Her gün — 12.5 öncesinden kalan bütün seferlerin değeri.
  static const int dailyMask =
      monday | tuesday | wednesday | thursday | friday | saturday | sunday;

  static const int weekdaysMask =
      monday | tuesday | wednesday | thursday | friday;

  static const int weekendMask = saturday | sunday;

  static const OperatingDays daily = OperatingDays(dailyMask);

  /// Kod → bit, **Pazartesi'den Pazar'a** (kontrat sırası).
  static const List<(int, String, String)> _days = [
    (monday, 'mon', 'Pzt'),
    (tuesday, 'tue', 'Sal'),
    (wednesday, 'wed', 'Çar'),
    (thursday, 'thu', 'Per'),
    (friday, 'fri', 'Cum'),
    (saturday, 'sat', 'Cmt'),
    (sunday, 'sun', 'Paz'),
  ];

  final int mask;

  /// Sunucudan gelen kod listesinden maske.
  ///
  /// 🔴 **Boş/eksik liste "hiçbir gün" DEĞİL, "her gün" sayılır.** Alan 12.5'te
  /// additive olarak eklendi; tanımadığımız ya da hiç gelmeyen bir `days`'i
  /// "hiç çalışmıyor" saymak seferi ekrandan **sessizce silerdi** — oysa
  /// sunucunun `runsDaily` varsayılanı `true` ve uç seferleri günlere göre
  /// *elemiyor*, yalnız *bildiriyor* (görünmez sözleşme #46). Şüphede kalınca
  /// **göstermek** doğru yön.
  ///
  /// Tanınmayan kod **yok sayılır** (bir yazım hatası listeyi boşaltmamalı).
  factory OperatingDays.fromCodes(Iterable<String>? codes) {
    if (codes == null) return daily;

    var mask = 0;
    for (final raw in codes) {
      final code = raw.trim().toLowerCase();
      for (final (bit, known, _) in _days) {
        if (code == known) mask |= bit;
      }
    }

    return mask == 0 ? daily : OperatingDays(mask);
  }

  bool get runsDaily => mask == dailyMask;
  bool get runsWeekdaysOnly => mask == weekdaysMask;
  bool get runsWeekendOnly => mask == weekendMask;

  /// 🔴 Pazar kayması **yalnız burada** çözülür — `DateTime.weekday`
  /// Pazartesi=1 … Pazar=7.
  static int bitForWeekday(int weekday) {
    if (weekday < DateTime.monday || weekday > DateTime.sunday) return 0;
    return 1 << (weekday - 1);
  }

  bool runsOnWeekday(int weekday) => (mask & bitForWeekday(weekday)) != 0;

  bool runsOn(DateTime date) => runsOnWeekday(date.weekday);

  /// [fromWeekday] gününden başlayarak (o gün **dâhil**) seferin çalıştığı ilk
  /// güne kaç gün olduğu; hiç çalışmıyorsa `null`.
  ///
  /// ⚠️ Döngü **7 turdur, 6 değil**: yalnız Pazartesi çalışan bir sefere
  /// Pazartesi günü sorulursa cevap 0'dır, Salı sorulursa 6'dır — ama
  /// "bugünü atlayıp bir hafta sonrası" hesabı [DepartureTimes] tarafında
  /// 7. ofsetle yapılır (bkz. `nextAmong`).
  int? daysUntilNext(int fromWeekday) {
    if (mask == 0) return null;

    for (var offset = 0; offset < 7; offset++) {
      // Dart'ta gün 1..7 → 0 tabanına in, mod al, geri çık.
      final weekday = ((fromWeekday - 1 + offset) % 7) + 1;
      if (runsOnWeekday(weekday)) return offset;
    }

    return null;
  }

  /// DTO'ya çıkan kodlar, Pazartesi'den Pazar'a sıralı.
  List<String> get codes => [
    for (final (bit, code, _) in _days)
      if ((mask & bit) != 0) code,
  ];

  /// "Pzt · Çar · Cum" — kısa gün adları.
  List<String> get shortNames => [
    for (final (bit, _, name) in _days)
      if ((mask & bit) != 0) name,
  ];

  /// Kartta gösterilen rozet metni.
  ///
  /// "Her gün" bilinçli olarak **yazılır**: rozetsiz bir saat "gün bilgisi yok"
  /// mu yoksa "her gün" mü belli olmaz. Çağıran ekran her gün çalışan seferde
  /// rozeti gizlemeyi seçebilir ([runsDaily] ile) — karar orada verilir, burada
  /// metin her zaman doğrudur.
  String get label {
    if (runsDaily) return 'Her gün';
    if (runsWeekdaysOnly) return 'Hafta içi';
    if (runsWeekendOnly) return 'Hafta sonu';
    final names = shortNames;
    if (names.isEmpty) return 'Sefer günü belirtilmemiş';
    return names.join(' · ');
  }

  /// Ekran okuyucu için açık metin — "Pzt · Çar" kısaltması sesli okunduğunda
  /// anlaşılmaz.
  String get semanticsLabel {
    if (runsDaily) return 'her gün';
    if (runsWeekdaysOnly) return 'hafta içi her gün';
    if (runsWeekendOnly) return 'hafta sonu';
    const long = {
      'Pzt': 'Pazartesi',
      'Sal': 'Salı',
      'Çar': 'Çarşamba',
      'Per': 'Perşembe',
      'Cum': 'Cuma',
      'Cmt': 'Cumartesi',
      'Paz': 'Pazar',
    };
    final names = shortNames;
    if (names.isEmpty) return 'sefer günü belirtilmemiş';
    return names.map((name) => long[name] ?? name).join(', ');
  }

  /// "Pzt" — [weekday] Dart günü (1..7). Sıradaki sefer satırında kullanılır.
  static String shortNameOfWeekday(int weekday) {
    for (final (bit, _, name) in _days) {
      if (bit == bitForWeekday(weekday)) return name;
    }
    return '';
  }

  @override
  bool operator ==(Object other) => other is OperatingDays && other.mask == mask;

  @override
  int get hashCode => mask.hashCode;

  @override
  String toString() => 'OperatingDays($label)';
}
