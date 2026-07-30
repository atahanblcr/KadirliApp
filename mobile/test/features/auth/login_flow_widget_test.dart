import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/theme/theme_controller.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// Giriş akışının uçtan uca (ekran + router + ağ zinciri) testi.
///
/// Gerçek `GoRouter` redirect'i koşuyor: durum değiştikçe ekranlar kendiliğinden
/// değişmeli — hiçbir ekran elle `context.go` çağırmıyor.
void main() {
  Map<String, dynamic> meBody({String username = 'ahmetk'}) => {
    'id': '11111111-1111-1111-1111-111111111111',
    'phone': '+905321110001',
    'username': username,
    'role': 'user',
    'primaryNeighborhoodName': 'Savrun',
  };

  FakeHttpAdapter authAdapter({required bool isNewUser}) => routedAdapter({
    ...homeStubs(),
    '/v1/auth/login': (_) async => jsonResponse(
      successEnvelope({
        'message': 'OTP gönderildi',
        'expiresIn': 300,
        'retryAfter': 60,
        'otp': '123456',
      }),
    ),
    '/v1/auth/verify-otp': (_) async => jsonResponse(
      successEnvelope(
        isNewUser
            ? {'isNewUser': true, 'tempToken': 'TEMP'}
            : {
                'isNewUser': false,
                'accessToken': 'ACCESS',
                'refreshToken': 'REFRESH',
                'expiresIn': 86400,
              },
      ),
    ),
    '/v1/auth/register': (_) async => jsonResponse(
      successEnvelope({'accessToken': 'ACCESS', 'refreshToken': 'REFRESH'}),
    ),
    '/v1/users/me': (_) async =>
        jsonResponse(successEnvelope(meBody(username: isNewUser ? 'yenikomsu' : 'ahmetk'))),
    '/v1/neighborhoods': (_) async => jsonResponse(
      successEnvelope([
        {'id': '22222222-2222-2222-2222-222222222222', 'name': 'Savrun', 'slug': 'savrun'},
        {'id': '33333333-3333-3333-3333-333333333333', 'name': 'Cengiz Topel', 'slug': 'ct'},
      ]),
    ),
  });

  testWidgets('ilk açılışta giriş ekranı gelir (oturum yok, misafir seçimi yok)', (
    tester,
  ) async {
    await pumpApp(tester, adapter: routedAdapter(homeStubs()));

    expect(find.text('Telefonunuzla giriş yapın'), findsOneWidget);
    expect(find.text('Kod Gönder'), findsOneWidget);
  });

  testWidgets('geçersiz numara sunucuya gitmez, alan altında uyarı çıkar', (tester) async {
    final adapter = routedAdapter(homeStubs());
    await pumpApp(tester, adapter: adapter);

    await tester.enterText(find.byType(TextField), '532 111 00');
    await tester.tap(find.text('Kod Gönder'));
    await tester.pump();

    expect(find.text('Numara 5 ile başlayan 10 hane olmalı.'), findsOneWidget);
    expect(adapter.countOf('/v1/auth/login'), 0);
  });

  testWidgets('kayıtlı kullanıcı: telefon → kod → Ana Sayfa', (tester) async {
    final adapter = authAdapter(isNewUser: false);
    final container = await pumpApp(tester, adapter: adapter);

    await tester.enterText(find.byType(TextField), '5321110001');
    await tester.tap(find.text('Kod Gönder'));
    await settleApp(tester);

    // Kod ekranı: dev modda kod otomatik dolu.
    expect(find.text('Doğrula'), findsOneWidget);
    expect(adapter.countOf('/v1/auth/login'), 1);

    await tester.tap(find.text('Doğrula'));
    await settleApp(tester);

    expect(find.textContaining('ahmetk'), findsOneWidget);
    expect(adapter.countOf('/v1/auth/verify-otp'), 1);
    // ⚠️ Kod ekranı yığından TAMAMEN kalkmalı: `push` ile açıldığı için
    // redirect yalnız altındaki konumu değiştiriyordu ve kullanıcı boşalmış
    // kod ekranında sıkışıyordu (31 Tem 2026 canlı testinde yakalandı).
    expect(find.text('Doğrula'), findsNothing);

    final store = container.read(tokenStoreProvider);
    expect(await store.readAccessToken(), 'ACCESS');
  });

  testWidgets('yeni kullanıcı: kod → kayıt ekranı → mahalle seçimi → Ana Sayfa', (
    tester,
  ) async {
    final adapter = authAdapter(isNewUser: true);
    await pumpApp(tester, adapter: adapter);

    await tester.enterText(find.byType(TextField), '5339990001');
    await tester.tap(find.text('Kod Gönder'));
    await settleApp(tester);

    await tester.tap(find.text('Doğrula'));
    await settleApp(tester);

    // Kayıt ekranı geldi (router "registering" durumunu gördü).
    expect(find.text('Numaranız doğrulandı 🎉'), findsOneWidget);

    // Kullanıcı adı + mahalle
    await tester.enterText(find.byType(TextField).first, 'yenikomsu');
    await tester.tap(find.byType(DropdownButtonFormField<String>));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Savrun').last);
    await tester.pumpAndSettle();

    await tester.tap(find.text('Kaydı Tamamla'));
    await settleApp(tester);

    expect(find.textContaining('yenikomsu'), findsOneWidget);
    expect(adapter.countOf('/v1/auth/register'), 1);
  });

  testWidgets('mahalle seçilmeden kayıt gönderilmez', (tester) async {
    final adapter = authAdapter(isNewUser: true);
    await pumpApp(tester, adapter: adapter);

    await tester.enterText(find.byType(TextField), '5339990001');
    await tester.tap(find.text('Kod Gönder'));
    await settleApp(tester);
    await tester.tap(find.text('Doğrula'));
    await settleApp(tester);

    await tester.enterText(find.byType(TextField).first, 'yenikomsu');
    await tester.tap(find.text('Kaydı Tamamla'));
    await tester.pump();

    expect(find.text('Mahalle seçin.'), findsOneWidget);
    expect(adapter.countOf('/v1/auth/register'), 0);
  });

  testWidgets('hatalı kod: INVALID_OTP mesajı görünür, ekran değişmez', (tester) async {
    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/auth/login': (_) async => jsonResponse(
        successEnvelope({'message': 'OTP gönderildi', 'expiresIn': 300, 'retryAfter': 60}),
      ),
      '/v1/auth/verify-otp': (_) async => jsonResponse(
        errorEnvelope('INVALID_OTP', 'Geçersiz veya süresi dolmuş OTP.'),
        statusCode: 400,
      ),
    });
    await pumpApp(tester, adapter: adapter);

    await tester.enterText(find.byType(TextField), '5321110001');
    await tester.tap(find.text('Kod Gönder'));
    await settleApp(tester);

    await tester.enterText(find.byType(TextField), '000000');
    await settleApp(tester);

    expect(find.text('Geçersiz veya süresi dolmuş OTP.'), findsOneWidget);
    expect(find.text('Doğrula'), findsOneWidget); // hâlâ kod ekranındayız
  });

  testWidgets('misafir olarak devam → Ana Sayfa ve tercih kalıcı', (tester) async {
    final container = await pumpApp(tester, adapter: routedAdapter(homeStubs()));

    await tester.tap(find.text('Misafir olarak devam et'));
    await settleApp(tester);

    expect(find.textContaining('👋'), findsOneWidget);
    expect(find.text('Modüller'), findsOneWidget);
    expect(container.read(sharedPreferencesProvider).getBool('auth.guestChoice'), isTrue);
  });

  testWidgets('oturum varken açılışta doğrudan Ana Sayfa', (tester) async {
    await pumpApp(
      tester,
      tokenStore: InMemoryTokenStore(accessToken: 'ACCESS', refreshToken: 'REFRESH'),
      adapter: routedAdapter({
        ...homeStubs(),
        '/v1/users/me': (_) async => jsonResponse(successEnvelope(meBody())),
      }),
    );

    expect(find.textContaining('ahmetk'), findsOneWidget);
    expect(find.text('Modüller'), findsOneWidget);
  });

  testWidgets('çıkış yapınca oturum kapanır ve Giriş ekranına dönülür', (tester) async {
    final store = InMemoryTokenStore(accessToken: 'ACCESS', refreshToken: 'REFRESH');
    await pumpApp(
      tester,
      tokenStore: store,
      adapter: routedAdapter({
        ...homeStubs(),
        '/v1/users/me': (_) async => jsonResponse(successEnvelope(meBody())),
        '/v1/auth/logout': (_) async =>
            jsonResponse(successEnvelope({'message': 'Çıkış yapıldı'})),
      }),
    );

    // 11.4: çıkış Ayarlar ekranında (sağ üst ⚙️).
    await tester.tap(find.byTooltip('Ayarlar'));
    await tester.pumpAndSettle();

    // 11.5: Ayarlar büyüdü (bildirim tercihleri + hesap işlemleri) → çıkış
    // satırı ekranın altında kalıyor.
    await tester.scrollUntilVisible(find.text('Çıkış yap'), 200);
    await tester.pumpAndSettle();

    await tester.tap(find.text('Çıkış yap'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Çıkış yap').last); // onay diyaloğu
    await settleApp(tester);

    expect(await store.hasSession(), isFalse);
    expect(find.text('Telefonunuzla giriş yapın'), findsOneWidget);
  });

  testWidgets('misafir Profil sekmesinde daveti görür, sekme kabuğu kalır', (tester) async {
    await pumpApp(
      tester,
      prefs: {'auth.guestChoice': true},
      adapter: routedAdapter(homeStubs()),
    );

    await tester.tap(find.text('Profil'));
    await tester.pumpAndSettle();

    // Sert yönlendirme YOK: giriş ekranına atılmadık, davet ekranın içinde.
    expect(find.text('Hesabınıza giriş yapın'), findsOneWidget);
    expect(find.text('Telefonunuzla giriş yapın'), findsNothing);
    expect(find.text('Ana Sayfa'), findsOneWidget); // alt sekmeler yerinde

    // Davetteki "Giriş yap" gerçek giriş akışını açar.
    await tester.tap(find.text('Giriş yap'));
    await tester.pumpAndSettle();
    expect(find.text('Telefonunuzla giriş yapın'), findsOneWidget);
  });
}
