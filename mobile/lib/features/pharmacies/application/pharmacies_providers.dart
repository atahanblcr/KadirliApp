import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/paging/paged_feed.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../data/models/duty_schedule.dart';
import '../data/models/pharmacy.dart';
import '../data/pharmacies_repository.dart';

/// Eczane ekranının sekmesi.
enum PharmacyTab {
  /// Bugünün nöbetçisi + aylık nöbet takvimi (varsayılan — aranan bilgi bu).
  duty,

  /// Tüm eczaneler, aramalı.
  all,
}

class PharmacyTabController extends Notifier<PharmacyTab> {
  @override
  PharmacyTab build() => PharmacyTab.duty;

  void select(PharmacyTab tab) => state = tab;
}

final pharmacyTabProvider = NotifierProvider<PharmacyTabController, PharmacyTab>(
  PharmacyTabController.new,
);

/// Takvimde gösterilen ay. 11.10'da ortak takvime taşındı ([CalendarMonth]) —
/// eczane tarafındaki adlar okunabilirlik için duruyor.
typedef DutyMonth = CalendarMonth;

DutyMonth dutyMonthOf(DateTime date) => calendarMonthOf(date);

/// Bir ay ileri/geri (Aralık→Ocak sarması dahil).
DutyMonth shiftDutyMonth(DutyMonth month, int delta) =>
    shiftCalendarMonth(month, delta);

/// Takvimde seçili ay — açılışta **Kadirli'nin içinde bulunduğu ay**.
class DutyMonthController extends Notifier<DutyMonth> {
  @override
  DutyMonth build() => dutyMonthOf(AppDate.nowInTurkey);

  void next() => state = shiftDutyMonth(state, 1);
  void previous() => state = shiftDutyMonth(state, -1);
  void reset() => state = dutyMonthOf(AppDate.nowInTurkey);
}

final dutyMonthProvider = NotifierProvider<DutyMonthController, DutyMonth>(
  DutyMonthController.new,
);

/// Ayın nöbet listesi. Ay değişince family anahtarı değişir → önceki ay
/// önbellekte kalır (ileri-geri gezinme yeniden istek atmaz).
final dutyScheduleProvider = FutureProvider.family<List<DutySchedule>, DutyMonth>(
  (ref, month) => ref
      .watch(pharmaciesRepositoryProvider)
      .schedule(year: month.year, month: month.month),
  retry: apiRetry,
);

/// Takvimde kullanıcının seçtiği gün (`yyyy-MM-dd`); `null` = seçim yok.
class SelectedDutyDayController extends Notifier<String?> {
  @override
  String? build() => null;

  /// Aynı güne tekrar dokunmak seçimi kaldırır.
  void toggle(String dayKey) => state = state == dayKey ? null : dayKey;

  void clear() => state = null;
}

final selectedDutyDayProvider =
    NotifierProvider<SelectedDutyDayController, String?>(
      SelectedDutyDayController.new,
    );

/// Tek eczane (detay ekranı + 11.13 deep-link).
final pharmacyDetailProvider = FutureProvider.autoDispose
    .family<Pharmacy, String>(
      (ref, id) => ref.watch(pharmaciesRepositoryProvider).detail(id),
      retry: apiRetry,
    );

/// "Tüm eczaneler" sekmesinin akışı — filtre = arama metni ('' = filtresiz).
typedef PharmacyFeedState = PagedFeedState<Pharmacy, String>;

class PharmacyFeedController extends PagedFeedController<Pharmacy, String> {
  @override
  String get initialFilter => '';

  @override
  Future<PagedResult<Pharmacy>> fetchPage({
    required int page,
    required int limit,
    required String filter,
  }) => ref
      .read(pharmaciesRepositoryProvider)
      .list(page: page, limit: limit, search: filter);

  @override
  String idOf(Pharmacy item) => item.id;

  void search(String term) => applyFilter(term.trim());
}

final pharmacyFeedProvider =
    NotifierProvider<PharmacyFeedController, PharmacyFeedState>(
      PharmacyFeedController.new,
    );

/// Bir eczanenin **seçili aydaki** nöbet günleri (detay ekranında gösterilir).
///
/// Ayrı bir uç yok; ayın listesi zaten çekiliyor, burada süzülüyor.
List<DutySchedule> dutyDaysOf(List<DutySchedule> schedule, String pharmacyId) =>
    (schedule.where((entry) => entry.pharmacyId == pharmacyId).toList()
      ..sort((a, b) => a.dutyDate.compareTo(b.dutyDate)));
