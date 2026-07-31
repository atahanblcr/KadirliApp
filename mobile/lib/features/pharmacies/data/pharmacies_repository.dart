import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/utils/utils.dart';
import 'models/duty_schedule.dart';
import 'models/on_duty_pharmacy.dart';
import 'models/pharmacy.dart';

/// Eczane uçları (API_CONTRACT §10).
class PharmaciesRepository {
  PharmaciesRepository(this._api);

  final ApiClient _api;

  /// [date] verilmezse sunucu **Türkiye saatiyle bugünü** kullanır
  /// (`TurkeyClock` — istemcinin tarih göndermesi gerekmez).
  Future<List<OnDutyPharmacy>> onDuty({DateTime? date}) => _api.getList(
    '/v1/pharmacies/on-duty',
    OnDutyPharmacy.fromJson,
    query: {if (date != null) 'date': AppDate.isoDay(date)},
  );

  /// Aylık nöbet listesi (sayfasız düz liste).
  Future<List<DutySchedule>> schedule({
    required int year,
    required int month,
  }) => _api.getList(
    '/v1/pharmacies/schedule',
    DutySchedule.fromJson,
    query: {'year': year, 'month': month},
  );

  /// Tüm eczaneler (sayfalı; `search` ada/adrese bakar).
  Future<PagedResult<Pharmacy>> list({
    int page = 1,
    int limit = 20,
    String? search,
  }) => _api.getPaged(
    '/v1/pharmacies',
    Pharmacy.fromJson,
    page: page,
    limit: limit,
    query: {'search': ?_blankToNull(search)},
  );

  Future<Pharmacy> detail(String id) =>
      _api.getObject('/v1/pharmacies/$id', Pharmacy.fromJson);

  static String? _blankToNull(String? value) {
    final trimmed = value?.trim();
    return (trimmed == null || trimmed.isEmpty) ? null : trimmed;
  }
}

final pharmaciesRepositoryProvider = Provider<PharmaciesRepository>(
  (ref) => PharmaciesRepository(ref.watch(apiClientProvider)),
);
