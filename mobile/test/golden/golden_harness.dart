import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:kadirli_app/core/theme/app_theme.dart';

/// # Golden (görsel regresyon) test altyapısı — Faz 11.15
///
/// ## Neden var
/// Dar sütunda `Row` içindeki `Text` taşması bu projede **altı kez** tekrarladı
/// (11.7 `PharmacyTile` · 11.8 `AdCard` · 11.9 `_FavoriteTile` · 11.10 kampanya
/// rozeti · 11.11 `LookupDropdown`+`AppTextField` · 11.13 bildirim filtre
/// şeridi). Her seferinde **el emeğiyle** yakalandı: ya biri o ekranı 1.4 yazı
/// ölçeğinde açtı, ya da bir widget testi tesadüfen dar yüzeyde koştu.
/// Golden test bunu **mekanik** hâle getirir — yedincisi kendiliğinden kırmızı.
///
/// ## Kapsam kararı: bileşen, ekran DEĞİL
/// Tam ekran golden'ı her metin değişiminde kırılır ve insanlar "yine kırıldı,
/// güncelle geç" alışkanlığı edinir — testin değeri sıfırlanır. Bu yüzden
/// kapsam **ortak bileşenler + modül liste kartları**: gerçek bir düzen hatası
/// olmadan kırılmazlar.
///
/// ## Matris
/// Her bileşen için **iki dosya** üretilir (açık + koyu). Her dosya, bileşeni
/// **360 dp genişlikte** (piyasadaki en dar telefon) ve **iki yazı ölçeğinde
/// (1.0 ve 1.4)** üst üste gösterir — taşmaların çıktığı tam koşullar.
///
/// ## Yazı tipi tuzağı
/// `flutter test` özel yazı tiplerini kendiliğinden yüklemez; yüklenmezse metin
/// varsayılan test fontuyla çizilir ve **satır kırılmaları gerçek uygulamadan
/// farklı** olur. [loadAppFonts] `FontManifest.json`'daki her aileyi (Nunito +
/// MaterialIcons) açıkça yükler.
///
/// ## Golden üretme / güncelleme
/// ```bash
/// cd mobile
/// flutter test --update-goldens test/golden      # referans görüntüleri üret
/// flutter test test/golden                       # yalnız karşılaştır
/// ```
/// ⚠️ **CI golden ÜRETMEZ, yalnız karşılaştırır** — üretmesine izin verilirse
/// hatalı düzen "yeni doğru" diye kaydedilir ve test kendini onaylar.

/// Golden'ların çizildiği yüzey genişliği — dar telefon (MOBILE_UX_PLAN §0.6).
const double goldenSurfaceWidth = 360;

/// Denenen yazı ölçekleri. 1.4 uygulamanın üst sınırı (`app.dart`'ta clamp'li).
const List<double> goldenTextScales = [1.0, 1.4];

bool _fontsLoaded = false;

/// pubspec'te bildirilen tüm yazı tiplerini test motoruna yükler.
///
/// `FontManifest.json` üzerinden gidilir; böylece pubspec'e yeni bir ağırlık
/// eklendiğinde burası da kendiliğinden kapsar (elle liste çürümez).
Future<void> loadAppFonts() async {
  if (_fontsLoaded) return;
  TestWidgetsFlutterBinding.ensureInitialized();

  final manifest =
      jsonDecode(await rootBundle.loadString('FontManifest.json')) as List<dynamic>;

  for (final entry in manifest.cast<Map<String, dynamic>>()) {
    final family = entry['family'] as String;
    final loader = FontLoader(family);
    for (final font in (entry['fonts'] as List<dynamic>).cast<Map<String, dynamic>>()) {
      loader.addFont(rootBundle.load(font['asset'] as String));
    }
    await loader.load();
  }

  await initializeDateFormatting('tr_TR');
  _fontsLoaded = true;
}

