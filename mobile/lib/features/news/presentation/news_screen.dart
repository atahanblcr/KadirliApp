import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/paging/paged_list_footer.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/widgets.dart';
import '../application/news_providers.dart';
import '../application/saved_news_controller.dart';
import 'widgets/news_card.dart';
import 'widgets/news_featured_card.dart';

/// Haberler (12.14) — 13. modül.
///
/// Kaynak `silagazetesi.com.tr` ama **istemci oraya hiç bağlanmaz**: zincir
/// `WordPress → (Hangfire) → bizim Postgres → /v1/news → mobil` (12.12).
///
/// Ekranın üç şeridi var ve üçü de **sunucuda** süzüyor: manşet (`?featured=true`),
/// kategori (`?categoryId=`) ve arama (`?search=`). İstemcide süzmek 20'lik
/// sayfadan kayıt eleyip `totalCount`'u ve sonsuz kaydırmayı yalancı yapardı.
class NewsScreen extends ConsumerStatefulWidget {
  const NewsScreen({super.key});

  @override
  ConsumerState<NewsScreen> createState() => _NewsScreenState();
}

class _NewsScreenState extends ConsumerState<NewsScreen> {
  final _scrollController = ScrollController();
  final _searchController = TextEditingController();

  static const _loadMoreThreshold = 400.0;

  @override
  void initState() {
    super.initState();
    _searchController.text = ref.read(newsFeedProvider).filter.search;
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
      ref.read(newsFeedProvider.notifier).loadMore();
    }
  }

  @override
  Widget build(BuildContext context) {
    final controller = ref.read(newsFeedProvider.notifier);
    final savedCount = ref.watch(savedNewsProvider).length;

    // "Filtreleri temizle" arama kutusunu da temizlemeli — bu projede iki kez
    // atlanmış bir bağ (11.7/11.8).
    ref.listen(newsFeedProvider.select((state) => state.filter.search), (
      _,
      search,
    ) {
      if (search != _searchController.text) _searchController.text = search;
    });

    return AppScaffold(
      title: 'Haberler',
      actions: [
        IconButton(
          tooltip: 'Kaydedilenler',
          // Sayı rozet olarak basılmıyor: 0 iken de görünen bir "0" rozeti,
          // bilgi değil gürültü. Boş liste ekranın kendisi zaten anlatıyor.
          icon: Icon(
            savedCount > 0
                ? Icons.bookmarks_rounded
                : Icons.bookmarks_outlined,
          ),
          onPressed: () => context.push(AppRoutes.savedNews),
        ),
      ],
      onRefresh: () async {
        ref.invalidate(newsCategoriesProvider);
        ref.invalidate(newsFeaturedProvider);
        await controller.refresh();
      },
      body: Column(
        children: [
          AppSpacing.gapMd,
          Padding(
            padding: AppSpacing.screenPadding,
            child: AppTextField(
              controller: _searchController,
              hint: 'Haberlerde ara',
              prefixIcon: Icons.search_rounded,
              textInputAction: TextInputAction.search,
              onChanged: controller.search,
            ),
          ),
          AppSpacing.gapMd,
          const _CategoryFilter(),
          AppSpacing.gapSm,
          Expanded(child: _NewsList(scrollController: _scrollController)),
        ],
      ),
    );
  }
}

/// Kategori şeridi. Liste alınamazsa şerit **hiç çizilmez** (11.6'dan beri
/// geçerli kural: çalışmayan filtre gösterilmez).
///
/// ⚠️ Şeritte "Tümü" **var**: sunucunun kategori sözlüğü panelden değişebiliyor
/// (12.13 dışlaması) ve süzgeci kaldırmanın açık bir yolu olmalı.
/// ⚠️ Kaydı **0 olan** kategori de gösteriliyor: sayı bir anlık görüntüdür,
/// senkron bir dakika sonra kayıt getirebilir — sunucunun döndürdüğü bir
/// kategoriyi istemcinin gizlemesi "şüphede kalınca gizle" olurdu (§7 madde 49
/// bunun tersini söylüyor).
class _CategoryFilter extends ConsumerWidget {
  const _CategoryFilter();

  static const _height = 44.0;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final categories = ref.watch(newsCategoriesProvider);
    final selectedId = ref.watch(
      newsFeedProvider.select((state) => state.filter.categoryId),
    );
    final controller = ref.read(newsFeedProvider.notifier);

    return switch (categories) {
      AsyncData(value: final items) when items.isNotEmpty => SizedBox(
        height: _height,
        // ⚠️ Yatay `ListView` tembeldir: ekran dışı chip hiç kurulmaz ve test
        // onu bulamaz (11.8) → `SingleChildScrollView` + `Row`.
        child: SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
          child: Row(
            children: [
              FilterChoiceChip(
                label: 'Tümü',
                dense: true,
                selected: selectedId == null,
                onTap: () => controller.selectCategory(null),
              ),
              for (final category in items) ...[
                AppSpacing.wGapSm,
                FilterChoiceChip(
                  label: category.label,
                  dense: true,
                  selected: selectedId == category.id,
                  onTap: () => controller.selectCategory(category.id),
                ),
              ],
            ],
          ),
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
              SkeletonBox(height: 32, width: 96, borderRadius: AppRadius.rPill),
              AppSpacing.wGapSm,
              SkeletonBox(height: 32, width: 84, borderRadius: AppRadius.rPill),
            ],
          ),
        ),
      ),
      _ => const SizedBox.shrink(),
    };
  }
}

