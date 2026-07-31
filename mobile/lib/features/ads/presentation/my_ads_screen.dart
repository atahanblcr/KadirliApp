import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/network/network.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/widgets.dart';
import '../application/my_ads_controller.dart';
import '../data/ads_repository.dart';
import '../data/models/my_ad.dart';
import 'widgets/my_ad_card.dart';

/// "İlanlarım" — kullanıcının her statüdeki ilanı (`GET /v1/users/me/ads`).
///
/// Profil sekmesinin **alt rotası** (`/profil/ilanlarim`): alt sekme çubuğu
/// yerinde kalır, geri tuşu profile döner.
class MyAdsScreen extends ConsumerStatefulWidget {
  const MyAdsScreen({super.key});

  @override
  ConsumerState<MyAdsScreen> createState() => _MyAdsScreenState();
}

class _MyAdsScreenState extends ConsumerState<MyAdsScreen> {
  final _scrollController = ScrollController();

  /// Üzerinde istek süren ilan (uzat/sil) — kartın butonları kilitlenir.
  String? _busyId;

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
      ref.read(myAdsProvider.notifier).loadMore();
    }
  }

  void _snack(String message) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }

  // --- Aksiyonlar ---

  Future<void> _delete(MyAd ad) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('İlan silinsin mi?'),
        content: Text(
          '“${ad.title}” ilanı yayından kaldırılacak. Bu işlem geri alınamaz.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Vazgeç'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(
              backgroundColor: Theme.of(context).palette.danger,
            ),
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Sil'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    setState(() => _busyId = ad.id);
    try {
      await ref.read(adsRepositoryProvider).deleteAd(ad.id);
      ref.read(myAdsProvider.notifier).removeLocally(ad.id);
      if (mounted) _snack('İlan silindi.');
    } on ApiException catch (error) {
      if (mounted) _snack(error.message);
    } finally {
      if (mounted) setState(() => _busyId = null);
    }
  }

  Future<void> _extend(MyAd ad) async {
    setState(() => _busyId = ad.id);
    try {
      final result = await ref.read(adsRepositoryProvider).extend(ad.id);
      ref.read(myAdsProvider.notifier).applyExtension(result);
      if (mounted) {
        _snack(
          'İlan 30 gün uzatıldı. Kalan uzatma hakkı: '
          '${result.remainingExtensions}',
        );
      }
    } on ApiException catch (error) {
      // 409 = uzatma hakkı doldu; sunucunun Türkçe mesajı zaten açıklayıcı.
      if (mounted) _snack(error.message);
    } finally {
      if (mounted) setState(() => _busyId = null);
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(myAdsProvider);
    final controller = ref.read(myAdsProvider.notifier);

    return AppScaffold(
      title: 'İlanlarım',
      onRefresh: controller.refresh,
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push(AppRoutes.adCreate),
        icon: const Icon(Icons.add_rounded),
        label: const Text('İlan ver'),
      ),
      body: Column(
        children: [
          const _StatusFilterStrip(),
          AppSpacing.gapSm,
          Expanded(child: _body(context, state, controller)),
        ],
      ),
    );
  }

  Widget _body(
    BuildContext context,
    MyAdsState state,
    MyAdsController controller,
  ) {
    final theme = Theme.of(context);

    if (state.isLoadingFirstPage) return const LoadingView(itemCount: 4);

    if (state.error != null && state.items.isEmpty) {
      return ErrorView(
        message: state.error!.message,
        traceId: state.error!.traceId,
        onRetry: controller.retry,
      );
    }

    if (state.isEmpty) {
      final filtered = state.filter != null;
      return EmptyView(
        icon: Icons.sell_outlined,
        title: filtered
            ? '${state.filter!.label} ilanınız yok'
            : 'Henüz ilanınız yok',
        message: filtered
            ? 'Bu durumda ilanınız bulunmuyor. Filtreyi kaldırıp tümünü '
                  'görebilirsiniz.'
            : 'Satmak istediğiniz bir şey mi var? İlk ilanınızı birkaç '
                  'dakikada verebilirsiniz.',
        actionLabel: filtered ? 'Filtreyi kaldır' : 'İlan ver',
        onAction: filtered
            ? () => controller.selectStatus(null)
            : () => context.push(AppRoutes.adCreate),
      );
    }

    return ListView.separated(
      controller: _scrollController,
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.sm,
        AppSpacing.lg,
        // FAB'ın altında kalan son kart için ek boşluk.
        AppSpacing.huge + AppSpacing.xl,
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
              'Toplam ${state.totalCount} ilan',
              textAlign: TextAlign.center,
              style: theme.textTheme.labelSmall?.copyWith(
                color: theme.palette.muted,
              ),
            ),
          );
        }

        final ad = state.items[index];
        return MyAdCard(
          ad: ad,
          busy: _busyId == ad.id,
          // Yayında olmayan ilanın public detayı 404 verir; sahibi için
          // açıktır (backend `RequesterId` kontrolü) → her statüde açılabilir.
          onTap: () => context.push(AppRoutes.adDetail(ad.id)),
          onEdit: () => context.push(AppRoutes.adEdit(ad.id)),
          onDelete: () => _delete(ad),
          onExtend: () => _extend(ad),
        );
      },
    );
  }
}

/// Statü filtresi şeridi — hangi ilanın nerede olduğunu tek dokunuşla görmek.
class _StatusFilterStrip extends ConsumerWidget {
  const _StatusFilterStrip();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final selected = ref.watch(myAdsProvider.select((state) => state.filter));
    final controller = ref.read(myAdsProvider.notifier);

    return SizedBox(
      height: 44,
      // ⚠️ Yatay `ListView` tembel → ekran dışı chip hiç kurulmuyor (11.8).
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
        child: Row(
          children: [
            _StatusChipButton(
              label: 'Tümü',
              selected: selected == null,
              onTap: () => controller.selectStatus(null),
            ),
            for (final status in AdStatus.filterable) ...[
              AppSpacing.wGapSm,
              _StatusChipButton(
                label: status.label,
                selected: selected == status,
                onTap: () => controller.selectStatus(status),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _StatusChipButton extends StatelessWidget {
  const _StatusChipButton({
    required this.label,
    required this.selected,
    required this.onTap,
  });

  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final accent = theme.colorScheme.primary;

    return Semantics(
      button: true,
      selected: selected,
      label: label,
      child: Material(
        color: selected ? accent : theme.colorScheme.surface,
        shape: RoundedRectangleBorder(
          borderRadius: AppRadius.rPill,
          side: BorderSide(color: selected ? accent : palette.border),
        ),
        child: InkWell(
          onTap: onTap,
          borderRadius: AppRadius.rPill,
          child: Padding(
            padding: const EdgeInsets.symmetric(
              horizontal: AppSpacing.lg,
              vertical: AppSpacing.sm,
            ),
            child: Text(
              label,
              style: theme.textTheme.labelLarge?.copyWith(
                color: selected
                    ? theme.colorScheme.onPrimary
                    : theme.colorScheme.onSurface,
                fontWeight: selected ? FontWeight.w700 : FontWeight.w600,
              ),
            ),
          ),
        ),
      ),
    );
  }
}
