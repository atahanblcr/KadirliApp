import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/taxi_driver.dart';

/// Taksi uçları (API_CONTRACT §10).
class TaxisRepository {
  TaxisRepository(this._api);

  final ApiClient _api;

  /// Sayfalı sürücü listesi. ⚠️ Arama parametresinin adı **`searchTerm`**
  /// (diğer modüllerde `search`) — `QueryTaxiDriverDto` böyle tanımlı.
  /// Sunucu ada **ve plakaya** bakar.
  Future<PagedResult<TaxiDriver>> drivers({
    int page = 1,
    int limit = 20,
    String? search,
  }) => _api.getPaged(
    '/v1/taxis/drivers',
    TaxiDriver.fromJson,
    page: page,
    limit: limit,
    query: {'searchTerm': ?_blankToNull(search)},
  );

  Future<TaxiDriver> driver(String id) =>
      _api.getObject('/v1/taxis/drivers/$id', TaxiDriver.fromJson);

  /// Çağrı kaydı oluşturur ve **aranacak telefonu** döndürür (`[A]`).
  ///
  /// Her çağrı yeni bir `taxi_calls` satırıdır (favori/görüntülemenin aksine
  /// tekrarlanabilir eylem) ve sürücünün `total_calls` sayacını artırır.
  Future<String> call(String id) async {
    final data = await _api.post('/v1/taxis/drivers/$id/call');
    if (data is Map && data['phone'] is String) return data['phone'] as String;
    throw ApiException.unexpectedResponse(cause: data);
  }

  static String? _blankToNull(String? value) {
    final trimmed = value?.trim();
    return (trimmed == null || trimmed.isEmpty) ? null : trimmed;
  }
}

final taxisRepositoryProvider = Provider<TaxisRepository>(
  (ref) => TaxisRepository(ref.watch(apiClientProvider)),
);
