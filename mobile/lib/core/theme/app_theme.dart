import 'package:flutter/cupertino.dart' show CupertinoPageTransitionsBuilder;
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import 'app_colors.dart';
import 'app_spacing.dart';
import 'app_typography.dart';

/// Uygulamanın açık/koyu [ThemeData]'ları.
///
/// Kural: widget'larda sabit renk YAZILMAZ — her şey buradan
/// (`Theme.of(context).colorScheme` / `.palette`) okunur.
abstract final class AppTheme {
  static ThemeData get light => _build(_lightScheme, AppPalette.light, Brightness.light);
  static ThemeData get dark => _build(_darkScheme, AppPalette.dark, Brightness.dark);

  static const _lightScheme = ColorScheme(
    brightness: Brightness.light,
    primary: AppColors.primary,
    onPrimary: Colors.white,
    primaryContainer: AppColors.primaryTint,
    onPrimaryContainer: AppColors.primaryDeep,
    secondary: AppColors.accent,
    onSecondary: Colors.white,
    secondaryContainer: AppColors.accentTint,
    onSecondaryContainer: Color(0xFF6B3E12),
    tertiary: AppColors.info,
    onTertiary: Colors.white,
    error: AppColors.danger,
    onError: Colors.white,
    errorContainer: Color(0xFFFBE6E6),
    onErrorContainer: Color(0xFF8A2020),
    surface: AppColors.surface,
    onSurface: AppColors.ink,
    onSurfaceVariant: AppColors.muted,
    surfaceContainerLowest: Colors.white,
    surfaceContainerLow: Color(0xFFFCFBF8),
    surfaceContainer: AppColors.background,
    surfaceContainerHigh: Color(0xFFF2F0EA),
    surfaceContainerHighest: Color(0xFFEDEBE5),
    outline: AppColors.border,
    outlineVariant: Color(0xFFF0EEE8),
    inverseSurface: AppColors.ink,
    onInverseSurface: Color(0xFFF3F6F4),
    inversePrimary: AppColors.primaryDark,
    shadow: Color(0x141E2A24),
    scrim: Color(0x801E2A24),
  );

  static const _darkScheme = ColorScheme(
    brightness: Brightness.dark,
    primary: AppColors.primaryDark,
    onPrimary: Color(0xFF06231A),
    primaryContainer: AppColors.primaryTintDark,
    onPrimaryContainer: AppColors.primaryDeepDark,
    secondary: AppColors.accentDark,
    onSecondary: Color(0xFF2B1A08),
    secondaryContainer: AppColors.accentTintDark,
    onSecondaryContainer: Color(0xFFF3C48E),
    tertiary: AppColors.infoDark,
    onTertiary: Color(0xFF08203A),
    error: AppColors.dangerDark,
    onError: Color(0xFF3A0E0E),
    errorContainer: Color(0xFF3D1E1E),
    onErrorContainer: Color(0xFFF3B9B9),
    surface: AppColors.surfaceDark,
    onSurface: AppColors.inkDark,
    onSurfaceVariant: AppColors.mutedDark,
    surfaceContainerLowest: Color(0xFF0D120F),
    surfaceContainerLow: Color(0xFF161E1A),
    surfaceContainer: AppColors.surfaceDark,
    surfaceContainerHigh: Color(0xFF222C27),
    surfaceContainerHighest: Color(0xFF29342E),
    outline: AppColors.borderDark,
    outlineVariant: Color(0xFF212B26),
    inverseSurface: AppColors.inkDark,
    onInverseSurface: AppColors.backgroundDark,
    inversePrimary: AppColors.primary,
    shadow: Color(0x66000000),
    scrim: Color(0xB3000000),
  );

  static ThemeData _build(ColorScheme scheme, AppPalette palette, Brightness brightness) {
    final isDark = brightness == Brightness.dark;
    final background = isDark ? AppColors.backgroundDark : AppColors.background;
    final ink = scheme.onSurface;
    final muted = palette.muted;
    final textTheme = AppTypography.textTheme(ink, muted);

    return ThemeData(
      useMaterial3: true,
      brightness: brightness,
      colorScheme: scheme,
      fontFamily: AppTypography.fontFamily,
      scaffoldBackgroundColor: background,
      canvasColor: background,
      textTheme: textTheme,
      extensions: [palette],
      splashFactory: InkSparkle.splashFactory,

      appBarTheme: AppBarThemeData(
        backgroundColor: background,
        foregroundColor: ink,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        scrolledUnderElevation: 0.5,
        centerTitle: false,
        titleTextStyle: textTheme.titleLarge,
        systemOverlayStyle: isDark ? SystemUiOverlayStyle.light : SystemUiOverlayStyle.dark,
      ),

      cardTheme: CardThemeData(
        color: scheme.surface,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: AppRadius.rMd,
          side: BorderSide(color: palette.border),
        ),
      ),

      dividerTheme: DividerThemeData(color: palette.border, thickness: 1, space: 1),

      // Not: Sekme kabuğu 11.4'te NavigationBar ile kurulacak.
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: scheme.surface,
        surfaceTintColor: Colors.transparent,
        indicatorColor: scheme.primaryContainer,
        elevation: 0,
        height: 64,
        labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
        labelTextStyle: WidgetStateProperty.resolveWith(
          (states) => states.contains(WidgetState.selected)
              ? textTheme.labelMedium?.copyWith(color: scheme.primary)
              : textTheme.labelMedium?.copyWith(color: muted),
        ),
        iconTheme: WidgetStateProperty.resolveWith(
          (states) => IconThemeData(
            size: 24,
            color: states.contains(WidgetState.selected) ? scheme.primary : muted,
          ),
        ),
      ),

