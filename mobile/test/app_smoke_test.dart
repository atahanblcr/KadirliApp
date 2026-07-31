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
///
/// ⚠️ 11.4'ten sonra tema seçici Ana Sayfa'da değil **Ayarlar**'da (sağ üst ⚙️),
/// ve selamlama saate göre değişiyor (Günaydın/İyi günler/…) → metin tam
/// eşleşme yerine 👋 ile aranıyor.
void main() {
  const guest = {'auth.guestChoice': true};

  testWidgets('uygulama açılır ve Ana Sayfa hub + sekmeler görünür', (tester) async {
    await pumpApp(tester, prefs: guest, adapter: routedAdapter(homeStubs()));

    expect(find.textContaining('👋'), findsOneWidget);
    expect(find.text('Modüller'), findsOneWidget);

    // Alt sekme kabuğu ayakta. ("İlanlar" hem sekmede hem modül ızgarasında
    // geçiyor → arama NavigationBar'ın içine daraltılır.)
    Finder tab(String label) => find.descendant(
      of: find.byType(NavigationBar),
      matching: find.text(label),
    );
    expect(tab('Ana Sayfa'), findsOneWidget);
    expect(tab('İlanlar'), findsOneWidget);
    expect(tab('Bildirim'), findsOneWidget);
    expect(tab('Profil'), findsOneWidget);
  });

  testWidgets('oturumu olmayan yeni kullanıcı Giriş ekranıyla karşılanır', (tester) async {
    await pumpApp(tester, adapter: routedAdapter(homeStubs()));

    expect(find.text('Telefonunuzla giriş yapın'), findsOneWidget);
  });

  testWidgets('Nunito fontu ve marka renkleri uygulanır', (tester) async {
    await pumpApp(tester, prefs: guest, adapter: routedAdapter(homeStubs()));

    final theme = Theme.of(tester.element(find.text('Modüller')));
    expect(theme.textTheme.bodyLarge?.fontFamily, 'Nunito');
    expect(theme.colorScheme.primary, AppColors.primary);
    expect(theme.scaffoldBackgroundColor, AppColors.background);
    expect(theme.palette.accent, AppColors.accent);
  });

  testWidgets('Ayarlar\'dan tema Açık↔Koyu değişir ve tercih kalıcıdır', (tester) async {
    await pumpApp(tester, prefs: guest, adapter: routedAdapter(homeStubs()));

    expect(Theme.of(tester.element(find.text('Modüller'))).brightness, Brightness.light);

    // Sağ üst ⚙️ → Ayarlar.
    await tester.tap(find.byTooltip('Ayarlar'));
    await tester.pumpAndSettle();
    expect(find.text('Görünüm'), findsOneWidget);

    await tester.tap(find.text('Koyu'));
    await tester.pumpAndSettle();

    final darkTheme = Theme.of(tester.element(find.text('Koyu')));
    expect(darkTheme.brightness, Brightness.dark);
    expect(darkTheme.colorScheme.primary, AppColors.primaryDark);
    expect(darkTheme.scaffoldBackgroundColor, AppColors.backgroundDark);

    // Tercih shared_preferences'a yazıldı mı → yeniden açılışta korunur.
    final prefs = await SharedPreferences.getInstance();
    expect(prefs.getString('settings.themeMode'), 'dark');
  });

  group('yazı ölçeği sınırı', () {
    // `MediaQuery.withClampedTextScaling` (app.dart) 11.1'de eklendi ama hiçbir
    // test uygulandığını doğrulamıyordu — sessizce kaldırılsa kimse fark
    // etmezdi. Sınır kalkarsa cihaz ayarı 3.0'a çıkan kullanıcıda tüm düzen
    // dağılır.
    testWidgets('sistem ölçeği aşırı büyükse 1.4\'e kırpılır', (tester) async {
      tester.platformDispatcher.textScaleFactorTestValue = 3;
      addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);

      await pumpApp(tester, prefs: guest, adapter: routedAdapter(homeStubs()));

      final scaler = MediaQuery.textScalerOf(
        tester.element(find.text('Modüller')),
      );
      expect(scaler.scale(10), closeTo(14, 0.01), reason: '1.4 üst sınırı');
    });

    testWidgets('sistem ölçeği çok küçükse 0.9\'a yükseltilir', (tester) async {
      tester.platformDispatcher.textScaleFactorTestValue = 0.5;
      addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);

      await pumpApp(tester, prefs: guest, adapter: routedAdapter(homeStubs()));

      final scaler = MediaQuery.textScalerOf(
        tester.element(find.text('Modüller')),
      );
      expect(scaler.scale(10), closeTo(9, 0.01), reason: '0.9 alt sınırı');
    });

    testWidgets('en büyük ölçekte Ana Sayfa taşmadan çizilir', (tester) async {
      tester.view.physicalSize = const Size(1080, 2400);
      tester.view.devicePixelRatio = 3;
      tester.platformDispatcher.textScaleFactorTestValue = 1.4;
      addTearDown(tester.view.reset);
      addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);

      await pumpApp(
        tester,
        prefs: guest,
        adapter: routedAdapter(
          homeStubs(
            onDuty: [
              {
                'pharmacyId': 'p1',
                'pharmacyName': 'Şifa Eczanesi',
                'address': 'Cumhuriyet Mah. Uzun Sokak No 12 Kadirli/Osmaniye',
                'phone': '03287181234',
                'dutyDate': '2026-07-31T00:00:00Z',
                'startTime': '19:00',
                'endTime': '09:00',
              },
            ],
          ),
        ),
      );

      expect(tester.takeException(), isNull);
    });
  });
}
