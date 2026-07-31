import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/features/announcements/application/announcements_providers.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// Duyuru akışı denetleyicisi (11.6) — sayfalama + tür filtresi.
///
/// Sayfalı liste `AsyncValue` yerine kendi durumunu taşıyor; bu testler
/// "ikinci sayfa hatası okunan içeriği silmez" gibi tam da bu yüzden var olan
/// davranışları kilitliyor.
void main() {
  Map<String, dynamic> item(String id, {String title = 'Duyuru'}) => {
    'id': id,
    'title': '$title $id',
    'body': 'İçerik',
    'typeId': 'type-1',
    'typeName': 'Belediye Duyurusu',
    'priority': 0,
    'status': 'active',
    'sentAt': '2026-07-30T09:00:00Z',
    'createdAt': '2026-07-30T09:00:00Z',
  };

  Map<String, dynamic> page(
    List<Map<String, dynamic>> items, {
    required int currentPage,
    required int totalPages,
    int? totalCount,
  }) => {
    'items': items,
    'totalCount': totalCount ?? items.length,
    'pageSize': 20,
    'currentPage': currentPage,
    'totalPages': totalPages,
  };

  /// Akış hazır olana kadar bekler (ilk sayfa isteği `build`'den sonra gider).
  Future<AnnouncementFeedState> settled(container) async {
    for (var i = 0; i < 20; i++) {
      final state =
          container.read(announcementFeedProvider) as AnnouncementFeedState;
      if (!state.isLoadingFirstPage) return state;
      await Future<void>.delayed(Duration.zero);
    }
    return container.read(announcementFeedProvider) as AnnouncementFeedState;
  }

  test('ilk sayfa yüklenir, sonraki sayfa varsa hasMore true olur', () async {
    final adapter = routedAdapter({
      '/v1/announcements': (_) async => jsonResponse(
        successEnvelope(
          page([item('1'), item('2')], currentPage: 1, totalPages: 3, totalCount: 45),
        ),
      ),
    });
    final container = await testContainer(adapter: adapter);

    final state = await settled(container);

    expect(state.items, hasLength(2));
    expect(state.hasMore, isTrue);
    expect(state.totalCount, 45);
    expect(state.error, isNull);
  });

  test('loadMore ikinci sayfayı ekler ve mükerrer kaydı eler', () async {
    var call = 0;
    final adapter = routedAdapter({
      '/v1/announcements': (options) async {
        call++;
        return jsonResponse(
          successEnvelope(
            call == 1
                ? page([item('1'), item('2')], currentPage: 1, totalPages: 2)
                // 2. sayfada araya yeni duyuru girdiği için "2" tekrar geliyor.
                : page([item('2'), item('3')], currentPage: 2, totalPages: 2),
          ),
        );
      },
    });
    final container = await testContainer(adapter: adapter);
    await settled(container);

    await container.read(announcementFeedProvider.notifier).loadMore();

    final state = container.read(announcementFeedProvider);
    expect(state.items.map((a) => a.id), ['1', '2', '3']);
    expect(state.hasMore, isFalse);
    expect(adapter.lastOf('/v1/announcements')?.queryParameters['page'], 2);
  });

  test('son sayfadayken loadMore yeni istek atmaz', () async {
    final adapter = routedAdapter({
      '/v1/announcements': (_) async => jsonResponse(
        successEnvelope(page([item('1')], currentPage: 1, totalPages: 1)),
      ),
    });
    final container = await testContainer(adapter: adapter);
    await settled(container);

    await container.read(announcementFeedProvider.notifier).loadMore();

    expect(adapter.countOf('/v1/announcements'), 1);
  });

  test('ikinci sayfa hatası mevcut kayıtları SİLMEZ', () async {
    var call = 0;
    final adapter = routedAdapter({
      '/v1/announcements': (_) async {
        call++;
        if (call == 1) {
          return jsonResponse(
            successEnvelope(page([item('1')], currentPage: 1, totalPages: 2)),
          );
        }
        return jsonResponse(
          errorEnvelope('INTERNAL_ERROR', 'Sunucu hatası.'),
          statusCode: 500,
        );
      },
    });
    final container = await testContainer(adapter: adapter);
    await settled(container);

    await container.read(announcementFeedProvider.notifier).loadMore();

    final state = container.read(announcementFeedProvider);
    expect(state.items, hasLength(1), reason: 'okunan içerik kaybolmamalı');
    expect(state.loadMoreError, isNotNull);
    expect(state.error, isNull, reason: 'tüm ekran hataya düşmemeli');
  });

  test('tür seçimi listeyi sıfırlar ve typeId sorguya girer', () async {
    final adapter = routedAdapter({
      '/v1/announcements': (_) async => jsonResponse(
        successEnvelope(page([item('1')], currentPage: 1, totalPages: 1)),
      ),
    });
    final container = await testContainer(adapter: adapter);
    await settled(container);

    container.read(announcementFeedProvider.notifier).selectType('type-9');
    await settled(container);

    expect(container.read(announcementFeedProvider).typeId, 'type-9');
    expect(
      adapter.lastOf('/v1/announcements')?.queryParameters['typeId'],
      'type-9',
    );
  });

  test('aynı türe tekrar dokunmak filtreyi kaldırır', () async {
    final adapter = routedAdapter({
      '/v1/announcements': (_) async => jsonResponse(
        successEnvelope(page([item('1')], currentPage: 1, totalPages: 1)),
      ),
    });
    final container = await testContainer(adapter: adapter);
    await settled(container);
    final notifier = container.read(announcementFeedProvider.notifier);

    notifier.selectType('type-9');
    await settled(container);
    notifier.selectType('type-9');
    await settled(container);

    expect(container.read(announcementFeedProvider).typeId, isNull);
    expect(
      adapter.lastOf('/v1/announcements')?.queryParameters.containsKey('typeId'),
      isFalse,
      reason: 'filtre kalkınca typeId gönderilmemeli',
    );
  });

  test('ilk sayfa hatası tüm ekranı hata durumuna geçirir', () async {
    final adapter = routedAdapter({
      '/v1/announcements': (_) async => jsonResponse(
        errorEnvelope('INTERNAL_ERROR', 'Sunucu hatası.'),
        statusCode: 500,
      ),
    });
    final container = await testContainer(adapter: adapter);

    final state = await settled(container);

    expect(state.error, isNotNull);
    expect(state.items, isEmpty);
    expect(state.isEmpty, isFalse, reason: 'hata ile "kayıt yok" ayrı durumlar');
  });
}
