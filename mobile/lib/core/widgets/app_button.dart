import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';

/// Buton türleri — MOBILE_UX_PLAN §1'deki renk rollerine bağlı.
enum AppButtonVariant {
  /// Ana aksiyon (dolu yeşil).
  primary,

  /// İkincil "sıcak" CTA — az kullan (acil/vurgulu).
  accent,

  /// Çerçeveli/şeffaf ikincil aksiyon.
  ghost,

  /// Yıkıcı aksiyon (sil, hesabı kapat).
  danger,
}

enum AppButtonSize { normal, small }

/// Uygulamanın tek buton bileşeni.
///
/// - Minimum 48dp dokunma alanı (erişilebilirlik).
/// - Basılınca 0.98 ölçek + renk koyulaşma (MOBILE_UX_PLAN §4);
///   "hareketi azalt" sistem ayarı açıksa ölçek animasyonu atlanır.
/// - [loading] iken buton kilitlenir ve etiket yerinde küçük bir gösterge döner.
class AppButton extends StatefulWidget {
  const AppButton({
    super.key,
    required this.label,
    this.onPressed,
    this.variant = AppButtonVariant.primary,
    this.size = AppButtonSize.normal,
    this.icon,
    this.loading = false,
    this.expand = false,
  });

  /// Kısayol: ikincil/çerçeveli buton.
  const AppButton.ghost({
    super.key,
    required this.label,
    this.onPressed,
    this.size = AppButtonSize.normal,
    this.icon,
    this.loading = false,
    this.expand = false,
  }) : variant = AppButtonVariant.ghost;

  /// Kısayol: yıkıcı aksiyon.
  const AppButton.danger({
    super.key,
    required this.label,
    this.onPressed,
    this.size = AppButtonSize.normal,
    this.icon,
    this.loading = false,
    this.expand = false,
  }) : variant = AppButtonVariant.danger;

  final String label;
  final VoidCallback? onPressed;
  final AppButtonVariant variant;
  final AppButtonSize size;
  final IconData? icon;
  final bool loading;

  /// Genişliği doldursun mu (form altı ana aksiyonlar için).
  final bool expand;

  bool get _enabled => onPressed != null && !loading;

  @override
  State<AppButton> createState() => _AppButtonState();
}

class _AppButtonState extends State<AppButton> {
  bool _pressed = false;

  void _setPressed(bool value) {
    if (_pressed == value) return;
    setState(() => _pressed = value);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final palette = theme.palette;
    final reduceMotion = MediaQuery.disableAnimationsOf(context);

    final (Color background, Color foreground, Color? borderColor) = switch (widget.variant) {
      AppButtonVariant.primary => (scheme.primary, scheme.onPrimary, null),
      AppButtonVariant.accent => (palette.accent, Colors.white, null),
      AppButtonVariant.ghost => (Colors.transparent, scheme.primary, palette.border),
      AppButtonVariant.danger => (palette.danger, Colors.white, null),
    };

    final height = widget.size == AppButtonSize.small ? 40.0 : AppA11y.minTapSize;
    final horizontalPadding = widget.size == AppButtonSize.small ? AppSpacing.md : AppSpacing.xl;
    final textStyle = (widget.size == AppButtonSize.small
        ? theme.textTheme.labelMedium
        : theme.textTheme.labelLarge)!;

    // Devre dışı görünüm marka rengini soldurmak yerine nötr yüzeye döner —
    // koyu temada soluk yeşil üstünde koyu metin okunmuyordu (AA altı).
    // Yükleniyor ≠ devre dışı: yüklenirken buton rengini korur, yalnız kilitlenir.
    final disabled = widget.onPressed == null;
    final isGhost = widget.variant == AppButtonVariant.ghost;
    final effectiveBackground = disabled && !isGhost ? scheme.surfaceContainerHighest : background;
    final effectiveForeground = disabled ? palette.muted : foreground;

    final Widget content = Row(
      mainAxisSize: widget.expand ? MainAxisSize.max : MainAxisSize.min,
      mainAxisAlignment: MainAxisAlignment.center,
      children: widget.loading
          ? [
              SizedBox(
                height: 20,
                width: 20,
                child: CircularProgressIndicator(strokeWidth: 2.2, color: effectiveForeground),
              ),
            ]
          : [
              if (widget.icon != null) ...[
                Icon(widget.icon, size: 20, color: effectiveForeground),
                AppSpacing.wGapSm,
              ],
              Flexible(
                child: Text(
                  widget.label,
                  style: textStyle.copyWith(color: effectiveForeground),
                  overflow: TextOverflow.ellipsis,
                  textAlign: TextAlign.center,
                ),
              ),
            ],
    );

    final button = AnimatedContainer(
      duration: AppDurations.fast,
      curve: Curves.easeOut,
      height: height,
      padding: EdgeInsets.symmetric(horizontal: horizontalPadding),
      // ⚠️ `alignment` VERİLMEZ: Container'a hizalama verildiğinde kutu gevşek
      // kısıtlarda tüm genişliği kaplar — buton içeriği kadar kalmalı.
      decoration: BoxDecoration(
        // Basılıyken hafif koyulaşma — dolu ve çerçeveli varyantlarda tutarlı.
        color: _pressed && widget._enabled
            ? Color.alphaBlend(Colors.black.withValues(alpha: 0.12), effectiveBackground)
            : effectiveBackground,
        borderRadius: AppRadius.rMd,
        border: borderColor != null ? Border.all(color: borderColor) : null,
      ),
      child: content,
    );

    final scaled = AnimatedScale(
      scale: _pressed && widget._enabled && !reduceMotion ? 0.98 : 1.0,
      duration: AppDurations.fast,
      curve: Curves.easeOut,
      child: button,
    );

    return Semantics(
      button: true,
      enabled: widget._enabled,
      label: widget.label,
      child: GestureDetector(
        behavior: HitTestBehavior.opaque,
        onTapDown: widget._enabled ? (_) => _setPressed(true) : null,
        onTapUp: widget._enabled ? (_) => _setPressed(false) : null,
        onTapCancel: () => _setPressed(false),
        onTap: widget._enabled ? widget.onPressed : null,
        child: widget.expand ? SizedBox(width: double.infinity, child: scaled) : scaled,
      ),
    );
  }
}
