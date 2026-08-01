import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/named_lookup.dart';
import 'models/neighborhood.dart';

/// Form/filtre ekranlarının ortak sözlük (lookup) uçları.
///
/// Hepsi sunucuda 15 dk cache'li, nadiren değişir ve birden çok modül tarafından
/// paylaşılır: mahalleler 11.3 (kayıt) + 11.5 (profil) + 11.11 (vefat formu),
/// mezarlık/cami 11.11 (vefat formu + detay) kullanıyor.
class LookupsRepository {
  LookupsRepository(this._api);

  final ApiClient _api;

  Future<List<Neighborhood>> neighborhoods() =>
      _api.getList('/v1/neighborhoods', Neighborhood.fromJson);

  /// Mezarlıklar — vefat bildiriminde "defnedileceği yer" seçimi.
  Future<List<NamedLookup>> cemeteries() =>
      _api.getList('/v1/deaths/cemeteries', NamedLookup.fromJson);

  /// Camiler — "cenaze namazının kılınacağı cami" seçimi.
  Future<List<NamedLookup>> mosques() =>
      _api.getList('/v1/deaths/mosques', NamedLookup.fromJson);
}

final lookupsRepositoryProvider = Provider<LookupsRepository>(
  (ref) => LookupsRepository(ref.watch(apiClientProvider)),
);

/// Mahalle listesi. Sunucu tarafı 15 dk cache'li, istemcide de tekrar
/// çekilmesin diye `keepAlive` (liste oturum boyunca sabit sayılır).
final neighborhoodsProvider = FutureProvider<List<Neighborhood>>((ref) {
  ref.keepAlive();
  return ref.watch(lookupsRepositoryProvider).neighborhoods();
});

/// Mezarlık listesi (11.11) — vefat formu ve detaydaki konum bilgisi.
final cemeteriesProvider = FutureProvider<List<NamedLookup>>((ref) {
  ref.keepAlive();
  return ref.watch(lookupsRepositoryProvider).cemeteries();
}, retry: apiRetry);

/// Cami listesi (11.11).
final mosquesProvider = FutureProvider<List<NamedLookup>>((ref) {
  ref.keepAlive();
  return ref.watch(lookupsRepositoryProvider).mosques();
}, retry: apiRetry);
