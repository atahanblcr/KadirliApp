import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../network/api_exception.dart';
import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';
import 'info_banner.dart';
import 'skeleton.dart';

/// Sözlük (lookup) ucundan beslenen açılır liste — yükleniyor/hata/boş
/// durumlarıyla birlikte.
///
/// 11.5'te profil ekranındaki mahalle alanı olarak yazılmıştı; 11.11'in vefat
/// formunda **üç** böyle alan olunca (cami, mezarlık, mahalle) ortak bileşene
/// çıkarıldı. Liste alınamazsa açılır liste yerine tekrar denenebilir bir uyarı
/// çizilir — boş bir seçim kutusu "seçenek yok" gibi görünürdü.
class LookupDropdown<T> extends StatelessWidget {
  const LookupDropdown({
    super.key,
    required this.label,
    required this.items,
    required this.value,
    required this.idOf,
    required this.labelOf,
    required this.onChanged,
    required this.onRetry,
    this.hint,
    this.helper,
    this.errorText,
    this.isRequired = false,
    this.enabled = true,
    this.emptyMessage,
  });

  final String label;
  final AsyncValue<List<T>> items;
  final String? value;

  final String Function(T item) idOf;
  final String Function(T item) labelOf;

  final ValueChanged<String?> onChanged;
  final VoidCallback onRetry;

  final String? hint;
  final String? helper;
  final String? errorText;
  final bool isRequired;
  final bool enabled;

  /// Uç boş liste döndürdüğünde yazılacak açıklama.
  final String? emptyMessage;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            // ⚠️ Uzun etiket ("Cenaze namazının kılınacağı cami") dar ekranda
            // `RenderFlex` taşıyordu — bu projede aynı sınıf hatanın beşinci
            // tekrarı (11.7/11.8/11.9/11.10) → `Flexible` + ellipsis.
            Flexible(
              child: Text(
                label,
                style: theme.textTheme.labelMedium?.copyWith(
                  color: palette.muted,
                ),
                overflow: TextOverflow.ellipsis,
              ),
            ),
            if (isRequired)
              Text(
                ' *',
                style: theme.textTheme.labelMedium?.copyWith(
                  color: palette.danger,
                ),
              ),
          ],
        ),
        AppSpacing.gapXs,
        switch (items) {
          AsyncData(value: final list) when list.isEmpty => InfoBanner(
            tone: InfoBannerTone.info,
            message: emptyMessage ?? 'Şu an seçilebilecek bir kayıt yok.',
          ),
          AsyncData(value: final list) => DropdownButtonFormField<String>(
            // Sunucudan silinmiş bir id ekranda "boş seçim" gibi görünmesin.
            initialValue: list.any((item) => idOf(item) == value) ? value : null,
            isExpanded: true,
            decoration: InputDecoration(
              errorText: errorText,
              helperText: helper,
              // ⚠️ Varsayılan 1 satır kısıtı yardımcı metni "..." ile kesiyor.
              helperMaxLines: 3,
              errorMaxLines: 3,
            ),
            hint: Text(hint ?? 'Seçin'),
            items: [
              for (final item in list)
                DropdownMenuItem(
                  value: idOf(item),
                  child: Text(labelOf(item), overflow: TextOverflow.ellipsis),
                ),
            ],
            onChanged: enabled ? onChanged : null,
          ),
          AsyncError(:final error) => InfoBanner(
            tone: InfoBannerTone.danger,
            message: error is ApiException
                ? '${error.message} Liste yüklenemedi.'
                : 'Liste alınamadı.',
            icon: Icons.refresh_rounded,
            onClose: onRetry,
          ),
          _ => const SkeletonBox(height: AppA11y.minTapSize),
        },
      ],
    );
  }
}
