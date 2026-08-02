import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/paging/paged_list_footer.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/widgets.dart';
import '../application/taxis_providers.dart';
import '../data/recent_taxi_calls_store.dart';
import 'widgets/taxi_driver_tile.dart';

/// Taksiciler (11.11).
///
/// Uç yalnız **doğrulanmış + aktif** sürücüleri döndürüyor → filtre şeridine
/// gerek yok; arama (ad **ve plaka**) + liste + doğrudan "Ara" yeterli.
class TaxisScreen extends ConsumerStatefulWidget {
  const TaxisScreen({super.key});

  @override
  ConsumerState<TaxisScreen> createState() => _TaxisScreenState();
}

class _TaxisScreenState extends ConsumerState<TaxisScreen> {
  final _scrollController = ScrollController();
  final _searchController = TextEditingController();

  static const _loadMoreThreshold = 400.0;

  @override
  void initState() {
    super.initState();
    _searchController.text = ref.read(taxisFeedProvider).filter;
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
      ref.read(taxisFeedProvider.notifier).loadMore();
    }
  }

  @override
  Widget build(BuildContext context) {
    final controller = ref.read(taxisFeedProvider.notifier);

    ref.listen(taxisFeedProvider.select((state) => state.filter), (_, search) {
      if (search != _searchController.text) _searchController.text = search;
    });

    return AppScaffold(
      title: 'Taksiciler',
      onRefresh: controller.refresh,
      body: Column(
        children: [
          AppSpacing.gapMd,
          Padding(
            padding: AppSpacing.screenPadding,
            child: AppTextField(
              controller: _searchController,
              hint: 'İsim veya plaka ara',
              prefixIcon: Icons.search_rounded,
              textInputAction: TextInputAction.search,
              onChanged: controller.search,
            ),
          ),
          AppSpacing.gapSm,
          Expanded(child: _Body(scrollController: _scrollController)),
        ],
      ),
    );
  }
}

/// "Son aradıklarınız" — cihazda saklanan üç kayıt (bkz. `RecentTaxiCallsStore`).
/// Arama yapılırken gizlenir: kullanıcı o an belirli birini arıyordur.
class _RecentCalls extends ConsumerWidget {
  const _RecentCalls();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final recents = ref.watch(recentTaxiCallsProvider);
    final searching = ref.watch(
      taxisFeedProvider.select((state) => state.filter.isNotEmpty),
    );
    if (recents.isEmpty || searching) return const SizedBox.shrink();

    final theme = Theme.of(context);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Expanded(
              child: Text(
                'Son aradıklarınız',
                style: theme.textTheme.titleSmall,
              ),
            ),
            TextButton(
              onPressed: () =>
                  ref.read(recentTaxiCallsProvider.notifier).clear(),
              child: const Text('Temizle'),
            ),
          ],
        ),
        AppSpacing.gapSm,
        Wrap(
          spacing: AppSpacing.sm,
          runSpacing: AppSpacing.sm,
          children: [
            for (final recent in recents)
              ActionChip(
                avatar: const Icon(Icons.history_rounded, size: 16),
                label: Text(
                  recent.plaka == null
                      ? recent.name
                      : '${recent.name} · ${recent.plaka}',
                ),
                onPressed: () =>
                    context.push(AppRoutes.taxiDriverDetail(recent.id)),
              ),
          ],
        ),
        AppSpacing.gapLg,
      ],
    );
  }
}

class _Body extends ConsumerWidget {
  const _Body({required this.scrollController});

  final ScrollController scrollController;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(taxisFeedProvider);
    final controller = ref.read(taxisFeedProvider.notifier);

    if (state.isLoadingFirstPage) {
      return const LoadingView(itemCount: 4, hasImage: false);
    }

    if (state.error != null && state.items.isEmpty) {
      return ErrorView(
        message: state.error!.message,
        traceId: state.error!.traceId,
        onRetry: controller.retry,
      );
    }

    if (state.isEmpty) {
      final hasSearch = state.filter.isNotEmpty;
      return EmptyView(
        icon: Icons.local_taxi_rounded,
        title: hasSearch ? 'Sonuç bulunamadı' : 'Kayıtlı taksici yok',
        message: hasSearch
            ? '"${state.filter}" için bir sürücü bulunamadı.'
            : 'Doğrulanmış taksi sürücüleri eklendiğinde burada görünecek.',
        actionLabel: hasSearch ? 'Aramayı temizle' : null,
        onAction: hasSearch ? controller.clearFilters : null,
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
      // +1 baştaki "son aradıklarınız", +1 sondaki yükleme/özet satırı.
      itemCount: state.items.length + 2,
      separatorBuilder: (_, _) => AppSpacing.gapMd,
      itemBuilder: (context, index) {
        if (index == 0) return const _RecentCalls();

        if (index == state.items.length + 1) {
          return PagedListFooter(
            state: state,
            onLoadMore: controller.loadMore,
            itemNoun: 'taksici',
          );
        }

        final driver = state.items[index - 1];
        return TaxiDriverTile(
          driver: driver,
          onTap: () => context.push(AppRoutes.taxiDriverDetail(driver.id)),
        );
      },
    );
  }
}
