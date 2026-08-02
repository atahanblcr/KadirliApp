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
import '../../home/application/home_providers.dart';
import '../application/pharmacies_providers.dart';
import '../data/models/duty_schedule.dart';
import '../data/models/on_duty_pharmacy.dart';
import 'widgets/duty_calendar.dart';
import 'widgets/pharmacy_tile.dart';

/// Nöbetçi Eczane (11.7).
///
/// İki sekme: **Nöbetçi** (bugünün nöbetçisi + aylık takvim) ve **Eczaneler**
/// (aramalı tam liste). Varsayılan sekme nöbet — kullanıcı bu ekrana genelde
/// "şu an açık eczane hangisi" diye geliyor.
class PharmaciesScreen extends ConsumerWidget {
  const PharmaciesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tab = ref.watch(pharmacyTabProvider);

    return AppScaffold(
      title: 'Nöbetçi Eczane',
      onRefresh: () async {
        if (tab == PharmacyTab.duty) {
          ref.invalidate(onDutyPharmaciesProvider);
          ref.invalidate(dutyScheduleProvider(ref.read(dutyMonthProvider)));
        } else {
          await ref.read(pharmacyFeedProvider.notifier).refresh();
        }
      },
      body: Column(
        children: [
          AppSpacing.gapMd,
          Padding(
            padding: AppSpacing.screenPadding,
            child: SegmentedButton<PharmacyTab>(
              segments: const [
                ButtonSegment(
                  value: PharmacyTab.duty,
                  label: Text('Nöbetçi'),
                  icon: Icon(Icons.nightlight_round, size: 18),
                ),
                ButtonSegment(
                  value: PharmacyTab.all,
                  label: Text('Eczaneler'),
                  icon: Icon(Icons.list_rounded, size: 18),
                ),
              ],
              selected: {tab},
              showSelectedIcon: false,
              onSelectionChanged: (selection) =>
                  ref.read(pharmacyTabProvider.notifier).select(selection.first),
            ),
          ),
          AppSpacing.gapSm,
          Expanded(
            child: tab == PharmacyTab.duty
                ? const _DutyTab()
                : const _AllPharmaciesTab(),
          ),
        ],
      ),
    );
  }
}

// ---------------------------------------------------------------- Nöbet sekmesi

class _DutyTab extends ConsumerWidget {
  const _DutyTab();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final month = ref.watch(dutyMonthProvider);
    final schedule = ref.watch(dutyScheduleProvider(month));
    final selectedDay = ref.watch(selectedDutyDayProvider);

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.sm,
        AppSpacing.lg,
        AppSpacing.xxl,
      ),
      children: [
        const _TodayOnDutyCard(),
        AppSpacing.gapXl,

        const SectionHeader(
          title: 'Nöbet takvimi',
          subtitle: 'Nöbetçi eczanesi olan günler işaretli',
        ),
        AppCard(
          child: Column(
            children: [
              MonthSwitcher(
                year: month.year,
                month: month.month,
                onPrevious: () {
                  ref.read(dutyMonthProvider.notifier).previous();
                  ref.read(selectedDutyDayProvider.notifier).clear();
                },
                onNext: () {
                  ref.read(dutyMonthProvider.notifier).next();
                  ref.read(selectedDutyDayProvider.notifier).clear();
                },
              ),
              AppSpacing.gapMd,
              switch (schedule) {
                AsyncData(value: final entries) => DutyCalendar(
                  year: month.year,
                  month: month.month,
                  schedule: entries,
                  selectedDayKey: selectedDay,
                  onDaySelected: (key) =>
                      ref.read(selectedDutyDayProvider.notifier).toggle(key),
                ),
                AsyncError(:final error) => _InlineError(
                  message: error is ApiException
                      ? error.message
                      : 'Nöbet takvimi alınamadı.',
                  onRetry: () => ref.invalidate(dutyScheduleProvider(month)),
                ),
                _ => const Padding(
                  padding: EdgeInsets.symmetric(vertical: AppSpacing.xl),
                  child: LoadingView.compact(),
                ),
              },
            ],
          ),
        ),

        if (schedule case AsyncData(value: final entries)) ...[
          AppSpacing.gapLg,
          _SelectedDayDetails(entries: entries, dayKey: selectedDay),
        ],
      ],
    );
  }
}

