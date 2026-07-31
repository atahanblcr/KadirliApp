import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../data/announcements_repository.dart';
import '../data/models/announcement.dart';
import '../data/models/announcement_type.dart';

/// Duyuru türleri (filtre chip'leri). Nadiren değişir → `autoDispose` yok.
final announcementTypesProvider = FutureProvider<List<AnnouncementType>>(
  (ref) => ref.watch(announcementsRepositoryProvider).types(),
  retry: apiRetry,
);

/// Tek duyuru (detay ekranı ve 11.13 push deep-link'i).
///
/// `autoDispose`: detaydan çıkınca bellekte tutmanın anlamı yok, geri dönüldüğünde
/// taze veri istenir (görüntülenme sayacı da yeniden tetiklenir).
final announcementDetailProvider = FutureProvider.autoDispose
    .family<Announcement, String>(
      (ref, id) => ref.watch(announcementsRepositoryProvider).detail(id),
      retry: apiRetry,
    );

/// Sayfalı duyuru akışının durumu.
///
/// Neden `AsyncValue` değil: sonsuz kaydırmada **üç** yükleme durumu var (ilk
/// sayfa / sonraki sayfa / tazeleme) ve ikinci sayfa hatası ekrandaki mevcut
/// kayıtları silmemeli. `AsyncValue` tek bir "loading/error" taşıdığı için
/// bunları ayırt edemiyor.
@immutable
class AnnouncementFeedState {
  const AnnouncementFeedState({
    this.typeId,
    this.items = const [],
    this.isLoadingFirstPage = true,
    this.isLoadingMore = false,
    this.hasMore = false,
    this.totalCount = 0,
    this.error,
    this.loadMoreError,
  });

  /// Seçili tür filtresi (`null` = tümü).
  final String? typeId;

  final List<Announcement> items;

  /// İlk sayfa (ya da filtre değişimi) yükleniyor → skeleton.
  final bool isLoadingFirstPage;

  /// Liste sonunda sonraki sayfa yükleniyor → alt spinner.
  final bool isLoadingMore;

  final bool hasMore;
  final int totalCount;

  /// İlk sayfa hatası → tüm ekran hata durumuna geçer.
  final ApiException? error;

  /// Sonraki sayfa hatası → liste kalır, altta "tekrar dene" satırı çıkar.
  final ApiException? loadMoreError;

  bool get isEmpty => items.isEmpty && !isLoadingFirstPage && error == null;

  /// Boş sonuç filtre yüzünden mi (mesajı ona göre yazmak için)?
  bool get isFiltered => typeId != null;

  AnnouncementFeedState copyWith({
    String? typeId,
    bool resetTypeId = false,
    List<Announcement>? items,
    bool? isLoadingFirstPage,
    bool? isLoadingMore,
    bool? hasMore,
    int? totalCount,
    ApiException? error,
    bool clearError = false,
    ApiException? loadMoreError,
    bool clearLoadMoreError = false,
  }) => AnnouncementFeedState(
    typeId: resetTypeId ? null : (typeId ?? this.typeId),
    items: items ?? this.items,
    isLoadingFirstPage: isLoadingFirstPage ?? this.isLoadingFirstPage,
    isLoadingMore: isLoadingMore ?? this.isLoadingMore,
    hasMore: hasMore ?? this.hasMore,
    totalCount: totalCount ?? this.totalCount,
    error: clearError ? null : (error ?? this.error),
    loadMoreError: clearLoadMoreError ? null : (loadMoreError ?? this.loadMoreError),
  );
}

/// Duyuru listesinin tek denetleyicisi: filtre + sayfalama + tazeleme.
///
/// Filtre ayrı bir provider'da tutulmuyor; tür değişimi listeyi **sıfırlayan**
/// bir işlem olduğu için aynı denetleyicide durması akışı tek yerde tutuyor
/// (yarış durumu: eski isteğin geç dönen yanıtı [_requestId] ile elenir).
class AnnouncementFeedController extends Notifier<AnnouncementFeedState> {
  static const pageSize = 20;

  int _requestId = 0;
  int _page = 1;

  @override
  AnnouncementFeedState build() {
    Future.microtask(_loadFirstPage);
    return const AnnouncementFeedState();
  }

  AnnouncementsRepository get _repository =>
      ref.read(announcementsRepositoryProvider);

  /// Tür filtresi seç/kaldır (aynı türe tekrar dokunmak filtreyi kaldırır).
  void selectType(String? typeId) {
    final next = state.typeId == typeId ? null : typeId;
    if (next == state.typeId) return;
    state = AnnouncementFeedState(typeId: next);
    _loadFirstPage();
  }

  /// Pull-to-refresh — mevcut kayıtlar ekranda kalır, yerine yenisi gelir.
  Future<void> refresh() => _loadFirstPage(keepItems: true);

  /// İlk sayfa hatasından sonra "Tekrar dene".
  Future<void> retry() => _loadFirstPage();

  Future<void> _loadFirstPage({bool keepItems = false}) async {
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
      final result = await _repository.list(
        page: 1,
        limit: pageSize,
        typeId: state.typeId,
      );
      if (token != _requestId) return; // filtre değişti / yeni tazeleme geldi
      state = state.copyWith(
        items: result.items,
        isLoadingFirstPage: false,
        hasMore: result.hasNextPage,
        totalCount: result.totalCount,
        clearError: true,
      );
    } on ApiException catch (error) {
      if (token != _requestId) return;
      state = state.copyWith(
        isLoadingFirstPage: false,
        items: keepItems ? state.items : const [],
        error: error,
      );
    }
  }

  /// Liste sonuna gelindiğinde bir sonraki sayfa.
  Future<void> loadMore() async {
    if (state.isLoadingMore || state.isLoadingFirstPage || !state.hasMore) {
      return;
    }
    final token = _requestId;
    state = state.copyWith(isLoadingMore: true, clearLoadMoreError: true);

    try {
      final result = await _repository.list(
        page: _page + 1,
        limit: pageSize,
        typeId: state.typeId,
      );
      if (token != _requestId) return;
      _page += 1;
      state = state.copyWith(
        // Sunucu aynı kaydı iki sayfada döndürebilir (araya yeni duyuru
        // girerse) → id'ye göre tekilleştirilir, liste "çift kayıt" göstermez.
        items: _mergeUnique(state.items, result.items),
        isLoadingMore: false,
        hasMore: result.hasNextPage,
        totalCount: result.totalCount,
      );
    } on ApiException catch (error) {
      if (token != _requestId) return;
      state = state.copyWith(isLoadingMore: false, loadMoreError: error);
    }
  }

  static List<Announcement> _mergeUnique(
    List<Announcement> current,
    List<Announcement> incoming,
  ) {
    final seen = current.map((item) => item.id).toSet();
    return [
      ...current,
      ...incoming.where((item) => seen.add(item.id)),
    ];
  }
}

final announcementFeedProvider =
    NotifierProvider<AnnouncementFeedController, AnnouncementFeedState>(
      AnnouncementFeedController.new,
    );
