import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/intercity_route.dart';
import 'models/intracity_route.dart';

/// Ulaşım uçları (API_CONTRACT §10).
///
/// ⚠️ Arama parametresi **`searchTerm`** (`QueryTransportDto`) — 11.11'de
/// taksi ucunda yaşanan tuzağın aynısı; `search` yazılırsa sunucu sessizce
/// yok sayar ve kullanıcı "arama çalışmıyor" sanır.
class TransportRepository {
  TransportRepository(this._api);

  final ApiClient _api;

  /// Şehirlerarası hatlar — arama **hedef şehir ve firma adında** koşar.
  ///
  /// [vehicleType] `"bus"` | `"minibus"`; `null` ise parametre **hiç
  /// gönderilmez** ("Tümü"). ⚠️ Süzme **sunucuda** yapılır: sayfalı bir listeyi
  /// istemcide süzmek `totalCount`'u ("N hat") ve sonsuz kaydırmayı yalancı
  /// yapardı — 20'lik sayfadan 3 kayıt eleyip "17 hat" demek gibi.
  Future<PagedResult<IntercityRoute>> intercity({
    int page = 1,
    int limit = 20,
    String? search,
    String? vehicleType,
  }) => _api.getPaged(
    '/v1/transport/intercity-routes',
    IntercityRoute.fromJson,
    page: page,
    limit: limit,
    query: {
      'searchTerm': ?_blankToNull(search),
      'vehicleType': ?_blankToNull(vehicleType),
    },
  );

  /// Şehir içi hatlar — arama **hat adı ve numarasında** koşar.
  Future<PagedResult<IntracityRoute>> intracity({
    int page = 1,
    int limit = 20,
    String? search,
  }) => _api.getPaged(
    '/v1/transport/intracity-routes',
    IntracityRoute.fromJson,
    page: page,
    limit: limit,
    query: {'searchTerm': ?_blankToNull(search)},
  );

  static String? _blankToNull(String? value) {
    final trimmed = value?.trim();
    return (trimmed == null || trimmed.isEmpty) ? null : trimmed;
  }
}

final transportRepositoryProvider = Provider<TransportRepository>(
  (ref) => TransportRepository(ref.watch(apiClientProvider)),
);
