import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';

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

  testWidgets('oturum açıkken Bildirimler sekmesi sayıyı gösterir', (tester) async {
    await pumpApp(
      tester,
      tokenStore: InMemoryTokenStore(accessToken: 'ACCESS', refreshToken: 'REFRESH'),
      adapter: routedAdapter({
        ...homeStubs(unreadCount: 2),
        '/v1/users/me': (_) async => jsonResponse(successEnvelope(meBody())),
      }),
    );

    await tester.tap(
      find.descendant(of: find.byType(NavigationBar), matching: find.text('Bildirim')),
    );
    await tester.pumpAndSettle();

    expect(find.text('2 okunmamış bildirim'), findsOneWidget);
  });
}
