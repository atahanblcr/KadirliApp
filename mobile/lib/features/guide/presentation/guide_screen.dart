import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../application/guide_providers.dart';
import '../data/models/guide_category.dart';
import 'widgets/guide_item_tile.dart';

/// Şehir Rehberi (11.7).
///
/// **Tek ekran, iki filtre:** kategori chip'leri + arama. Plan "kategoriler →
/// liste → detay" diyordu; kategori sayısı az (6) ve kullanıcı çoğunlukla
/// "hastane" yazıp aramak istiyor — ayrı kategori ekranı fazladan bir dokunuş
/// olurdu. Kategoriler yine gezilebilir, sadece aynı ekranda.
class GuideScreen extends ConsumerStatefulWidget {
  const GuideScreen({super.key});

  @override
  ConsumerState<GuideScreen> createState() => _GuideScreenState();
}

class _GuideScreenState extends ConsumerState<GuideScreen> {
  final _scrollController = ScrollController();
  final _searchController = TextEditingController();

  static const _loadMoreThreshold = 400.0;

  @override
  void initState() {
    super.initState();
    _searchController.text = ref.read(guideFeedProvider).filter.search;
    _scrollController.addListener(_onScroll);
  }

  @override
  void dispose() {
    _scrollController.dispose();
    _searchController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (!_scrollController.hasClients) return;
    final position = _scrollController.position;
    if (position.pixels >= position.maxScrollExtent - _loadMoreThreshold) {
      ref.read(guideFeedProvider.notifier).loadMore();
    }
  }

  @override
  Widget build(BuildContext context) {
    final controller = ref.read(guideFeedProvider.notifier);

    // 🐛 11.8'de yakalanan aynı hata burada da vardı: "Filtreleri temizle"
    // aramayı sıfırlıyor ama kutuda eski metin kalıyordu.
    ref.listen(guideFeedProvider.select((state) => state.filter.search), (
      _,
      search,
    ) {
      if (search != _searchController.text) _searchController.text = search;
    });

    return AppScaffold(
      title: 'Şehir Rehberi',
      onRefresh: () async {
        ref.invalidate(guideCategoriesProvider);
        await controller.refresh();
      },
      body: Column(
        children: [
          AppSpacing.gapMd,
          Padding(
            padding: AppSpacing.screenPadding,
            child: AppTextField(
              controller: _searchController,
              hint: 'Kurum, kişi veya hizmet ara',
              prefixIcon: Icons.search_rounded,
              textInputAction: TextInputAction.search,
              onChanged: controller.search,
            ),
          ),
          AppSpacing.gapMd,
          const _CategoryFilter(),
          AppSpacing.gapSm,
          Expanded(child: _Body(scrollController: _scrollController)),
        ],
      ),
    );
  }
}

/// Kategori chip'leri. Tür listesi alınamazsa şerit **hiç çizilmez** (11.6
/// kararının aynısı: çalışmayan filtre gösterilmez).
class _CategoryFilter extends ConsumerWidget {
  const _CategoryFilter();

  static const _height = 44.0;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final categories = ref.watch(guideCategoriesProvider);
    final selectedId = ref.watch(
      guideFeedProvider.select((state) => state.filter.categoryId),
    );

    return switch (categories) {
      AsyncData(value: final items) when items.isNotEmpty => SizedBox(
        height: _height,
        child: ListView.separated(
          scrollDirection: Axis.horizontal,
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
          itemCount: items.length + 1,
          separatorBuilder: (_, _) => AppSpacing.wGapSm,
          itemBuilder: (context, index) {
            if (index == 0) {
              return _CategoryChip(
                label: 'Tümü',
                icon: Icons.apps_rounded,
                selected: selectedId == null,
                onTap: () =>
                    ref.read(guideFeedProvider.notifier).selectCategory(null),
              );
            }
            final category = items[index - 1];
            return _CategoryChip(
              label: category.name,
              icon: category.materialIcon,
              selected: selectedId == category.id,
              emphasize: category.isEmergency,
              onTap: () => ref
                  .read(guideFeedProvider.notifier)
                  .selectCategory(category.id),
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
              SkeletonBox(height: 32, width: 118, borderRadius: AppRadius.rPill),
              AppSpacing.wGapSm,
              SkeletonBox(height: 32, width: 90, borderRadius: AppRadius.rPill),
            ],
          ),
        ),
      ),
      _ => const SizedBox.shrink(),
    };
  }
}

