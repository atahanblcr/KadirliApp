import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/features/news/application/news_text_scale.dart';

import '../../helpers/pump_app.dart';

/// Okuma boyutu tercihi (plan dışı ek, 12.14) — saf denetleyici testi.
///
/// **Neden widget testi değil:** tercih bir `SharedPreferences` yazımı; widget testinde
/// sahte saat platform kanalının Future'ını beklemiyor ve iddia **flaky** oluyor
/// (bu projede sabit gecikmelerin defalarca flaky test ürettiği not düşülmüş).
/// Denetleyici seviyesinde aynı sözleşme kararlı biçimde kilitlenebiliyor.
void main() {
  test('varsayılan Normal', () async {
    final container = await testContainer(prefs: const {});

    expect(container.read(newsTextScaleProvider), NewsTextScale.normal);
  });

  test('seçim KALICI — cihazda saklanır', () async {
    final container = await testContainer(prefs: const {});

    await container.read(newsTextScaleProvider.notifier).select(NewsTextScale.huge);

    expect(container.read(newsTextScaleProvider), NewsTextScale.huge);
    // Saklanmasaydı ayar her açılışta sıfırlanır ve kendisi işe yaramaz olurdu.
    final container2 = await testContainer(prefs: const {'news.textScale': 'huge'});
    expect(container2.read(newsTextScaleProvider), NewsTextScale.huge);
  });

  test('tanınmayan değer Normale düşer (liste boşalmaz)', () async {
    // Sürüm geçişinde bozulan bir tercih ekranı kilitlememeli — §5'in
    // "bilinmeyen değer varsayılana düşer" kuralının istemci karşılığı.
    final container = await testContainer(prefs: const {'news.textScale': 'devasa'});

    expect(container.read(newsTextScaleProvider), NewsTextScale.normal);
  });

  group('çarpım tavanı', () {
    Future<TextScaler> scalerAt(
      WidgetTester tester, {
      required double system,
      required NewsTextScale scale,
    }) async {
      late TextScaler result;
      await tester.pumpWidget(
        MediaQuery(
          data: MediaQueryData(textScaler: TextScaler.linear(system)),
          child: Builder(
            builder: (context) {
              result = effectiveNewsScaler(context, scale);
              return const SizedBox.shrink();
            },
          ),
        ),
      );
      return result;
    }

    testWidgets('sistem ölçeğiyle ÇARPILIR', (tester) async {
      final scaler = await scalerAt(
        tester,
        system: 1.0,
        scale: NewsTextScale.large,
      );

      expect(scaler.scale(1), closeTo(1.15, 0.001));
    });

    testWidgets('1.4 sistem + en büyük tercih TAVANDA kalır', (tester) async {
      // 🔴 Tavansız çarpım 1.4 × 1.3 = 1.82 olurdu: ekranın en dar yerleri hiç
      // denenmemiş bir ölçekte çizilir ve bu projenin yedi kez tekrarlamış taşma
      // sınıfına yeni bir kapı açılırdı.
      final scaler = await scalerAt(
        tester,
        system: 1.4,
        scale: NewsTextScale.huge,
      );

      expect(scaler.scale(1), kNewsMaxTextScale);
    });
  });
}
