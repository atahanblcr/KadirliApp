import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/theme/app_colors.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'helpers/pump_app.dart';

/// Açılış + tema temelleri.
///
/// ⚠️ 11.3'ten sonra açılış doğrudan Ana Sayfa'ya düşmüyor: oturumu ve "misafir
/// devam" tercihi olmayan kullanıcı Giriş ekranına yönlendiriliyor. Tema
/// testleri bu yüzden misafir tercihi işaretli açılıyor.
void main() {
  const guest = {'auth.guestChoice': true};

  testWidgets('uygulama açılır ve karşılama ekranı görünür', (tester) async {
    await pumpApp(tester, prefs: guest, adapter: routedAdapter({}));

    expect(find.text('Merhaba 👋'), findsOneWidget);
    expect(find.text('Kadirli'), findsOneWidget);
  });

  testWidgets('oturumu olmayan yeni kullanıcı Giriş ekranıyla karşılanır', (tester) async {
    await pumpApp(tester, adapter: routedAdapter({}));

    expect(find.text('Telefonunuzla giriş yapın'), findsOneWidget);
  });

  testWidgets('Nunito fontu ve marka renkleri uygulanır', (tester) async {
    await pumpApp(tester, prefs: guest, adapter: routedAdapter({}));

    final theme = Theme.of(tester.element(find.text('Merhaba 👋')));
    expect(theme.textTheme.bodyLarge?.fontFamily, 'Nunito');
    expect(theme.colorScheme.primary, AppColors.primary);
    expect(theme.scaffoldBackgroundColor, AppColors.background);
    expect(theme.palette.accent, AppColors.accent);
  });

  testWidgets('tema Açık↔Koyu değişir ve tercih kalıcıdır', (tester) async {
    await pumpApp(tester, prefs: guest, adapter: routedAdapter({}));

    expect(Theme.of(tester.element(find.text('Merhaba 👋'))).brightness, Brightness.light);

    // Tema seçici "Hesap" kartının altında — testte görünür alana kaydırılır.
    await tester.scrollUntilVisible(find.text('Koyu'), 200);
    await tester.tap(find.text('Koyu'));
    await tester.pumpAndSettle();

    // Kaydırma sonrası selamlama liste dışında kaldı → tema seçiciyi çapa al.
    final darkTheme = Theme.of(tester.element(find.text('Koyu')));
    expect(darkTheme.brightness, Brightness.dark);
    expect(darkTheme.colorScheme.primary, AppColors.primaryDark);
    expect(darkTheme.scaffoldBackgroundColor, AppColors.backgroundDark);

    // Tercih shared_preferences'a yazıldı mı → yeniden açılışta korunur.
    final prefs = await SharedPreferences.getInstance();
    expect(prefs.getString('settings.themeMode'), 'dark');
  });
}
