import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../data/models/power_outage.dart';
import '../data/power_outages_repository.dart';

/// Tüm kesintiler — uç sayfasız ve tarih filtresiz olduğu için **tek istek**
/// her şeyi getirir; süren/planlı/geçmiş ayrımı istemcide yapılır
/// (11.4 kararı, Progress 10.x'te kontrat bilinçli olarak donduruldu).
///
/// Ana Sayfa şeridi de bunu türetir → hub'dan modüle girmek yeni istek atmaz.
final allPowerOutagesProvider = FutureProvider<List<PowerOutage>>(
  (ref) => ref.watch(powerOutagesRepositoryProvider).all(),
  retry: apiRetry,
);

/// Liste ekranının sekmesi.
enum PowerOutageTab {
  /// Süren + planlanan (varsayılan — kullanıcıyı ilgilendiren).
  current,

  /// Bitmiş kesintiler.
  past,
}

/// Seçili sekme.
class PowerOutageTabController extends Notifier<PowerOutageTab> {
  @override
  PowerOutageTab build() => PowerOutageTab.current;

  void select(PowerOutageTab tab) => state = tab;
}

final powerOutageTabProvider =
    NotifierProvider<PowerOutageTabController, PowerOutageTab>(
      PowerOutageTabController.new,
    );

/// "Sadece mahallem" anahtarı — oturum açmış ve mahallesi olan kullanıcıda
/// anlamlı (ekran anahtarı yoksa hiç göstermez).
class OnlyMyNeighborhoodController extends Notifier<bool> {
  @override
  bool build() => false;

  // ignore: avoid_positional_boolean_parameters
  void set(bool value) => state = value;

  void toggle() => state = !state;
}

final onlyMyNeighborhoodProvider =
    NotifierProvider<OnlyMyNeighborhoodController, bool>(
      OnlyMyNeighborhoodController.new,
    );

/// Tek kesinti (detay ekranı + 11.13 deep-link).
final powerOutageDetailProvider = FutureProvider.autoDispose
    .family<PowerOutage, String>(
      (ref, id) => ref.watch(powerOutagesRepositoryProvider).detail(id),
      retry: apiRetry,
    );

/// Ekranda gösterilecek gruplanmış liste.
@immutable
class PowerOutageGroups {
  const PowerOutageGroups({
    this.active = const [],
    this.upcoming = const [],
    this.past = const [],
    this.hiddenByNeighborhood = 0,
  });

  /// Şu an süren kesintiler (en erken biten önce).
  final List<PowerOutage> active;

  /// Henüz başlamamışlar (en yakın önce).
  final List<PowerOutage> upcoming;

  /// Bitmişler (en yeni önce).
  final List<PowerOutage> past;

  /// "Sadece mahallem" yüzünden gizlenen kayıt sayısı — kullanıcı listeyi boş
  /// görüp "veri yok" sanmasın diye ekranda yazılır.
  final int hiddenByNeighborhood;

  bool get hasCurrent => active.isNotEmpty || upcoming.isNotEmpty;

  int get currentCount => active.length + upcoming.length;

  int get pastCount => past.length;

  /// Sunucudan gelen ham listeyi gruplara ayırır.
  ///
  /// [neighborhood] verilirse yalnız o mahallenin (ve mahalle bilgisi olmayan
  /// = şehir geneli) kayıtları kalır: şehir geneli kesinti herkesi ilgilendirir,
  /// filtrelenip saklanması yanlış olurdu.
  factory PowerOutageGroups.from(
    List<PowerOutage> outages, {
    DateTime? now,
    String? neighborhood,
  }) {
    final reference = now ?? DateTime.now();

    var hidden = 0;
    final visible = <PowerOutage>[];
    for (final outage in outages) {
      final cityWide = (outage.neighborhood?.trim().isEmpty ?? true);
      if (neighborhood != null &&
          !cityWide &&
          !outage.matchesNeighborhood(neighborhood)) {
        hidden++;
        continue;
      }
      visible.add(outage);
    }

    final active = <PowerOutage>[];
    final upcoming = <PowerOutage>[];
    final past = <PowerOutage>[];
    for (final outage in visible) {
      switch (outage.status(now: reference)) {
        case PowerOutageStatus.active:
          active.add(outage);
        case PowerOutageStatus.upcoming:
          upcoming.add(outage);
        case PowerOutageStatus.past:
          past.add(outage);
      }
    }

    active.sort((a, b) => a.endTime.compareTo(b.endTime));
    upcoming.sort((a, b) => a.startTime.compareTo(b.startTime));
    past.sort((a, b) => b.startTime.compareTo(a.startTime));

    return PowerOutageGroups(
      active: active,
      upcoming: upcoming,
      past: past,
      hiddenByNeighborhood: hidden,
    );
  }
}
