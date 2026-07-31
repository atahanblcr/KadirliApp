import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/widgets/widgets.dart';
import '../../application/announcements_providers.dart';

/// Duyuru listesinin tür filtresi — yatay kaydırılan chip şeridi.
///
/// **Hata durumunda şerit hiç çizilmez:** tür listesi alınamadıysa duyurular
/// yine okunabilir olmalı; kullanıcıya çalışmayan bir filtre göstermek "işlevsiz
/// buton" olurdu (MOBILE_UX_PLAN). Yükleniyorken yerini tutan skeleton çıkar ki
/// liste zıplamasın.
class AnnouncementTypeFilter extends ConsumerWidget {
  const AnnouncementTypeFilter({super.key});

  static const _height = 44.0;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final types = ref.watch(announcementTypesProvider);
    final selectedId = ref.watch(
      announcementFeedProvider.select((state) => state.typeId),
    );

    return switch (types) {
      AsyncData(value: final items) when items.isNotEmpty => SizedBox(
        height: _height,
        child: ListView.separated(
          scrollDirection: Axis.horizontal,
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
          itemCount: items.length + 1,
          separatorBuilder: (_, _) => AppSpacing.wGapSm,
          itemBuilder: (context, index) {
            if (index == 0) {
              return _TypeChip(
                label: 'Tümü',
                icon: Icons.apps_rounded,
                selected: selectedId == null,
                onTap: () => ref
                    .read(announcementFeedProvider.notifier)
                    .selectType(null),
              );
            }
            final type = items[index - 1];
            return _TypeChip(
              label: type.name,
              icon: type.materialIcon,
              accent: type.accentColor,
              selected: selectedId == type.id,
              onTap: () => ref
                  .read(announcementFeedProvider.notifier)
                  .selectType(type.id),
            );
          },
        ),
      ),
      AsyncLoading() => const SizedBox(
        height: _height,
        child: Padding(
          padding: EdgeInsets.symmetric(horizontal: AppSpacing.lg),
          child: Row(
            children: [
              SkeletonBox(height: 32, width: 72, borderRadius: AppRadius.rPill),
              AppSpacing.wGapSm,
              SkeletonBox(height: 32, width: 110, borderRadius: AppRadius.rPill),
              AppSpacing.wGapSm,
              SkeletonBox(height: 32, width: 96, borderRadius: AppRadius.rPill),
            ],
          ),
        ),
      ),
      // Hata / boş tür listesi → filtre yok, liste tam ekran.
      _ => const SizedBox.shrink(),
    };
  }
}

/// Tek filtre chip'i.
///
/// [accent] sunucudan gelen tür rengi: **yalnız seçili olmayan chip'in ikon
/// tonunda** kullanılır. Seçili chip tamamen temanın birincil renginde kalır →
/// "hangisi seçili" sorusu her zaman aynı görsel kuralla cevaplanır
/// (bkz. `AnnouncementType.accentColor` notu).
class _TypeChip extends StatelessWidget {
  const _TypeChip({
    required this.label,
    required this.icon,
    required this.selected,
    required this.onTap,
    this.accent,
  });

  final String label;
  final IconData icon;
  final bool selected;
  final VoidCallback onTap;
  final Color? accent;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final background = selected
        ? theme.colorScheme.primary
        : theme.colorScheme.surface;
    final foreground = selected
        ? theme.colorScheme.onPrimary
        : theme.colorScheme.onSurface;
    final iconColor = selected ? foreground : (accent ?? palette.muted);

    return Semantics(
      button: true,
      selected: selected,
      label: label,
      child: Material(
        color: background,
        shape: RoundedRectangleBorder(
          borderRadius: AppRadius.rPill,
          side: BorderSide(
            color: selected ? theme.colorScheme.primary : palette.border,
          ),
        ),
        child: InkWell(
          onTap: onTap,
          borderRadius: AppRadius.rPill,
          child: Padding(
            padding: const EdgeInsets.symmetric(
              horizontal: AppSpacing.md,
              vertical: AppSpacing.sm,
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(icon, size: 16, color: iconColor),
                AppSpacing.wGapSm,
                Text(
                  label,
                  style: theme.textTheme.labelLarge?.copyWith(
                    color: foreground,
                    fontWeight: selected ? FontWeight.w700 : FontWeight.w600,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
