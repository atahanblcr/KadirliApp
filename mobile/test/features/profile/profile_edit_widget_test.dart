import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/widgets/widgets.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/profile_fixtures.dart';
import '../../helpers/pump_app.dart';

/// Profil düzenleme ekranı: 30 gün kısıtının önden gösterilmesi, PATCH
/// gövdesinin yalnız değişenleri taşıması, sunucu hatasının alan altına düşmesi.
void main() {
  /// Oturumu açıp Profil sekmesinden düzenleme ekranına gider.
  Future<void> openEditScreen(
    WidgetTester tester,
    FakeHttpAdapter adapter,
  ) async {
    await pumpApp(
      tester,
      tokenStore: InMemoryTokenStore(accessToken: 'A', refreshToken: 'R'),
      adapter: adapter,
    );

    await tester.tap(find.text('Profil'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Profili düzenle'));
    await tester.pumpAndSettle();
  }

  FakeHttpAdapter adapterWith({required Map<String, dynamic> profile}) =>
      routedAdapter({
    ...homeStubs(),
    '/v1/users/me': (_) async => jsonResponse(successEnvelope(profile)),
    '/v1/neighborhoods': (_) async => jsonResponse(successEnvelope(neighborhoodsBody())),
  });

  /// Etiketinden alanı bulur (yardımcı metinlerde de sayı geçtiği için
  /// `widgetWithText` güvenilir değil).
  AppTextField fieldByLabel(WidgetTester tester, String label) => tester.widget(
    find.ancestor(of: find.text(label), matching: find.byType(AppTextField)),
  );

  /// Kaydet butonu form uzayınca ekranın altına kayıyor → önce görünür yapılır.
  Future<void> tapSave(WidgetTester tester) async {
    await tester.ensureVisible(find.text('Kaydet'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Kaydet'));
    await tester.pumpAndSettle();
  }

  testWidgets('mevcut profil forma dolu gelir', (tester) async {
    await openEditScreen(tester, adapterWith(profile: profileBody()));

    expect(fieldByLabel(tester, 'Kullanıcı adı').controller?.text, 'ahmetk');
    expect(fieldByLabel(tester, 'Yaş (isteğe bağlı)').controller?.text, '30');
    expect(find.text('Savrun (mahalle)'), findsWidgets);
  });

  testWidgets('30 gün kısıtı varsa alan kilitli ve kalan gün yazıyor', (tester) async {
    final soon = DateTime.now().toUtc().subtract(const Duration(days: 20));
    await openEditScreen(
      tester,
      adapterWith(
        profile: profileBody(usernameLastChangedAt: soon.toIso8601String()),
      ),
    );

    expect(
      find.textContaining('Kullanıcı adınızı 10 gün sonra'),
      findsOneWidget,
    );
    expect(fieldByLabel(tester, 'Kullanıcı adı').enabled, isFalse);
  });

  testWidgets('kaydet yalnız değişen alanı gönderir', (tester) async {
    final adapter = adapterWith(profile: profileBody());
    await openEditScreen(tester, adapter);

    await tester.enterText(find.widgetWithText(TextField, 'ahmetk'), 'yeniad');
    await tapSave(tester);

    final request = adapter.lastOf('/v1/users/me')!;
    expect(request.method, 'PATCH');
    expect(request.data, {'username': 'yeniad'});
  });

  testWidgets('hiçbir şey değişmediyse istek atılmaz', (tester) async {
    final adapter = adapterWith(profile: profileBody());
    await openEditScreen(tester, adapter);

    final before = adapter.countOf('/v1/users/me');
    await tapSave(tester);

    expect(adapter.countOf('/v1/users/me'), before);
    expect(find.text('Değişiklik yapılmadı.'), findsOneWidget);
  });

  testWidgets('yerel kural: 3 karakterden kısa ad sunucuya gitmez', (tester) async {
    final adapter = adapterWith(profile: profileBody());
    await openEditScreen(tester, adapter);

    final before = adapter.countOf('/v1/users/me');
    await tester.enterText(find.widgetWithText(TextField, 'ahmetk'), 'ab');
    await tapSave(tester);

    expect(find.text('Kullanıcı adı 3-30 karakter olmalı.'), findsOneWidget);
    expect(adapter.countOf('/v1/users/me'), before);
  });

  testWidgets('sunucu USERNAME_CHANGE_LIMIT derse mesaj alanın altında çıkar', (
    tester,
  ) async {
    var patched = false;
    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/users/me': (options) async {
        if (options.method == 'PATCH') {
          patched = true;
          return jsonResponse(
            errorEnvelope(
              ApiErrorCodes.usernameChangeLimit,
              'Kullanıcı adı 30 günde bir değiştirilebilir.',
            ),
            statusCode: 400,
          );
        }
        return jsonResponse(successEnvelope(profileBody()));
      },
      '/v1/neighborhoods': (_) async =>
          jsonResponse(successEnvelope(neighborhoodsBody())),
    });

    await pumpApp(
      tester,
      tokenStore: InMemoryTokenStore(accessToken: 'A', refreshToken: 'R'),
      adapter: adapter,
    );
    await tester.tap(find.text('Profil'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Profili düzenle'));
    await tester.pumpAndSettle();

    await tester.enterText(find.widgetWithText(TextField, 'ahmetk'), 'baskaad');
    await tapSave(tester);

    expect(patched, isTrue);
    expect(find.text('Kullanıcı adı 30 günde bir değiştirilebilir.'), findsOneWidget);
    // Ekran kapanmadı — kullanıcı düzeltebilir.
    expect(find.text('Kaydet'), findsOneWidget);
  });

  testWidgets('başarılı kayıt oturumdaki profili tazeler', (tester) async {
    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/users/me': (options) async => jsonResponse(
        successEnvelope(
          profileBody(username: options.method == 'PATCH' ? 'yeniad' : 'ahmetk'),
        ),
      ),
      '/v1/neighborhoods': (_) async =>
          jsonResponse(successEnvelope(neighborhoodsBody())),
    });

    await pumpApp(
      tester,
      tokenStore: InMemoryTokenStore(accessToken: 'A', refreshToken: 'R'),
      adapter: adapter,
    );
    await tester.tap(find.text('Profil'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Profili düzenle'));
    await tester.pumpAndSettle();

    await tester.enterText(find.widgetWithText(TextField, 'ahmetk'), 'yeniad');
    await tapSave(tester);

    // Düzenleme ekranı kapandı, Profil sekmesinde yeni ad görünüyor.
    expect(find.text('Kaydet'), findsNothing);
    expect(find.text('yeniad'), findsOneWidget);
  });
}
