import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../application/transport_providers.dart';
import '../application/transport_vehicle.dart';
import 'widgets/intercity_route_card.dart';
import 'widgets/intracity_route_card.dart';

/// Ulaşım (11.12).
///
/// İki sekme: **Şehirlerarası** (otobüs kalkış saatleri) ve **Şehir içi**
/// (hat + duraklar). Varsayılan şehirlerarası — bu ekrana gelme sebebi
/// çoğunlukla "Adana/Osmaniye otobüsü kaçta?".
///
/// **KARAR — detay ekranı yok:** sunucuda `transport/.../{id}` ucu yok, saatler
/// ve duraklar zaten liste gövdesinde geliyor. Kart yerinde açılıyor; sahte bir
/// detay rotası (listeyi tarayıp id bulan) derin bağlantıda ve ikinci sayfada
/// kırılırdı.
class TransportScreen extends ConsumerStatefulWidget {
  const TransportScreen({super.key});

  @override
  ConsumerState<TransportScreen> createState() => _TransportScreenState();
}

class _TransportScreenState extends ConsumerState<TransportScreen> {
  /// "Sıradaki kalkış" geri sayımı dakika hassasiyetinde → 30 sn'de bir
  /// tazelenir (11.6 kesinti ekranının deseni).
  Timer? _ticker;

  @override
  void initState() {
    super.initState();
    _ticker = Timer.periodic(const Duration(seconds: 30), (_) {
      if (mounted) setState(() {});
    });
  }

  @override
  void dispose() {
    _ticker?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final tab = ref.watch(transportTabProvider);

    return AppScaffold(
      title: 'Ulaşım',
      onRefresh: () async {
        if (tab == TransportTab.intercity) {
          await ref.read(intercityFeedProvider.notifier).refresh();
        } else {
          await ref.read(intracityFeedProvider.notifier).refresh();
        }
      },
      body: Column(
        children: [
          AppSpacing.gapMd,
          Padding(
            padding: AppSpacing.screenPadding,
            child: SegmentedButton<TransportTab>(
              segments: const [
                ButtonSegment(
                  value: TransportTab.intercity,
                  label: Text('Şehirlerarası'),
                  icon: Icon(Icons.directions_bus_rounded, size: 18),
                ),
                ButtonSegment(
                  value: TransportTab.intracity,
                  label: Text('Şehir içi'),
                  icon: Icon(Icons.route_rounded, size: 18),
                ),
              ],
              selected: {tab},
              showSelectedIcon: false,
              onSelectionChanged: (selection) => ref
                  .read(transportTabProvider.notifier)
                  .select(selection.first),
            ),
          ),
          AppSpacing.gapSm,
          Expanded(
            child: tab == TransportTab.intercity
                ? const _IntercityTab()
                : const _IntracityTab(),
          ),
        ],
      ),
    );
  }
}

// ------------------------------------------------------------ Şehirlerarası

class _IntercityTab extends ConsumerStatefulWidget {
  const _IntercityTab();

  @override
  ConsumerState<_IntercityTab> createState() => _IntercityTabState();
}

class _IntercityTabState extends ConsumerState<_IntercityTab> {
  final _scrollController = ScrollController();
  final _searchController = TextEditingController();

  static const _loadMoreThreshold = 400.0;