class _CategoryChip extends StatelessWidget {
  const _CategoryChip({
    required this.label,
    required this.icon,
    required this.selected,
    required this.onTap,
    this.emphasize = false,
  });

  final String label;
  final IconData icon;
  final bool selected;
  final VoidCallback onTap;

  /// Acil numaralar chip'i seçili değilken de dikkat çeker (danger tonu).
  final bool emphasize;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final accent = emphasize ? palette.danger : theme.colorScheme.primary;
    final background = selected ? accent : theme.colorScheme.surface;
    final foreground = selected
        ? theme.colorScheme.onPrimary
        : (emphasize ? accent : theme.colorScheme.onSurface);

    return Semantics(
      button: true,
      selected: selected,
      label: label,
      child: Material(
        color: background,
        shape: RoundedRectangleBorder(
          borderRadius: AppRadius.rPill,
          side: BorderSide(
            color: selected || emphasize ? accent : palette.border,
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
                Icon(
                  icon,
                  size: 16,
                  color: selected ? foreground : (emphasize ? accent : palette.muted),
                ),
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

class _Body extends ConsumerWidget {
  const _Body({required this.scrollController});

  final ScrollController scrollController;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(guideFeedProvider);
    final controller = ref.read(guideFeedProvider.notifier);
    final theme = Theme.of(context);

    if (state.isLoadingFirstPage) {
      return const LoadingView(itemCount: 5, hasImage: false);
    }

    if (state.error != null && state.items.isEmpty) {
      return ErrorView(
        message: state.error!.message,
        traceId: state.error!.traceId,
        onRetry: controller.retry,
      );
    }

    if (state.isEmpty) {
      final filter = state.filter;
      return EmptyView(
        icon: Icons.menu_book_outlined,
        title: filter.isActive ? 'Sonuç bulunamadı' : 'Rehber henüz boş',
        message: filter.isActive
            ? 'Farklı bir arama ya da kategori deneyebilirsiniz.'
            : 'Kurum ve iletişim bilgileri eklendiğinde burada görünecek.',
        actionLabel: filter.isActive ? 'Filtreleri temizle' : null,
        onAction: filter.isActive
            ? () => controller.applyFilter(const GuideFilter())
            : null,
      );
    }

    return ListView.separated(
      controller: scrollController,
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.sm,
        AppSpacing.lg,
        AppSpacing.xxl,
      ),
      itemCount: state.items.length + 1,
      separatorBuilder: (_, _) => AppSpacing.gapSm,
      itemBuilder: (context, index) {
        if (index == state.items.length) {
          if (state.isLoadingMore) return const LoadingView.compact();
          if (state.loadMoreError != null) {
            return Padding(
              padding: const EdgeInsets.only(top: AppSpacing.md),
              child: AppButton.ghost(
                label: 'Devamını yükle',
                icon: Icons.refresh_rounded,
                size: AppButtonSize.small,
                expand: true,
                onPressed: controller.loadMore,
              ),
            );
          }
          if (state.hasMore) return const SizedBox(height: AppSpacing.lg);
          return Padding(
            padding: const EdgeInsets.only(top: AppSpacing.lg),
            child: Text(
              'Toplam ${state.totalCount} kayıt',
              textAlign: TextAlign.center,
              style: theme.textTheme.labelSmall?.copyWith(
                color: theme.palette.muted,
              ),
            ),
          );
        }

        final item = state.items[index];
        return GuideItemTile(
          item: item,
          // Telefon varsa listeden **doğrudan arama** — rehberin asıl işi bu,
          // kullanıcıyı detaya girmeye zorlamıyoruz.
          onCall: item.hasPhone ? () => AppLinks.call(item.phone!) : null,
          onTap: () => context.push(AppRoutes.guideItemDetail(item.id)),
        );
      },
    );
  }
}

/// Kategori adından ikon — dışarıdan da kullanılabilsin diye ayrı
/// (detay ekranı da aynı ikonu gösteriyor).
IconData guideIconFor(GuideCategory? category) =>
    category?.materialIcon ?? Icons.label_rounded;
