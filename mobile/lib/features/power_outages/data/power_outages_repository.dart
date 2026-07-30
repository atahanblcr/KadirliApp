import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/power_outage.dart';

/// Elektrik kesintisi uçları. 11.6 detay ekranını ekleyecek.
class PowerOutagesRepository {
  PowerOutagesRepository(this._api);

  final ApiClient _api;

  /// Tüm kayıtlar (uç sayfasız; sunucu başlangıç saatine göre yeniden eskiye).
  Future<List<PowerOutage>> all() =>
      _api.getList('/v1/power-outages', PowerOutage.fromJson);

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
