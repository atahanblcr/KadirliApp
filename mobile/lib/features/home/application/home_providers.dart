import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../announcements/data/announcements_repository.dart';
import '../../announcements/data/models/announcement.dart';
import '../../notifications/application/unread_count_provider.dart';
import '../../pharmacies/data/models/on_duty_pharmacy.dart';
import '../../pharmacies/data/pharmacies_repository.dart';
import '../../power_outages/data/models/power_outage.dart';
import '../../power_outages/data/power_outages_repository.dart';

/// Ana Sayfa'nın üç veri kaynağı **ayrı** provider'lar: biri patlarsa diğerleri
/// görünmeye devam eder (tek bir "hub isteği" olsaydı nöbetçi eczane hatası
/// duyuruları da götürürdü).
///
/// `autoDispose` **yok**: sekmeler arası gidip gelmek yeniden istek atmasın;
/// tazeleme pull-to-refresh ile açıkça yapılır ([refreshHome]).

/// Bugünün nöbetçi eczaneleri (aynı güne birden fazla atanabilir).
final onDutyPharmaciesProvider = FutureProvider<List<OnDutyPharmacy>>(
  (ref) => ref.watch(pharmaciesRepositoryProvider).onDuty(),
  retry: apiRetry,
);

/// Süren + planlanan elektrik kesintileri (en yakın önce).
final relevantOutagesProvider = FutureProvider<List<PowerOutage>>(
  (ref) => ref.watch(powerOutagesRepositoryProvider).relevant(),
  retry: apiRetry,
);

/// Ana Sayfa vitrinindeki son duyurular.
final latestAnnouncementsProvider = FutureProvider<List<Announcement>>(
  (ref) => ref.watch(announcementsRepositoryProvider).latest(),
  retry: apiRetry,
);

/// Pull-to-refresh: üçünü birden tazeler ve **hepsi bitene kadar** bekler
/// (RefreshIndicator'ın dönmesi gerçek işi yansıtsın).
///
/// Hatalar burada yutulur: her kart kendi hata durumunu zaten gösteriyor,
/// refresh göstergesinin patlaması gerekmez.
Future<void> refreshHome(WidgetRef ref) async {
  ref.invalidate(onDutyPharmaciesProvider);
  ref.invalidate(relevantOutagesProvider);
  ref.invalidate(latestAnnouncementsProvider);
  // Rozet de tazelenir: kullanıcı Ana Sayfa'yı aşağı çekince "her şey
  // yenilendi" bekliyor, alt sekmedeki sayının bayat kalması tutarsız olurdu.
  // (Anonimken zaten istek atılmaz.)
  ref.invalidate(unreadNotificationCountProvider);

  await Future.wait([
    _ignoreError(ref.read(onDutyPharmaciesProvider.future)),
    _ignoreError(ref.read(relevantOutagesProvider.future)),
    _ignoreError(ref.read(latestAnnouncementsProvider.future)),
    _ignoreError(ref.read(unreadNotificationCountProvider.future)),
  ]);
}

Future<void> _ignoreError(Future<Object?> future) async {
  try {
    await future;
  } on Object {
    // Yoksayılır — durum provider'da, gösterim kartta.
  }
}
