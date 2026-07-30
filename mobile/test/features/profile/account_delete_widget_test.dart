import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/widgets/widgets.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/profile_fixtures.dart';
import '../../helpers/pump_app.dart';

/// Hesap silme (mağaza zorunluluğu): onay → `DELETE /v1/users/me` → yerel
/// oturum kapanır → Giriş ekranı.
void main() {
  Future<InMemoryTokenStore> openDeleteScreen(
    WidgetTester tester,
    FakeHttpAdapter adapter,
  ) async {
    final store = InMemoryTokenStore(accessToken: 'A', refreshToken: 'REFRESH');
    await pumpApp(tester, tokenStore: store, adapter: adapter);

    await tester.tap(find.byTooltip('Ayarlar'));
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(find.text('Hesabı sil'), 200);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Hesabı sil'));
    await tester.pumpAndSettle();
    return store;
  }

  FakeHttpAdapter adapterWith({
    Map<String, dynamic>? profile,
    Object? deleteResponse,
    int deleteStatus = 200,
  }) => routedAdapter({
    ...homeStubs(),
    '/v1/users/me': (options) async => options.method == 'DELETE'
        ? jsonResponse(deleteResponse ?? successEnvelope(true), statusCode: deleteStatus)
        : jsonResponse(successEnvelope(profile ?? profileBody())),
  });

  testWidgets('sonuçlar listelenir, onaysız silme yapılmaz', (tester) async {
    final adapter = adapterWith();
    await openDeleteScreen(tester, adapter);

    expect(find.text('Hesabınızı silmek üzeresiniz'), findsOneWidget);
    expect(find.textContaining('İlanlarınız yayından kaldırılır'), findsOneWidget);

    await tester.tap(find.text('Hesabımı sil'));
    await tester.pumpAndSettle();
    // Onay diyaloğu açıldı, henüz istek yok.
    expect(find.text('Hesabınız silinsin mi?'), findsOneWidget);
    expect(
      adapter.requests.where((r) => r.method == 'DELETE'),
      isEmpty,
    );

    await tester.tap(find.text('Vazgeç').first);
    await tester.pumpAndSettle();
    expect(
      adapter.requests.where((r) => r.method == 'DELETE'),
      isEmpty,
    );
  });

  testWidgets('onaydan sonra hesap silinir ve Giriş ekranına dönülür', (
    tester,
  ) async {
    final adapter = adapterWith();
    final store = await openDeleteScreen(tester, adapter);

    await tester.tap(find.text('Hesabımı sil'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Evet, sil'));
    await settleApp(tester);

    final deleteRequest = adapter.requests.lastWhere((r) => r.method == 'DELETE');
    expect(deleteRequest.path, '/v1/users/me');
    expect(deleteRequest.data, {'refreshToken': 'REFRESH'});

    // Yerel oturum kapandı; çıkış ucu ÇAĞRILMADI (hesap zaten pasif).
    expect(await store.hasSession(), isFalse);
    expect(adapter.countOf('/v1/auth/logout'), 0);

    expect(find.text('Telefonunuzla giriş yapın'), findsOneWidget);
    expect(find.textContaining('Hesabınız silindi'), findsOneWidget);
    // ⚠️ Silme ekranı yığından TAMAMEN kalkmalı: redirect yalnız altındaki
    // konumu değiştirdiği için ekran üstte kalıp sonsuz spinner gösteriyordu
    // (canlı testte yakalandı, bu iddia olmadan test bunu göremiyordu).
    expect(find.text('Hesabımı sil'), findsNothing);
    expect(find.byType(CircularProgressIndicator), findsNothing);
  });

  testWidgets('SELF_DELETE_FORBIDDEN mesajı gösterilir, oturum korunur', (
    tester,
  ) async {
    final adapter = adapterWith(
      profile: profileBody(role: 'admin'),
      deleteResponse: errorEnvelope(
        ApiErrorCodes.selfDeleteForbidden,
        'Yönetici/personel hesapları bu uçtan silinemez; panel üzerinden yönetilir.',
      ),
      deleteStatus: 403,
    );
    final store = await openDeleteScreen(tester, adapter);

    // Yönetici hesabında buton baştan kapalı — kullanıcı hataya hiç çarpmaz.
    expect(
      find.textContaining('Yönetici ve personel hesapları uygulamadan silinemez'),
      findsOneWidget,
    );
    // Uyarı şeridi eklendiği için buton ekranın altına kaydı.
    await tester.scrollUntilVisible(find.text('Hesabımı sil'), 200);
    await tester.pumpAndSettle();

    final button = tester.widget<AppButton>(
      find.widgetWithText(AppButton, 'Hesabımı sil'),
    );
    expect(button.onPressed, isNull);
    expect(await store.hasSession(), isTrue);
  });
}
