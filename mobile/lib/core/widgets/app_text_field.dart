import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';

/// Uygulamanın tek metin girişi bileşeni.
///
/// Etiket **alanın üstünde** durur (Material'ın yüzen etiketi yerine): form
/// alanları çoğunlukla ön ekli/maskeli olduğu için yüzen etiket ile ön ek
/// çakışıyor, ayrıca 12sp etiket + 16sp gövde tipografi ölçeğine uyuyor
/// (MOBILE_UX_PLAN §2). Hata metni alanın altında `danger` renginde.
class AppTextField extends StatelessWidget {
  const AppTextField({
    super.key,
    this.label,
    this.hint,
    this.helper,
    this.errorText,
    this.controller,
    this.focusNode,
    this.keyboardType,
    this.textInputAction,
    this.textCapitalization = TextCapitalization.none,
    this.inputFormatters,
    this.prefixText,
    this.prefixIcon,
    this.suffix,
    this.maxLength,
    this.maxLines = 1,
    this.autofocus = false,
    this.enabled = true,
    this.autofillHints,
    this.onChanged,
    this.onSubmitted,
    this.textAlign = TextAlign.start,
    this.letterSpacing,
    this.required = false,
  });

  final String? label;
  final String? hint;
  final String? helper;
  final String? errorText;
  final TextEditingController? controller;
  final FocusNode? focusNode;
  final TextInputType? keyboardType;
  final TextInputAction? textInputAction;
  final TextCapitalization textCapitalization;
  final List<TextInputFormatter>? inputFormatters;

  /// Sabit ön ek (ör. telefon alanında `+90`).
  final String? prefixText;
  final IconData? prefixIcon;
  final Widget? suffix;

  final int? maxLength;
  final int maxLines;
  final bool autofocus;
  final bool enabled;
  final Iterable<String>? autofillHints;
  final ValueChanged<String>? onChanged;
  final ValueChanged<String>? onSubmitted;
  final TextAlign textAlign;

  /// OTP alanı gibi hane hane okunan girişlerde harf aralığını açar.
  final double? letterSpacing;

  /// Etiketin yanına `*` koyar (zorunlu alan işareti).
  final bool required;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final hasError = errorText != null && errorText!.isNotEmpty;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (label != null) ...[
          Row(
            children: [
              Text(
                label!,
                style: theme.textTheme.labelMedium?.copyWith(color: palette.muted),
              ),
              if (required)
                Text(
                  ' *',
                  style: theme.textTheme.labelMedium?.copyWith(color: palette.danger),
                ),
            ],
          ),
          AppSpacing.gapXs,
        ],
        TextField(
          controller: controller,
          focusNode: focusNode,
          enabled: enabled,
          autofocus: autofocus,
          keyboardType: keyboardType,
          textInputAction: textInputAction,
          textCapitalization: textCapitalization,
          inputFormatters: inputFormatters,
          maxLength: maxLength,
          maxLines: maxLines,
          autofillHints: autofillHints,
          onChanged: onChanged,
          onSubmitted: onSubmitted,
          textAlign: textAlign,
          style: theme.textTheme.bodyLarge?.copyWith(letterSpacing: letterSpacing),
          cursorColor: theme.colorScheme.primary,
          decoration: InputDecoration(
            hintText: hint,
            counterText: '', // maxLength sayacı gizli — tasarımda yer almıyor.
            // ⚠️ `prefix`/`prefixText` YALNIZ alan odaklıyken görünür; ülke kodu
            // baştan görünmeli (kullanıcı "+90 yazmalı mıyım?" diye düşünmesin)
            // → ön ek ikonla birlikte `prefixIcon` yuvasında çizilir.
            prefixIcon: (prefixIcon == null && prefixText == null)
                ? null
                : Padding(
                    padding: const EdgeInsets.only(
                      left: AppSpacing.lg,
                      right: AppSpacing.sm,
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        if (prefixIcon != null)
                          Icon(prefixIcon, size: 20, color: palette.muted),
                        if (prefixText != null) ...[
                          if (prefixIcon != null) AppSpacing.wGapSm,
                          Text(prefixText!, style: theme.textTheme.bodyLarge),
                        ],
                      ],
                    ),
                  ),
            prefixIconConstraints: const BoxConstraints(minHeight: 0, minWidth: 0),
            suffixIcon: suffix,
            errorText: hasError ? errorText : null,
            helperText: helper,
            helperMaxLines: 3,
            errorMaxLines: 3,
          ),
        ),
      ],
    );
  }
}
