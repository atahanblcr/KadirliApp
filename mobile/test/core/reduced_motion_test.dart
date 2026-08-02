import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/theme/app_theme.dart';
import 'package:kadirli_app/core/widgets/widgets.dart';

/// "Hareketi azalt" (`MediaQuery.disableAnimations`) saygısı — Faz 11.15.
///
/// 📌 iOS "Hareketi Azalt" / Android "Animasyonları kaldır" açıkken hareket
/// **vestibüler rahatsızlık** tetikleyebiliyor. Bu ayar bugüne kadar
/// `AppButton` ve `SkeletonBox`ta gözetiliyordu ama hiç **test edilmiyordu** →
/// yeni bir animasyon eklendiğinde kimse hatırlamak zorunda kalmasın diye
/// davranış buraya kilitlendi.
///
/// ⚠️ Sayfa geçişleri ayrıca kod yazmayı gerektirmiyor: Flutter'ın
/// `AnimationController`'ı `AnimationBehavior.normal` ile bu ayarda süreyi
/// kendiliğinden kısaltıyor. Kendi yazdığımız **sonsuz** animasyonlar ise
/// (shimmer) tam tersine hızlanır → onları açıkça durdurmak zorundayız.
void main() {
  Future<void> pump(
    WidgetTester tester,
    Widget child, {
    required bool reduceMotion,
  }) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: MediaQuery(
          data: MediaQueryData(disableAnimations: reduceMotion),
          child: Scaffold(body: Center(child: child)),
        ),
      ),
    );
    await tester.pump();
  }

  testWidgets('shimmer kapalıyken iskelet SABİT çizilir', (tester) async {
    await pump(tester, const SkeletonBox(width: 100), reduceMotion: true);

    // Shimmer `ShaderMask` ile çiziliyor; kapalıyken hiç kurulmamalı.
    expect(find.byType(ShaderMask), findsNothing);
  });

  testWidgets('shimmer açıkken parlama katmanı vardır', (tester) async {
    await pump(tester, const SkeletonBox(width: 100), reduceMotion: false);

    expect(find.byType(ShaderMask), findsOneWidget);
  });

  testWidgets('buton basma ölçeği kapalıyken uygulanmaz', (tester) async {
    await pump(
      tester,
      AppButton(label: 'Kaydet', onPressed: () {}),
      reduceMotion: true,
    );

    final gesture = await tester.startGesture(tester.getCenter(find.text('Kaydet')));
    await tester.pump(const Duration(milliseconds: 200));

    final scale = tester.widget<AnimatedScale>(find.byType(AnimatedScale)).scale;
    expect(scale, 1.0, reason: 'Hareket azaltılmışken ölçek değişmemeli');

    await gesture.up();
  });

  testWidgets('buton basma ölçeği açıkken 0.98e iner', (tester) async {
    await pump(
      tester,
      AppButton(label: 'Kaydet', onPressed: () {}),
      reduceMotion: false,
    );

    final gesture = await tester.startGesture(tester.getCenter(find.text('Kaydet')));
    await tester.pump(const Duration(milliseconds: 200));

    final scale = tester.widget<AnimatedScale>(find.byType(AnimatedScale)).scale;
    expect(scale, 0.98);

    await gesture.up();
  });

  testWidgets('iskelet ekran okuyucuya "içerik yükleniyor" der, kutuları saymaz', (
    tester,
  ) async {
    final handle = tester.ensureSemantics();
    await pump(tester, const LoadingView(itemCount: 3), reduceMotion: true);

    expect(find.bySemanticsLabel('İçerik yükleniyor'), findsOneWidget);
    handle.dispose();
  });
}
