import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/paging/paged_feed.dart';

/// Ortak sayfalama çekirdeği (`core/paging/paged_feed.dart`) — 11.7'de
/// çıkarıldı, bugün **dört modül** (duyurular, eczane, rehber, ilanlar) buna
/// dayanıyor.
///
/// Modül testleri sayfalama/filtre davranışını dolaylı olarak doğruluyor ama
/// çekirdeğin en ince kısmı olan **yarış durumu** (`_requestId`) hiçbir yerde
/// test edilmiyordu: geç dönen eski yanıt ekranı bozmamalı. Bu dosya tam olarak
/// onu kilitliyor.
void main() {
  /// Testin elle kontrol ettiği sahte kaynak: her istek bir `Completer`
  /// döndürür, sonucu test istediği anda verir → yanıt sırası tersine
  /// çevrilebilir.
  late List<_Request> requests;

  setUp(() => requests = []);

  PagedResult<String> page(
    List<String> items, {
    int currentPage = 1,
    int totalPages = 1,
  }) => PagedResult<String>(
    items: items,
    totalCount: items.length,
    pageSize: 20,
    currentPage: currentPage,
    totalPages: totalPages,
  );

  final provider = NotifierProvider<_TestFeed, PagedFeedState<String, String>>(
    () => _TestFeed((pageNumber, filter) {
      final request = _Request(pageNumber, filter);
      requests.add(request);
      return request.completer.future;
    }),
  );

  Future<void> tick() async {
    for (var i = 0; i < 5; i++) {
      await Future<void>.delayed(Duration.zero);
    }
  }

  test('geç dönen ESKİ filtrenin yanıtı yeni listeyi ezmez', () async {
    final container = ProviderContainer();
    addTearDown(container.dispose);

    container.read(provider);
    await tick();
    expect(requests.single.filter, 'A', reason: 'ilk sayfa başlangıç filtresi');

    // Kullanıcı ilk yanıt gelmeden filtreyi değiştirdi.
    container.read(provider.notifier).applyFilter('B');
    await tick();
    expect(requests.length, 2);

    // Önce YENİ filtre cevaplandı…
    requests[1].completer.complete(page(['b1', 'b2']));
    await tick();
    expect(container.read(provider).items, ['b1', 'b2']);

    // …sonra eski istek geç geldi: yok sayılmalı.
    requests[0].completer.complete(page(['a1', 'a2', 'a3']));
    await tick();

    final state = container.read(provider);
    expect(state.items, ['b1', 'b2'], reason: 'eski yanıt listeyi ezmemeli');
    expect(state.filter, 'B');
    expect(state.isLoadingFirstPage, isFalse);
  });

  test('geç dönen eski tazelemenin yanıtı da elenir', () async {
    final container = ProviderContainer();
    addTearDown(container.dispose);

    container.read(provider);
    await tick();
    requests[0].completer.complete(page(['ilk']));
    await tick();

    final notifier = container.read(provider.notifier);
    unawaited(notifier.refresh()); // 1. tazeleme (yavaş)
    await tick();
    unawaited(notifier.refresh()); // 2. tazeleme (hızlı)
    await tick();
    expect(requests.length, 3);

    requests[2].completer.complete(page(['yeni']));
    await tick();
    requests[1].completer.complete(page(['eski']));
    await tick();

    expect(container.read(provider).items, ['yeni']);
  });

  test('loadMore sürerken filtre değişirse gelen sayfa eklenmez', () async {
    final container = ProviderContainer();
    addTearDown(container.dispose);

    container.read(provider);
    await tick();
    requests[0].completer.complete(
      page(['a1'], currentPage: 1, totalPages: 2),
    );
    await tick();

    final notifier = container.read(provider.notifier);
    unawaited(notifier.loadMore());
    await tick();
    expect(requests.length, 2);
    expect(requests[1].page, 2);

    notifier.applyFilter('B'); // kullanıcı beklerken filtreyi değiştirdi
    await tick();
    requests[2].completer.complete(page(['b1']));
    await tick();

    // 2. sayfa geç geldi: B listesine A'nın kayıtları eklenmemeli.
    requests[1].completer.complete(page(['a2'], currentPage: 2, totalPages: 2));
    await tick();

    expect(container.read(provider).items, ['b1']);
    expect(container.read(provider).isLoadingMore, isFalse);
  });

  test('aynı filtre yeniden uygulanınca istek atılmaz', () async {
    final container = ProviderContainer();
    addTearDown(container.dispose);

    container.read(provider);
    await tick();
    requests[0].completer.complete(page(['a1']));
    await tick();

    container.read(provider.notifier).applyFilter('A');
    await tick();

    expect(requests.length, 1, reason: 'no-op olmalı');
  });

  test('tazeleme hatası ekrandaki kayıtları SİLMEZ', () async {
    final container = ProviderContainer();
    addTearDown(container.dispose);

    container.read(provider);
    await tick();
    requests[0].completer.complete(page(['a1', 'a2']));
    await tick();

    unawaited(container.read(provider.notifier).refresh());
    await tick();
    requests[1].completer.completeError(
      ApiException(code: ApiErrorCodes.networkError),
    );
    await tick();

    final state = container.read(provider);
    expect(
      state.items,
      ['a1', 'a2'],
      reason: 'pull-to-refresh başarısız olunca liste boşalmamalı',
    );
    expect(state.error, isNotNull, reason: 'hata yine de bildirilmeli');
  });

  test('filtre değişimi hatası listeyi temizler (eski filtrenin sonucu kalmaz)',
      () async {
    final container = ProviderContainer();
    addTearDown(container.dispose);

    container.read(provider);
    await tick();
    requests[0].completer.complete(page(['a1']));
    await tick();

    container.read(provider.notifier).applyFilter('B');
    await tick();
    requests[1].completer.completeError(
      ApiException(code: ApiErrorCodes.internalError),
    );
    await tick();

    final state = container.read(provider);
    expect(state.items, isEmpty, reason: 'B filtresinin sonucu A değildir');
    expect(state.error, isNotNull);
  });

  test('mükerrer kayıt elenir, sıra korunur', () async {
    final container = ProviderContainer();
    addTearDown(container.dispose);

    container.read(provider);
    await tick();
    requests[0].completer.complete(
      page(['x', 'y'], currentPage: 1, totalPages: 2),
    );
    await tick();

    unawaited(container.read(provider.notifier).loadMore());
    await tick();
    // Araya yeni kayıt girmiş: sunucu 'y'yi ikinci sayfada tekrar döndürdü.
    requests[1].completer.complete(
      page(['y', 'z'], currentPage: 2, totalPages: 2),
    );
    await tick();

    expect(container.read(provider).items, ['x', 'y', 'z']);
    expect(container.read(provider).hasMore, isFalse);
  });

  test('son sayfadayken loadMore yeni istek atmaz', () async {
    final container = ProviderContainer();
    addTearDown(container.dispose);

    container.read(provider);
    await tick();
    requests[0].completer.complete(page(['x']));
    await tick();

    await container.read(provider.notifier).loadMore();
    await tick();

    expect(requests.length, 1);
  });
}

class _Request {
  _Request(this.page, this.filter);

  final int page;
  final String filter;
  final completer = Completer<PagedResult<String>>();
}

class _TestFeed extends PagedFeedController<String, String> {
  _TestFeed(this._fetch);

  final Future<PagedResult<String>> Function(int page, String filter) _fetch;

  @override
  String get initialFilter => 'A';

  @override
  Future<PagedResult<String>> fetchPage({
    required int page,
    required int limit,
    required String filter,
  }) => _fetch(page, filter);

  @override
  String idOf(String item) => item;
}
