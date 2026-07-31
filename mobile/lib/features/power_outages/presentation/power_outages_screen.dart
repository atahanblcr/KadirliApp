import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/network/network.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/widgets.dart';
import '../../auth/application/auth_controller.dart';
import '../application/power_outages_providers.dart';
import '../data/models/power_outage.dart';
import 'widgets/power_outage_tile.dart';

/// Elektrik kesintileri (11.6).
///
/// Uç **tüm** kayıtları tek seferde döndürüyor (sayfalama/tarih filtresi yok),
/// bu yüzden gruplama, filtreleme ve geri sayım tamamen istemcide.
class PowerOutagesScreen extends ConsumerStatefulWidget {
  const PowerOutagesScreen({super.key});

  @override
  ConsumerState<PowerOutagesScreen> createState() => _PowerOutagesScreenState();
}

class _PowerOutagesScreenState extends ConsumerState<PowerOutagesScreen> {
  Timer? _ticker;

  /// Geri sayımların ("bitmesine 45 dakika") dakikada bir tazelenmesi.
  /// Veri değil yalnız **zaman** değiştiği için yeni istek atılmaz.
  DateTime _now = DateTime.now();

  @override
  void initState() {
    super.initState();
    _ticker = Timer.periodic(const Duration(seconds: 30), (_) {
      if (mounted) setState(() => _now = DateTime.now());
    });
  }

  @override
  void dispose() {
    _ticker?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final outages = ref.watch(allPowerOutagesProvider);
    final tab = ref.watch(powerOutageTabProvider);
    final onlyMine = ref.watch(onlyMyNeighborhoodProvider);
    final myNeighborhood = ref.watch(
      currentUserProvider.select((user) => user?.primaryNeighborhoodName),
    );

    final groups = switch (outages) {
      AsyncData(value: final items) => PowerOutageGroups.from(
        items,
        now: _now,
        neighborhood: onlyMine ? myNeighborhood : null,
      ),
      _ => null,
    };

    return AppScaffold(
      title: 'Elektrik Kesintileri',
      onRefresh: () async {
        ref.invalidate(allPowerOutagesProvider);
        await ref.read(allPowerOutagesProvider.future).catchError(
          (Object _) => const <PowerOutage>[],
        );
      },
      body: switch (outages) {
        AsyncError(:final error) => _ErrorBody(
          error: error,
          onRetry: () => ref.invalidate(allPowerOutagesProvider),
        ),
        AsyncData() => Column(
          children: [
            AppSpacing.gapMd,
            _TabSelector(groups: groups!),
            if (myNeighborhood != null) ...[
              AppSpacing.gapSm,
              _OnlyMineToggle(neighborhood: myNeighborhood, value: onlyMine),
            ],
            AppSpacing.gapSm,
            Expanded(
              child: tab == PowerOutageTab.current
                  ? _CurrentList(groups: groups, now: _now, mine: onlyMine ? null : myNeighborhood)
                  : _PastList(groups: groups, now: _now),
            ),
          ],
        ),
        _ => const LoadingView(itemCount: 4, hasImage: false),
      },
    );
  }
}

class _ErrorBody extends StatelessWidget {
  const _ErrorBody({required this.error, required this.onRetry});

  final Object error;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final api = error is ApiException ? error as ApiException : null;
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      children: [
        SizedBox(height: MediaQuery.sizeOf(context).height * 0.15),
        ErrorView(
          message: api?.message ?? 'Kesinti bilgisi alınamadı.',
          traceId: api?.traceId,
          onRetry: onRetry,
        ),
      ],
    );
  }
}

/// Güncel / Geçmiş seçici — sayılar rozet olarak yazılır.
class _TabSelector extends ConsumerWidget {
  const _TabSelector({required this.groups});

  final PowerOutageGroups groups;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tab = ref.watch(powerOutageTabProvider);

    return Padding(
      padding: AppSpacing.screenPadding,
      child: SegmentedButton<PowerOutageTab>(
        segments: [
          ButtonSegment(
            value: PowerOutageTab.current,
            label: Text('Güncel (${groups.currentCount})'),
            icon: const Icon(Icons.bolt_rounded, size: 18),
          ),
          ButtonSegment(
            value: PowerOutageTab.past,
            label: Text('Geçmiş (${groups.pastCount})'),
            icon: const Icon(Icons.history_rounded, size: 18),
          ),
        ],
        selected: {tab},
        showSelectedIcon: false,
        onSelectionChanged: (selection) =>
            ref.read(powerOutageTabProvider.notifier).select(selection.first),
      ),
    );
  }
}

