import 'package:flutter/material.dart';

/// Ham renk token'ları — `MOBILE_UX_PLAN.md` §1 ile birebir.
///
/// Bu sınıf doğrudan widget'larda kullanılmaz; [AppPalette] (tema uzantısı) ve
/// [ColorScheme] üzerinden okunur. Böylece açık/koyu tema tek yerde çözülür.
abstract final class AppColors {
  // ---- Açık tema ----
  static const primary = Color(0xFF2C7A57);
  static const primaryDeep = Color(0xFF215B41); // basılı/hover
  static const primaryTint = Color(0xFFE8F3EC); // seçili zemin, rozet
  static const accent = Color(0xFFE08A3C); // sıcak ikincil CTA / "acil"
  static const accentTint = Color(0xFFFBEEE0);
  static const background = Color(0xFFFAF9F6); // sıcak kırık-beyaz
  static const surface = Color(0xFFFFFFFF);
  static const border = Color(0xFFE7E4DD);
  static const ink = Color(0xFF1E2A24); // güçlü metin
  static const muted = Color(0xFF5C6B63); // ikincil metin

  // ---- Koyu tema ----
  static const primaryDark = Color(0xFF46B083); // kontrast için açıldı
  static const primaryDeepDark = Color(0xFF6ACBA1);
  static const primaryTintDark = Color(0xFF1E3A2D);
  static const accentDark = Color(0xFFE9A05C);
  static const accentTintDark = Color(0xFF3A2A18);
  static const backgroundDark = Color(0xFF121815);
  static const surfaceDark = Color(0xFF1B2420);
  static const borderDark = Color(0xFF2A352F);
  static const inkDark = Color(0xFFECF1EE);
  static const mutedDark = Color(0xFF9DB0A6);

  // ---- Anlamsal (açık) ----
  static const success = Color(0xFF2E8B57);
  static const info = Color(0xFF2F6FB0);
  static const warning = Color(0xFFE0A32E);
  static const danger = Color(0xFFD64545);

  // ---- Anlamsal (koyu — kontrast için açıldı) ----
  static const successDark = Color(0xFF52B583);
  static const infoDark = Color(0xFF5E9BDA);
  static const warningDark = Color(0xFFEBBB5C);
  static const dangerDark = Color(0xFFE97A7A);
}

/// Material [ColorScheme]'in karşılamadığı marka/anlam token'ları.
///
/// Kullanım: `Theme.of(context).palette.success` (bkz. [AppPaletteX]).
@immutable
class AppPalette extends ThemeExtension<AppPalette> {
  const AppPalette({
    required this.accent,
    required this.accentTint,
    required this.success,
    required this.info,
    required this.warning,
    required this.danger,
    required this.border,
    required this.muted,
    required this.skeletonBase,
    required this.skeletonHighlight,
  });

  /// Sıcak ikincil CTA / "acil şerit" vurgusu. Az kullanılır.
  final Color accent;
  final Color accentTint;

  final Color success;
  final Color info;
  final Color warning;
  final Color danger;

  /// İnce ayraç / kart kenarı.
  final Color border;

  /// İkincil metin (tarih, açıklama).
  final Color muted;

  /// Skeleton shimmer zemin/parlama renkleri.
  final Color skeletonBase;
  final Color skeletonHighlight;

  static const light = AppPalette(
    accent: AppColors.accent,
    accentTint: AppColors.accentTint,
    success: AppColors.success,
    info: AppColors.info,
    warning: AppColors.warning,
    danger: AppColors.danger,
    border: AppColors.border,
    muted: AppColors.muted,
    skeletonBase: Color(0xFFEDEBE5),
    skeletonHighlight: Color(0xFFF8F7F4),
  );

  static const dark = AppPalette(
    accent: AppColors.accentDark,
    accentTint: AppColors.accentTintDark,
    success: AppColors.successDark,
    info: AppColors.infoDark,
    warning: AppColors.warningDark,
    danger: AppColors.dangerDark,
    border: AppColors.borderDark,
    muted: AppColors.mutedDark,
    skeletonBase: Color(0xFF232E29),
    skeletonHighlight: Color(0xFF2E3B35),
  );

  @override
  AppPalette copyWith({
    Color? accent,
    Color? accentTint,
    Color? success,
    Color? info,
    Color? warning,
    Color? danger,
    Color? border,
    Color? muted,
    Color? skeletonBase,
    Color? skeletonHighlight,
  }) {
    return AppPalette(
      accent: accent ?? this.accent,
      accentTint: accentTint ?? this.accentTint,
      success: success ?? this.success,
      info: info ?? this.info,
      warning: warning ?? this.warning,
      danger: danger ?? this.danger,
      border: border ?? this.border,
      muted: muted ?? this.muted,
      skeletonBase: skeletonBase ?? this.skeletonBase,
      skeletonHighlight: skeletonHighlight ?? this.skeletonHighlight,
    );
  }

  @override
  AppPalette lerp(ThemeExtension<AppPalette>? other, double t) {
    if (other is! AppPalette) return this;
    return AppPalette(
      accent: Color.lerp(accent, other.accent, t)!,
      accentTint: Color.lerp(accentTint, other.accentTint, t)!,
      success: Color.lerp(success, other.success, t)!,
      info: Color.lerp(info, other.info, t)!,
      warning: Color.lerp(warning, other.warning, t)!,
      danger: Color.lerp(danger, other.danger, t)!,
      border: Color.lerp(border, other.border, t)!,
      muted: Color.lerp(muted, other.muted, t)!,
      skeletonBase: Color.lerp(skeletonBase, other.skeletonBase, t)!,
      skeletonHighlight: Color.lerp(skeletonHighlight, other.skeletonHighlight, t)!,
    );
  }
}

extension AppPaletteX on ThemeData {
  /// Marka/anlam renkleri. Tema uzantısı tanımlı değilse açık palete düşer.
  AppPalette get palette => extension<AppPalette>() ?? AppPalette.light;
}
