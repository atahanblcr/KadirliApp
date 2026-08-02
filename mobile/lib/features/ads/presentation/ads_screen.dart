import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/network/network.dart';
import '../../../core/paging/paged_list_footer.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../../auth/presentation/widgets/login_required_sheet.dart';
import '../application/ads_providers.dart';
import '../application/favorite_ads_controller.dart';
import '../data/models/ad_category.dart';
import 'widgets/ad_card.dart';
import 'widgets/price_filter_sheet.dart';

/// İlanlar sekmesi (11.8): arama + kategori + sıralama + fiyat aralığı ile
/// sayfalı, sonsuz kaydırmalı liste.
///
/// Sekme olduğu için geri butonu yok; detay `/ilanlar/<id>` alt rotasına
/// **aynı dalın Navigator'ında** açılır (alt sekme çubuğu yerinde kalır,
/// geri liste konumunu korur).
class AdsScreen extends ConsumerStatefulWidget {
  const AdsScreen({super.key});

  @override
  ConsumerState<AdsScreen> createState() => _AdsScreenState();
}

class _AdsScreenState extends ConsumerState<AdsScreen> {
  final _scrollController = ScrollController();
  final _searchController = TextEditingController();
  final _searchDebouncer = Debouncer();

  static const _loadMoreThreshold = 400.0;

  @override
  void initState() {
    super.initState();
    _searchController.text = ref.read(adsFeedProvider).filter.search;
    _scrollController.addListener(_onScroll);
  }

  @override
  void dispose() {
    _searchDebouncer.dispose();
    _scrollController.dispose();
    _searchController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (!_scrollController.hasClients) return;
    final position = _scrollController.position;
    if (position.pixels >= position.maxScrollExtent - _loadMoreThreshold) {
      ref.read(adsFeedProvider.notifier).loadMore();
    }
  }

  void _onSearchChanged(String value) {
    // Arama sunucuda başlık **ve açıklamada** koşuyor → her tuşta istek atmak
    // hem pahalı hem de listeyi titretiyor.
    _searchDebouncer.run(() {
      if (!mounted) return;
      ref.read(adsFeedProvider.notifier).search(value);
    });
  }

  void _clearSearch() {
    _searchController.clear();
    _searchDebouncer.flush(() => ref.read(adsFeedProvider.notifier).search(''));
  }

  @override
  Widget build(BuildContext context) {
    final controller = ref.read(adsFeedProvider.notifier);

    // 🐛 Canlı testte yakalandı: "Filtreleri temizle" / boş sonuç ekranı
    // filtreyi sıfırlıyor ama kutudaki metin ekranda kalıyordu → kullanıcı
    // "renaultzzzz" yazısını görüp listenin neden dolu olduğunu anlamıyordu.
    // Filtre dışarıdan değiştiğinde kutu da senkronlanır (kullanıcı yazarken
    // metin zaten eşit olduğu için bu dal çalışmaz).
    ref.listen(adsFeedProvider.select((state) => state.filter.search), (
      _,
      search,
    ) {
      if (search != _searchController.text) _searchController.text = search;
    });

    final hasSearchText = ref.watch(
      adsFeedProvider.select((state) => state.filter.search.isNotEmpty),
    );

    return AppScaffold(
      title: 'İlanlar',
      showBackButton: false,
      onRefresh: () async {
        ref.invalidate(adRootCategoriesProvider);
        ref.read(favoriteAdsProvider.notifier).load();
        await controller.refresh();
      },
      // 11.9: pazaryerinin ana aksiyonu. Anonim kullanıcı router'la giriş
      // ekranına ATILMAZ (kabuk kapanır) — önce nazik davet gösterilir,
      // vazgeçerse listede kalır.
      floatingActionButton: FloatingActionButton.extended(
        heroTag: 'ads-create',
        onPressed: () async {
          if (!await ensureSignedIn(
            context,
            ref,
            reason: 'İlan verebilmek için giriş yapmanız gerekiyor.',
          )) {
            return;
          }
          if (!context.mounted) return;
          context.push(AppRoutes.adCreate);
        },
        icon: const Icon(Icons.add_rounded),
        label: const Text('İlan ver'),
      ),
      body: Column(
        children: [
          AppSpacing.gapMd,
          Padding(
            padding: AppSpacing.screenPadding,
            child: AppTextField(
              controller: _searchController,
              hint: 'Ne aramıştınız?',
              prefixIcon: Icons.search_rounded,
              textInputAction: TextInputAction.search,
              onChanged: _onSearchChanged,
              onSubmitted: (value) =>
                  _searchDebouncer.flush(() => controller.search(value)),
              suffix: hasSearchText
                  ? IconButton(
                      tooltip: 'Aramayı temizle',
                      icon: const Icon(Icons.close_rounded, size: 20),
                      onPressed: _clearSearch,
                    )
                  : null,
            ),
          ),
          AppSpacing.gapMd,
          const _CategoryStrip(),
          AppSpacing.gapSm,
          const _SortStrip(),
          AppSpacing.gapSm,
          Expanded(child: _Body(scrollController: _scrollController)),
        ],
      ),
    );
  }
}

