import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/features/notifications/application/unread_count_provider.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// Bildirim rozeti + Bildirimler sekmesi (11.4).
void main() {
  Map<String, dynamic> meBody() => {
    'id': '11111111-1111-1111-1111-111111111111',
    'phone': '+905321110001',
    'username': 'ahmetk',
    'role': 'user',
    'primaryNeighborhoodName': 'Savrun',
  };

  testWidgets('misafirken bildirim ucuna hiç istek gitmez', (tester) async {
    final adapter = routedAdapter(homeStubs(unreadCount: 5));
    await pumpApp(tester, prefs: {'auth.guestChoice': true}, adapter: adapter);

    // Uç `[A]` — anonim kullanıcı için istek atmak boşuna 401 üretirdi.
    expect(adapter.countOf('/v1/notifications'), 0);
    expect(find.byType(Badge), findsNothing);
  });

  testWidgets('oturum açıkken okunmamış sayısı rozette görünür', (tester) async {
    final adapter = routedAdapter({
      ...homeStubs(unreadCount: 3),
      '/v1/users/me': (_) async => jsonResponse(successEnvelope(meBody())),
    });
    await pumpApp(
      tester,
      tokenStore: InMemoryTokenStore(accessToken: 'ACCESS', refreshToken: 'REFRESH'),
      adapter: adapter,
    );

    expect(adapter.countOf('/v1/notifications'), 1);
    expect(find.text('3'), findsOneWidget);
  });

  testWidgets('99\'dan fazlası "99+" olarak kısalır', (tester) async {
    await pumpApp(
      tester,
      tokenStore: InMemoryTokenStore(accessToken: 'ACCESS', refreshToken: 'REFRESH'),
      adapter: routedAdapter({
        ...homeStubs(unreadCount: 1250),
        '/v1/users/me': (_) async => jsonResponse(successEnvelope(meBody())),
      }),
    );

    expect(find.text('99+'), findsOneWidget);
  });

  testWidgets('misafir Bildirimler sekmesinde davet görür', (tester) async {
    await pumpApp(
      tester,
      prefs: {'auth.guestChoice': true},
      adapter: routedAdapter(homeStubs()),
    );

    await tester.tap(
      find.descendant(of: find.byType(NavigationBar), matching: find.text('Bildirim')),
    );
    await tester.pumpAndSettle();

    expect(find.text('Bildirimleriniz burada olacak'), findsOneWidget);
  });

  // ⚠️ 11.13'te bu iddia bilinçli olarak değişti: ekran artık "N okunmamış
  // bildirim" özeti gösteren bir yer tutucu değil, **gerçek liste**. Rozetin
  // sayısı yukarıdaki testlerde doğrulanıyor; burada listenin geldiği ve
  // sekmenin çalıştığı doğrulanıyor. (Modül "yakında"dan gerçeğe geçince eski
  // testlerin kırılması **beklenen sinyaldir** — 11.6'da da böyle olmuştu.)
  testWidgets('oturum açıkken Bildirimler sekmesi listeyi gösterir', (
    tester,
  ) async {
    await pumpApp(
      tester,
      tokenStore: InMemoryTokenStore(accessToken: 'ACCESS', refreshToken: 'REFRESH'),
      adapter: routedAdapter({
        ...homeStubs(unreadCount: 0),
        '/v1/users/me': (_) async => jsonResponse(successEnvelope(meBody())),
      }),
    );

    await tester.tap(
      find.descendant(of: find.byType(NavigationBar), matching: find.text('Bildirim')),
    );
    await tester.pumpAndSettle();

    expect(find.text('Henüz bildiriminiz yok'), findsOneWidget);
  });

  testWidgets('rozet ARTINCA kısa bir "pop" oynar (11.15 cilası)', (tester) async {
    // 📌 Push ile gelen bildirim uygulama açıkken rozeti sessizce artırıyordu;
    // kullanıcı alt çubuğa bakmıyorsa değişimi hiç fark etmiyordu.
    var unread = 1;
    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/users/me': (_) async => jsonResponse(successEnvelope(meBody())),
      '/v1/notifications': (_) async => jsonResponse(
        successEnvelope({
          'items': <dynamic>[],
          'totalCount': 0,
          'pageSize': 1,
          'currentPage': 1,
          'totalPages': 0,
          'unreadCount': unread,
        }),
      ),
    });
    final container = await pumpApp(
      tester,
      tokenStore: InMemoryTokenStore(accessToken: 'ACCESS', refreshToken: 'REFRESH'),
      adapter: adapter,
    );
    expect(find.text('1'), findsOneWidget);

    // Dinlenmeye bırakılınca ölçek 1.0'a döner.
    final restingScale =
        tester.widget<ScaleTransition>(find.byKey(const ValueKey('unreadBadgePop'))).scale.value;
    expect(restingScale, 1.0);

    unread = 2;
    container.invalidate(unreadNotificationCountProvider);
    // ⚠️ `waitUntil` widget testinde KULLANILAMAZ: gerçek zamanı bekler, test
    // saati ise `pump` ile ilerler → süit kilitlenir. Kareler elle ilerletilir.
    for (var i = 0; i < 8 && !tester.any(find.text('2')); i++) {
      await tester.pump(const Duration(milliseconds: 16));
    }
    expect(find.text('2'), findsOneWidget);
    await tester.pump(const Duration(milliseconds: 80));

    final poppedScale =
        tester.widget<ScaleTransition>(find.byKey(const ValueKey('unreadBadgePop'))).scale.value;
    expect(poppedScale, greaterThan(1.0), reason: 'Artışta rozet büyümeli');

    // Animasyon kalıcı değil: biraz sonra 1.0'a döner.
    await tester.pump(const Duration(milliseconds: 400));
    expect(
      tester.widget<ScaleTransition>(find.byKey(const ValueKey('unreadBadgePop'))).scale.value,
      1.0,
    );
  });
}