/// Bugünün nöbetçisi — ekranın en üstündeki "şu an lazım olan" kart.
class _TodayOnDutyCard extends ConsumerWidget {
  const _TodayOnDutyCard();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final state = ref.watch(onDutyPharmaciesProvider);

    return switch (state) {
      AsyncData(value: final pharmacies) when pharmacies.isEmpty => AppCard(
        color: theme.colorScheme.surface,
        child: Row(
          children: [
            Icon(Icons.info_outline_rounded, color: theme.palette.muted),
            AppSpacing.wGapMd,
            Expanded(
              child: Text(
                'Bugünün nöbetçi eczanesi henüz girilmedi. '
                'Takvimden diğer günlere bakabilirsiniz.',
                style: theme.textTheme.bodyMedium,
              ),
            ),
          ],
        ),
      ),
      AsyncData(value: final pharmacies) => Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          for (final pharmacy in pharmacies) ...[
            _OnDutyCard(pharmacy: pharmacy),
            if (pharmacy != pharmacies.last) AppSpacing.gapMd,
          ],
        ],
      ),
      AsyncError(:final error) => _InlineError(
        message: error is ApiException
            ? error.message
            : 'Nöbetçi eczane bilgisi alınamadı.',
        onRetry: () => ref.invalidate(onDutyPharmaciesProvider),
      ),
      _ => const SkeletonCardList(
        itemCount: 1,
        hasImage: false,
        shrinkWrap: true,
        padding: EdgeInsets.zero,
      ),
    };
  }
}

class _OnDutyCard extends StatelessWidget {
  const _OnDutyCard({required this.pharmacy});

  final OnDutyPharmacy pharmacy;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return AppCard(
      color: theme.colorScheme.primaryContainer,
      borderColor: theme.colorScheme.primary.withValues(alpha: 0.22),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'BUGÜN NÖBETÇİ',
            style: theme.textTheme.labelSmall?.copyWith(
              color: theme.colorScheme.onPrimaryContainer.withValues(alpha: 0.75),
              letterSpacing: 0.6,
            ),
          ),
          AppSpacing.gapSm,
          Text(pharmacy.name, style: theme.textTheme.headlineSmall),
          if (pharmacy.dutyHours != null) ...[
            AppSpacing.gapSm,
            Row(
              children: [
                Icon(Icons.schedule_rounded, size: 16, color: palette.muted),
                AppSpacing.wGapSm,
                Text(pharmacy.dutyHours!, style: theme.textTheme.bodyMedium),
              ],
            ),
          ],
          if ((pharmacy.address ?? '').trim().isNotEmpty) ...[
            AppSpacing.gapSm,
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(Icons.place_rounded, size: 16, color: palette.muted),
                AppSpacing.wGapSm,
                Expanded(
                  child: Text(
                    pharmacy.address!.trim(),
                    style: theme.textTheme.bodyMedium,
                  ),
                ),
              ],
            ),
          ],
          AppSpacing.gapLg,
          ContactActions(
            phone: pharmacy.phone,
            latitude: pharmacy.latitude,
            longitude: pharmacy.longitude,
            mapLabel: pharmacy.name,
            address: pharmacy.address,
            callLabel: 'Eczaneyi ara',
          ),
        ],
      ),
    );
  }
}

/// Takvimde bir güne dokununca o günün nöbetçileri; seçim yoksa ipucu.
class _SelectedDayDetails extends StatelessWidget {
  const _SelectedDayDetails({required this.entries, required this.dayKey});

  final List<DutySchedule> entries;
  final String? dayKey;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (dayKey == null) {
      return Text(
        entries.isEmpty
            ? 'Bu ay için nöbet kaydı bulunmuyor.'
            : 'Ayrıntı için takvimden işaretli bir güne dokunun.',
        textAlign: TextAlign.center,
        style: theme.textTheme.bodySmall?.copyWith(color: theme.palette.muted),
      );
    }

    final dayEntries = entries.where((entry) => entry.dayKey == dayKey).toList();
    if (dayEntries.isEmpty) return const SizedBox.shrink();