/// Tek şeritli, **iki katmanlı** kategori filtresi.
///
/// Kök seçili değilken kök kategoriler listelenir (alt kategorisi olanda `›`
/// işareti). Bir kök seçilince şerit o kökün içine iner: `[Tümü] [Araçlar]
/// [Otomobil] [Motosiklet] …` — hangi chip vurguluysa filtre odur, belirsizlik
/// yok.
///
/// ⚠️ Sunucu filtresi tam eşleşme: "Araçlar" seçiliyken **yalnız doğrudan
/// Araçlar'a yazılmış** ilanlar döner. Alt kategorileri şeritte göstermek bu
/// yüzden şart — kullanıcı aradığını bulamayınca "uygulama bozuk" demesin.
class _CategoryStrip extends ConsumerWidget {
  const _CategoryStrip();

  static const _height = 44.0;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final categories = ref.watch(adRootCategoriesProvider);
    final filter = ref.watch(adsFeedProvider.select((state) => state.filter));
    final controller = ref.read(adsFeedProvider.notifier);

    return switch (categories) {
      AsyncData(value: final roots) when roots.isNotEmpty => SizedBox(
        height: _height,
        child: _buildStrip(context, ref, roots, filter, controller),
      ),
      AsyncLoading() => const SizedBox(
        height: _height,
        child: Padding(
          padding: EdgeInsets.symmetric(horizontal: AppSpacing.lg),
          child: Row(
            children: [
              SkeletonBox(height: 32, width: 72, borderRadius: AppRadius.rPill),
              AppSpacing.wGapSm,
              SkeletonBox(
                height: 32,
                width: 104,
                borderRadius: AppRadius.rPill,
              ),
              AppSpacing.wGapSm,
              SkeletonBox(height: 32, width: 88, borderRadius: AppRadius.rPill),
            ],
          ),
        ),
      ),
      // Kategoriler alınamazsa şerit hiç çizilmez (11.6 kararı: çalışmayan
      // filtre gösterilmez); liste yine çalışır.
      _ => const SizedBox.shrink(),
    };
  }

  Widget _buildStrip(
    BuildContext context,
    WidgetRef ref,
    List<AdCategory> roots,
    AdsFilter filter,
    AdsFeedController controller,
  ) {
    final rootId = filter.rootCategoryId;
    final selectedRoot = rootId == null
        ? null
        : roots.where((category) => category.id == rootId).firstOrNull;

    final chips = <Widget>[
      FilterChoiceChip(
        label: 'Tümü',
        icon: Icons.apps_rounded,
        selected: filter.categoryId == null,
        onTap: () => controller.selectRootCategory(null),
      ),
    ];

    if (selectedRoot == null) {
      for (final category in roots) {
        chips.add(
          FilterChoiceChip(
            label: category.name,
            icon: category.materialIcon,
            selected: false,
            trailing: category.hasSubCategories
                ? Icons.chevron_right_rounded
                : null,
            onTap: () => controller.selectRootCategory(category.id),
          ),
        );
      }
    } else {
      chips.add(
        FilterChoiceChip(
          label: selectedRoot.name,
          icon: selectedRoot.materialIcon,
          selected: filter.categoryId == selectedRoot.id,
          onTap: () => controller.selectRootCategory(selectedRoot.id),
        ),
      );
      if (selectedRoot.hasSubCategories) {
        final subs = ref.watch(adSubCategoriesProvider(selectedRoot.id)).value;
        for (final sub in subs ?? const <AdCategory>[]) {
          chips.add(
            FilterChoiceChip(
              label: sub.name,
              icon: sub.materialIcon,
              selected: filter.categoryId == sub.id,
              onTap: () => controller.selectSubCategory(sub.id),
            ),
          );
        }
      }
    }

    // ⚠️ `ListView` yerine `SingleChildScrollView`: şerit en fazla ~15 chip
    // taşıyor ve `ListView` **tembel** olduğu için ekran dışındaki chip hiç
    // kurulmuyor — hem erişilebilirlik ağacından düşüyor hem de widget
    // testinde "yok" görünüyor. Bu boyutta tembelliğin faydası yok.
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
      child: Row(
        children: [
          for (final chip in chips) ...[
            chip,
            if (chip != chips.last) AppSpacing.wGapSm,
          ],
        ],
      ),
    );
  }
}

