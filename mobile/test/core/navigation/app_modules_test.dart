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
      // ⚠️ Desen 12.14'te genişletildi: eskiden `^11\.\d+$` yazıyordu ve
      // **faz numarasını** çiviliyordu, oysa iddia "alt-faz dolu ve biçimli"
      // olmalı. 13. modül (Haberler, 12.14) bu yüzden testi kırdı — kuralın
      // kendisi değil, elle tutulan deseni eskimişti. (Bu projenin
      // `CODE_REVIEW_CHECKLIST` §2'deki "taramanın kapsamı da elle tutulan bir
      // listedir" dersinin küçük bir tekrarı.)
      expect(module.phase, matches(RegExp(r'^\d+\.\d+[a-z]?$')));
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

      expect(
        find.byType(ErrorWidget),
        findsNothing,
        reason: '${module.id} ekranı patladı',
      );

      if (module.ready) {
        // Gerçeklenmiş modül (11.6+): "yakında" ekranı ARTIK ÇIKMAMALI.
        // Ekran başlığı modül etiketiyle birebir aynı olmak zorunda değil
        // (ızgarada "Kesinti", ekranda "Elektrik Kesintileri").
        expect(
          find.textContaining('yakında'),
          findsNothing,
          reason: '${module.id} ready=true ama hâlâ "yakında" ekranı açıyor',
        );
      } else {
        expect(
          find.text('${module.label} yakında'),
          findsOneWidget,
          reason: '${module.id} rotası boş/hatalı ekran açtı',
        );
      }
    }
  });
}
