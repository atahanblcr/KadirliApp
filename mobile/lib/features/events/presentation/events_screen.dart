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
import '../application/events_providers.dart';
import '../data/models/event_calendar_item.dart';
import 'widgets/event_card.dart';

/// Etkinlikler (11.10).
///
/// İki sekme: **Liste** (yaklaşan/geçmiş + kategori + ücretsiz filtresi, arama,
/// sonsuz kaydırma) ve **Takvim** (aylık ızgara — 11.7'nin nöbet takviminden
/// çıkarılan ortak [MonthCalendar]). Varsayılan liste: kullanıcı ekrana
/// çoğunlukla "yakında ne var" diye geliyor, takvim ise planlama aracı.
class EventsScreen extends ConsumerWidget {
  const EventsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tab = ref.watch(eventsTabProvider);

    return AppScaffold(
      title: 'Etkinlikler',
      onRefresh: () async {
        ref.invalidate(eventCategoriesProvider);
        if (tab == EventsTab.calendar) {
          ref.invalidate(eventCalendarProvider(ref.read(eventMonthProvider)));
        } else {
          await ref.read(eventsFeedProvider.notifier).refresh();
        }
      },
      body: Column(
        children: [
          AppSpacing.gapMd,
          Padding(
            padding: AppSpacing.screenPadding,
            child: SegmentedButton<EventsTab>(
              segments: const [
                ButtonSegment(
                  value: EventsTab.list,
                  label: Text('Liste'),
                  icon: Icon(Icons.list_rounded, size: 18),
                ),
                ButtonSegment(
                  value: EventsTab.calendar,
                  label: Text('Takvim'),
                  icon: Icon(Icons.calendar_month_rounded, size: 18),
                ),
              ],
              selected: {tab},
              showSelectedIcon: false,
              onSelectionChanged: (selection) =>
                  ref.read(eventsTabProvider.notifier).select(selection.first),
            ),
          ),
          AppSpacing.gapSm,
          Expanded(
            child: tab == EventsTab.list
                ? const _ListTab()
                : const _CalendarTab(),
          ),
        ],
      ),
    );
  }
}

// -------------------------------------------------------------- Liste sekmesi

class _ListTab extends ConsumerStatefulWidget {
  const _ListTab();

  @override
  ConsumerState<_ListTab> createState() => _ListTabState();
}

class _ListTabState extends ConsumerState<_ListTab> {
  final _scrollController = ScrollController();
  final _searchController = TextEditingController();

  static const _loadMoreThreshold = 400.0;

  @override
  void initState() {
    super.initState();
    _searchController.text = ref.read(eventsFeedProvider).filter.search;
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
      ref.read(eventsFeedProvider.notifier).loadMore();
    }
  }

  @override
  Widget build(BuildContext context) {
    final controller = ref.read(eventsFeedProvider.notifier);

    // Filtre dışarıdan sıfırlanırsa (ör. "Filtreleri temizle") arama kutusu da
    // temizlenmeli — 11.7/11.8'de iki kez çıkan hata.
    ref.listen(eventsFeedProvider.select((state) => state.filter.search), (
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
            hint: 'Etkinlik veya mekan ara',
            prefixIcon: Icons.search_rounded,
            textInputAction: TextInputAction.search,
            onChanged: controller.search,
          ),
        ),
        AppSpacing.gapMd,
        const _ScopeFilter(),
        AppSpacing.gapSm,
        const _PlaceFilter(),
        AppSpacing.gapSm,
        const _CategoryFilter(),
        AppSpacing.gapSm,
        Expanded(child: _ListBody(scrollController: _scrollController)),
      ],
    );
  }
}

/// Yaklaşan / Geçmiş + "Ücretsiz" — zaman kapsamı ve fiyat aynı şeritte.
class _ScopeFilter extends ConsumerWidget {
  const _ScopeFilter();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final filter = ref.watch(eventsFeedProvider.select((state) => state.filter));
    final controller = ref.read(eventsFeedProvider.notifier);

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
      child: Row(
        children: [
          // ⚠️ İkonsuz + `dense`: üç chip ikonlarıyla 360 dp'lik telefonda
          // sığmıyor, "Ücretsiz" ekranın dışında kalıyordu (test yakaladı) —
          // şerit kaydırılabilir olsa da görünmeyen filtre keşfedilmiyor.
          // Anlam etiketin kendisinde olduğu için ikon kaybı bilgi kaybı değil.
          FilterChoiceChip(
            label: 'Yaklaşan',
            dense: true,
            selected: filter.scope == EventScope.upcoming,
            onTap: () => controller.selectScope(EventScope.upcoming),
          ),
          AppSpacing.wGapSm,
          FilterChoiceChip(
            label: 'Geçmiş',
            dense: true,
            selected: filter.scope == EventScope.past,
            onTap: () => controller.selectScope(EventScope.past),
          ),
          AppSpacing.wGapSm,
          FilterChoiceChip(
            label: 'Ücretsiz',
            dense: true,
            selected: filter.onlyFree,
            onTap: controller.toggleFreeOnly,
          ),
        ],
      ),
    );
  }
}