/// "Sadece mahallem" anahtarı — yalnız mahallesi bilinen kullanıcıya çizilir.
class _OnlyMineToggle extends ConsumerWidget {
  const _OnlyMineToggle({required this.neighborhood, required this.value});

  final String neighborhood;
  final bool value;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    return Padding(
      padding: AppSpacing.screenPadding,
      child: SwitchListTile.adaptive(
        contentPadding: EdgeInsets.zero,
        dense: true,
        value: value,
        onChanged: (_) =>
            ref.read(onlyMyNeighborhoodProvider.notifier).toggle(),
        title: Text('Sadece $neighborhood', style: theme.textTheme.bodyMedium),
        subtitle: Text(
          'Şehir geneli kesintiler her zaman görünür',
          style: theme.textTheme.labelSmall?.copyWith(color: theme.palette.muted),
        ),
      ),
    );
  }
}

/// Süren + planlanan kesintiler, iki başlık altında.
class _CurrentList extends StatelessWidget {
  const _CurrentList({required this.groups, required this.now, this.mine});

  final PowerOutageGroups groups;
  final DateTime now;

  /// Filtre kapalıyken kullanıcının mahallesi — eşleşen kart "Mahalleniz"
  /// rozetiyle öne çıkar (filtreyi açmadan da gözden kaçmaz).
  final String? mine;

  @override
  Widget build(BuildContext context) {
    if (!groups.hasCurrent) {
      return _EmptyScrollable(
        child: EmptyView(
          icon: Icons.check_circle_outline_rounded,
          title: 'Planlı kesinti yok',
          message: groups.hiddenByNeighborhood > 0
              ? 'Mahallenizde planlı kesinti görünmüyor. '
                    '${groups.hiddenByNeighborhood} kesinti filtre dışında.'
              : 'Şu an süren ya da planlanan elektrik kesintisi bulunmuyor.',
        ),
      );
    }

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.sm,
        AppSpacing.lg,
        AppSpacing.xxl,
      ),
      children: [
        if (groups.active.isNotEmpty) ...[
          const SectionHeader(title: 'Şu an sürüyor'),
          for (final outage in groups.active) ...[
            _tile(context, outage),
            AppSpacing.gapSm,
          ],
          AppSpacing.gapMd,
        ],
        if (groups.upcoming.isNotEmpty) ...[
          const SectionHeader(title: 'Planlanan'),
          for (final outage in groups.upcoming) ...[
            _tile(context, outage),
            AppSpacing.gapSm,
          ],
        ],
        if (groups.hiddenByNeighborhood > 0)
          Padding(
            padding: const EdgeInsets.only(top: AppSpacing.md),
            child: Text(
              '${groups.hiddenByNeighborhood} kesinti mahalle filtresi yüzünden gizli.',
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.labelSmall?.copyWith(
                color: Theme.of(context).palette.muted,
              ),
            ),
          ),
      ],
    );
  }

  Widget _tile(BuildContext context, PowerOutage outage) => PowerOutageTile(
    outage: outage,
    now: now,
    highlightNeighborhood: outage.matchesNeighborhood(mine),
    onTap: () => context.push(AppRoutes.powerOutageDetail(outage.id)),
  );
}

class _PastList extends StatelessWidget {
  const _PastList({required this.groups, required this.now});

  final PowerOutageGroups groups;
  final DateTime now;

  @override
  Widget build(BuildContext context) {
    if (groups.past.isEmpty) {
      return const _EmptyScrollable(
        child: EmptyView(
          icon: Icons.history_rounded,
          title: 'Geçmiş kayıt yok',
          message: 'Tamamlanmış bir kesinti kaydı bulunmuyor.',
        ),
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.sm,
        AppSpacing.lg,
        AppSpacing.xxl,
      ),
      itemCount: groups.past.length,
      separatorBuilder: (_, _) => AppSpacing.gapSm,
      itemBuilder: (context, index) {
        final outage = groups.past[index];
        return PowerOutageTile(
          outage: outage,
          now: now,
          onTap: () => context.push(AppRoutes.powerOutageDetail(outage.id)),
        );
      },
    );
  }
}

/// Boş durumu kaydırılabilir tutar → pull-to-refresh çalışmaya devam eder.
class _EmptyScrollable extends StatelessWidget {
  const _EmptyScrollable({required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) => LayoutBuilder(
    builder: (context, constraints) => SingleChildScrollView(
      physics: const AlwaysScrollableScrollPhysics(),
      child: ConstrainedBox(
        constraints: BoxConstraints(minHeight: constraints.maxHeight),
        child: child,
      ),
    ),
  );
}