/// Manşet şeridi (plan dışı ek) — yalnız **süzgeçsiz** listede.
///
/// Kullanıcı "Spor" seçmişken başka kategoriden bir manşet basmak, süzgecin
/// çalışmadığı izlenimi verirdi. Manşet alınamazsa şerit sessizce **hiç
/// çizilmez**: ana liste zaten aynı haberleri taşıyor, yani bir hata mesajı
/// göstermek kullanıcıya çözemeyeceği bir sorunu anlatmak olurdu.
class _FeaturedStrip extends ConsumerWidget {
  const _FeaturedStrip();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final featured = ref.watch(newsFeaturedProvider);
    final items = featured.value ?? const [];
    if (items.isEmpty) return const SizedBox.shrink();

    final cardWidth = MediaQuery.sizeOf(context).width * 0.78;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.xs),
          child: Row(
            children: [
              Icon(
                Icons.star_rounded,
                size: 18,
                color: Theme.of(context).colorScheme.primary,
              ),
              AppSpacing.wGapSm,
              Text('Öne çıkanlar', style: Theme.of(context).textTheme.titleSmall),
            ],
          ),
        ),
        AppSpacing.gapSm,
        SizedBox(
          // Yükseklik sabit: şeridin içinde farklı boylarda kartlar olsa
          // yatay kaydırma sırasında liste "titrer".
          height: cardWidth * 9 / 16 + 132,
          child: ListView.separated(
            scrollDirection: Axis.horizontal,
            clipBehavior: Clip.none,
            itemCount: items.length,
            separatorBuilder: (_, _) => AppSpacing.wGapMd,
            itemBuilder: (context, index) {
              final article = items[index];
              return NewsFeaturedCard(
                article: article,
                width: cardWidth,
                onTap: () => context.push(AppRoutes.newsDetail(article.id)),
              );
            },
          ),
        ),
        AppSpacing.gapLg,
      ],
    );
  }
}

class _NewsList extends ConsumerWidget {
  const _NewsList({required this.scrollController});

  final ScrollController scrollController;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(newsFeedProvider);
    final controller = ref.read(newsFeedProvider.notifier);
    final saved = ref.watch(savedNewsProvider);
    final filter = state.filter;

    if (state.isLoadingFirstPage) {
      return const LoadingView(itemCount: 5);
    }

    if (state.error != null && state.items.isEmpty) {
      return ErrorView(
        message: state.error!.message,
        traceId: state.error!.traceId,
        onRetry: controller.retry,
      );
    }

    if (state.isEmpty) {
      return EmptyView(
        icon: Icons.newspaper_outlined,
        // 🔴 Boşalan liste **sebebini söyler**: "hiç haber yok" ile "bu
        // kategoride haber yok" aynı cümle değil (checklist §5).
        title: switch (filter) {
          NewsFilter(isSearchTooShort: true) => 'Aramaya devam edin',
          NewsFilter(isActive: true) => 'Bu filtrede haber yok',
          _ => 'Henüz haber yok',
        },
        message: switch (filter) {
          // Sunucu 2 karakterin altında **süzmüyor**; "sonuç yok" demek yalan
          // olurdu — kullanıcıya gerçekte ne olduğu söyleniyor.
          NewsFilter(isSearchTooShort: true) =>
            'Arama için en az ${NewsFilter.minSearchLength} harf yazın.',
          NewsFilter(isActive: true) =>
            'Farklı bir kategori seçebilir ya da aramanızı değiştirebilirsiniz.',
          _ => 'Yeni haberler yayımlandığında burada görünecek.',
        },
        actionLabel: filter.isActive ? 'Filtreleri temizle' : null,
        onAction: filter.isActive ? controller.clearFilters : null,
      );
    }

    // Manşet şeridi listenin **başlığı** olarak akar (ayrı bir kaydırma alanı
    // değil): iki dikey kaydırma alanı üst üste binince pull-to-refresh ve
    // sonsuz kaydırma birbirini yer.
    final showFeatured = !filter.isActive;

    return ListView.separated(
      controller: scrollController,
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.sm,
        AppSpacing.lg,
        AppSpacing.xxl,
      ),
      itemCount: state.items.length + (showFeatured ? 2 : 1),
      separatorBuilder: (_, _) => AppSpacing.gapSm,
      itemBuilder: (context, index) {
        if (showFeatured && index == 0) return const _FeaturedStrip();

        final itemIndex = index - (showFeatured ? 1 : 0);
        if (itemIndex == state.items.length) {
          return PagedListFooter(
            state: state,
            onLoadMore: controller.loadMore,
            itemNoun: 'haber',
          );
        }

        final article = state.items[itemIndex];
        return NewsCard(
          article: article,
          isSaved: isNewsSaved(saved, article.id),
          onTap: () => context.push(AppRoutes.newsDetail(article.id)),
        );
      },
    );
  }
}
