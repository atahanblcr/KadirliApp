import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/widgets.dart';
import '../application/places_providers.dart';
import 'widgets/place_card.dart';

/// Gezilecek yerler (11.11).
///
/// Arama + kategori chip'leri **birlikte** uygulanır (11.7 rehber kararı).
/// Kategori şeridi yalnız kategori listesi gelirse çizilir — olmayan bir
/// listeden chip üretmek "işlevsiz buton" olurdu (11.6 kararı).
class PlacesScreen extends ConsumerStatefulWidget {
  const PlacesScreen({super.key});

  @override
  ConsumerState<PlacesScreen> createState() => _PlacesScreenState();
}

class _PlacesScreenState extends ConsumerState<PlacesScreen> {
  final _scrollController = ScrollController();
  final _searchController = TextEditingController();

  static const _loadMoreThreshold = 400.0;

  @override
  void initState() {
    super.initState();
    _searchController.text = ref.read(placesFeedProvider).filter.search;
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
      ref.read(placesFeedProvider.notifier).loadMore();
    }
  }

  @override
  Widget build(BuildContext context) {
    final controller = ref.read(placesFeedProvider.notifier);

    // "Filtreleri temizle" arama kutusunu da temizlemeli (11.7/11.8 hatası).
    ref.listen(placesFeedProvider.select((state) => state.filter.search), (
      _,
      search,
    ) {
      if (search != _searchController.text) _searchController.text = search;
    });

    return AppScaffold(
      title: 'Gezilecek Yerler',
      onRefresh: controller.refresh,
      body: Column(
        children: [
          AppSpacing.gapMd,
          Padding(
            padding: AppSpacing.screenPadding,
            child: AppTextField(
              controller: _searchController,
              hint: 'Mekan adı ara',
              prefixIcon: Icons.search_rounded,
              textInputAction: TextInputAction.search,
              onChanged: controller.search,
            ),
          ),
          const _CategoryStrip(),
          AppSpacing.gapSm,
          Expanded(child: _Body(scrollController: _scrollController)),
        ],
      ),
    );
  }
}

class _CategoryStrip extends ConsumerWidget {
  const _CategoryStrip();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final categories = ref.watch(placeCategoriesProvider).value;
    if (categories == null || categories.isEmpty) {
      return const SizedBox.shrink();
    }

    final selected = ref.watch(
      placesFeedProvider.select((state) => state.filter.categoryId),
    );
    final controller = ref.read(placesFeedProvider.notifier);

    // ⚠️ Yatay `ListView` tembel kurulum yapıyor → ekran dışındaki chip hiç
    // oluşturulmuyor (erişilebilirlik + test sorunu) → `SingleChildScrollView`.
    return Padding(
      padding: const EdgeInsets.only(top: AppSpacing.md),
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        padding: AppSpacing.screenPadding,
        child: Row(
          children: [
            FilterChoiceChip(
              label: 'Tümü',
              selected: selected == null,
              onTap: () => controller.selectCategory(null),
            ),
            for (final category in categories) ...[
              AppSpacing.wGapSm,
              FilterChoiceChip(
                label: category.name,
                icon: category.materialIcon,
                selected: selected == category.id,
                onTap: () => controller.selectCategory(category.id),
              ),
            ],
          ],
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
    final state = ref.watch(placesFeedProvider);
    final controller = ref.read(placesFeedProvider.notifier);
    final theme = Theme.of(context);

    if (state.isLoadingFirstPage) {
      return const LoadingView(itemCount: 3);
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
        icon: Icons.travel_explore_rounded,
        title: filter.isActive ? 'Sonuç bulunamadı' : 'Henüz mekan eklenmemiş',
        message: filter.isActive
            ? 'Aramanız mekan adında yapılıyor; farklı bir ad ya da kategori '
                  'deneyebilirsiniz.'
            : 'Kadirli\'nin gezilecek yerleri eklendiğinde burada görünecek.',
        actionLabel: filter.isActive ? 'Filtreleri temizle' : null,
        onAction: filter.isActive ? controller.clearFilters : null,
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
      separatorBuilder: (_, _) => AppSpacing.gapMd,
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
              'Toplam ${state.totalCount} mekan',
              textAlign: TextAlign.center,
              style: theme.textTheme.labelSmall?.copyWith(
                color: theme.palette.muted,
              ),
            ),
          );
        }

        final place = state.items[index];
        return PlaceCard(
          place: place,
          onTap: () => context.push(AppRoutes.placeDetail(place.id)),
        );
      },
    );
  }
}
