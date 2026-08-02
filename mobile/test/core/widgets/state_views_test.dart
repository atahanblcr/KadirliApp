import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/widgets/widgets.dart';

/// 11.15 — boş/hata görünümlerinin **kaydırılabilir** olması.
///
/// 🐛 Yakalanan sistematik hata: `RefreshIndicator` jesti ancak kaydırılabilir
/// bir alt ağaç varsa çalışır. `EmptyView`/`ErrorView` ise `Center` idi →
/// **pull-to-refresh tam da kullanıcının en çok ihtiyaç duyduğu iki anda
/// ölüydü** ("liste boş, yenileyeyim" / "hata aldım, tekrar deneyeyim").
/// 11.6'da duyuru ve kesinti ekranlarında tek tek çözülmüştü; kalan 12 liste
/// ekranında açıktı. Düzeltme çağrı yerinde değil bileşenin içinde — yeni
/// yazılan ekran bunu unutamaz.
///
/// ⚠️ Bu testler düzeltme geri alınınca (ScrollableStateBody kaldırılınca)
/// kırmızıya döner: `fling` jesti yenilemeyi tetiklemez.
void main() {
  Future<void> pumpStateScreen(WidgetTester tester, Widget state, VoidCallback onRefresh) async {
    await tester.pumpWidget(
      ProviderScope(
        child: MaterialApp(
          home: AppScaffold(
            title: 'Test',
            onRefresh: () async => onRefresh(),
            body: state,
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  testWidgets('BOŞ durumda pull-to-refresh çalışır', (tester) async {
    var refreshed = 0;
    await pumpStateScreen(
      tester,
      const EmptyView(title: 'Henüz duyuru yok'),
      () => refreshed++,
    );

    expect(find.text('Henüz duyuru yok'), findsOneWidget);
    await tester.fling(find.byType(SingleChildScrollView), const Offset(0, 300), 1000);
    await tester.pumpAndSettle();

    expect(refreshed, 1, reason: 'Boş durum kaydırılabilir olmalı');
  });

  testWidgets('HATA durumunda pull-to-refresh çalışır', (tester) async {
    var refreshed = 0;
    await pumpStateScreen(
      tester,
      const ErrorView(message: 'İçerik yüklenemedi.'),
      () => refreshed++,
    );

    await tester.fling(find.byType(SingleChildScrollView), const Offset(0, 300), 1000);
    await tester.pumpAndSettle();

    expect(refreshed, 1, reason: 'Hata durumu kaydırılabilir olmalı');
  });

  testWidgets('"Tekrar dene" düğmesi hata durumunda çalışmaya devam eder', (tester) async {
    var retried = 0;
    await pumpStateScreen(
      tester,
      ErrorView(message: 'Olmadı.', onRetry: () => retried++),
      () {},
    );

    await tester.tap(find.text('Tekrar dene'));
    await tester.pump();

    expect(retried, 1);
  });

  testWidgets('yüksekliği SINIRSIZ ebeveynde kaydırma sarmalayıcısı kurulmaz', (tester) async {
    // ⚠️ `ListView` çocuğu olarak kullanıldığında (ör. bir vitrin içinde)
    // `SingleChildScrollView` "unbounded height" ile patlardı → içerik olduğu
    // gibi geçmeli. Bu test o kaçış yolunu kilitliyor.
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: SingleChildScrollView(
            child: Column(children: [EmptyView(title: 'Kayıt yok')]),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
    expect(find.text('Kayıt yok'), findsOneWidget);
  });
}