/// Konum şeridi (Faz 12.4): Kadirli · Osmaniye · Çevre iller.
///
/// ⚠️ Şerit **sabit** — bir uçtan beslenmiyor, çünkü üç kapsamın tanımı sunucuda
/// (`locationScope`) yaşıyor ve ilçe listesinin kendisi kullanıcıya sorulmuyor.
/// "Tümü" ayrı bir chip değil: seçili chip'e tekrar dokunmak süzgeci kaldırıyor
/// (kategori şeridiyle aynı el alışkanlığı).
class _PlaceFilter extends ConsumerWidget {
  const _PlaceFilter();

  static const _places = [
    EventPlace.local,
    EventPlace.province,
    EventPlace.nearby,
  ];

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final selected = ref.watch(
      eventsFeedProvider.select((state) => state.filter.place),
    );
    final controller = ref.read(eventsFeedProvider.notifier);

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
      child: Row(
        children: [
          for (final place in _places) ...[
            if (place != _places.first) AppSpacing.wGapSm,
            // ⚠️ `dense` + ikonsuz: üç chip 360 dp'de ikonlarıyla taşıyordu —
            // zaman şeridinde (`_ScopeFilter`) aynı karar aynı sebeple alındı.
            FilterChoiceChip(
              label: place.label,
              dense: true,
              selected: selected == place,
              onTap: () => controller.selectPlace(place),
            ),
          ],
        ],
      ),
    );
  }
}

/// Kategori chip'leri. Kategori listesi alınamazsa şerit **hiç çizilmez**
/// (11.6'dan beri geçerli kural: çalışmayan filtre gösterilmez).
class _CategoryFilter extends ConsumerWidget {
  const _CategoryFilter();

  static const _height = 44.0;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final categories = ref.watch(eventCategoriesProvider);
    final selectedId = ref.watch(
      eventsFeedProvider.select((state) => state.filter.categoryId),
    );
    final controller = ref.read(eventsFeedProvider.notifier);

    return switch (categories) {
      AsyncData(value: final items) when items.isNotEmpty => SizedBox(
        height: _height,
        // ⚠️ Yatay `ListView` tembel: ekran dışı chip hiç kurulmaz (11.8).
        child: SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
          child: Row(
            children: [
              FilterChoiceChip(
                label: 'Tüm türler',
                icon: Icons.apps_rounded,
                selected: selectedId == null,
                onTap: () => controller.selectCategory(null),
              ),
              for (final category in items) ...[
                AppSpacing.wGapSm,
                FilterChoiceChip(
                  label: category.name,
                  icon: category.materialIcon,
                  selected: selectedId == category.id,
                  onTap: () => controller.selectCategory(category.id),
                ),
              ],
            ],
          ),
        ),
      ),
      AsyncLoading() => const SizedBox(
        height: _height,
        child: Padding(
          padding: EdgeInsets.symmetric(horizontal: AppSpacing.lg),
          child: Row(
            children: [
              SkeletonBox(height: 32, width: 96, borderRadius: AppRadius.rPill),
              AppSpacing.wGapSm,
              SkeletonBox(height: 32, width: 84, borderRadius: AppRadius.rPill),
              AppSpacing.wGapSm,
              SkeletonBox(height: 32, width: 72, borderRadius: AppRadius.rPill),
            ],
          ),
        ),
      ),
      _ => const SizedBox.shrink(),
    };
  }
}

class _ListBody extends ConsumerWidget {
  const _ListBody({required this.scrollController});

  final ScrollController scrollController;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(eventsFeedProvider);
    final controller = ref.read(eventsFeedProvider.notifier);

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
      final filter = state.filter;
      return EmptyView(
        icon: Icons.celebration_outlined,
        title: filter.scope == EventScope.past
            ? 'Geçmiş etkinlik yok'
            : 'Yaklaşan etkinlik yok',
        message: filter.isActive
            ? 'Filtreleri değiştirerek diğer etkinliklere bakabilirsiniz.'
            : 'Yeni etkinlikler duyurulduğunda burada görünecek.',
        actionLabel: filter.isActive ? 'Filtreleri temizle' : null,
        onAction: filter.isActive ? controller.clearFilters : null,
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
      separatorBuilder: (_, _) => AppSpacing.gapSm,
      itemBuilder: (context, index) {
        if (index == state.items.length) {
          return PagedListFooter(
            state: state,
            onLoadMore: controller.loadMore,
            itemNoun: 'etkinlik',
          );
        }

        final event = state.items[index];
        return EventCard(
          event: event,
          onTap: () => context.push(AppRoutes.eventDetail(event.id)),
        );
      },
    );
  }
}

