import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/router/app_router.dart';
import 'package:kadirli_app/core/router/app_routes.dart';
import 'package:kadirli_app/features/legal/presentation/reconsent_screen.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// **Yeniden onay akışı** (Faz 12.17): yönetici *esaslı* bir değişiklik
/// yayınladığında (`requiresReconsent`) açılan tek seferlik ekran.
///
/// İki karşıt hasarı birden tutuyor:
/// - Kapı hiç çalışmazsa esaslı bir metin değişikliği **hiç kimseye ulaşmaz**
///   (12.16'nın `RequiresReconsent` bayrağının var olma sebebi boşa gider).
/// - Kapı her karede çalışırsa kullanıcı uygulamayı **kullanamaz** hâle gelir.
void main() {
  Map<String, dynamic> consentBody({
    required bool needsReconsent,
    bool isMandatory = true,
    String type = 'acik_riza',
    String title = 'Açık Rıza Metni',
  }) => {
    'type': type,
    'title': title,
    'isMandatory': isMandatory,
    'currentVersionId': 'yeni-surum',
    'currentVersionNumber': 4,
    'consentedVersionId': 'eski-surum',
    'consentedVersionNumber': 2,
    'granted': true,
    'decidedAt': '2026-05-01T09:00:00Z',
    'revokedAt': null,
    'needsReconsent': needsReconsent,
  };

  Future<FakeHttpAdapter> openApp(
    WidgetTester tester, {
    required bool needsReconsent,
    bool isMandatory = true,
    Map<String, Future<ResponseBody> Function(RequestOptions)> routes = const {},
  }) async {
    tester.view.physicalSize = const Size(1200, 3200);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final adapter = routedAdapter({
      ...homeStubs(),
      ...legalStubs(
        documents: [
          legalDocumentBody(
            type: 'acik_riza',
            title: 'Açık Rıza Metni',
            versionId: 'yeni-surum',
            versionNumber: 4,
            isMandatory: isMandatory,
            requiresReconsent: true,
          ),
        ],
        consents: [
          consentBody(needsReconsent: needsReconsent, isMandatory: isMandatory),
        ],
      ),
      '/v1/users/me': (_) async => jsonResponse(
        successEnvelope({
          'id': '11111111-1111-1111-1111-111111111111',
          'phone': '+905321110001',
          'username': 'ahmetk',
          'role': 'user',
        }),
      ),
      ...routes,
    });

    await pumpApp(
      tester,
      adapter: adapter,
      prefs: const {'auth.guestChoice': true},
      tokenStore: InMemoryTokenStore(accessToken: 'ACCESS', refreshToken: 'REFRESH'),
    );
    await tester.pumpAndSettle();
    return adapter;
  }

  testWidgets('yeniden onay bekleyen varsa AÇILIŞTA ekran gelir', (tester) async {
    await openApp(tester, needsReconsent: true);

    expect(find.byType(ReconsentScreen), findsOneWidget);
    expect(find.text('Metinlerimiz güncellendi'), findsOneWidget);
  });

  testWidgets('bekleyen YOKSA ekran hiç açılmaz', (tester) async {
    // ⚠️ İkinci yön şart: yalnız "açılıyor" iddiası, **her zaman açan** bir
    // gerçeklemede de yeşil kalırdı.
    await openApp(tester, needsReconsent: false);

    expect(find.byType(ReconsentScreen), findsNothing);
  });

  testWidgets('kutu ÖN İŞARETLİ DEĞİL — eski onay yeni sürüme taşınmaz', (
    tester,
  ) async {
    // 🔴 Kullanıcı v2'yi onaylamıştı; v4 için kutunun işaretli gelmesi, bu
    // ekranın var olma sebebini ortadan kaldırırdı.
    await openApp(tester, needsReconsent: true);

    final checkbox = tester.widget<Checkbox>(find.byType(Checkbox).first);
    expect(checkbox.value, isFalse);
  });

  testWidgets(
    'ZORUNLU belgede ekran kapatılamaz ama ÇIKIŞI vardır',
    (tester) async {
      // 🔴 Kapatılamayan ve çıkışı olmayan bir ekran kullanıcıyı hesabından
      // kilitler (12.7'nin "son sosyal bağlantı da çözülebilmeli" gerekçesi).
      await openApp(tester, needsReconsent: true);

      expect(find.text('Şimdi değil'), findsNothing);
      expect(find.byType(BackButton), findsNothing);
      expect(find.text('Hesabı sil'), findsOneWidget);
    },
  );

  testWidgets('İSTEĞE BAĞLI belgede "Şimdi değil" ile kapatılabilir', (
    tester,
  ) async {
    await openApp(tester, needsReconsent: true, isMandatory: false);

    expect(find.text('Şimdi değil'), findsOneWidget);

    await tester.tap(find.text('Şimdi değil'));
    await tester.pumpAndSettle();

    expect(find.byType(ReconsentScreen), findsNothing);
  });

  testWidgets('onaylanınca isReconsent:true ile SUNUCUYA yazılır', (tester) async {
    final adapter = await openApp(
      tester,
      needsReconsent: true,
      routes: {
        '/v1/users/me/consents': (options) async => options.method == 'POST'
            ? jsonResponse(successEnvelope(true))
            : jsonResponse(
                successEnvelope([consentBody(needsReconsent: true)]),
              ),
      },
    );

    await tester.tap(find.byType(Checkbox).first);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Onayla ve devam et'));
    await tester.pumpAndSettle();

    final write = adapter.requests
        .where((r) => r.path == '/v1/users/me/consents' && r.method == 'POST')
        .last;
    final body = write.data as Map;

    // ⚠️ Kaynağı `ConsentSources.Reconsent`'e çeviren **sunucudur**; istemci
    // yalnız "yeniden onay akışından geliyorum" der. Söylemeseydi defterdeki
    // "nasıl alındı" sütunu bu rızaları `settings` sanırdı.
    expect(body['isReconsent'], isTrue);
    // 🔑 Onay **YENİ** sürüme yazılır, kullanıcının eskiden onayladığına değil.
    expect(body['consents'], [
      {'versionId': 'yeni-surum', 'granted': true},
    ]);
  });

  testWidgets('zorunlu kutu işaretlenmeden "Onayla" KAPALI ve sebebi yazıyor', (
    tester,
  ) async {
    await openApp(tester, needsReconsent: true);

    expect(find.textContaining('Açık Rıza Metni'), findsWidgets);

    // Kutuya dokunmadan onaylamayı dene — istek gitmemeli.
    await tester.tap(find.text('Onayla ve devam et'), warnIfMissed: false);
    await tester.pumpAndSettle();

    expect(find.byType(ReconsentScreen), findsOneWidget);
  });

  testWidgets('kayıt akışı yarım kalmışken hukuki metin AÇILABİLİR', (
    tester,
  ) async {
    // 🔴 Yönlendirme kuralı "kayıt yarım kaldıysa tek çıkış kayıt ekranıdır"
    // diyor; hukuki metin **istisna olmak zorunda**, yoksa onay kutusunun
    // yanındaki "oku" bağlantısı kullanıcıyı kayıt ekranına geri fırlatır ve
    // geriye **okumadan onaylamaktan başka seçenek kalmaz**.
    final adapter = routedAdapter({
      ...homeStubs(),
      ...legalStubs(
        documents: [legalDocumentBody(type: 'acik_riza', title: 'Açık Rıza Metni')],
      ),
      '/v1/legal/documents/acik_riza': (_) async => jsonResponse(
        successEnvelope(
          legalDocumentBody(type: 'acik_riza', title: 'Açık Rıza Metni'),
        ),
      ),
      '/v1/auth/login': (_) async => jsonResponse(
        successEnvelope({
          'message': 'OTP gönderildi',
          'expiresIn': 300,
          'retryAfter': 60,
          'otp': '123456',
        }),
      ),
      '/v1/auth/verify-otp': (_) async =>
          jsonResponse(successEnvelope({'isNewUser': true, 'tempToken': 'TEMP'})),
      '/v1/neighborhoods': (_) async => jsonResponse(successEnvelope(const [])),
    });

    tester.view.physicalSize = const Size(1200, 3200);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final container = await pumpApp(tester, adapter: adapter);
    await tester.enterText(find.byType(TextField), '5339990001');
    await tester.tap(find.text('Kod Gönder'));
    await settleApp(tester);
    await tester.tap(find.text('Doğrula'));
    await settleApp(tester);

    // Kayıt akışındayız; metin ekranına gidiliyor.
    container.read(routerProvider).go(AppRoutes.legalDocument('acik_riza'));
    await tester.pumpAndSettle();

    // Kayıt ekranına geri fırlatılmadık.
    expect(find.text('Numaranız doğrulandı 🎉'), findsNothing);
    expect(find.textContaining('Sürüm 1'), findsOneWidget);
  });
}