  @override
  void initState() {
    super.initState();
    _searchController.text = ref.read(intercityFeedProvider).filter.search;
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
      ref.read(intercityFeedProvider.notifier).loadMore();
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(intercityFeedProvider);
    final controller = ref.read(intercityFeedProvider.notifier);
    final expandedId = ref.watch(expandedIntercityProvider);

    // "Filtreleri temizle" arama kutusunu da temizlemeli (11.7/11.8 hatası).
    ref.listen(intercityFeedProvider.select((state) => state.filter.search), (
      _,
      search,
    ) {
      if (search != _searchController.text) _searchController.text = search;
    });

    return Column(
      children: [
        Padding(
          padding: AppSpacing.screenPadding,
          child: AppTextField(
            controller: _searchController,
            hint: 'Şehir veya firma ara',
            prefixIcon: Icons.search_rounded,
            textInputAction: TextInputAction.search,
            onChanged: controller.search,
          ),
        ),
        AppSpacing.gapSm,
        // ⚠️ Yatay `ListView` **tembeldir** — ekran dışı chip hiç kurulmaz ve
        // testte bulunamaz → şeritlerde `SingleChildScrollView + Row` (§8).
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          padding: AppSpacing.screenPadding,
          child: Row(
            children: [
              for (final vehicle in TransportVehicle.filters) ...[
                FilterChoiceChip(
                  label: vehicle.label,
                  icon: vehicle.icon,
                  selected: state.filter.vehicle == vehicle,
                  onTap: () => controller.selectVehicle(vehicle),
                ),
                AppSpacing.wGapSm,
              ],
            ],
          ),
        ),
        AppSpacing.gapSm,
        Expanded(
          child: Builder(
            builder: (context) {
              if (state.isLoadingFirstPage) {
                return const LoadingView(itemCount: 3, hasImage: false);
              }
              if (state.error != null && state.items.isEmpty) {
                return ErrorView(
                  message: state.error!.message,
                  traceId: state.error!.traceId,
                  onRetry: controller.retry,
                );
              }
              if (state.isEmpty) {
                final hasSearch = state.filter.search.isNotEmpty;
                final hasVehicle =
                    state.filter.vehicle != TransportVehicle.all;
                final hasFilter = hasSearch || hasVehicle;
                return EmptyView(
                  icon: Icons.directions_bus_rounded,
                  title: hasFilter
                      ? 'Sonuç bulunamadı'
                      : 'Henüz hat eklenmemiş',
                  message: hasVehicle && !hasSearch
                      // Süzgeç yüzünden boşalan liste sebebini söylemeli:
                      // "hiç hat yok" ile "bu araç tipinde hat yok" farklı.
                      ? '${state.filter.vehicle.label} tipinde hat bulunmuyor. '
                            '"Tümü" ile bütün hatları görebilirsiniz.'
                      : hasSearch
                      ? 'Arama gideceğiniz şehirde ve firma adında yapılıyor.'
                      : 'Şehirlerarası otobüs hatları eklendiğinde burada '
                            'kalkış saatleriyle görünecek.',
                  // Yalnız arama varsa "Aramayı temizle" daha doğru: araç
                  // şeridine dokunulmamışken "filtreler" çoğulu yalan söyler.
                  actionLabel: !hasFilter
                      ? null
                      : (hasVehicle ? 'Filtreleri temizle' : 'Aramayı temizle'),
                  onAction: !hasFilter
                      ? null
                      : (hasVehicle
                            ? controller.clearFilters
                            : controller.clearSearch),
                );
              }

              return ListView.separated(
                controller: _scrollController,
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
                    return _FeedFooter(
                      isLoadingMore: state.isLoadingMore,
                      hasLoadMoreError: state.loadMoreError != null,
                      hasMore: state.hasMore,
                      onLoadMore: controller.loadMore,
                      totalLabel: '${state.totalCount} hat',
                    );
                  }

                  final route = state.items[index];
                  return Builder(
                    builder: (cardContext) => IntercityRouteCard(
                      route: route,
                      expanded: expandedId == route.id,
                      onToggle: () => ref
                          .read(expandedIntercityProvider.notifier)
                          .toggle(route.id),
                      onShare: () => AppShare.text(
                        route.shareText,
                        subject: 'Kadirli → ${route.destination} otobüs saatleri',
                        origin: AppShare.originOf(cardContext),
                      ),
                    ),
                  );
                },
              );
            },
          ),
        ),
      ],
    );
  }
}

// --------------------------------------------------------------- Şehir içi

class _IntracityTab extends ConsumerStatefulWidget {
  const _IntracityTab();

  @override
  ConsumerState<_IntracityTab> createState() => _IntracityTabState();
}

class _IntracityTabState extends ConsumerState<_IntracityTab> {
  final _scrollController = ScrollController();
  final _searchController = TextEditingController();

