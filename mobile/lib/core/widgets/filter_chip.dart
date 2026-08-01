import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';

/// Filtre şeritlerinin ortak chip'i (ikon + etiket, hap biçimli).
///
/// 11.6'dan beri her liste ekranı kendi kopyasını taşıyordu (duyuru türü,
/// rehber kategorisi, ilan sıralaması); 11.10'da etkinlik ve kampanya da aynı
/// şeridi isteyince tek bileşene çıkarıldı. Seçili chip **tema birincil
/// rengiyle dolu**: sunucudan gelen kategori renkleri koyu temada okunmaz hâle
/// gelebiliyor (11.6 kararı), bu yüzden renk yalnız [accent] ile bilinçli
/// olarak değiştirilebilir (ör. "Acil numaralar" kırmızısı).
class FilterChoiceChip extends StatelessWidget {
  const FilterChoiceChip({
    super.key,
    required this.label,
    required this.selected,
    this.icon,
    required this.onTap,
    this.accent,
    this.trailing,
    this.dense = false,
  });

  final String label;

  /// Etiketin solundaki ikon. **Opsiyonel:** dar filtre şeritlerinde üç chip
  /// 360 dp'ye sığmıyordu (11.10) — ikonsuz chip ~24 dp yer kazandırıyor ve
  /// anlam zaten etikette yazılı olduğu için bilgi kaybı olmuyor.
  final IconData? icon;
  final bool selected;
  final VoidCallback onTap;

  /// Seçili zeminin/çerçevenin rengi (varsayılan: tema birincil rengi).
  /// Verilirse chip seçili değilken de bu tonda vurgulanır.
  final Color? accent;

  /// Etiketin sağındaki ek ikon (ör. alt kategoriye inildiğini gösteren ok).
  final IconData? trailing;

  /// Daha küçük dolgu — ikinci sıradaki (alt) şeritler için.
  final bool dense;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final accentColor = accent ?? theme.colorScheme.primary;
    final emphasized = accent != null;
    final background = selected ? accentColor : theme.colorScheme.surface;
    final foreground = selected
        ? theme.colorScheme.onPrimary
        : (emphasized ? accentColor : theme.colorScheme.onSurface);

    return Semantics(
      button: true,
      selected: selected,
      label: label,
      child: Material(
        color: background,
        shape: RoundedRectangleBorder(
          borderRadius: AppRadius.rPill,
          side: BorderSide(
            color: selected || emphasized ? accentColor : palette.border,
          ),
        ),
        child: InkWell(
          onTap: onTap,
          borderRadius: AppRadius.rPill,
          child: Padding(
            padding: EdgeInsets.symmetric(
              horizontal: AppSpacing.md,
              vertical: dense ? AppSpacing.xs : AppSpacing.sm,
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                if (icon != null) ...[
                  Icon(
                    icon,
                    size: dense ? 15 : 16,
                    color: selected
                        ? foreground
                        : (emphasized ? accentColor : palette.muted),
                  ),
                  AppSpacing.wGapSm,
                ],
                Text(
                  label,
                  style:
                      (dense
                              ? theme.textTheme.labelMedium
                              : theme.textTheme.labelLarge)
                          ?.copyWith(
                            color: foreground,
                            fontWeight: selected
                                ? FontWeight.w700
                                : FontWeight.w600,
                          ),
                ),
                if (trailing != null) ...[
                  AppSpacing.wGapXs,
                  Icon(trailing, size: dense ? 14 : 15, color: foreground),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}
