import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/paging/paged_list_footer.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/widgets.dart';
import '../application/announcements_providers.dart';
import 'widgets/announcement_tile.dart';
import 'widgets/announcement_type_filter.dart';

/// Duyurular listesi (11.6) — tür filtresi + sonsuz kaydırma.
class AnnouncementsScreen extends ConsumerStatefulWidget {
  const AnnouncementsScreen({super.key});

  @override
  ConsumerState<AnnouncementsScreen> createState() =>
      _AnnouncementsScreenState();
}

class _AnnouncementsScreenState extends ConsumerState<AnnouncementsScreen> {
  final _scrollController = ScrollController();

  /// Liste sonuna bu kadar kala sonraki sayfa istenir — kullanıcı boşluğu
  /// görmeden devam eder.
  static const _loadMoreThreshold = 400.0;

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_onScroll);
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (!_scrollController.hasClients) return;
    final position = _scrollController.position;
    if (position.pixels >= position.maxScrollExtent - _loadMoreThreshold) {
      ref.read(announcementFeedProvider.notifier).loadMore();
    }
  }

  @override
  Widget build(BuildContext context) {
    final controller = ref.read(announcementFeedProvider.notifier);

    return AppScaffold(
      title: 'Duyurular',
      onRefresh: controller.refresh,
      body: Column(
        children: [
          AppSpacing.gapSm,
          const AnnouncementTypeFilter(),
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
    final state = ref.watch(announcementFeedProvider);
    final controller = ref.read(announcementFeedProvider.notifier);

    if (state.isLoadingFirstPage) {
      return const LoadingView(itemCount: 5, hasImage: false);
    }

    if (state.error != null && state.items.isEmpty) {
      final error = state.error!;
      return ErrorView(
        message: error.message,
        traceId: error.traceId,
        onRetry: controller.retry,
      );
    }

    if (state.isEmpty) {
      return EmptyView(
        icon: Icons.campaign_outlined,
        title: state.isFiltered ? 'Bu türde duyuru yok' : 'Henüz duyuru yok',
        message: state.isFiltered
            ? 'Başka bir tür seçebilir ya da tüm duyurulara bakabilirsiniz.'
            : 'Yeni duyurular burada görünecek.',
        actionLabel: state.isFiltered ? 'Tüm duyurular' : null,
        onAction: state.isFiltered ? () => controller.selectType(null) : null,
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
      // +1 = altbilgi satırı (sayfa yükleniyor / hata / "hepsi bu kadar").
      itemCount: state.items.length + 1,
      separatorBuilder: (_, _) => AppSpacing.gapSm,
      itemBuilder: (context, index) {
        if (index == state.items.length) {
          return PagedListFooter(
            state: state,
            onLoadMore: controller.loadMore,
            itemNoun: 'duyuru',
          );
        }
        final announcement = state.items[index];
        return AnnouncementTile(
          announcement: announcement,
          onTap: () => context.push(
            AppRoutes.announcementDetail(announcement.id),
          ),
        );
      },
    );
  }
}
