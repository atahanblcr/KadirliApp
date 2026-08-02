import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/paging/paged_feed.dart';
import 'package:kadirli_app/core/paging/paged_list_footer.dart';

/// 11.15 — sonsuz kaydırmalı listelerin son satırı (14 liste ekranının ortağı).
///
/// Bu altbilgi 11 ekranda birebir kopyalanmıştı; kopyalar ayrışmıştı:
/// 10'u sayfa hatasının **sebebini** göstermiyordu, eczane ekranında ise
/// altbilgi **hiç yoktu** (2. sayfa patlarsa liste sessizce eksik kalıyordu).
void main() {
  Future<void> pumpFooter(
    WidgetTester tester,
    PagedFeedState<String, String?> state, {
    VoidCallback? onLoadMore,
  }) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: PagedListFooter(
            state: state,
            onLoadMore: onLoadMore ?? () {},
            itemNoun: 'eczane',
          ),
        ),
      ),
    );
    await tester.pump();
  }

  const base = PagedFeedState<String, String?>(
    filter: null,
    items: ['a', 'b'],
    isLoadingFirstPage: false,
  );

  testWidgets('sonraki sayfa yüklenirken gösterge çıkar', (tester) async {
    await pumpFooter(tester, base.copyWith(isLoadingMore: true, hasMore: true));

    expect(find.byType(CircularProgressIndicator), findsOneWidget);
  });

  testWidgets('sayfa hatası SEBEBİYLE birlikte gösterilir ve tekrar denenebilir', (tester) async {
    var loadMoreCalls = 0;
    await pumpFooter(
      tester,
      base.copyWith(
        hasMore: true,
        loadMoreError: ApiException(
          code: 'NETWORK_ERROR',
          message: 'İnternet bağlantısı kurulamadı.',
        ),
      ),
      onLoadMore: () => loadMoreCalls++,
    );

    // ⚠️ Yalnız düğme göstermek yetmez: kullanıcı NEDEN devamının gelmediğini
    // bilmeli (11 ekranın 10'unda eksikti).
    expect(find.text('İnternet bağlantısı kurulamadı.'), findsOneWidget);

    await tester.tap(find.text('Devamını yükle'));
    await tester.pump();
    expect(loadMoreCalls, 1);
  });

  testWidgets('daha fazlası varken bitiş satırı YAZILMAZ', (tester) async {
    await pumpFooter(tester, base.copyWith(hasMore: true, totalCount: 50));

    expect(find.textContaining('Toplam'), findsNothing);
    expect(find.text('Hepsi bu kadar'), findsNothing);
  });

  testWidgets('liste bitince toplam sayı yazılır', (tester) async {
    await pumpFooter(tester, base.copyWith(totalCount: 12));

    expect(find.text('Toplam 12 eczane'), findsOneWidget);
  });

  testWidgets('toplam bilinmiyorsa sayı uydurulmaz', (tester) async {
    // `totalCount` 0 iken "Toplam 0 eczane" yazmak yanlış olurdu: liste dolu
    // ama sunucu toplamı bildirmemiş olabilir.
    await pumpFooter(tester, base.copyWith(totalCount: 0));

    expect(find.text('Hepsi bu kadar'), findsOneWidget);
  });
}
