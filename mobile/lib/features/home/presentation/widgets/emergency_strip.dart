import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/router/app_routes.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/utils/utils.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../pharmacies/data/models/on_duty_pharmacy.dart';
import '../../../power_outages/data/models/power_outage.dart';
import '../../application/home_providers.dart';

/// Ana Sayfa'nın en üstündeki **acil şerit** (MOBILE_UX_PLAN §5).
///
/// Şehirde "şu an lazım olan" iki bilgi: bugünün nöbetçi eczanesi ve elektrik
/// kesintisi durumu. İkisi ayrı istek olduğu için **satırlar birbirinden
/// bağımsız** yükleniyor/hata veriyor — eczane ucu patlarsa kesinti bilgisi
/// yine görünür.
class EmergencyStrip extends ConsumerWidget {
  const EmergencyStrip({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);

    return AppCard(
      color: theme.colorScheme.primaryContainer,
      borderColor: theme.colorScheme.primary.withValues(alpha: 0.22),
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.lg,
        vertical: AppSpacing.md,
      ),
      semanticLabel: 'Şu an Kadirli: nöbetçi eczane ve elektrik kesintisi durumu',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'ŞU AN KADİRLİ',
            style: theme.textTheme.labelSmall?.copyWith(
              color: theme.colorScheme.onPrimaryContainer.withValues(alpha: 0.75),
              letterSpacing: 0.6,
            ),
          ),
          AppSpacing.gapSm,
          StripRow<OnDutyPharmacy>(
            icon: Icons.local_pharmacy_rounded,
            state: ref.watch(onDutyPharmaciesProvider),
            onTap: () => context.push(AppRoutes.pharmacies),
            onRetry: () => ref.invalidate(onDutyPharmaciesProvider),
            emptyText: 'Bugünün nöbetçi eczanesi henüz girilmedi',
            builder: _pharmacyContent,
          ),
          Divider(
            height: AppSpacing.lg,
            color: theme.colorScheme.onPrimaryContainer.withValues(alpha: 0.12),
          ),
          StripRow<PowerOutage>(
            icon: Icons.bolt_rounded,
            state: ref.watch(relevantOutagesProvider),
            onTap: () => context.push(AppRoutes.powerOutages),
            onRetry: () => ref.invalidate(relevantOutagesProvider),
            emptyText: 'Planlı elektrik kesintisi yok',
            builder: _outageContent,
          ),
        ],
      ),
    );
  }

  /// Aynı güne birden fazla eczane atanabilir → ilki + "+N".
  static StripContent _pharmacyContent(List<OnDutyPharmacy> pharmacies) {
    final first = pharmacies.first;
    final extra = pharmacies.length - 1;
    return StripContent(
      text: 'Bugün nöbetçi: ${first.name}${extra > 0 ? ' +$extra' : ''}',
      tone: StripTone.highlight,
    );
  }

  /// Süren kesinti uyarı (kırmızı), planlanan yalnız vurgulu gösterilir.
  static StripContent _outageContent(List<PowerOutage> outages) {
    final first = outages.first;
    final place = (first.neighborhood?.trim().isNotEmpty ?? false)
        ? first.neighborhood!.trim()
        : 'Kadirli';
    final extra = outages.length - 1;
    final active = first.isActive();

    final when = active
        ? 'kesinti sürüyor · ${AppDate.time(first.endTime)}\'ye kadar'
        : 'planlı kesinti · ${AppDate.dateShort(first.startTime)} '
              '${AppDate.time(first.startTime)}';

    return StripContent(
      text: '$place · $when${extra > 0 ? ' +$extra' : ''}',
      tone: active ? StripTone.alert : StripTone.highlight,
    );
  }
}

/// Satırın vurgu derecesi — palet dışı renk yok.
enum StripTone {
  /// Sakin/olumlu bilgi ("kesinti yok").
  calm,

  /// Dikkate değer bilgi (nöbetçi eczane, planlı kesinti).
  highlight,

  /// Şu an süren olumsuz durum.
  alert,
}

@immutable
class StripContent {
  const StripContent({required this.text, required this.tone});

  final String text;
  final StripTone tone;
}

/// Şeridin tek satırı: yükleniyor / boş / dolu / hata durumlarını kapsar.
///
/// Hatada satır **tıklanınca yeniden dener** (tüm sayfayı hataya düşürmek
/// yerine — şeridin diğer satırı çalışıyor olabilir).
class StripRow<T> extends StatelessWidget {
  const StripRow({
    super.key,
    required this.icon,
    required this.state,
    required this.builder,
    required this.emptyText,
    required this.onTap,
    required this.onRetry,
  });

  final IconData icon;
  final AsyncValue<List<T>> state;
  final StripContent Function(List<T> items) builder;
  final String emptyText;
  final VoidCallback onTap;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final onContainer = theme.colorScheme.onPrimaryContainer;

    return switch (state) {
      AsyncError() => _Row(
        icon: icon,
        color: onContainer.withValues(alpha: 0.55),
        onTap: onRetry,
        child: Row(
          children: [
            Expanded(
              child: Text(
                'Bilgi alınamadı',
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: onContainer.withValues(alpha: 0.7),
                ),
              ),
            ),
            Icon(
              Icons.refresh_rounded,
              size: 18,
              color: onContainer.withValues(alpha: 0.7),
            ),
          ],
        ),
      ),
      AsyncData(value: final items) => _dataRow(theme, onContainer, items),
      // Yükleniyor (ilk açılış) — skeleton, spinner değil.
      _ => _Row(
        icon: icon,
        color: onContainer.withValues(alpha: 0.4),
        child: const SkeletonBox(height: 14, width: 170),
      ),
    };
  }

  Widget _dataRow(ThemeData theme, Color onContainer, List<T> items) {
    final content = items.isEmpty
        ? StripContent(text: emptyText, tone: StripTone.calm)
        : builder(items);

    final color = switch (content.tone) {
      StripTone.calm => onContainer.withValues(alpha: 0.7),
      StripTone.highlight => onContainer,
      StripTone.alert => theme.palette.danger,
    };

    return _Row(
      icon: icon,
      color: color,
      onTap: onTap,
      child: Text(
        content.text,
        style: theme.textTheme.bodyMedium?.copyWith(
          color: color,
          fontWeight: content.tone == StripTone.calm ? FontWeight.w400 : FontWeight.w600,
        ),
      ),
    );
  }
}

class _Row extends StatelessWidget {
  const _Row({required this.icon, required this.color, required this.child, this.onTap});

  final IconData icon;
  final Color color;
  final Widget child;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final row = Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
      child: Row(
        children: [
          Icon(icon, size: 20, color: color),
          AppSpacing.wGapMd,
          Expanded(child: child),
        ],
      ),
    );

    if (onTap == null) return row;
    return InkWell(onTap: onTap, borderRadius: AppRadius.rSm, child: row);
  }
}
