import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/on_duty_pharmacy.dart';

/// Eczane uçları. 11.4 yalnız "bugün nöbetçi"yi kullanır; 11.7 aylık takvim +
/// eczane listesi/detayını buraya ekleyecek.
class PharmaciesRepository {
  PharmaciesRepository(this._api);

  final ApiClient _api;

  /// [date] verilmezse sunucu **Türkiye saatiyle bugünü** kullanır
  /// (`TurkeyClock` — istemcinin tarih göndermesi gerekmez).
  Future<List<OnDutyPharmacy>> onDuty({DateTime? date}) => _api.getList(
    '/v1/pharmacies/on-duty',
    OnDutyPharmacy.fromJson,
    query: {
      if (date != null)
        'date':
            '${date.year.toString().padLeft(4, '0')}-'
            '${date.month.toString().padLeft(2, '0')}-'
            '${date.day.toString().padLeft(2, '0')}',
    },
  );
}

final pharmaciesRepositoryProvider = Provider<PharmaciesRepository>(
  (ref) => PharmaciesRepository(ref.watch(apiClientProvider)),
);
