import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:kadirli_app/core/navigation/app_modules.dart';

import '../../helpers/pump_app.dart';

/// **"İşlevsiz buton yok" denetimi** (MOBILE_UX_PLAN'ın şartı).
///
/// Ana Sayfa ızgarası [kAppModules] listesinden üretiliyor. Bu test her modülün
/// gerçekten açılabilir bir rotası olduğunu doğrular — listeye yeni modül
/// eklenip rotası unutulursa burası kırılır.
void main() {
  test('modül kaydı tutarlı: benzersiz kimlik, rota ve dolu alanlar', () {
    final ids = kAppModules.map((module) => module.id).toSet();
    final routes = kAppModules.map((module) => module.route).toSet();

    expect(ids.length, kAppModules.length, reason: 'modül kimlikleri benzersiz olmalı');
    expect(routes.length, kAppModules.length, reason: 'rotalar benzersiz olmalı');

    for (final module in kAppModules) {
      expect(module.label, isNotEmpty);
      expect(module.summary, isNotEmpty);
      expect(module.phase, matches(RegExp(r'^11\.\d+$')));
      expect(module.route, startsWith('/'));
      expect(module.endpoints, isNotEmpty, reason: '${module.id} hangi uca bağlanacak?');
      expect(moduleForRoute(module.route)?.id, module.id);
    }
  });

  testWidgets('her modül kartı açılabilir bir ekrana gider', (tester) async {
    await pumpApp(
      tester,
      prefs: const {'auth.guestChoice': true},
      adapter: routedAdapter(homeStubs()),
    );

    final router = GoRouter.of(tester.element(find.text('Modüller')));

    for (final module in kAppModules) {
      router.go(module.route);
      await tester.pumpAndSettle();

      // Ekran açıldı ve doğru modülü gösteriyor (başlıkta modül adı var).
      expect(
        find.text(module.label),
        findsWidgets,
        reason: '${module.id} rotası boş/hatalı ekran açtı',
      );
      expect(
        find.byType(ErrorWidget),
        findsNothing,
        reason: '${module.id} ekranı patladı',
      );
    }
  });
}
