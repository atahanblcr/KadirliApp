import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../network/network.dart';

/// Sayfalı + filtrelenebilir liste akışının **ortak durumu**.
///
/// **Neden `AsyncValue` değil** (11.6'da öğrenildi): sonsuz kaydırmada üç ayrı
/// yükleme hâli var — ilk sayfa / sonraki sayfa / tazeleme — ve **sonraki sayfa
/// hatası ekrandaki kayıtları silmemeli**. `AsyncValue` tek bir loading/error
/// taşıdığı için bu ayrımı ifade edemiyor.
///
/// [T] öğe tipi, [F] filtre tipi (duyurularda `String?` typeId, rehberde
/// kategori+arama taşıyan küçük bir değer nesnesi). Filtre **durumun içinde**
/// tutuluyor ki filtre şeridi `select` ile yeniden çizilebilsin.
@immutable
class PagedFeedState<T, F> {
  const PagedFeedState({
    required this.filter,
    this.items = const [],
    this.isLoadingFirstPage = true,
    this.isLoadingMore = false,
    this.hasMore = false,
    this.totalCount = 0,
    this.error,
    this.loadMoreError,
  });

  final F filter;

  final List<T> items;

  /// İlk sayfa (ya da filtre değişimi) yükleniyor → skeleton.
  final bool isLoadingFirstPage;

  /// Liste sonunda sonraki sayfa yükleniyor → alt gösterge.
  final bool isLoadingMore;

  final bool hasMore;
  final int totalCount;

  /// İlk sayfa hatası → tüm ekran hata durumuna geçer.
  final ApiException? error;

  /// Sonraki sayfa hatası → liste kalır, altta "Devamını yükle" çıkar.
  final ApiException? loadMoreError;

  /// Yükleme bitti, hata yok ve gerçekten kayıt yok.
  bool get isEmpty => items.isEmpty && !isLoadingFirstPage && error == null;

  PagedFeedState<T, F> copyWith({
    F? filter,
    List<T>? items,
    bool? isLoadingFirstPage,
    bool? isLoadingMore,
    bool? hasMore,
    int? totalCount,
    ApiException? error,
    bool clearError = false,
    ApiException? loadMoreError,
    bool clearLoadMoreError = false,
  }) => PagedFeedState<T, F>(
    filter: filter ?? this.filter,
    items: items ?? this.items,
    isLoadingFirstPage: isLoadingFirstPage ?? this.isLoadingFirstPage,
    isLoadingMore: isLoadingMore ?? this.isLoadingMore,
    hasMore: hasMore ?? this.hasMore,
    totalCount: totalCount ?? this.totalCount,
    error: clearError ? null : (error ?? this.error),
    loadMoreError: clearLoadMoreError ? null : (loadMoreError ?? this.loadMoreError),
  );
}

/// Sayfalı liste denetleyicisinin ortak davranışı: ilk sayfa, sonraki sayfa,
/// tazeleme, filtre değişimi ve **yarış durumu** yönetimi.
///
/// Alt sınıf yalnız üç şeyi söyler: başlangıç filtresi, bir sayfanın nasıl
/// çekileceği ve öğenin kimliği (mükerrer eleme için).
///
/// Filtre değişimi listeyi **sıfırlayan** bir işlem olduğu için ayrı bir
/// provider'a bölünmedi; geç dönen eski yanıt [_requestId] ile eleniyor.
abstract class PagedFeedController<T, F>
    extends Notifier<PagedFeedState<T, F>> {
  int _requestId = 0;
  int _page = 1;

  /// Sayfa boyutu (public uçlarda sunucu 50'ye kırpar).
  int get pageSize => 20;

  /// İlk açılıştaki filtre (genelde "filtre yok").
  F get initialFilter;

  /// Bir sayfayı çeker. Ağ hatası [ApiException] olarak yukarı çıkmalı.
  Future<PagedResult<T>> fetchPage({
    required int page,
    required int limit,
    required F filter,
  });

  /// Mükerrer eleme anahtarı — araya yeni kayıt girerse sunucu aynı öğeyi iki
  /// sayfada döndürebilir.
  String idOf(T item);

  @override
  PagedFeedState<T, F> build() {
    // İlk sayfa `build` içinde beklenmez: provider kurulumu senkron kalmalı.
    // ⚠️ `ref.mounted` kontrolü şart: provider mikro-görev çalışmadan atılırsa
    // (test kapsayıcısının kapanması, `autoDispose`) `ref` kullanmak
    // "Cannot use the Ref ... after it has been disposed" hatası veriyordu.
    Future.microtask(() {
      if (ref.mounted) loadFirstPage();
    });
    return PagedFeedState<T, F>(filter: initialFilter);
  }

  /// Filtreyi değiştirir ve listeyi baştan yükler (aynı filtre → no-op).
  void applyFilter(F filter) {
    if (filter == state.filter) return;
    state = PagedFeedState<T, F>(filter: filter);
    loadFirstPage();
  }

  /// Pull-to-refresh — mevcut kayıtlar yenisi gelene kadar ekranda kalır.
  Future<void> refresh() => loadFirstPage(keepItems: true);

  /// İlk sayfa hatasından sonra "Tekrar dene".
  Future<void> retry() => loadFirstPage();

  Future<void> loadFirstPage({bool keepItems = false}) async {
    if (!ref.mounted) return;
    final token = ++_requestId;
    _page = 1;
    state = state.copyWith(
      items: keepItems ? state.items : const [],
      isLoadingFirstPage: !keepItems || state.items.isEmpty,
      isLoadingMore: false,
      clearError: true,
      clearLoadMoreError: true,
    );

    try {
      final result = await fetchPage(
        page: 1,
        limit: pageSize,
        filter: state.filter,
      );
      // Atılmış provider'ın durumunu yazmak da hata verir.
      if (token != _requestId || !ref.mounted) return; // filtre değişti / yeni tazeleme
      state = state.copyWith(
        items: result.items,
        isLoadingFirstPage: false,
        hasMore: result.hasNextPage,
        totalCount: result.totalCount,
        clearError: true,
      );
    } on ApiException catch (error) {
      if (token != _requestId || !ref.mounted) return;
      state = state.copyWith(
        isLoadingFirstPage: false,
        items: keepItems ? state.items : const [],
        error: error,
      );
    }
  }

  /// Liste sonuna gelindiğinde bir sonraki sayfa.
  Future<void> loadMore() async {
    if (state.isLoadingMore ||
        state.isLoadingFirstPage ||
        !state.hasMore ||
        !ref.mounted) {
      return;
    }
    final token = _requestId;
    state = state.copyWith(isLoadingMore: true, clearLoadMoreError: true);

    try {
      final result = await fetchPage(
        page: _page + 1,
        limit: pageSize,
        filter: state.filter,
      );
      if (token != _requestId || !ref.mounted) return;
      _page += 1;
      state = state.copyWith(
        items: _mergeUnique(state.items, result.items),
        isLoadingMore: false,
        hasMore: result.hasNextPage,
        totalCount: result.totalCount,
      );
    } on ApiException catch (error) {
      if (token != _requestId || !ref.mounted) return;
      state = state.copyWith(isLoadingMore: false, loadMoreError: error);
    }
  }

  List<T> _mergeUnique(List<T> current, List<T> incoming) {
    final seen = current.map(idOf).toSet();
    return [
      ...current,
      ...incoming.where((item) => seen.add(idOf(item))),
    ];
  }
}
