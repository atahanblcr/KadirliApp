import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/power_outage.dart';

/// Elektrik kesintisi uçları (API_CONTRACT §10).
class PowerOutagesRepository {
  PowerOutagesRepository(this._api);

  final ApiClient _api;

  /// Tüm kayıtlar (uç sayfasız; sunucu başlangıç saatine göre yeniden eskiye).
  Future<List<PowerOutage>> all() =>
      _api.getList('/v1/power-outages', PowerOutage.fromJson);

  /// Tek kesinti.
  ///
  /// ⚠️ Duyuru detayındaki quirk'in aynısı burada da var: bulunamayan kayıt
  /// **HTTP 200 + `success:false` + `NOT_FOUND`** döner (canlıda doğrulandı;
  /// API_CONTRACT yalnız announcements'ı istisna olarak yazıyor). Zarf
  /// interceptor'ı ikisini de `ApiException`'a çevirdiğinden ekran farkı
  /// görmez.
  Future<PowerOutage> detail(String id) =>
      _api.getObject('/v1/power-outages/$id', PowerOutage.fromJson);

  /// Ana Sayfa şeridi için: süren + planlanan kesintiler, en yakın önce.
  Future<List<PowerOutage>> relevant({DateTime? now}) async {
    final outages = await all();
    final upcoming = outages.where((outage) => outage.isRelevant(now: now)).toList()
      ..sort((a, b) => a.startTime.compareTo(b.startTime));
    return upcoming;
  }
}

final powerOutagesRepositoryProvider = Provider<PowerOutagesRepository>(
  (ref) => PowerOutagesRepository(ref.watch(apiClientProvider)),
);