// ------------------------------------------------------------- Takvim sekmesi

class _CalendarTab extends ConsumerWidget {
  const _CalendarTab();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final month = ref.watch(eventMonthProvider);
    final calendar = ref.watch(eventCalendarProvider(month));
    final selectedDay = ref.watch(selectedEventDayProvider);

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.sm,
        AppSpacing.lg,
        AppSpacing.xxl,
      ),
      children: [
        AppCard(
          child: Column(
            children: [
              MonthSwitcher(
                year: month.year,
                month: month.month,
                onPrevious: () {
                  ref.read(eventMonthProvider.notifier).previous();
                  ref.read(selectedEventDayProvider.notifier).clear();
                },
                onNext: () {
                  ref.read(eventMonthProvider.notifier).next();
                  ref.read(selectedEventDayProvider.notifier).clear();
                },
              ),
              AppSpacing.gapMd,
              switch (calendar) {
                AsyncData(value: final items) => MonthCalendar(
                  year: month.year,
                  month: month.month,
                  markedDays: eventCountsByDay(items),
                  selectedDayKey: selectedDay,
                  onDaySelected: (key) =>
                      ref.read(selectedEventDayProvider.notifier).toggle(key),
                  markedSemantics: (count) =>
                      count == 1 ? 'etkinlik var' : '$count etkinlik var',
                ),
                AsyncError(:final error) => _InlineError(
                  message: error is ApiException
                      ? error.message
                      : 'Etkinlik takvimi alınamadı.',
                  onRetry: () => ref.invalidate(eventCalendarProvider(month)),
                ),
                _ => const Padding(
                  padding: EdgeInsets.symmetric(vertical: AppSpacing.xl),
                  child: LoadingView.compact(),
                ),
              },
            ],
          ),
        ),
        if (calendar case AsyncData(value: final items)) ...[
          AppSpacing.gapLg,
          _SelectedDayEvents(items: items, dayKey: selectedDay),
        ],
      ],
    );
  }
}

/// Takvimde bir güne dokununca o günün etkinlikleri; seçim yoksa ipucu.
class _SelectedDayEvents extends StatelessWidget {
  const _SelectedDayEvents({required this.items, required this.dayKey});

  final List<EventCalendarItem> items;
  final String? dayKey;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (dayKey == null) {
      return Text(
        items.isEmpty
            ? 'Bu ay için etkinlik kaydı bulunmuyor.'
            : 'Ayrıntı için takvimden işaretli bir güne dokunun.',
        textAlign: TextAlign.center,
        style: theme.textTheme.bodySmall?.copyWith(color: theme.palette.muted),
      );
    }

    final dayEvents = eventsOfDay(items, dayKey);
    if (dayEvents.isEmpty) return const SizedBox.shrink();

    final date = DateTime.parse('${dayKey}T00:00:00Z');

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        SectionHeader(title: AppDate.dayWithWeekday(date)),
        for (final item in dayEvents) ...[
          AppCard(
            onTap: () => context.push(AppRoutes.eventDetail(item.id)),
            padding: const EdgeInsets.all(AppSpacing.md),
            child: Row(
              children: [
                Icon(
                  Icons.schedule_rounded,
                  size: 18,
                  color: theme.colorScheme.primary,
                ),
                AppSpacing.wGapSm,
                Text(
                  item.timeLabel,
                  style: theme.textTheme.titleSmall?.copyWith(
                    color: theme.colorScheme.primary,
                  ),
                ),
                AppSpacing.wGapMd,
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        item.title,
                        style: theme.textTheme.titleSmall,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                      ),
                      if ((item.venueName ?? '').trim().isNotEmpty)
                        Text(
                          item.venueName!.trim(),
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: theme.palette.muted,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                    ],
                  ),
                ),
                Icon(
                  Icons.chevron_right_rounded,
                  color: theme.palette.muted,
                ),
              ],
            ),
          ),
          if (item != dayEvents.last) AppSpacing.gapSm,
        ],
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
  Widget build(BuildContext context) => Column(
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
  );
}
