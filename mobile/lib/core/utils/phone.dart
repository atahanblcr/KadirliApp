import 'package:flutter/services.dart';

/// Türk cep telefonu numarası kuralları — tek yerde.
///
/// Sunucu numaraları **E.164** biçiminde tutuyor (`+905321110001`; seed ve
/// mock veriler de bu biçimde) ve OTP anahtarları ham telefon string'iyle
/// üretiliyor → istemci **her zaman** `+90` önekli normalize biçim gönderir,
/// aksi halde aynı kullanıcı iki farklı OTP kaydı yaratır.
///
/// Kullanıcı arayüzünde yalnız ulusal 10 hane girilir (önek sabit gösterilir).
abstract final class AppPhone {
  static const countryCode = '+90';

  /// `5xx xxx xx xx` → 10 hane.
  static const nationalLength = 10;

  /// Girdi maskesindeki grup uzunlukları (532 111 00 01).
  static const _groups = [3, 3, 2, 2];

  static String digitsOf(String input) => input.replaceAll(RegExp(r'\D'), '');

  /// Her girdiyi ulusal 10 haneye indirir: `+90 532 111 00 01`, `0532...`,
  /// `90532...`, `532...` → `5321110001`.
  static String national(String input) {
    var digits = digitsOf(input);
    if (digits.startsWith('90') && digits.length > nationalLength) {
      digits = digits.substring(2);
    }
    if (digits.startsWith('0')) digits = digits.substring(1);
    if (digits.length > nationalLength) {
      digits = digits.substring(digits.length - nationalLength);
    }
    return digits;
  }

  /// Türkiye'de tüm cep numaraları `5` ile başlar (sabit hatlar OTP alamaz).
  static bool isValid(String input) {
    final digits = national(input);
    return digits.length == nationalLength && digits.startsWith('5');
  }

  /// Sunucuya gönderilecek biçim; geçersizse null.
  static String? toE164(String input) =>
      isValid(input) ? '$countryCode${national(input)}' : null;

  /// `5321110001` → `532 111 00 01` (eksik haneler kadarını biçimler).
  static String formatNational(String input) {
    final digits = national(input);
    if (digits.isEmpty) return '';

    final parts = <String>[];
    var index = 0;
    for (final size in _groups) {
      if (index >= digits.length) break;
      final end = (index + size).clamp(0, digits.length);
      parts.add(digits.substring(index, end));
      index = end;
    }
    return parts.join(' ');
  }

  /// Ekranda/özet metinlerde gösterim: `+90 532 111 00 01`.
  static String display(String input) {
    final formatted = formatNational(input);
    return formatted.isEmpty ? '' : '$countryCode $formatted';
  }

  /// Numaranın son 2 hanesi dışını gizler — OTP ekranındaki "…01'e kod
  /// gönderildi" bilgisi için (tam numara gereksiz).
  static String masked(String input) {
    final digits = national(input);
    if (digits.length < 4) return display(input);
    final visible = digits.substring(digits.length - 2);
    return '$countryCode ${digits.substring(0, 3)} ••• •• $visible';
  }
}

/// Yazarken `532 111 00 01` maskesi uygular ve 10 haneyi aşmaz.
class PhoneInputFormatter extends TextInputFormatter {
  const PhoneInputFormatter();

  @override
  TextEditingValue formatEditUpdate(TextEditingValue oldValue, TextEditingValue newValue) {
    // 10 hane doluyken fazladan yazılan hane yok sayılır. (Normalizasyon
    // yapıştırma için "son 10 hane"yi alır — yazarken bu, baştaki haneleri
    // kaydırırdı; o yüzden ekleme burada baştan reddedilir.)
    if (AppPhone.national(oldValue.text).length == AppPhone.nationalLength &&
        AppPhone.digitsOf(newValue.text).length > AppPhone.digitsOf(oldValue.text).length) {
      return oldValue;
    }

    final digitsBeforeCursor = AppPhone.digitsOf(
      newValue.text.substring(0, newValue.selection.end.clamp(0, newValue.text.length)),
    ).length;

    final formatted = AppPhone.formatNational(newValue.text);

    // İmleci "aynı hane sayısından sonra" konumlandır — sonuna yazma dışındaki
    // düzenlemelerde de imleç kaymaz.
    var offset = formatted.length;
    var seen = 0;
    for (var i = 0; i < formatted.length; i++) {
      if (seen == digitsBeforeCursor) {
        offset = i;
        break;
      }
      if (formatted.codeUnitAt(i) != 0x20) seen++;
    }
    if (seen == digitsBeforeCursor && offset == formatted.length) {
      offset = formatted.length;
    }

    return TextEditingValue(
      text: formatted,
      selection: TextSelection.collapsed(offset: offset.clamp(0, formatted.length)),
    );
  }
}