  static const _loadMoreThreshold = 400.0;

  @override
  void initState() {
    super.initState();
    _searchController.text = ref.read(intracityFeedProvider).filter;
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
      ref.read(intracityFeedProvider.notifier).loadMore();
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(intracityFeedProvider);
    final controller = ref.read(intracityFeedProvider.notifier);
    final expandedId = ref.watch(expandedIntracityProvider);

    ref.listen(intracityFeedProvider.select((state) => state.filter), (
      _,
      search,
    ) {
      if (search != _searchController.text) _searchController.text = search;
    });

    return Column(
      children: [
        Padding(
          padding: AppSpacing.screenPadding,
          child: AppTextField(
            controller: _searchController,
            hint: 'Hat adı veya numarası ara',
            prefixIcon: Icons.search_rounded,
            textInputAction: TextInputAction.search,
            onChanged: controller.search,
          ),
        ),
        AppSpacing.gapSm,
        Expanded(
          child: Builder(
            builder: (context) {
              if (state.isLoadingFirstPage) {
                return const LoadingView(itemCount: 3, hasImage: false);
              }
              if (state.error != null && state.items.isEmpty) {
                return ErrorView(
                  message: state.error!.message,
                  traceId: state.error!.traceId,
                  onRetry: controller.retry,
                );
              }
              if (state.isEmpty) {
                final hasFilter = state.filter.isNotEmpty;
                return EmptyView(
                  icon: Icons.route_rounded,
                  title: hasFilter
                      ? 'Sonuç bulunamadı'
                      : 'Henüz hat eklenmemiş',
                  message: hasFilter
                      ? 'Arama hat adında ve numarasında yapılıyor.'
                      : 'Şehir içi otobüs hatları eklendiğinde durakları ve '
                            'servis saatleriyle burada görünecek.',
                  actionLabel: hasFilter ? 'Aramayı temizle' : null,
                  onAction: hasFilter ? controller.clearFilters : null,
                );
              }

              return ListView.separated(
                controller: _scrollController,
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
                    return _FeedFooter(
                      isLoadingMore: state.isLoadingMore,
                      hasLoadMoreError: state.loadMoreError != null,
                      hasMore: state.hasMore,
                      onLoadMore: controller.loadMore,
                      totalLabel: '${state.totalCount} hat',
                    );
                  }

                  final route = state.items[index];
                  return Builder(
                    builder: (cardContext) => IntracityRouteCard(
                      route: route,
                      expanded: expandedId == route.id,
                      onToggle: () => ref
                          .read(expandedIntracityProvider.notifier)
                          .toggle(route.id),
                      onShare: () => AppShare.text(
                        route.shareText,
                        subject: '${route.routeNumber} numaralı hat',
                        origin: AppShare.originOf(cardContext),
                      ),
                    ),
                  );
                },
              );
            },
          ),
        ),
      ],
    );
  }
}

// ------------------------------------------------------------------ ortak

class _FeedFooter extends StatelessWidget {
  const _FeedFooter({
    required this.isLoadingMore,
    required this.hasLoadMoreError,
    required this.hasMore,
    required this.onLoadMore,
    required this.totalLabel,
  });

  final bool isLoadingMore;
  final bool hasLoadMoreError;
  final bool hasMore;
  final VoidCallback onLoadMore;
  final String totalLabel;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (isLoadingMore) return const LoadingView.compact();
    if (hasLoadMoreError) {
      return Padding(
        padding: const EdgeInsets.only(top: AppSpacing.md),
        child: AppButton.ghost(
          label: 'Devamını yükle',
          icon: Icons.refresh_rounded,
          size: AppButtonSize.small,
          expand: true,
          onPressed: onLoadMore,
        ),
      );
    }
    if (hasMore) return const SizedBox(height: AppSpacing.lg);

    return Padding(
      padding: const EdgeInsets.only(top: AppSpacing.lg),
      child: Text(
        'Toplam $totalLabel',
        textAlign: TextAlign.center,
        style: theme.textTheme.labelSmall?.copyWith(color: theme.palette.muted),
      ),
    );
  }
}

