import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../../core/preferences/app_preferences.dart' show sharedPreferencesProvider;
import 'models/taxi_driver.dart';

/// Kullanıcının **son aradığı taksiciler** (yerel, cihazda).
///
/// ⭐ Plan dışı (11.11): taksi çağırmak tekrarlanan bir eylem ve insanlar
/// genelde aynı şoförü arıyor. Sunucuda "benim çağrılarım" ucu yok
/// (`taxi_calls` yalnız yönetici istatistiği), o yüzden liste yerelde tutuluyor.
///
/// **Neden kimlik değil anlık görüntü saklanıyor:** yalnız id saklansaydı bölüm
/// ancak sürücü o an yüklenmiş sayfada varsa çizilebilirdi (sayfalı liste →
/// güvenilmez). Ad/plaka anlık görüntüsü doğrudan çizilir; **telefon
/// saklanmaz** — arama yine `POST /drivers/{id}/call` ile yapılır, yani numara
/// her zaman sunucudan taze gelir ve çağrı sayacı işler.
@immutable
class RecentTaxiDriver {
  const RecentTaxiDriver({
    required this.id,
    required this.name,
    this.plaka,
    required this.calledAt,
  });

  final String id;
  final String name;
  final String? plaka;
  final DateTime calledAt;

  Map<String, dynamic> toJson() => {
    'id': id,
    'name': name,
    'plaka': plaka,
    'calledAt': calledAt.toUtc().toIso8601String(),
  };

  static RecentTaxiDriver? fromJson(Map<String, dynamic> json) {
    final id = json['id'];
    final name = json['name'];
    if (id is! String || id.isEmpty || name is! String || name.isEmpty) {
      return null;
    }
    return RecentTaxiDriver(
      id: id,
      name: name,
      plaka: json['plaka'] as String?,
      calledAt: DateTime.tryParse((json['calledAt'] as String?) ?? '') ??
          DateTime.now().toUtc(),
    );
  }
}

class RecentTaxiCallsStore {
  RecentTaxiCallsStore(this._prefs);

  static const _key = 'taxis.recentCalls';

  /// Üçten fazlası liste ekranının üstünü kaplar ve "son" olma anlamını yitirir.
  static const maxItems = 3;

  final SharedPreferences _prefs;

  List<RecentTaxiDriver> read() {
    final raw = _prefs.getString(_key);
    if (raw == null || raw.isEmpty) return const [];
    try {
      final decoded = jsonDecode(raw);
      if (decoded is! List) return const [];
      return [
        for (final item in decoded)
          if (item is Map)
            ?RecentTaxiDriver.fromJson(Map<String, dynamic>.from(item)),
      ];
    } catch (_) {
      // Bozuk kayıt kullanıcının işini engellemesin.
      return const [];
    }
  }

  /// En başa ekler; aynı sürücü zaten varsa yalnız öne alınır (mükerrer yok).
  Future<List<RecentTaxiDriver>> remember(TaxiDriver driver) async {
    final entry = RecentTaxiDriver(
      id: driver.id,
      name: driver.name,
      plaka: driver.plateLabel,
      calledAt: DateTime.now().toUtc(),
    );
    final next = [
      entry,
      ...read().where((item) => item.id != driver.id),
    ].take(maxItems).toList(growable: false);

    await _prefs.setString(
      _key,
      jsonEncode([for (final item in next) item.toJson()]),
    );
    return next;
  }

  Future<void> clear() => _prefs.remove(_key);
}

final recentTaxiCallsStoreProvider = Provider<RecentTaxiCallsStore>(
  (ref) => RecentTaxiCallsStore(ref.watch(sharedPreferencesProvider)),
);

/// Son aranan taksiciler — çağrıdan sonra denetleyici tarafından güncellenir.
class RecentTaxiCallsController extends Notifier<List<RecentTaxiDriver>> {
  @override
  List<RecentTaxiDriver> build() =>
      ref.watch(recentTaxiCallsStoreProvider).read();

  Future<void> remember(TaxiDriver driver) async {
    final next = await ref.read(recentTaxiCallsStoreProvider).remember(driver);
    if (ref.mounted) state = next;
  }

  Future<void> clear() async {
    await ref.read(recentTaxiCallsStoreProvider).clear();
    if (ref.mounted) state = const [];
  }
}

final recentTaxiCallsProvider =
    NotifierProvider<RecentTaxiCallsController, List<RecentTaxiDriver>>(
      RecentTaxiCallsController.new,
    );
