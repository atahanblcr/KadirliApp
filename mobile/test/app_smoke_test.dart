import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/app.dart';
import 'package:kadirli_app/core/theme/app_colors.dart';
import 'package:kadirli_app/core/theme/theme_controller.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  setUp(() => SharedPreferences.setMockInitialValues({}));

  Future<void> pumpApp(WidgetTester tester) async {
    final prefs = await SharedPreferences.getInstance();
    await tester.pumpWidget(
      ProviderScope(
        overrides: [sharedPreferencesProvider.overrideWithValue(prefs)],
        child: const KadirliApp(),
      ),
    );
    await tester.pumpAndSettle();
  }

  testWidgets('uygulama açılır ve karşılama ekranı görünür', (tester) async {
    await pumpApp(tester);

    expect(find.text('Merhaba 👋'), findsOneWidget);
    expect(find.text('Kadirli'), findsOneWidget);
  });

  testWidgets('Nunito fontu ve marka renkleri uygulanır', (tester) async {
    await pumpApp(tester);

    final theme = Theme.of(tester.element(find.text('Merhaba 👋')));
    expect(theme.textTheme.bodyLarge?.fontFamily, 'Nunito');
    expect(theme.colorScheme.primary, AppColors.primary);
    expect(theme.scaffoldBackgroundColor, AppColors.background);
    expect(theme.palette.accent, AppColors.accent);
  });

  testWidgets('tema Açık↔Koyu değişir ve tercih kalıcıdır', (tester) async {
    await pumpApp(tester);

    expect(Theme.of(tester.element(find.text('Merhaba 👋'))).brightness, Brightness.light);

    await tester.tap(find.text('Koyu'));
    await tester.pumpAndSettle();

    final darkTheme = Theme.of(tester.element(find.text('Merhaba 👋')));
    expect(darkTheme.brightness, Brightness.dark);
    expect(darkTheme.colorScheme.primary, AppColors.primaryDark);
    expect(darkTheme.scaffoldBackgroundColor, AppColors.backgroundDark);

    // Tercih shared_preferences'a yazıldı mı → yeniden açılışta korunur.
    final prefs = await SharedPreferences.getInstance();
    expect(prefs.getString('settings.themeMode'), 'dark');
  });
}
