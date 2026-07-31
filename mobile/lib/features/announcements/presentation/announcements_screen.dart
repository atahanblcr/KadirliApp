import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
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
      return _ScrollableState(
        controller: scrollController,
        child: ErrorView(
          message: error.message,
          traceId: error.traceId,
          onRetry: controller.retry,
        ),
      );
    }

    if (state.isEmpty) {
      return _ScrollableState(
        controller: scrollController,
        child: EmptyView(
          icon: Icons.campaign_outlined,
          title: state.isFiltered
              ? 'Bu türde duyuru yok'
              : 'Henüz duyuru yok',
          message: state.isFiltered
              ? 'Başka bir tür seçebilir ya da tüm duyurulara bakabilirsiniz.'
              : 'Yeni duyurular burada görünecek.',
          actionLabel: state.isFiltered ? 'Tüm duyurular' : null,
          onAction: state.isFiltered ? () => controller.selectType(null) : null,
        ),
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
          return _FeedFooter(scrollController: scrollController);
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

/// Boş/hata durumlarını da kaydırılabilir tutar — yoksa `RefreshIndicator`
/// çalışmaz ve kullanıcı "aşağı çekip yenile" refleksini kullanamaz.
class _ScrollableState extends StatelessWidget {
  const _ScrollableState({required this.controller, required this.child});

  final ScrollController controller;
  final Widget child;

  @override
  Widget build(BuildContext context) => LayoutBuilder(
    builder: (context, constraints) => SingleChildScrollView(
      controller: controller,
      physics: const AlwaysScrollableScrollPhysics(),
      child: ConstrainedBox(
        constraints: BoxConstraints(minHeight: constraints.maxHeight),
        child: child,
      ),
    ),
  );
}

/// Listenin altı: sonraki sayfa göstergesi / sayfa hatası / bitiş çizgisi.
class _FeedFooter extends ConsumerWidget {
  const _FeedFooter({required this.scrollController});

  final ScrollController scrollController;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(announcementFeedProvider);
    final controller = ref.read(announcementFeedProvider.notifier);
    final theme = Theme.of(context);

    if (state.isLoadingMore) {
      return const Padding(
        padding: EdgeInsets.only(top: AppSpacing.md),
        child: LoadingView.compact(),
      );
    }

    // Sonraki sayfa patladı: mevcut kayıtlar duruyor, yalnız devamı yeniden
    // denenir (tüm ekranı hataya düşürmek okunan içeriği silerdi).
    if (state.loadMoreError != null) {
      return Padding(
        padding: const EdgeInsets.only(top: AppSpacing.lg),
        child: AppCard(
          child: Column(
            children: [
              Text(
                state.loadMoreError!.message,
                style: theme.textTheme.bodyMedium,
                textAlign: TextAlign.center,
              ),
              AppSpacing.gapMd,
              AppButton.ghost(
                label: 'Devamını yükle',
                icon: Icons.refresh_rounded,
                size: AppButtonSize.small,
                expand: true,
                onPressed: controller.loadMore,
              ),
            ],
          ),
        ),
      );
    }

    if (state.hasMore) return const SizedBox(height: AppSpacing.lg);

    // Liste bitti — "daha fazlası var mı?" sorusunu kapatan sakin bir satır.
    return Padding(
      padding: const EdgeInsets.only(top: AppSpacing.lg),
      child: Text(
        state.totalCount > 0
            ? 'Toplam ${state.totalCount} duyuru'
            : 'Hepsi bu kadar',
        textAlign: TextAlign.center,
        style: theme.textTheme.labelSmall?.copyWith(color: theme.palette.muted),
      ),
    );
  }
}
