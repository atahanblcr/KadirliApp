import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/paging/paged_feed.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../data/events_repository.dart';
import '../data/models/event.dart';
import '../data/models/event_calendar_item.dart';
import '../data/models/event_category.dart';

/// Etkinlik ekranının sekmesi.
enum EventsTab {
  /// Aramalı/filtreli liste (varsayılan — "bu hafta ne var" sorusu).
  list,

  /// Aylık takvim ızgarası.
  calendar,
}

class EventsTabController extends Notifier<EventsTab> {
  @override
  EventsTab build() => EventsTab.list;

  void select(EventsTab tab) => state = tab;
}

final eventsTabProvider = NotifierProvider<EventsTabController, EventsTab>(
  EventsTabController.new,
);

/// Kategori lookup'ı (filtre şeridi). Nadiren değişir → `autoDispose` yok.
final eventCategoriesProvider = FutureProvider<List<EventCategory>>(
  (ref) => ref.watch(eventsRepositoryProvider).categories(),
  retry: apiRetry,
);

/// Tek etkinlik (detay + 11.13 deep-link).
final eventDetailProvider = FutureProvider.autoDispose.family<Event, String>(
  (ref, id) => ref.watch(eventsRepositoryProvider).detail(id),
  retry: apiRetry,
);

// ------------------------------------------------------------------- Liste

/// Listenin zaman kapsamı.
///
/// Etkinlik uygulamasında asıl soru "ne zaman ne var"; kullanıcı **yaklaşan**
/// etkinliklere bakar, geçmişi ise nadiren merak eder → varsayılan `upcoming`,
/// geçmiş ayrı bir chip'in arkasında.
enum EventScope { upcoming, past }

/// Listenin konum kapsamı (Faz 12.4).
///
/// ⚠️ Değerler sunucunun `locationScope` sözlüğüyle **birebir** aynı olmak
/// zorunda: tanınmayan bir değer 400 üretmez, **sessizce yok sayılır** ve
/// kullanıcı süzülmemiş listeye bakarken filtreyi seçili görür.
enum EventPlace {
  /// Süzgeç yok — her yer.
  all(null, 'Tümü'),

  /// Yalnız Kadirli.
  local('local', 'Kadirli'),

  /// Osmaniye ilinin tamamı (Kadirli dâhil).
  province('province', 'Osmaniye'),

  /// Osmaniye dışı — tanımın sahibi sunucu.
  nearby('nearby', 'Çevre iller');

  const EventPlace(this.query, this.label);

  /// `?locationScope=` değeri; `null` ise parametre hiç gönderilmez.
  final String? query;

  final String label;
}

@immutable
class EventFilter {
  const EventFilter({
    this.scope = EventScope.upcoming,
    this.place = EventPlace.all,
    this.categoryId,
    this.search = '',
    this.onlyFree = false,
  });

  final EventScope scope;

  /// Faz 12.4 — konum kapsamı.
  final EventPlace place;

  final String? categoryId;
  final String search;

  /// `?isFree=true` — plan bu filtreyi saymıyordu ama uç destekliyor ve
  /// "ücretsiz etkinlik" en çok sorulan ayrımlardan biri.
  final bool onlyFree;

  /// Varsayılandan (yaklaşan + filtresiz) sapıldı mı — "Filtreleri temizle".
  bool get isActive =>
      scope != EventScope.upcoming ||
      place != EventPlace.all ||
      categoryId != null ||
      search.trim().isNotEmpty ||
      onlyFree;

  EventFilter copyWith({
    EventScope? scope,
    EventPlace? place,
    String? categoryId,
    bool clearCategory = false,
    String? search,
    bool? onlyFree,
  }) => EventFilter(
    scope: scope ?? this.scope,
    place: place ?? this.place,
    categoryId: clearCategory ? null : (categoryId ?? this.categoryId),
    search: search ?? this.search,
    onlyFree: onlyFree ?? this.onlyFree,
  );

  @override
  bool operator ==(Object other) =>
      other is EventFilter &&
      other.scope == scope &&
      other.place == place &&
      other.categoryId == categoryId &&
      other.search == search &&
      other.onlyFree == onlyFree;

  @override
  int get hashCode => Object.hash(scope, place, categoryId, search, onlyFree);
}

typedef EventsFeedState = PagedFeedState<Event, EventFilter>;

class EventsFeedController extends PagedFeedController<Event, EventFilter> {
  @override
  EventFilter get initialFilter => const EventFilter();