    final date = DateTime.parse('$dayKey' 'T00:00:00Z');

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        SectionHeader(title: AppDate.dayWithWeekday(date)),
        for (final entry in dayEntries) ...[
          PharmacyTile(
            name: entry.pharmacyName,
            badge: entry.hours,
            workingHours: entry.crossesMidnight
                ? 'Nöbet ertesi gün ${entry.endTime}\'a kadar sürer'
                : null,
          ),
          if (entry != dayEntries.last) AppSpacing.gapSm,
        ],
      ],
    );
  }
}

// ------------------------------------------------------------ Eczaneler sekmesi

class _AllPharmaciesTab extends ConsumerStatefulWidget {
  const _AllPharmaciesTab();

  @override
  ConsumerState<_AllPharmaciesTab> createState() => _AllPharmaciesTabState();
}

class _AllPharmaciesTabState extends ConsumerState<_AllPharmaciesTab> {
  final _scrollController = ScrollController();
  final _searchController = TextEditingController();

  static const _loadMoreThreshold = 400.0;

  @override
  void initState() {
    super.initState();
    _searchController.text = ref.read(pharmacyFeedProvider).filter;
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
      ref.read(pharmacyFeedProvider.notifier).loadMore();
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(pharmacyFeedProvider);
    final controller = ref.read(pharmacyFeedProvider.notifier);

    return Column(
      children: [
        Padding(
          padding: AppSpacing.screenPadding,
          child: AppTextField(
            controller: _searchController,
            hint: 'Eczane adı veya adres ara',
            prefixIcon: Icons.search_rounded,
            textInputAction: TextInputAction.search,
            onChanged: controller.search,
          ),
        ),
        AppSpacing.gapSm,
        Expanded(
          child: switch (state) {
            _ when state.isLoadingFirstPage => const LoadingView(
              itemCount: 5,
              hasImage: false,
            ),
            _ when state.error != null && state.items.isEmpty => ErrorView(
              message: state.error!.message,
              traceId: state.error!.traceId,
              onRetry: controller.retry,
            ),
            _ when state.isEmpty => EmptyView(
              icon: Icons.local_pharmacy_outlined,
              title: state.filter.isEmpty
                  ? 'Eczane kaydı yok'
                  : 'Eşleşen eczane yok',
              message: state.filter.isEmpty
                  ? 'Eczaneler eklendiğinde burada görünecek.'
                  : '"${state.filter}" için sonuç bulunamadı.',
            ),
            _ => ListView.separated(
              controller: _scrollController,
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.lg,
                AppSpacing.sm,
                AppSpacing.lg,
                AppSpacing.xxl,
              ),
              // +1 = altbilgi (sayfa yükleniyor / sayfa hatası / liste sonu).
              // ⚠️ 11.15'e kadar burada YALNIZ yükleniyor göstergesi vardı:
              // 2. sayfa patlarsa liste sessizce eksik kalıyordu.
              itemCount: state.items.length + 1,
              separatorBuilder: (_, _) => AppSpacing.gapSm,
              itemBuilder: (context, index) {
                if (index == state.items.length) {
                  return PagedListFooter(
                    state: state,
                    onLoadMore: controller.loadMore,
                    itemNoun: 'eczane',
                  );
                }
                final pharmacy = state.items[index];
                return PharmacyTile(
                  name: pharmacy.name,
                  address: pharmacy.address,
                  pharmacistName: pharmacy.pharmacistName,
                  workingHours: pharmacy.workingHours,
                  onTap: () =>
                      context.push(AppRoutes.pharmacyDetail(pharmacy.id)),
                );
              },
            ),
          },
        ),
      ],
    );
  }
}

/// Kart içi küçük hata + tekrar dene (tüm ekranı hataya düşürmeden).
class _InlineError extends StatelessWidget {
  const _InlineError({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) => AppCard(
    child: Column(
      children: [
        Text(
          message,
          style: Theme.of(context).textTheme.bodyMedium,
          textAlign: TextAlign.center,
        ),
        AppSpacing.gapMd,
        AppButton.ghost(
          label: 'Tekrar dene',
          icon: Icons.refresh_rounded,
          size: AppButtonSize.small,
          expand: true,
          onPressed: onRetry,
        ),
      ],
    ),
  );
}
