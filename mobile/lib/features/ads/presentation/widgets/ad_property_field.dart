import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/widgets/widgets.dart';
import '../../data/models/category_property.dart';

/// Kategoriye özel **dinamik** form alanı.
///
/// Sunucu her kategori için alan tanımlarını döndürüyor
/// (`GET /v1/ads/categories/{id}/properties`) ve tipe göre doğruluyor
/// (`AdSubmissionRules`): sayısal alan sayı, boolean `true/false`, select
/// tanımlı seçeneklerden biri olmalı. Bu widget aynı sözleşmeyi **giriş
/// tarafında** uygular — kullanıcı formu doldurup 400 yemez.
///
/// Değer her zaman **metin** olarak taşınır (`propertyValues` sözlüğü
/// `Dictionary<Guid,string>`); boolean `"true"/"false"`, çoklu seçim
/// virgülle birleşik.
class AdPropertyField extends StatelessWidget {
  const AdPropertyField({
    super.key,
    required this.property,
    required this.value,
    required this.onChanged,
    this.controller,
    this.errorText,
    this.enabled = true,
  });

  final CategoryProperty property;

  /// Seçilmemiş/boş alan `null` (zorunlu alan denetimi bunu kullanır).
  final String? value;

  final ValueChanged<String?> onChanged;

  /// Metin/sayı alanlarının controller'ı — form ekranı sahibidir (widget
  /// yeniden çizildiğinde yazılan metin kaybolmasın).
  final TextEditingController? controller;

  final String? errorText;
  final bool enabled;

  @override
  Widget build(BuildContext context) => switch (property.kind) {
    AdPropertyKind.boolean => _BooleanField(
      property: property,
      value: value,
      onChanged: onChanged,
      errorText: errorText,
      enabled: enabled,
    ),
    AdPropertyKind.select => _SelectField(
      property: property,
      value: value,
      onChanged: onChanged,
      errorText: errorText,
      enabled: enabled,
    ),
    AdPropertyKind.multiSelect => _MultiSelectField(
      property: property,
      value: value,
      onChanged: onChanged,
      errorText: errorText,
      enabled: enabled,
    ),
    AdPropertyKind.number => AppTextField(
      key: ValueKey('property-${property.id}'),
      label: property.propertyName,
      required: property.isRequired,
      // Sabit bir örnek ("Örn. 2018") her sayısal alanda doğru olmuyor
      // ("Kilometre: Örn. 2018") → etiket zaten yeterli, ipucu yazılmıyor.
      keyboardType: const TextInputType.numberWithOptions(decimal: true),
      // Sunucu `decimal.TryParse(InvariantCulture)` yapıyor → ondalık ayracı
      // **nokta**. Virgül yazan kullanıcı 400 yememeli: girişte noktaya
      // çevrilir (Türkçe klavyede ondalık tuşu virgüldür).
      inputFormatters: [
        FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]')),
      ],
      errorText: errorText,
      enabled: enabled,
      controller: controller,
      onChanged: (text) {
        final normalized = text.replaceAll(',', '.').trim();
        onChanged(normalized.isEmpty ? null : normalized);
      },
    ),
    AdPropertyKind.text => AppTextField(
      key: ValueKey('property-${property.id}'),
      label: property.propertyName,
      required: property.isRequired,
      maxLength: 500,
      errorText: errorText,
      enabled: enabled,
      controller: controller,
      onChanged: (text) => onChanged(text.trim().isEmpty ? null : text.trim()),
    ),
  };
}

class _FieldLabel extends StatelessWidget {
  const _FieldLabel({required this.property});

  final CategoryProperty property;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Text(
              property.propertyName,
              style: theme.textTheme.labelMedium?.copyWith(color: palette.muted),
            ),
            if (property.isRequired)
              Text(
                ' *',
                style: theme.textTheme.labelMedium?.copyWith(color: palette.danger),
              ),
          ],
        ),
        AppSpacing.gapXs,
      ],
    );
  }
}

class _FieldError extends StatelessWidget {
  const _FieldError({required this.errorText});

  final String? errorText;

  @override
  Widget build(BuildContext context) {
    if (errorText == null || errorText!.isEmpty) return const SizedBox.shrink();
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(top: AppSpacing.xs),
      child: Text(
        errorText!,
        style: theme.textTheme.bodySmall?.copyWith(color: theme.palette.danger),
      ),
    );
  }
}

/// Evet / Hayır — **başlangıçta seçili değil**: "hayır" ile "cevaplanmadı"
/// aynı şey değil (opsiyonel alanda dokunulmadıysa sunucuya hiç gitmez).
class _BooleanField extends StatelessWidget {
  const _BooleanField({
    required this.property,
    required this.value,
    required this.onChanged,
    required this.errorText,
    required this.enabled,
  });