  @override
  Future<PagedResult<Event>> fetchPage({
    required int page,
    required int limit,
    required EventFilter filter,
  }) {
    // Bugün Kadirli'de hangi gün ise ona göre bölünür: gece 23:00'te "yarınki"
    // etkinlik hâlâ yaklaşan, bugünkü etkinlik gün bitene kadar listede kalır.
    final today = AppDate.isoDay(DateTime.now());
    final isUpcoming = filter.scope == EventScope.upcoming;

    return ref
        .read(eventsRepositoryProvider)
        .list(
          page: page,
          limit: limit,
          search: filter.search,
          categoryId: filter.categoryId,
          startDate: isUpcoming ? today : null,
          // Geçmiş listesinde bugün yer almaz (bugünküler "Yaklaşan"da).
          endDate: isUpcoming ? null : _yesterday(),
          isFree: filter.onlyFree ? true : null,
          // ⚠️ Yaklaşan listede sıralama **artan** olmalı: sunucunun varsayılan
          // `date_desc`i ilk sayfaya en UZAK tarihli etkinlikleri koyuyordu.
          sort: isUpcoming ? 'date_asc' : 'date_desc',
          locationScope: filter.place.query,
        );
  }

  @override
  String idOf(Event item) => item.id;

  void selectCategory(String? categoryId) {
    final current = state.filter;
    final next = current.categoryId == categoryId ? null : categoryId;
    applyFilter(
      next == null
          ? current.copyWith(clearCategory: true)
          : current.copyWith(categoryId: next),
    );
  }

  void selectScope(EventScope scope) =>
      applyFilter(state.filter.copyWith(scope: scope));

  /// Faz 12.4 — konum kapsamı. Aynı chip'e tekrar dokunmak süzgeci kaldırır
  /// (kategori şeridiyle aynı davranış).
  void selectPlace(EventPlace place) => applyFilter(
    state.filter.copyWith(
      place: state.filter.place == place ? EventPlace.all : place,
    ),
  );

  void toggleFreeOnly() =>
      applyFilter(state.filter.copyWith(onlyFree: !state.filter.onlyFree));

  void search(String term) =>
      applyFilter(state.filter.copyWith(search: term.trim()));

  void clearFilters() => applyFilter(const EventFilter());

  static String _yesterday() =>
      AppDate.isoDay(DateTime.now().subtract(const Duration(days: 1)));
}

final eventsFeedProvider =
    NotifierProvider<EventsFeedController, EventsFeedState>(
      EventsFeedController.new,
    );

// ------------------------------------------------------------------ Takvim

/// Takvimde seçili ay — açılışta Kadirli'nin içinde bulunduğu ay.
class EventMonthController extends Notifier<CalendarMonth> {
  @override
  CalendarMonth build() => calendarMonthOf(AppDate.nowInTurkey);

  void next() => state = shiftCalendarMonth(state, 1);
  void previous() => state = shiftCalendarMonth(state, -1);
  void reset() => state = calendarMonthOf(AppDate.nowInTurkey);
}

final eventMonthProvider =
    NotifierProvider<EventMonthController, CalendarMonth>(
      EventMonthController.new,
    );

/// Ayın etkinlikleri. Ay değişince family anahtarı değişir → gezinilen aylar
/// önbellekte kalır (ileri-geri gitmek yeniden istek atmaz).
final eventCalendarProvider =
    FutureProvider.family<List<EventCalendarItem>, CalendarMonth>(
      (ref, month) => ref
          .watch(eventsRepositoryProvider)
          .calendar(year: month.year, month: month.month),
      retry: apiRetry,
    );

/// Takvimde seçili gün (`yyyy-MM-dd`); null = seçim yok.
class SelectedEventDayController extends Notifier<String?> {
  @override
  String? build() => null;

  /// Aynı güne tekrar dokunmak seçimi kaldırır.
  void toggle(String dayKey) => state = state == dayKey ? null : dayKey;

  void clear() => state = null;
}

final selectedEventDayProvider =
    NotifierProvider<SelectedEventDayController, String?>(
      SelectedEventDayController.new,
    );

/// Takvim ızgarasının "hangi günde kaç etkinlik var" eşlemesi.
Map<String, int> eventCountsByDay(List<EventCalendarItem> items) {
  final counts = <String, int>{};
  for (final item in items) {
    counts.update(item.dayKey, (value) => value + 1, ifAbsent: () => 1);
  }
  return counts;
}

/// Seçili günün etkinlikleri (saate göre sıralı).
List<EventCalendarItem> eventsOfDay(
  List<EventCalendarItem> items,
  String? dayKey,
) {
  if (dayKey == null) return const [];
  return items.where((item) => item.dayKey == dayKey).toList()
    ..sort((a, b) => a.eventTime.compareTo(b.eventTime));
}
