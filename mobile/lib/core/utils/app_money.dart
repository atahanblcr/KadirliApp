import 'package:intl/intl.dart';

/// Para biçimleme — ilan fiyatları (11.8), kampanya tutarları (11.10).
///
/// Sunucu `decimal` gönderiyor (`750000.00`); kullanıcıya Türkçe binlik
/// ayracıyla ve **kuruş varsa** kuruşlu gösterilir: `750.000 ₺`, `8.500,50 ₺`.
/// Kuruşu sıfır olan fiyatta ".00" göstermek pazaryerinde gürültü yapıyor.
abstract final class AppMoney {
  static const String symbol = '₺';
  static const String _locale = 'tr_TR';

  static final NumberFormat _whole = NumberFormat('#,##0', _locale);
  static final NumberFormat _fractional = NumberFormat('#,##0.00', _locale);

  /// "750.000 ₺" — kuruş yalnız gerçekten varsa yazılır.
  static String amount(num value) {
    final hasKurus = (value - value.roundToDouble()).abs() >= 0.005;
    return '${(hasKurus ? _fractional : _whole).format(value)} $symbol';
  }

  /// Fiyatsız ilan (`price: null`) için nötr metin — "0 ₺" YAZILMAZ,
  /// bedava sanılır.
  static String price(num? value, {String empty = 'Fiyat belirtilmemiş'}) =>
      value == null ? empty : amount(value);

  /// Filtre rozetinde okunacak aralık etiketi.
  /// `(null, null)` → null; `(1000, null)` → "1.000 ₺ ve üzeri".
  static String? rangeLabel(num? min, num? max) {
    if (min == null && max == null) return null;
    if (min != null && max != null) return '${amount(min)} – ${amount(max)}';
    if (min != null) return '${amount(min)} ve üzeri';
    return '${amount(max!)} ve altı';
  }

  /// Kullanıcının yazdığı fiyat metnini sayıya çevirir.
  ///
  /// Türkçe klavyede "1.250,50" da "1250.50" da yazılabiliyor; ikisi de
  /// kabul edilir, geçersiz metin `null` döner (filtre uygulanmaz).
  static num? parse(String? raw) {
    final text = raw?.trim();
    if (text == null || text.isEmpty) return null;

    var cleaned = text.replaceAll(RegExp(r'[^\d.,]'), '');
    if (cleaned.isEmpty) return null;

    final lastComma = cleaned.lastIndexOf(',');
    final lastDot = cleaned.lastIndexOf('.');
    if (lastComma > lastDot) {
      // "1.250,50" → virgül ondalık ayracı, noktalar binlik.
      cleaned = cleaned.replaceAll('.', '').replaceFirst(',', '.');
    } else if (lastComma < 0 && lastDot >= 0) {
      // Yalnız nokta var → belirsiz: "50.000" binlik mi, "1250.50" ondalık mı?
      // Türkçe klavyede binlik ayracı **nokta**; her noktadan sonra tam 3 hane
      // varsa binlik kabul edilir ("50.000" → 50000), aksi hâlde ondalık
      // ("1250.50" → 1250,5).
      final groups = cleaned.split('.');
      final isThousandsGrouped =
          groups.length > 1 &&
          groups.skip(1).every((group) => group.length == 3);
      if (isThousandsGrouped) cleaned = cleaned.replaceAll('.', '');
    } else {
      // "1,250.50" → virgül binlik, nokta ondalık.
      cleaned = cleaned.replaceAll(',', '');
    }

    final value = num.tryParse(cleaned);
    if (value == null || value < 0) return null;
    return value;
  }
}
