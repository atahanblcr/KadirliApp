import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/network/network.dart';
import '../../../core/paging/paged_list_footer.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../application/favorite_ads_controller.dart';
import '../application/favorites_feed_controller.dart';
import '../data/models/favorite_ad.dart';

/// "Favorilerim" — `GET /v1/users/me/favorites`, favoriye eklenme sırasına
/// göre (yeni → eski).
///
/// ⚠️ **Yayından düşen ilan listeden silinmez, soluk gösterilir**
/// (`isAvailable=false`): kullanıcı "favorim nereye kayboldu?" dememeli;
/// ilanın süresi dolmuş ya da yeniden onaya girmiş olabilir, sonra geri
/// gelebilir. Gerçekten silinen ilan ise sunucu tarafında zaten listeden
/// düşüyor (soft-delete inner join).
class FavoritesScreen extends ConsumerStatefulWidget {
  const FavoritesScreen({super.key});

  @override
  ConsumerState<FavoritesScreen> createState() => _FavoritesScreenState();
}

class _FavoritesScreenState extends ConsumerState<FavoritesScreen> {
  final _scrollController = ScrollController();

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
      ref.read(favoritesFeedProvider.notifier).loadMore();
    }
  }

  /// Favoriden çıkarma: satır **anında** kaybolur, hata olursa geri gelir ve
  /// sebep yazılır (kalp ikonunun 11.8'deki iyimser deseninin liste sürümü).
  Future<void> _remove(FavoriteAd favorite, int index) async {
    final messenger = ScaffoldMessenger.of(context);
    final feed = ref.read(favoritesFeedProvider.notifier);

    feed.removeLocally(favorite.adId);
    try {
      // ⚠️ `toggle` DEĞİL: bu ekranda yön belli ("çıkar"). `toggle` favori
      // kimlik kümesine bakıyor ve küme henüz yüklenmemiş olabilir.
      await ref
          .read(favoriteAdsProvider.notifier)
          .setFavorite(favorite.adId, value: false);
      messenger
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            content: const Text('Favorilerden çıkarıldı'),
            action: SnackBarAction(
              label: 'Geri al',
              onPressed: () async {
                feed.restoreLocally(favorite, index);
                try {
                  await ref
                      .read(favoriteAdsProvider.notifier)
                      .setFavorite(favorite.adId, value: true);
                } on ApiException {
                  feed.removeLocally(favorite.adId);
                }
              },
            ),
          ),
        );
    } on ApiException catch (error) {
      feed.restoreLocally(favorite, index);
      messenger
        ..hideCurrentSnackBar()
        ..showSnackBar(SnackBar(content: Text(error.message)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(favoritesFeedProvider);
    final controller = ref.read(favoritesFeedProvider.notifier);

    return AppScaffold(
      title: 'Favorilerim',
      onRefresh: () async {
        ref.read(favoriteAdsProvider.notifier).load();
        await controller.refresh();
      },
      body: switch (state) {
        _ when state.isLoadingFirstPage => const LoadingView(itemCount: 5),
        _ when state.error != null && state.items.isEmpty => ErrorView(
          message: state.error!.message,
          traceId: state.error!.traceId,
          onRetry: controller.retry,
        ),
        _ when state.isEmpty => EmptyView(
          icon: Icons.favorite_border_rounded,
          title: 'Favoriniz yok',
          message:
              'Beğendiğiniz ilanların kalbine dokunun; buradan tek yerden '
              'takip edin.',
          actionLabel: 'İlanlara göz at',
          onAction: () => context.go(AppRoutes.ads),
        ),
        _ => ListView.separated(
          controller: _scrollController,
          padding: const EdgeInsets.fromLTRB(
            AppSpacing.lg,
            AppSpacing.md,
            AppSpacing.lg,
            AppSpacing.xxl,
          ),
          itemCount: state.items.length + 1,
          separatorBuilder: (_, _) => AppSpacing.gapSm,
          itemBuilder: (context, index) {
            if (index == state.items.length) {
              return PagedListFooter(
                state: state,
                onLoadMore: controller.loadMore,
                itemNoun: 'favori',
              );
            }

            final favorite = state.items[index];
            return _FavoriteTile(
              favorite: favorite,
              onTap: favorite.isAvailable
                  ? () => context.push(AppRoutes.adDetail(favorite.adId))
                  : null,
              onRemove: () => _remove(favorite, index),
            );
          },
        ),
      },
    );
  }
}

class _FavoriteTile extends StatelessWidget {
  const _FavoriteTile({
    required this.favorite,
    required this.onTap,
    required this.onRemove,
  });

  final FavoriteAd favorite;
  final VoidCallback? onTap;
  final VoidCallback onRemove;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final available = favorite.isAvailable;

    return Opacity(
      // Yayında olmayan favori soluk — ama okunabilirlik sınırının üstünde.
      opacity: available ? 1 : 0.6,
      child: AppCard(
        padding: EdgeInsets.zero,
        child: InkWell(
          onTap: onTap,
          borderRadius: AppRadius.rMd,
          child: Padding(
            padding: const EdgeInsets.all(AppSpacing.md),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                AppNetworkImage(
                  url: favorite.coverImageUrl,
                  width: 76,
                  height: 76,
                  borderRadius: AppRadius.rSm,
                ),
                AppSpacing.wGapMd,
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        favorite.title,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: theme.textTheme.titleSmall,
                      ),
                      AppSpacing.gapXs,
                      Text(
                        AppMoney.price(favorite.price),
                        style: theme.textTheme.bodyLarge?.copyWith(
                          color: favorite.price == null
                              ? palette.muted
                              : theme.colorScheme.primary,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      AppSpacing.gapXs,
                      if (!available)
                        // Metinli rozet: renk/solukluk tek başına yetmez.
                        Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: AppSpacing.sm,
                            vertical: AppSpacing.xxs,
                          ),
                          decoration: BoxDecoration(
                            color: palette.warning.withValues(alpha: 0.14),
                            borderRadius: AppRadius.rPill,
                          ),
                          child: Text(
                            'Şu an yayında değil',
                            style: theme.textTheme.labelSmall?.copyWith(
                              color: palette.warning,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        )
                      else
                        Wrap(
                          spacing: AppSpacing.lg,
                          runSpacing: AppSpacing.xs,
                          children: [
                            _Meta(
                              icon: Icons.visibility_outlined,
                              label: '${favorite.viewCount} görüntülenme',
                            ),
                            _Meta(
                              icon: Icons.favorite_rounded,
                              label: AppDate.relative(favorite.favoritedAt),
                            ),
                          ],
                        ),
                    ],
                  ),
                ),
                IconButton(
                  tooltip: 'Favorilerden çıkar',
                  onPressed: onRemove,
                  icon: Icon(Icons.favorite_rounded, color: palette.danger),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _Meta extends StatelessWidget {
  const _Meta({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    // ⚠️ Kartın metin sütunu dar (görsel + kalp butonu payı düşülünce ~168 px)
    // ve `Wrap` çocuğuna sınırlı genişlik veriyor → metin **kısalabilmeli**.
    // Aynı sınıf hata 11.7 `PharmacyTile` ve 11.8 `AdCard`'da da çıkmıştı.
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 14, color: theme.palette.muted),
        AppSpacing.wGapXs,
        Flexible(
          child: Text(
            label,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: theme.textTheme.labelMedium?.copyWith(
              color: theme.palette.muted,
            ),
          ),
        ),
      ],
    );
  }
}
