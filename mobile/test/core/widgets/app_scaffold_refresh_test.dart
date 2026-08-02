import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/widgets/widgets.dart';

/// `AppScaffold(onRefresh:)` — 11.12'de canlıda yakalanan hatanın regresyonu.
///
/// 🐛 İçerik ekrana **sığdığında** Android'in varsayılan `ClampingScrollPhysics`i
/// taşmaya izin vermiyor; aşağı çekme jesti `RefreshIndicator`a hiç ulaşmıyor ve
/// pull-to-refresh **sessizce ölüyordu**. Uzun listelerde çalıştığı için önceki
/// fazlarda fark edilmemişti (11.6'dan beri her liste ekranını ilgilendiriyor).
///
/// ⚠️ Tetikleyici dar: `ListView` **kendi `controller`ını aldığında** `primary`
/// `false` olur ve Flutter'ın otomatik `AlwaysScrollableScrollPhysics` takviyesi
/// devreye girmez. Bu projedeki **her sonsuz kaydırmalı liste** `loadMore`
/// dinleyicisi için controller veriyor → hepsi etkileniyordu. Test bu yüzden
/// controller'lı listeyle kurulmalı; controller'sız kurulursa hata görünmez.
void main() {
  Future<void> pumpScaffold(
    WidgetTester tester, {
    required int itemCount,
    required VoidCallback onRefresh,
  }) async {
    final controller = ScrollController();
    addTearDown(controller.dispose);

    // ⚠️ 11.15'ten beri `AppScaffold` offline şeridini
    // `connectivityStatusProvider`'dan okuyor → `ProviderScope` şart.
    await tester.pumpWidget(
      ProviderScope(
        child: MaterialApp(
          home: AppScaffold(
            title: 'Test',
            onRefresh: () async => onRefresh(),
            body: ListView(
              controller: controller,
              children: [
                for (var i = 0; i < itemCount; i++)
                  SizedBox(height: 60, child: Text('Satır $i')),
              ],
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  testWidgets('içerik ekrana SIĞDIĞINDA da pull-to-refresh tetiklenir', (
    tester,
  ) async {
    var refreshed = 0;
    // İki satır 800 dp'lik test yüzeyine rahatça sığıyor → hatanın koşulu.
    await pumpScaffold(tester, itemCount: 2, onRefresh: () => refreshed++);

    await tester.fling(find.byType(ListView), const Offset(0, 300), 1000);
    await tester.pumpAndSettle();

    expect(refreshed, 1);
  });

  testWidgets('uzun listede de çalışmaya devam eder', (tester) async {
    var refreshed = 0;
    await pumpScaffold(tester, itemCount: 40, onRefresh: () => refreshed++);

    await tester.fling(find.byType(ListView), const Offset(0, 400), 1000);
    await tester.pumpAndSettle();

    expect(refreshed, 1);
  });

  testWidgets('onRefresh verilmezse liste kaydırma davranışı değişmez', (
    tester,
  ) async {
    final controller = ScrollController();
    addTearDown(controller.dispose);

    await tester.pumpWidget(
      ProviderScope(
        child: MaterialApp(
          home: AppScaffold(
            title: 'Test',
            body: ListView(
              controller: controller,
              children: const [SizedBox(height: 60, child: Text('Tek satır'))],
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(RefreshIndicator), findsNothing);
    expect(find.text('Tek satır'), findsOneWidget);
  });
}