/// Sıralama chip'leri + fiyat aralığı chip'i tek şeritte.
class _SortStrip extends ConsumerWidget {
  const _SortStrip();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final filter = ref.watch(adsFeedProvider.select((state) => state.filter));
    final controller = ref.read(adsFeedProvider.notifier);
    final priceLabel = AppMoney.rangeLabel(filter.minPrice, filter.maxPrice);

    return SizedBox(
      height: 40,
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
        child: Row(
          children: [
            for (final sort in AdSort.values) ...[
              FilterChoiceChip(
                label: sort.label,
                icon: _sortIcon(sort),
                selected: filter.sort == sort,
                dense: true,
                onTap: () => controller.changeSort(sort),
              ),
              AppSpacing.wGapSm,
            ],
            FilterChoiceChip(
              label: priceLabel ?? 'Fiyat',
              icon: Icons.payments_outlined,
              selected: filter.hasPriceRange,
              dense: true,
              trailing: filter.hasPriceRange ? Icons.close_rounded : null,
              onTap: () async {
                if (filter.hasPriceRange) {
                  controller.clearPriceRange();
                  return;
                }
                final result = await showPriceFilterSheet(
                  context,
                  min: filter.minPrice,
                  max: filter.maxPrice,
                );
                if (result == null) return;
                controller.applyPriceRange(min: result.min, max: result.max);
              },
            ),
          ],
        ),
      ),
    );
  }

  static IconData _sortIcon(AdSort sort) => switch (sort) {
    AdSort.newest => Icons.schedule_rounded,
    AdSort.oldest => Icons.history_rounded,
    AdSort.priceAsc => Icons.arrow_upward_rounded,
    AdSort.priceDesc => Icons.arrow_downward_rounded,
  };
}

/// Şeritlerdeki ortak chip (rehberdeki `_CategoryChip`'in ilan ekranı sürümü:
/// sonuna ikon alabiliyor ve `dense` varyantı var).

class _Body extends ConsumerWidget {
  const _Body({required this.scrollController});

  final ScrollController scrollController;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(adsFeedProvider);
    final controller = ref.read(adsFeedProvider.notifier);

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
      final filter = state.filter;
      return EmptyView(
        icon: Icons.sell_outlined,
        title: filter.isActive ? 'Sonuç bulunamadı' : 'Henüz ilan yok',
        message: filter.isActive
            ? 'Farklı bir arama, kategori ya da fiyat aralığı deneyebilirsiniz.'
            : 'İlk ilanı siz verin — onaylandıktan sonra burada görünecek.',
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
        // "İlan ver" düğmesi son kartın üstünü kapatmasın (11.9).
        AppSpacing.huge + AppSpacing.xl,
      ),
      itemCount: state.items.length + 1,
      separatorBuilder: (_, _) => AppSpacing.gapSm,
      itemBuilder: (context, index) {
        if (index == state.items.length) {
          return PagedListFooter(
            state: state,
            onLoadMore: controller.loadMore,
            itemNoun: 'ilan',
          );
        }

        final ad = state.items[index];
        return AdCard(
          ad: ad,
          isFavorite: ref.watch(isFavoriteProvider(ad.id)),
          onFavoriteTap: () => toggleAdFavorite(context, ref, ad.id),
          onTap: () => context.push(AppRoutes.adDetail(ad.id)),
        );
      },
    );
  }
}

/// Favori kalbi: anonimse **hata değil davet** (`ensureSignedIn`), hata olursa
/// iyimser değişiklik geri alınır ve sebep şeritte yazılır.
///
/// Liste kartı ve detay ekranı aynı akışı kullanıyor.
Future<void> toggleAdFavorite(
  BuildContext context,
  WidgetRef ref,
  String adId,
) async {
  final messenger = ScaffoldMessenger.of(context);
  if (!await ensureSignedIn(
    context,
    ref,
    reason: 'Beğendiğiniz ilanları kaydetmek için giriş yapmanız gerekiyor.',
  )) {
    return;
  }

  try {
    final isFavorite = await ref
        .read(favoriteAdsProvider.notifier)
        .toggle(adId);
    messenger.showSnackBar(
      SnackBar(
        content: Text(
          isFavorite ? 'Favorilere eklendi' : 'Favorilerden çıkarıldı',
        ),
        duration: const Duration(seconds: 2),
      ),
    );
  } on ApiException catch (error) {
    messenger.showSnackBar(SnackBar(content: Text(error.message)));
  }
}