  final CategoryProperty property;
  final String? value;
  final ValueChanged<String?> onChanged;
  final String? errorText;
  final bool enabled;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _FieldLabel(property: property),
        Row(
          children: [
            for (final option in const [('true', 'Var'), ('false', 'Yok')]) ...[
              _OptionChip(
                label: option.$2,
                selected: value == option.$1,
                enabled: enabled,
                // Aynı seçeneğe tekrar dokunmak seçimi kaldırır (opsiyonel
                // alan yanlışlıkla doldurulmuş olabilir).
                onTap: () => onChanged(value == option.$1 ? null : option.$1),
              ),
              AppSpacing.wGapSm,
            ],
          ],
        ),
        _FieldError(errorText: errorText),
      ],
    );
  }
}

class _SelectField extends StatelessWidget {
  const _SelectField({
    required this.property,
    required this.value,
    required this.onChanged,
    required this.errorText,
    required this.enabled,
  });

  final CategoryProperty property;
  final String? value;
  final ValueChanged<String?> onChanged;
  final String? errorText;
  final bool enabled;

  @override
  Widget build(BuildContext context) {
    final options = property.sortedOptions;
    final safeValue = options.any((option) => option.optionValue == value)
        ? value
        : null;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _FieldLabel(property: property),
        DropdownButtonFormField<String>(
          key: ValueKey('property-${property.id}'),
          initialValue: safeValue,
          isExpanded: true,
          decoration: InputDecoration(errorText: errorText, errorMaxLines: 3),
          hint: Text('${property.propertyName} seçin'),
          items: [
            for (final option in options)
              DropdownMenuItem(
                value: option.optionValue,
                child: Text(option.optionValue, overflow: TextOverflow.ellipsis),
              ),
          ],
          onChanged: enabled ? onChanged : null,
        ),
      ],
    );
  }
}

/// Çoklu seçim — sunucu virgülle ayrılmış metni bekliyor
/// (`value.Split(',')` ile her parça seçenek listesinde aranıyor).
class _MultiSelectField extends StatelessWidget {
  const _MultiSelectField({
    required this.property,
    required this.value,
    required this.onChanged,
    required this.errorText,
    required this.enabled,
  });

  final CategoryProperty property;
  final String? value;
  final ValueChanged<String?> onChanged;
  final String? errorText;
  final bool enabled;

  Set<String> get _selected => (value ?? '')
      .split(',')
      .map((part) => part.trim())
      .where((part) => part.isNotEmpty)
      .toSet();

  @override
  Widget build(BuildContext context) {
    final selected = _selected;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _FieldLabel(property: property),
        Wrap(
          spacing: AppSpacing.sm,
          runSpacing: AppSpacing.sm,
          children: [
            for (final option in property.sortedOptions)
              _OptionChip(
                label: option.optionValue,
                selected: selected.contains(option.optionValue),
                enabled: enabled,
                onTap: () {
                  final next = {...selected};
                  if (!next.remove(option.optionValue)) {
                    next.add(option.optionValue);
                  }
                  onChanged(next.isEmpty ? null : next.join(','));
                },
              ),
          ],
        ),
        _FieldError(errorText: errorText),
      ],
    );
  }
}

/// Seçilebilir etiket — ilan filtre chip'inin form sürümü (min 48dp dokunma).
class _OptionChip extends StatelessWidget {
  const _OptionChip({
    required this.label,
    required this.selected,
    required this.onTap,
    required this.enabled,
  });

  final String label;
  final bool selected;
  final VoidCallback onTap;
  final bool enabled;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final accent = theme.colorScheme.primary;

    return Semantics(
      button: true,
      selected: selected,
      label: label,
      child: Material(
        color: selected ? accent : theme.colorScheme.surface,
        shape: RoundedRectangleBorder(
          borderRadius: AppRadius.rPill,
          side: BorderSide(color: selected ? accent : palette.border),
        ),
        child: InkWell(
          onTap: enabled ? onTap : null,
          borderRadius: AppRadius.rPill,
          child: Padding(
            padding: const EdgeInsets.symmetric(
              horizontal: AppSpacing.lg,
              vertical: AppSpacing.md,
            ),
            child: Text(
              label,
              style: theme.textTheme.labelLarge?.copyWith(
                color: selected
                    ? theme.colorScheme.onPrimary
                    : theme.colorScheme.onSurface,
                fontWeight: selected ? FontWeight.w700 : FontWeight.w600,
              ),
            ),
          ),
        ),
      ),
    );
  }
}
