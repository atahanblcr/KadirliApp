import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/theme/app_theme.dart';
import 'package:kadirli_app/core/widgets/widgets.dart';

void main() {
  Future<void> pump(WidgetTester tester, Widget child) {
    return tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: Scaffold(body: Center(child: child)),
      ),
    );
  }

  testWidgets('varsayılan buton içeriği kadar geniştir, ekranı kaplamaz', (tester) async {
    await pump(tester, AppButton(label: 'Kaydet', onPressed: () {}));

    final width = tester.getSize(find.byType(AppButton)).width;
    expect(width, lessThan(300));
    expect(tester.getSize(find.byType(AppButton)).height, 48);
  });

  testWidgets('expand:true tam genişlik kaplar', (tester) async {
    await pump(tester, AppButton(label: 'Kaydet', expand: true, onPressed: () {}));

    final screenWidth = tester.view.physicalSize.width / tester.view.devicePixelRatio;
    expect(tester.getSize(find.byType(AppButton)).width, screenWidth);
  });

  testWidgets('onPressed null iken dokunma yoksayılır', (tester) async {
    var tapped = 0;
    await pump(tester, AppButton(label: 'Pasif', onPressed: null));
    await tester.tap(find.byType(AppButton));
    expect(tapped, 0);

    await pump(tester, AppButton(label: 'Aktif', onPressed: () => tapped++));
    await tester.tap(find.byType(AppButton));
    expect(tapped, 1);
  });

  testWidgets('loading iken etiket yerine gösterge çıkar ve tıklanmaz', (tester) async {
    var tapped = 0;
    await pump(tester, AppButton(label: 'Gönder', loading: true, onPressed: () => tapped++));

    expect(find.text('Gönder'), findsNothing);
    expect(find.byType(CircularProgressIndicator), findsOneWidget);

    await tester.tap(find.byType(AppButton));
    expect(tapped, 0);
  });

  testWidgets('küçük boy bile 40dp altına inmez', (tester) async {
    await pump(tester, AppButton(label: 'Uzat', size: AppButtonSize.small, onPressed: () {}));
    expect(tester.getSize(find.byType(AppButton)).height, 40);
  });
}
