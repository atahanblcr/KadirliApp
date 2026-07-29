import 'package:flutter/material.dart';

/// Tipografi ölçeği — `MOBILE_UX_PLAN.md` §2.
///
/// Tek aile (Nunito), ağırlıklarla ayrışır: gövde 400, başlık 600, vurgu 700.
/// İnce (300) kullanılmaz. Satır yüksekliği ~1.4.
abstract final class AppTypography {
  static const String fontFamily = 'Nunito';

  static const FontWeight regular = FontWeight.w400;
  static const FontWeight semiBold = FontWeight.w600;
  static const FontWeight bold = FontWeight.w700;

  static const double _lineHeight = 1.4;
  static const double _tightLineHeight = 1.25; // büyük başlıklarda daha sıkı

  static const display = TextStyle(fontSize: 28, height: _tightLineHeight, fontWeight: semiBold);
  static const h1 = TextStyle(fontSize: 22, height: _tightLineHeight, fontWeight: semiBold);
  static const h2 = TextStyle(fontSize: 18, height: 1.3, fontWeight: semiBold);
  static const body = TextStyle(fontSize: 16, height: _lineHeight, fontWeight: regular);
  static const bodySm = TextStyle(fontSize: 14, height: _lineHeight, fontWeight: regular);
  static const caption = TextStyle(fontSize: 13, height: _lineHeight, fontWeight: regular);
  static const label = TextStyle(fontSize: 12, height: 1.2, fontWeight: semiBold);

  /// Material [TextTheme] eşlemesi. Material bileşenleri bu slot'ları okur:
  /// - display/headline → ekran başlıkları
  /// - title → kart/AppBar başlıkları
  /// - body → gövde metni
  /// - label → buton, chip, sekme etiketi
  static TextTheme textTheme(Color ink, Color muted) {
    return TextTheme(
      displayLarge: display.copyWith(color: ink),
      displayMedium: display.copyWith(color: ink),
      displaySmall: display.copyWith(color: ink),
      headlineLarge: display.copyWith(color: ink),
      headlineMedium: h1.copyWith(color: ink),
      headlineSmall: h1.copyWith(color: ink),
      titleLarge: h1.copyWith(color: ink),
      titleMedium: h2.copyWith(color: ink),
      titleSmall: body.copyWith(color: ink, fontWeight: semiBold),
      bodyLarge: body.copyWith(color: ink),
      bodyMedium: bodySm.copyWith(color: ink),
      bodySmall: caption.copyWith(color: muted),
      labelLarge: bodySm.copyWith(color: ink, fontWeight: semiBold),
      labelMedium: label.copyWith(color: ink),
      labelSmall: label.copyWith(color: muted),
    ).apply(fontFamily: fontFamily);
  }
}

/// Kısayol: `context.text.h2` yerine `Theme.of(context).textTheme.titleMedium`
/// yazmak zorunda kalmamak için.
extension AppTextThemeX on BuildContext {
  TextTheme get text => Theme.of(this).textTheme;
}
