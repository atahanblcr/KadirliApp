import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/widgets/widgets.dart';

import 'fake_http_adapter.dart';

/// 11.15 — çevrimdışı şeridi.
///
/// 📌 `OfflineBanner` 11.1'de yazılmıştı ama `AppScaffold(offline:)` sabit
/// `false` olduğu ve **hiçbir ekran değer geçmediği** için uygulamada hiç
/// görünmemişti — yalnız `/gelistirici/tasarim` stil kılavuzunda duruyordu.
///
/// Sinyal ayrı bir bağlantı paketinden değil **gerçek isteklerden** türetiliyor:
/// kullanıcı için önemli olan "Wi-Fi'a bağlı mıyım" değil, **sunucuya
/// ulaşılıyor mu**. Bu testler o sınıflandırmanın doğruluğunu kilitliyor.
void main() {
  ProviderContainer makeContainer(FakeHttpAdapter adapter) {
    final container = ProviderContainer(
      overrides: [
        tokenStoreProvider.overrideWithValue(InMemoryTokenStore()),
        dioProvider.overrideWith(
          (ref) => DioClient.create(
            tokenStore: ref.watch(tokenStoreProvider),
            onReachable: () =>
                ref.read(connectivityStatusProvider.notifier).goOnline(),
            onUnreachable: () =>
                ref.read(connectivityStatusProvider.notifier).goOffline(),
            baseUrl: 'http://localhost:5005',
            adapter: adapter,
          ),
        ),
      ],
    );
    addTearDown(container.dispose);
    return container;
  }

  test('başlangıçta çevrimiçi varsayılır (şerit gereksiz yere çıkmaz)', () {
    final container = makeContainer(FakeHttpAdapter((_) async => jsonResponse(successEnvelope({}))));

    expect(container.read(connectivityStatusProvider), isFalse);
  });

  test('bağlantı hatası çevrimdışı yapar, sonraki başarılı istek geri alır', () async {
    var failing = true;
    final adapter = FakeHttpAdapter((options) async {
      if (failing) {
        throw DioException.connectionError(
          requestOptions: options,
          reason: 'ağ yok',
        );
      }
      return jsonResponse(successEnvelope({'ok': true}));
    });
    final container = makeContainer(adapter);
    final client = container.read(apiClientProvider);

    await expectLater(client.get('/v1/announcements'), throwsA(isA<ApiException>()));
    expect(container.read(connectivityStatusProvider), isTrue);

    failing = false;
    await client.get('/v1/announcements');
    expect(container.read(connectivityStatusProvider), isFalse);
  });

  test('SUNUCU hatası (404/500) çevrimdışı SAYILMAZ', () async {
    // Kritik ayrım: sunucu cevap verdiyse bağlantı vardır. 404'te "internet
    // yok" şeridi göstermek kullanıcıyı yanlış yere bakmaya iter.
    final adapter = FakeHttpAdapter(
      (_) async => jsonResponse(
        errorEnvelope('NOT_FOUND', 'Duyuru bulunamadı.'),
        statusCode: 404,
      ),
    );
    final container = makeContainer(adapter);

    await expectLater(
      container.read(apiClientProvider).get('/v1/announcements/yok'),
      throwsA(isA<ApiException>()),
    );

    expect(container.read(connectivityStatusProvider), isFalse);
  });

  testWidgets('AppScaffold şeridi hiçbir ekran bağlamadan gösterir', (tester) async {
    final container = ProviderContainer();
    addTearDown(container.dispose);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: const MaterialApp(
          home: AppScaffold(title: 'Duyurular', body: SizedBox.shrink()),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('İnternet bağlantısı yok'), findsNothing);

    container.read(connectivityStatusProvider.notifier).goOffline();
    await tester.pumpAndSettle();

    expect(find.text('İnternet bağlantısı yok'), findsOneWidget);

    container.read(connectivityStatusProvider.notifier).goOnline();
    await tester.pumpAndSettle();

    expect(find.text('İnternet bağlantısı yok'), findsNothing);
  });
}
