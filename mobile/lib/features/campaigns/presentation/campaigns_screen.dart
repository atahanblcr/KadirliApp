import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/widgets.dart';
import '../application/campaigns_providers.dart';
import 'widgets/campaign_card.dart';

/// Kampanyalar (11.10) — esnaf indirimleri.
///
/// Tek liste + arama: uç yalnız **yürürlükteki** kampanyaları döndürdüğü için
/// "aktif/geçmiş" ayrımına gerek yok; kategori filtresi de yok (bkz.
/// `campaigns_providers.dart`).
class CampaignsScreen extends ConsumerStatefulWidget {
  const CampaignsScreen({super.key});

  @override
  ConsumerState<CampaignsScreen> createState() => _CampaignsScreenState();
}

class _CampaignsScreenState extends ConsumerState<CampaignsScreen> {
  final _scrollController = ScrollController();
  final _searchController = TextEditingController();

  static const _loadMoreThreshold = 400.0;

  @override
  void initState() {
    super.initState();
    _searchController.text = ref.read(campaignsFeedProvider).filter;
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
      ref.read(campaignsFeedProvider.notifier).loadMore();
    }
  }

  @override
  Widget build(BuildContext context) {
    final controller = ref.read(campaignsFeedProvider.notifier);

    // Filtre dışarıdan sıfırlanırsa arama kutusu da temizlenmeli (11.7/11.8).
    ref.listen(campaignsFeedProvider.select((state) => state.filter), (
      _,
      search,
    ) {
      if (search != _searchController.text) _searchController.text = search;
    });

    return AppScaffold(
      title: 'Kampanyalar',
      onRefresh: controller.refresh,
      body: Column(
        children: [
          AppSpacing.gapMd,
          Padding(
            padding: AppSpacing.screenPadding,
            child: AppTextField(
              controller: _searchController,
              hint: 'Kampanya veya işletme ara',
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

class _Body extends ConsumerWidget {
  const _Body({required this.scrollController});

  final ScrollController scrollController;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(campaignsFeedProvider);
    final controller = ref.read(campaignsFeedProvider.notifier);
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
      final hasSearch = state.filter.isNotEmpty;
      return EmptyView(
        icon: Icons.local_offer_outlined,
        title: hasSearch ? 'Sonuç bulunamadı' : 'Şu an kampanya yok',
        message: hasSearch
            ? '"${state.filter}" için yürürlükte bir kampanya bulunamadı.'
            : 'Esnaf kampanyaları eklendiğinde burada görünecek.',
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
              'Toplam ${state.totalCount} kampanya',
              textAlign: TextAlign.center,
              style: theme.textTheme.labelSmall?.copyWith(
                color: theme.palette.muted,
              ),
            ),
          );
        }

        final campaign = state.items[index];
        return CampaignCard(
          campaign: campaign,
          onTap: () => context.push(AppRoutes.campaignDetail(campaign.id)),
        );
      },
    );
  }
}
