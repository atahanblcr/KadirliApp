import 'package:flutter/widgets.dart';

/// 4'ün katları — tüm boşluklar buradan.
abstract final class AppSpacing {
  static const double xxs = 2;
  static const double xs = 4;
  static const double sm = 8;
  static const double md = 12;
  static const double lg = 16;
  static const double xl = 24;
  static const double xxl = 32;
  static const double huge = 48;

  /// Ekran kenar boşluğu (yatay).
  static const double screenH = 16;

  /// Dikey boşluk kısayolları.
  static const gapXs = SizedBox(height: xs);
  static const gapSm = SizedBox(height: sm);
  static const gapMd = SizedBox(height: md);
  static const gapLg = SizedBox(height: lg);
  static const gapXl = SizedBox(height: xl);

  /// Yatay boşluk kısayolları.
  static const wGapXs = SizedBox(width: xs);
  static const wGapSm = SizedBox(width: sm);
  static const wGapMd = SizedBox(width: md);
  static const wGapLg = SizedBox(width: lg);

  static const screenPadding = EdgeInsets.symmetric(horizontal: screenH);
}

/// Yumuşak köşeler — "sıcak ama güvenilir" (MOBILE_UX_PLAN §0.3).
abstract final class AppRadius {
  static const double sm = 10;
  static const double md = 14; // kart / buton varsayılanı
  static const double lg = 16;
  static const double xl = 20; // bottom sheet
  static const double pill = 999;

  static const BorderRadius rSm = BorderRadius.all(Radius.circular(sm));
  static const BorderRadius rMd = BorderRadius.all(Radius.circular(md));
  static const BorderRadius rLg = BorderRadius.all(Radius.circular(lg));
  static const BorderRadius rXl = BorderRadius.all(Radius.circular(xl));
  static const BorderRadius rPill = BorderRadius.all(Radius.circular(pill));
}

/// Hareket dili (MOBILE_UX_PLAN §4) — az & anlamlı.
abstract final class AppDurations {
  /// Buton basma / küçük geri bildirim.
  static const fast = Duration(milliseconds: 120);

  /// Rozet-sayaç "pop".
  static const pop = Duration(milliseconds: 150);

  /// Toast / boş→dolu geçişi.
  static const medium = Duration(milliseconds: 200);

  /// Sayfa geçişi.
  static const page = Duration(milliseconds: 220);

  /// Skeleton shimmer döngüsü.
  static const shimmer = Duration(milliseconds: 1400);
}

/// Erişilebilirlik sabitleri.
abstract final class AppA11y {
  /// Minimum dokunma alanı (MOBILE_UX_PLAN §0.1).
  static const double minTapSize = 48;
}