/// Golden sayfasındaki tek bir senaryo: başlık + çizilecek bileşen.
class GoldenScenario {
  const GoldenScenario(this.label, this.child);

  final String label;
  final Widget child;
}

/// Bir bileşenin senaryolarını iki temada da golden'a yazar.
///
/// [name] dosya adının kökü olur: `goldens/<name>_light.png` ve
/// `goldens/<name>_dark.png`.
Future<void> expectGoldenSheet(
  WidgetTester tester, {
  required String name,
  required List<GoldenScenario> scenarios,
  /// Yüzey yüksekliği — sayfa sığmazsa alt kısım kırpılır, bilinçli verilir.
  double height = 1400,
}) async {
  await loadAppFonts();

  for (final brightness in Brightness.values) {
    tester.view
      ..physicalSize = Size(goldenSurfaceWidth, height) * tester.view.devicePixelRatio
      ..devicePixelRatio = tester.view.devicePixelRatio;
    addTearDown(tester.view.reset);

    await tester.pumpWidget(
      ProviderScope(
        child: MaterialApp(
          debugShowCheckedModeBanner: false,
          theme: brightness == Brightness.light ? AppTheme.light : AppTheme.dark,
          locale: const Locale('tr', 'TR'),
          supportedLocales: const [Locale('tr', 'TR')],
          localizationsDelegates: const [
            GlobalMaterialLocalizations.delegate,
            GlobalWidgetsLocalizations.delegate,
            GlobalCupertinoLocalizations.delegate,
          ],
          home: _GoldenSheet(scenarios: scenarios),
        ),
      ),
    );
    // ⚠️ `pumpAndSettle` KULLANILAMAZ: yükleniyor senaryolarındaki
    // `CircularProgressIndicator` sonsuz animasyon olduğu için "settle" hâli
    // hiç gelmez (11.2'de splash'te öğrenilmişti). Sabit sayıda kare pump'lamak
    // hem sonlanır hem de **belirlenimci**dir — golden her koşuda aynı çıkar.
    await tester.pump();
    // Açılış sayfa geçişi (tema `CupertinoPageTransitionsBuilder` kullanıyor)
    // bitmeden çekilirse her şey yarı saydam ve kaymış çıkar.
    await tester.pump(const Duration(seconds: 1));

    final suffix = brightness == Brightness.light ? 'light' : 'dark';
    await expectLater(
      find.byType(_GoldenSheet),
      matchesGoldenFile('goldens/${name}_$suffix.png'),
    );
  }
}

/// Senaryoları "ölçek × senaryo" ızgarası olarak dizen sayfa.
class _GoldenSheet extends StatelessWidget {
  const _GoldenSheet({required this.scenarios});

  final List<GoldenScenario> scenarios;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      backgroundColor: theme.scaffoldBackgroundColor,
      body: SafeArea(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          mainAxisSize: MainAxisSize.min,
          children: [
            for (final scale in goldenTextScales)
              for (final scenario in scenarios) ...[
                Container(
                  width: double.infinity,
                  color: theme.colorScheme.primary,
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                  child: Text(
                    '${scenario.label} · ölçek ${scale.toStringAsFixed(1)}',
                    style: TextStyle(
                      fontSize: 9,
                      color: theme.colorScheme.onPrimary,
                      fontFamily: 'Nunito',
                    ),
                  ),
                ),
                MediaQuery(
                  // ⚠️ `disableAnimations`: shimmer/skeleton sonsuz animasyon
                  // olduğu için her karede farklı çizilir → golden kararsız
                  // olurdu. "Hareketi azalt" yolu zaten üründe destekleniyor.
                  data: MediaQuery.of(context).copyWith(
                    textScaler: TextScaler.linear(scale),
                    disableAnimations: true,
                  ),
                  child: Padding(
                    padding: const EdgeInsets.all(8),
                    child: scenario.child,
                  ),
                ),
              ],
          ],
        ),
      ),
    );
  }
}
