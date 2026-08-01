import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/widgets.dart';
import '../../auth/presentation/widgets/login_required_sheet.dart';
import '../application/deaths_providers.dart';
import 'widgets/death_notice_tile.dart';

/// Vefat ilanları (11.11).
///
/// **Ton kararı:** bu ekran uygulamanın en sade ekranı. Filtre şeridi, renkli
/// rozet, "popüler/yeni" etiketi, görüntülenme sayacı yok; yalnız arama, liste
/// ve "Vefat bildir". Bugün cenazesi olanlar için ekranın üstünde tek satırlık
/// sakin bir hatırlatma çıkar.
class DeathsScreen extends ConsumerStatefulWidget {
  const DeathsScreen({super.key});

  @override
  ConsumerState<DeathsScreen> createState() => _DeathsScreenState();
}

class _DeathsScreenState extends ConsumerState<DeathsScreen> {
  final _scrollController = ScrollController();
  final _searchController = TextEditingController();

  static const _loadMoreThreshold = 400.0;

  @override
  void initState() {
    super.initState();
    _searchController.text = ref.read(deathsFeedProvider).filter;
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
      ref.read(deathsFeedProvider.notifier).loadMore();
    }
  }

  Future<void> _report() async {
    // Korumalı aksiyon: anonim kullanıcı router'la Giriş'e ATILMAZ, davet görür
    // (11.10 "Kodu göster" kararının aynısı).
    if (!await ensureSignedIn(
      context,
      ref,
      reason:
          'Vefat bildirimi gönderebilmek için giriş yapmanız gerekiyor. '
          'Bildiriminiz yayına alınmadan önce görevlilerce kontrol edilir.',
    )) {
      return;
    }
    if (!mounted) return;
    context.push(AppRoutes.deathReport);
  }

  @override
  Widget build(BuildContext context) {
    final controller = ref.read(deathsFeedProvider.notifier);

    // Filtre dışarıdan sıfırlanırsa arama kutusu da temizlenmeli (11.7/11.8).
    ref.listen(deathsFeedProvider.select((state) => state.filter), (_, search) {
      if (search != _searchController.text) _searchController.text = search;
    });

    return AppScaffold(
      title: 'Vefat İlanları',
      onRefresh: controller.refresh,
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _report,
        icon: const Icon(Icons.add_rounded),
        label: const Text('Vefat bildir'),
      ),
      body: Column(
        children: [
          AppSpacing.gapMd,
          Padding(
            padding: AppSpacing.screenPadding,
            child: AppTextField(
              controller: _searchController,
              hint: 'İsimle ara',
              prefixIcon: Icons.search_rounded,
              textInputAction: TextInputAction.search,
              onChanged: controller.search,
            ),
          ),
          const _TodayNotice(),
          AppSpacing.gapSm,
          Expanded(child: _Body(scrollController: _scrollController)),
        ],
      ),
    );
  }
}

/// "Bugün 2 cenaze namazı var" — ek istek atmadan yüklenmiş listeden türer.
class _TodayNotice extends ConsumerWidget {
  const _TodayNotice();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final today = ref.watch(todaysFuneralsProvider);
    if (today.isEmpty) return const SizedBox.shrink();

    final theme = Theme.of(context);
    final label = today.length == 1
        ? 'Bugün cenaze namazı: ${today.first.deceasedName}, ${today.first.timeLabel}'
        : 'Bugün ${today.length} cenaze namazı var.';

    return Padding(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.md,
        AppSpacing.lg,
        0,
      ),
      child: Row(
        children: [
          Icon(
            Icons.info_outline_rounded,
            size: 16,
            color: theme.colorScheme.primary,
          ),
          AppSpacing.wGapSm,
          Expanded(
            child: Text(
              label,
              style: theme.textTheme.bodySmall?.copyWith(
                color: theme.colorScheme.primary,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
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
    final state = ref.watch(deathsFeedProvider);
    final controller = ref.read(deathsFeedProvider.notifier);
    final theme = Theme.of(context);

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
        icon: Icons.filter_vintage_rounded,
        title: hasSearch ? 'Sonuç bulunamadı' : 'Güncel vefat ilanı yok',
        message: hasSearch
            ? '"${state.filter}" için bir ilan bulunamadı.'
            : 'Yayındaki vefat ilanları burada görünür. '
                  'İlanlar cenazeden bir hafta sonra arşivlenir.',
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
        // FAB listenin son kartını örtmesin.
        AppSpacing.huge + AppSpacing.xl,
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
              'Toplam ${state.totalCount} ilan',
              textAlign: TextAlign.center,
              style: theme.textTheme.labelSmall?.copyWith(
                color: theme.palette.muted,
              ),
            ),
          );
        }

        final notice = state.items[index];
        return DeathNoticeTile(
          notice: notice,
          onTap: () => context.push(AppRoutes.deathDetail(notice.id)),
        );
      },
    );
  }
}
