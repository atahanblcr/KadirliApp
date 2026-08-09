import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/paging/paged_feed.dart';
import '../data/models/intercity_route.dart';
import '../data/models/intracity_route.dart';
import '../data/transport_repository.dart';
import 'transport_vehicle.dart';

/// Ulaşım ekranının iki sekmesi.
enum TransportTab {
  /// Şehirlerarası otobüs kalkışları (varsayılan — "Adana otobüsü kaçta?"
  /// bu modüle gelme sebebi).
  intercity,

  /// Şehir içi hatlar ve durakları.
  intracity,
}

class TransportTabController extends Notifier<TransportTab> {
  @override
  TransportTab build() => TransportTab.intercity;

  void select(TransportTab tab) => state = tab;
}

final transportTabProvider =
    NotifierProvider<TransportTabController, TransportTab>(
      TransportTabController.new,
    );

// ------------------------------------------------------- Şehirlerarası liste

/// Şehirlerarası listenin filtresi: arama **ve** araç tipi birlikte uygulanır
/// (rehberdeki `GuideFilter` deseni).
///
/// 🔑 İkisi tek nesnede: ayrı tutulsalardı araç şeridine dokunmak aramayı
/// sessizce düşürürdü — panelde 12.5'te tam bu yüzden arama ve araç süzgeci
/// **tek forma** alınmıştı; mobilde karşılığı budur.
@immutable
class IntercityFilter {
  const IntercityFilter({
    this.search = '',
    this.vehicle = TransportVehicle.all,
  });

  final String search;
  final TransportVehicle vehicle;

  IntercityFilter copyWith({String? search, TransportVehicle? vehicle}) =>
      IntercityFilter(
        search: search ?? this.search,
        vehicle: vehicle ?? this.vehicle,
      );

  @override
  bool operator ==(Object other) =>
      other is IntercityFilter &&
      other.search == search &&
      other.vehicle == vehicle;

  @override
  int get hashCode => Object.hash(search, vehicle);
}

typedef IntercityFeedState = PagedFeedState<IntercityRoute, IntercityFilter>;

/// ⚠️ Uç **sayfalı** (`PagedResult`) — planın "sayfasız düz liste olabilir"
/// uyarısının aksine; bu yüzden ortak [PagedFeedController] kullanılıyor ve
/// hat sayısı büyüse de ekran çalışır.
class IntercityFeedController
    extends PagedFeedController<IntercityRoute, IntercityFilter> {
  @override
  IntercityFilter get initialFilter => const IntercityFilter();

  @override
  Future<PagedResult<IntercityRoute>> fetchPage({
    required int page,
    required int limit,
    required IntercityFilter filter,
  }) => ref
      .read(transportRepositoryProvider)
      .intercity(
        page: page,
        limit: limit,
        search: filter.search,
        vehicleType: filter.vehicle.apiValue,
      );

  @override
  String idOf(IntercityRoute item) => item.id;

  void search(String term) =>
      applyFilter(state.filter.copyWith(search: term.trim()));

  void selectVehicle(TransportVehicle vehicle) =>
      applyFilter(state.filter.copyWith(vehicle: vehicle));

  /// "Aramayı temizle" **araç şeridine dokunmaz**: kullanıcı "Minibüs"ü seçip
  /// arama yaptıysa aramayı temizlemek onu otobüslere geri döndürmemeli.
  void clearSearch() => applyFilter(state.filter.copyWith(search: ''));

  void clearFilters() => applyFilter(const IntercityFilter());
}

final intercityFeedProvider =
    NotifierProvider<IntercityFeedController, IntercityFeedState>(
      IntercityFeedController.new,
    );

// ---------------------------------------------------------- Şehir içi liste

typedef IntracityFeedState = PagedFeedState<IntracityRoute, String>;

class IntracityFeedController
    extends PagedFeedController<IntracityRoute, String> {
  @override
  String get initialFilter => '';

  @override
  Future<PagedResult<IntracityRoute>> fetchPage({
    required int page,
    required int limit,
    required String filter,
  }) => ref
      .read(transportRepositoryProvider)
      .intracity(page: page, limit: limit, search: filter);

  @override
  String idOf(IntracityRoute item) => item.id;

  void search(String term) => applyFilter(term.trim());

  void clearFilters() => applyFilter('');
}

final intracityFeedProvider =
    NotifierProvider<IntracityFeedController, IntracityFeedState>(
      IntracityFeedController.new,
    );

/// Listede açık olan kartın kimliği (kalkış saatleri / durak listesi).
///
/// **KARAR:** ulaşımda detay ekranı yok — sunucuda `{id}` ucu da yok, saatler
/// ve duraklar zaten liste gövdesinde geliyor. Sahte bir detay rotası
/// (listeyi tarayıp id bulan) hem derin bağlantıda hem 2. sayfada kırılırdı.
/// Aynı anda tek kart açık kalır: iki hattın saatlerini yan yana okumak değil,
/// bir hattın saatini bulmak istiyoruz.
class ExpandedRouteController extends Notifier<String?> {
  @override
  String? build() => null;

  void toggle(String id) => state = state == id ? null : id;

  void collapse() => state = null;
}

final expandedIntercityProvider =
    NotifierProvider<ExpandedRouteController, String?>(
      ExpandedRouteController.new,
    );

final expandedIntracityProvider =
    NotifierProvider<ExpandedRouteController, String?>(
      ExpandedRouteController.new,
    );
