import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/neighborhood.dart';

/// Form/filtre ekranlarının ortak sözlük (lookup) uçları.
///
/// 11.3 yalnız mahalleleri kullanır (kayıt ekranı); mezarlık/cami/kategori
/// uçları ilgili fazlarda (11.11, 11.10) buraya eklenecek — hepsi aynı desen.
class LookupsRepository {
  LookupsRepository(this._api);

  final ApiClient _api;

  Future<List<Neighborhood>> neighborhoods() =>
      _api.getList('/v1/neighborhoods', Neighborhood.fromJson);
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