      inputDecorationTheme: InputDecorationThemeData(
        filled: true,
        fillColor: isDark ? scheme.surfaceContainerHigh : scheme.surfaceContainerLowest,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.lg,
          vertical: AppSpacing.md + 2,
        ),
        hintStyle: textTheme.bodyLarge?.copyWith(color: muted),
        labelStyle: textTheme.bodyMedium?.copyWith(color: muted),
        errorStyle: textTheme.bodySmall?.copyWith(color: scheme.error),
        border: OutlineInputBorder(
          borderRadius: AppRadius.rMd,
          borderSide: BorderSide(color: palette.border),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: AppRadius.rMd,
          borderSide: BorderSide(color: palette.border),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: AppRadius.rMd,
          borderSide: BorderSide(color: scheme.primary, width: 1.6),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: AppRadius.rMd,
          borderSide: BorderSide(color: scheme.error),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderRadius: AppRadius.rMd,
          borderSide: BorderSide(color: scheme.error, width: 1.6),
        ),
      ),

      // Material varsayılanı seçili segmenti `secondaryContainer` (sıcak turuncu)
      // ile boyuyor; tasarımda seçili durum yeşil tint olmalı.
      segmentedButtonTheme: SegmentedButtonThemeData(
        style: ButtonStyle(
          backgroundColor: WidgetStateProperty.resolveWith(
            (s) => s.contains(WidgetState.selected) ? scheme.primaryContainer : scheme.surface,
          ),
          foregroundColor: WidgetStateProperty.resolveWith(
            (s) => s.contains(WidgetState.selected) ? scheme.onPrimaryContainer : muted,
          ),
          iconColor: WidgetStateProperty.resolveWith(
            (s) => s.contains(WidgetState.selected) ? scheme.onPrimaryContainer : muted,
          ),
          textStyle: WidgetStatePropertyAll(textTheme.labelLarge),
          side: WidgetStatePropertyAll(BorderSide(color: palette.border)),
          minimumSize: const WidgetStatePropertyAll(Size(0, AppA11y.minTapSize)),
          shape: const WidgetStatePropertyAll(
            RoundedRectangleBorder(borderRadius: AppRadius.rMd),
          ),
        ),
      ),

      chipTheme: ChipThemeData(
        backgroundColor: scheme.surface,
        selectedColor: scheme.primaryContainer,
        side: BorderSide(color: palette.border),
        labelStyle: textTheme.labelLarge!,
        secondaryLabelStyle: textTheme.labelLarge!.copyWith(color: scheme.onPrimaryContainer),
        padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md, vertical: AppSpacing.sm),
        shape: const RoundedRectangleBorder(borderRadius: AppRadius.rPill),
        showCheckmark: false,
      ),

      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        backgroundColor: scheme.inverseSurface,
        contentTextStyle: textTheme.bodyMedium?.copyWith(color: scheme.onInverseSurface),
        shape: const RoundedRectangleBorder(borderRadius: AppRadius.rMd),
        insetPadding: const EdgeInsets.all(AppSpacing.lg),
      ),

      dialogTheme: DialogThemeData(
        backgroundColor: scheme.surface,
        surfaceTintColor: Colors.transparent,
        shape: const RoundedRectangleBorder(borderRadius: AppRadius.rLg),
        titleTextStyle: textTheme.titleMedium,
        contentTextStyle: textTheme.bodyLarge,
      ),

      bottomSheetTheme: BottomSheetThemeData(
        backgroundColor: scheme.surface,
        surfaceTintColor: Colors.transparent,
        showDragHandle: true,
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
        ),
      ),

      listTileTheme: ListTileThemeData(
        iconColor: muted,
        titleTextStyle: textTheme.bodyLarge,
        subtitleTextStyle: textTheme.bodySmall,
        shape: const RoundedRectangleBorder(borderRadius: AppRadius.rMd),
        minVerticalPadding: AppSpacing.md,
      ),

      switchTheme: SwitchThemeData(
        thumbColor: WidgetStateProperty.resolveWith(
          (s) => s.contains(WidgetState.selected) ? scheme.onPrimary : muted,
        ),
        trackColor: WidgetStateProperty.resolveWith(
          (s) => s.contains(WidgetState.selected) ? scheme.primary : palette.border,
        ),
      ),

      progressIndicatorTheme: ProgressIndicatorThemeData(
        color: scheme.primary,
        linearTrackColor: palette.border,
      ),

      iconTheme: IconThemeData(color: ink, size: 24),

      // Sayfa geçişi: yumuşak kayma + fade (MOBILE_UX_PLAN §4).
      pageTransitionsTheme: const PageTransitionsTheme(
        builders: {
          TargetPlatform.android: FadeForwardsPageTransitionsBuilder(),
          TargetPlatform.iOS: CupertinoPageTransitionsBuilder(),
        },
      ),
    );
  }
}
