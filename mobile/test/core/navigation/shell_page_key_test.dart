import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:kadirli_app/core/router/app_nav.dart';
import 'package:kadirli_app/core/router/app_router.dart';
import 'package:kadirli_app/core/router/app_routes.dart';

import '../../helpers/pump_app.dart';

/// 🐛 **12.2'den devralınan çökmenin kök nedeni ve düzeltmesi (12.3'te bulundu).**
///
/// İki oturum boyunca "yeniden üretilemedi" diye açık kalan
/// `Navigator._debugCheckDuplicatedPageKeys` assertion'ı burada **deterministik olarak**
/// üretiliyor ve `AppNav`'ın onu kapattığı gösteriliyor.
///
/// Mekanizma (ayrıntısı `lib/core/router/app_nav.dart`'ta): `go_router` imperative
/// sayfalara **rastgele**, kabuk (`StatefulShellRoute`) sayfalarına ise
/// **`route.hashCode`** anahtarı verir. Kabuk anahtarı deterministik olduğu için, kabuk
/// yığının tepesinde DEĞİLKEN bir kabuk rotası `push` edilirse listede **aynı anahtar iki
/// kez** belirir ve Navigator patlar.
///
/// 🔑 Bu test önceki denemelerden neden farklı: 12.2b'de yazılan test ekranlara dokunuyordu
/// ve düzeltme geri alındığında da **yeşil kalıyordu** — yani hiçbir şey kilitlemiyordu ve
/// silindi. Buradaki iddia mekanizmanın kendisi: sayfa anahtarları **doğrudan router'dan**
/// okunuyor.
///
/// ⚠️ Çakışma **kare basılmadan** ölçülüyor. Assertion'ın gerçekten atılmasına izin vermek
/// widget ağacını bozuk bırakıyor ve artık istisnalar aynı binding'i paylaşan sonraki
/// testlere sızıyor (ilk yazımda beş test birden kırmızıya döndü). Kanıtlanması gereken şey
/// zaten çökme değil, çökmeyi **üreten liste**.
void main() {
  Future<GoRouter> pumpShell(WidgetTester tester) async {
    final container = await pumpApp(
      tester,
      adapter: routedAdapter(homeStubs()),
      prefs: {'auth.guestChoice': true},
    );
    await settleApp(tester);
    return container.read(routerProvider);
  }

  List<String> pageKeysOf(GoRouter router) => router
      .routerDelegate
      .currentConfiguration
      .matches
      .map((m) => m.pageKey.value)
      .toList();

  /// Yığını sağlam bir hâle döndürür: bozuk sayfa listesiyle kare basılmasın.
  Future<void> recover(WidgetTester tester, GoRouter router) async {
    router.go(AppRoutes.home);
    await settleApp(tester);
  }

  testWidgets('KÖK NEDEN: kabuk en üstte değilken kabuk rotası push edilirse '
      'sayfa anahtarı TEKRARLANIR', (tester) async {
    final router = await pumpShell(tester);

    // 1) Kabuk dışı bir ekran yığına girer (Ayarlar — uygulamada beş yerde push ediliyor).
    router.push(AppRoutes.settings);
    await tester.pump();

    final beforeKeys = pageKeysOf(router);
    expect(beforeKeys.toSet().length, beforeKeys.length, reason: 'buraya kadar sağlam');

    // 2) Ham `push` ile bir kabuk rotası. go_router birleştiremez (tepe kabuk değil),
    //    listeye AYNI anahtarla ikinci bir ShellRouteMatch ekler.
    router.push(AppRoutes.notifications);

    final keys = pageKeysOf(router);
    expect(
      keys.length - keys.toSet().length,
      1,
      reason:
          'ham push ile kabuk rotası açmak MÜKERRER sayfa anahtarı üretir — '
          'Navigator._debugCheckDuplicatedPageKeys tam olarak bunu yakalıyor',
    );

    await recover(tester, router);
  });

  testWidgets('AppNav aynı senaryoda çakışmayı engeller', (tester) async {
    final router = await pumpShell(tester);

    router.push(AppRoutes.settings);
    await tester.pump();

    AppNav.push(router, AppRoutes.notifications);
    await tester.pump();
    await settleApp(tester);

    final keys = pageKeysOf(router);
    expect(keys.toSet().length, keys.length, reason: 'mükerrer sayfa anahtarı kalmamalı');
    expect(tester.takeException(), isNull, reason: 'hiçbir assertion atılmamalı');
  });

  /// Push bildiriminin deep-link hedefi bir sekme **alt** rotası olabilir
  /// (`/ilanlar/:id`). 12.3'ten beri kesinti bildirimi kendiliğinden gittiği için bu yol
  /// artık günlük olarak yürünüyor: modül ekranındayken gelen push'a dokunmak.
  testWidgets('sekme ALT rotası da korunur — /ilanlar/:id kabuk dışından açılabilir', (
    tester,
  ) async {
    final router = await pumpShell(tester);

    router.push(AppRoutes.settings);
    await tester.pump();

    AppNav.push(
      router,
      AppRoutes.adDetail('11111111-1111-1111-1111-111111111111'),
    );
    await tester.pump();
    await settleApp(tester);

    final keys = pageKeysOf(router);
    expect(keys.toSet().length, keys.length);
    expect(tester.takeException(), isNull);
  });

  /// ⚠️ `AppNav` bir "her şeyi go'ya çevir" kısayolu DEĞİL: kabuk dışı rotalar
  /// (modül ekranları, ayarlar, giriş akışı) `push` edilmeye devam etmeli — yoksa geri
  /// tuşu davranışı ve 11.4'ün "modüle girmek içeri girmektir" kararı bozulur.
  testWidgets('kabuk DIŞI rota hâlâ push edilir (geri tuşu davranışı korunur)', (
    tester,
  ) async {
    final router = await pumpShell(tester);
    final before = pageKeysOf(router).length;

    AppNav.push(router, AppRoutes.settings);
    await tester.pump();

    expect(
      pageKeysOf(router).length,
      before + 1,
      reason: 'kabuk dışı rota yığına EKLENMELİ, kabuğun yerini almamalı',
    );

    await settleApp(tester);
    expect(tester.takeException(), isNull);
  });

  /// Kabuk en üstteyken `push` zaten güvenli (go_router birleştirir) ve davranışı
  /// değişmemeli: sekme içi gezinme 11.8/11.9'da bilinçli olarak `push`.
  testWidgets('kabuk en üstteyken davranış DEĞİŞMEZ', (tester) async {
    final router = await pumpShell(tester);

    AppNav.push(router, AppRoutes.notifications);
    await tester.pump();
    await settleApp(tester);

    final keys = pageKeysOf(router);
    expect(keys.toSet().length, keys.length);
    expect(tester.takeException(), isNull);
  });
}
