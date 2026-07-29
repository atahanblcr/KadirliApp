import 'package:intl/intl.dart';

/// Tarih/saat biçimleme (API_CONTRACT §6).
///
/// Sunucu **UTC ISO-8601** gönderir; kullanıcıya Türkiye saatiyle gösterilir.
///
/// **KARAR — sabit +03 ofseti:** Türkiye 2016'dan beri kalıcı UTC+3 kullanıyor
/// (yaz saati uygulaması yok). Bu yüzden `timezone` paketinin tam IANA
/// veritabanını taşımak yerine sabit ofset uygulanıyor: sonuç cihazın saat
/// dilimi ne olursa olsun aynı (yurt dışındaki kullanıcı da "Kadirli saatini"
/// görür) ve testler makineden bağımsız çalışır. Türkiye ofseti değişirse
/// tek sabit güncellenir.
abstract final class AppDate {
  static const Duration turkeyOffset = Duration(hours: 3);
  static const String _locale = 'tr_TR';

  /// UTC (ya da yerel) bir zamanı Türkiye duvar saatine çevirir.
  static DateTime toTurkey(DateTime value) => value.toUtc().add(turkeyOffset);

  static DateTime get nowInTurkey => toTurkey(DateTime.now());

  /// "12 Ağustos 2026"
  static String date(DateTime value) => DateFormat('d MMMM y', _locale).format(toTurkey(value));

  /// "12 Ağu"
  static String dateShort(DateTime value) => DateFormat('d MMM', _locale).format(toTurkey(value));

  /// "14:30"
  static String time(DateTime value) => DateFormat('HH:mm', _locale).format(toTurkey(value));

  /// "12 Ağustos 2026, 14:30"
  static String dateTime(DateTime value) =>
      DateFormat('d MMMM y, HH:mm', _locale).format(toTurkey(value));

  /// "12 Ağustos Salı" — takvim/nöbet başlıkları için.
  static String dayWithWeekday(DateTime value) =>
      DateFormat('d MMMM EEEE', _locale).format(toTurkey(value));

  /// Aynı güne mi düşüyor (Türkiye saatine göre)?
  static bool isSameDay(DateTime a, DateTime b) {
    final x = toTurkey(a);
    final y = toTurkey(b);
    return x.year == y.year && x.month == y.month && x.day == y.day;
  }

  /// Liste kartlarındaki göreli zaman: "az önce", "5 dakika önce",
  /// "3 saat önce", "dün", "4 gün önce" — 7 günden eskisi tam tarih.
  static String relative(DateTime value, {DateTime? now}) {
    final reference = now ?? DateTime.now();
    final difference = reference.toUtc().difference(value.toUtc());

    if (difference.isNegative) {
      // Gelecek tarih (planlanmış etkinlik/duyuru) — tam tarih daha anlaşılır.
      return dateTime(value);
    }
    if (difference.inMinutes < 1) return 'az önce';
    if (difference.inMinutes < 60) return '${difference.inMinutes} dakika önce';
    if (difference.inHours < 24) return '${difference.inHours} saat önce';

    final dayDifference = _calendarDayDifference(value, reference);
    if (dayDifference == 1) return 'dün';
    if (dayDifference < 7) return '$dayDifference gün önce';
    return date(value);
  }

  static int _calendarDayDifference(DateTime value, DateTime reference) {
    final a = toTurkey(value);
    final b = toTurkey(reference);
    return DateTime(b.year, b.month, b.day).difference(DateTime(a.year, a.month, a.day)).inDays;
  }

  /// `yyyy-MM-dd` — sunucunun `?date=` / takvim parametreleri için
  /// (Türkiye günü; nöbetçi eczane "bugün" hesabı sunucuda da TR saatiyle).
  static String isoDay(DateTime value) => DateFormat('yyyy-MM-dd').format(toTurkey(value));

  /// Ulaşım uçlarındaki `"HH:mm"` string'ini güvenle gösterime hazırlar
  /// (bozuksa olduğu gibi döner).
  static String clockLabel(String raw) {
    final parts = raw.split(':');
    if (parts.length < 2) return raw;
    final hour = int.tryParse(parts[0]);
    final minute = int.tryParse(parts[1]);
    if (hour == null || minute == null) return raw;
    return '${hour.toString().padLeft(2, '0')}:${minute.toString().padLeft(2, '0')}';
  }
}
