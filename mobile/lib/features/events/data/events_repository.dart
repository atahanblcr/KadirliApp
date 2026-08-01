import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/event.dart';
import 'models/event_calendar_item.dart';
import 'models/event_category.dart';

/// Etkinlik uçları (API_CONTRACT §10). Public uç yalnız **onaylı** etkinlikleri
/// döndürür; istemcinin ayrıca `status` filtrelemesi gerekmez.
class EventsRepository {
  EventsRepository(this._api);

  final ApiClient _api;

  /// Sayfalı liste.
  ///
  /// [startDate]/[endDate] `yyyy-MM-dd`; [sort] `date_asc` (yaklaşan
  /// etkinliklerde en yakın önce) ya da varsayılan `date_desc`.
  Future<PagedResult<Event>> list({
    int page = 1,
    int limit = 20,
    String? search,
    String? categoryId,
    String? startDate,
    String? endDate,
    bool? isFree,
    String? sort,
  }) => _api.getPaged(
    '/v1/events',
    Event.fromJson,
    page: page,
    limit: limit,
    query: {
      'search': ?_blankToNull(search),
      'categoryId': ?_blankToNull(categoryId),
      'startDate': ?_blankToNull(startDate),
      'endDate': ?_blankToNull(endDate),
      'isFree': ?isFree,
      'sort': ?_blankToNull(sort),
    },
  );

  /// Bir ayın etkinlikleri (takvim ızgarası) — sayfasız düz liste.
  Future<List<EventCalendarItem>> calendar({
    required int year,
    required int month,
  }) => _api.getList(
    '/v1/events/calendar',
    EventCalendarItem.fromJson,
    query: {'year': year, 'month': month},
  );

  Future<Event> detail(String id) =>
      _api.getObject('/v1/events/$id', Event.fromJson);

  /// Filtre şeridi için kategoriler (sayfasız lookup).
  Future<List<EventCategory>> categories() =>
      _api.getList('/v1/events/categories', EventCategory.fromJson);

  static String? _blankToNull(String? value) {
    final trimmed = value?.trim();
    return (trimmed == null || trimmed.isEmpty) ? null : trimmed;
  }
}

final eventsRepositoryProvider = Provider<EventsRepository>(
  (ref) => EventsRepository(ref.watch(apiClientProvider)),
);
