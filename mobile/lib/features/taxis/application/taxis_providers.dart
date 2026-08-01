import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/paging/paged_feed.dart';
import '../../../core/utils/utils.dart';
import '../data/models/taxi_driver.dart';
import '../data/recent_taxi_calls_store.dart';
import '../data/taxis_repository.dart';

/// Tek sürücü (detay + 11.13 deep-link).
final taxiDriverProvider = FutureProvider.autoDispose.family<TaxiDriver, String>(
  (ref, id) => ref.watch(taxisRepositoryProvider).driver(id),
  retry: apiRetry,
);

/// Sürücü listesi — filtre yalnız arama metni (sunucu ad **ve plakada** arar).
typedef TaxisFeedState = PagedFeedState<TaxiDriver, String>;

class TaxisFeedController extends PagedFeedController<TaxiDriver, String> {
  @override
  String get initialFilter => '';

  @override
  Future<PagedResult<TaxiDriver>> fetchPage({
    required int page,
    required int limit,
    required String filter,
  }) => ref
      .read(taxisRepositoryProvider)
      .drivers(page: page, limit: limit, search: filter);

  @override
  String idOf(TaxiDriver item) => item.id;

  void search(String term) => applyFilter(term.trim());

  void clearFilters() => applyFilter('');
}

final taxisFeedProvider =
    NotifierProvider<TaxisFeedController, TaxisFeedState>(
      TaxisFeedController.new,
    );

// --- Arama (çağrı) akışı ---

enum TaxiCallStatus {
  /// Çağrı kaydedildi ve çevirici açıldı.
  opened,

  /// Çağrı kaydı yapılamadı ama kullanıcı yine de aranabildi.
  openedWithoutTracking,

  /// Telefon çeviricisi açılamadı (cihaz/emülatör kısıtı).
  dialerUnavailable,

  /// Ne çağrı kaydı ne arama yapılabildi.
  failed,
}

@immutable
class TaxiCallResult {
  const TaxiCallResult(this.status, {this.error});

  final TaxiCallStatus status;
  final ApiException? error;

  bool get isSuccess =>
      status == TaxiCallStatus.opened ||
      status == TaxiCallStatus.openedWithoutTracking;

  /// Kullanıcıya gösterilecek bilgi; her şey yolundaysa `null`
  /// (başarıda şerit göstermek gereksiz gürültü — çevirici zaten açıldı).
  String? get message => switch (status) {
    TaxiCallStatus.opened => null,
    TaxiCallStatus.openedWithoutTracking =>
      'Çağrı kaydı oluşturulamadı, numara doğrudan aranıyor.',
    TaxiCallStatus.dialerUnavailable => 'Telefon uygulaması açılamadı.',
    TaxiCallStatus.failed => error?.message ?? 'Arama başlatılamadı.',
  };
}

/// Taksi çağırma akışı. Aynı anda tek çağrı; işlem sürerken hangi sürücünün
/// butonunun döneceğini bilmek için durum = o sürücünün kimliği.
///
/// **Dayanıklılık kararı:** uç 5xx/ağ hatası verirse çağrı kaydı tutulamaz ama
/// kullanıcı taksiye ihtiyaç duyuyor → listeden gelen telefonla arama yine
/// denenir ve kullanıcıya sebebi **yazılır** (sessiz başarısızlık yok).
class TaxiCallController extends Notifier<String?> {
  @override
  String? build() => null;

  Future<TaxiCallResult> call(TaxiDriver driver) async {
    if (state != null) {
      return const TaxiCallResult(TaxiCallStatus.failed);
    }
    state = driver.id;
    try {
      final phone = await ref
          .read(taxisRepositoryProvider)
          .call(driver.id);
      if (!ref.mounted) return const TaxiCallResult(TaxiCallStatus.failed);

      await ref.read(recentTaxiCallsProvider.notifier).remember(driver);
      final opened = await AppLinks.call(phone);
      return TaxiCallResult(
        opened ? TaxiCallStatus.opened : TaxiCallStatus.dialerUnavailable,
      );
    } on ApiException catch (error) {
      if (driver.hasPhone) {
        final opened = await AppLinks.call(driver.phone);
        if (opened) {
          return TaxiCallResult(
            TaxiCallStatus.openedWithoutTracking,
            error: error,
          );
        }
      }
      return TaxiCallResult(TaxiCallStatus.failed, error: error);
    } finally {
      if (ref.mounted) state = null;
    }
  }
}

final taxiCallProvider = NotifierProvider<TaxiCallController, String?>(
  TaxiCallController.new,
);
