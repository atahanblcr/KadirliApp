import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/router/app_router.dart';
import 'package:kadirli_app/core/router/app_routes.dart';
import 'package:kadirli_app/core/widgets/widgets.dart';
import 'package:kadirli_app/features/legal/presentation/legal_document_screen.dart';
import 'package:kadirli_app/features/legal/presentation/legal_documents_screen.dart';
import 'package:kadirli_app/features/legal/presentation/legal_version_screen.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// **Ayarlar › Yasal metinler** (Faz 12.17).
///
/// Ekranın üç işi var ve üçü de plandaki bitti-kriterinde: metni okumak,
/// *"onayladığınız sürüm: v2"*, isteğe bağlı rızayı geri almak.
void main() {
  Map<String, dynamic> consentBody({
    String type = 'ticari_ileti',
    String title = 'Ticari İleti İzni',
    bool isMandatory = false,
    String currentVersionId = 'bbbbbbbb-2222-2222-2222-222222222222',
    int currentVersionNumber = 3,
    String? consentedVersionId,
    int? consentedVersionNumber,
    bool granted = false,
    String? decidedAt,
    bool needsReconsent = false,
  }) => {
    'type': type,
    'title': title,
    'isMandatory': isMandatory,
    'currentVersionId': currentVersionId,
    'currentVersionNumber': currentVersionNumber,
    'consentedVersionId': consentedVersionId,
    'consentedVersionNumber': consentedVersionNumber,
    'granted': granted,
    'decidedAt': decidedAt,
    'revokedAt': null,
    'needsReconsent': needsReconsent,
  };

  /// Uygulamayı ayağa kaldırıp doğrudan bir rotaya gider (12.14'ün
  /// `openDetail` deseni). Ayarlardan gidiş yolu `settings_screen_widget_test`
  /// içinde ayrıca kilitli; burada konu **ekranın kendisi**.
  Future<void> pumpAppAt(
    WidgetTester tester,
    FakeHttpAdapter adapter,
    String location, {
    bool signedIn = true,
  }) async {
    tester.view.physicalSize = const Size(1200, 3200);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final container = await pumpApp(
      tester,
      adapter: adapter,
      prefs: const {'auth.guestChoice': true},
      tokenStore: signedIn
          ? InMemoryTokenStore(accessToken: 'ACCESS', refreshToken: 'REFRESH')
          : InMemoryTokenStore(),
    );
    container.read(routerProvider).go(location);
    await tester.pumpAndSettle();
  }

  Map<String, Future<ResponseBody> Function(RequestOptions options)> baseRoutes({
    List<Map<String, dynamic>> documents = const [],
    List<Map<String, dynamic>> consents = const [],
  }) => {
    ...homeStubs(),
    ...legalStubs(documents: documents, consents: consents),
    '/v1/users/me': (_) async => jsonResponse(
      successEnvelope({
        'id': '11111111-1111-1111-1111-111111111111',
        'phone': '+905321110001',
        'username': 'ahmetk',
        'role': 'user',
      }),
    ),
  };

  testWidgets('yayında metin yoksa BOŞ DURUM sebebini söyler', (tester) async {
    // 🔑 "Henüz yayınlanmadı" ile "yüklenemedi" aynı ekran değildir; ve mağaza
    // şartı gereği web sitesindeki politikaya çıkış **kalmak zorunda**.
    final adapter = routedAdapter(baseRoutes());
    await pumpAppAt(tester, adapter, AppRoutes.legal);

    expect(find.text('Yayında metin yok'), findsOneWidget);
    expect(find.text('Web sitesinde aç'), findsOneWidget);
  });

  testWidgets('onaylanan sürüm ve tarih gösterilir', (tester) async {
    final adapter = routedAdapter(
      baseRoutes(
        documents: [
          legalDocumentBody(
            type: 'acik_riza',
            title: 'Açık Rıza Metni',
            versionId: 'aaaaaaaa-1111-1111-1111-111111111111',
            versionNumber: 3,
          ),
        ],
        consents: [
          consentBody(
            type: 'acik_riza',
            title: 'Açık Rıza Metni',
            isMandatory: true,
            currentVersionId: 'aaaaaaaa-1111-1111-1111-111111111111',
            currentVersionNumber: 3,
            consentedVersionId: 'aaaaaaaa-1111-1111-1111-111111111111',
            consentedVersionNumber: 3,
            granted: true,
            decidedAt: '2026-08-12T09:00:00Z',
          ),
        ],
      ),
    );
    await pumpAppAt(tester, adapter, AppRoutes.legal);

    expect(find.textContaining('Onayınız: v3'), findsOneWidget);
    expect(find.textContaining('12 Ağustos 2026'), findsOneWidget);
  });

  testWidgets(
    'ZORUNLU rızada "geri al" butonu HİÇ çizilmez, karşılığı söylenir',
    (tester) async {
      // 🔴 Kapalı bir anahtar çizmek işlevsiz buton olurdu; zorunlu rızanın
      // karşılığı hesap silmedir (12.16 kararı) ve ekran bunu **yazar**.
      final adapter = routedAdapter(
        baseRoutes(
          documents: [
            legalDocumentBody(
              type: 'acik_riza',
              title: 'Açık Rıza Metni',
              versionId: 'aaaaaaaa-1111-1111-1111-111111111111',
            ),
          ],
          consents: [
            consentBody(
              type: 'acik_riza',
              title: 'Açık Rıza Metni',
              isMandatory: true,
              currentVersionId: 'aaaaaaaa-1111-1111-1111-111111111111',
              consentedVersionId: 'aaaaaaaa-1111-1111-1111-111111111111',
              consentedVersionNumber: 1,
              granted: true,
              decidedAt: '2026-08-12T09:00:00Z',
            ),
          ],
        ),
      );
      await pumpAppAt(tester, adapter, AppRoutes.legal);

      expect(find.text('İzni geri al'), findsNothing);
      expect(find.textContaining('Hesabı sil'), findsOneWidget);
    },
  );

  testWidgets('isteğe bağlı izin geri alınınca granted:false SUNUCUYA yazılır', (
    tester,
  ) async {
    final adapter = routedAdapter({
      ...baseRoutes(
        documents: [
          legalDocumentBody(
            type: 'ticari_ileti',
            title: 'Ticari İleti İzni',
            versionId: 'bbbbbbbb-2222-2222-2222-222222222222',
            isMandatory: false,
          ),
        ],
        consents: [
          consentBody(
            consentedVersionId: 'bbbbbbbb-2222-2222-2222-222222222222',
            consentedVersionNumber: 3,
            granted: true,
            decidedAt: '2026-08-12T09:00:00Z',
          ),
        ],
      ),
      '/v1/users/me/consents': (options) async => options.method == 'POST'
          ? jsonResponse(successEnvelope(true))
          : jsonResponse(
              successEnvelope([
                consentBody(
                  consentedVersionId: 'bbbbbbbb-2222-2222-2222-222222222222',
                  consentedVersionNumber: 3,
                  granted: true,
                  decidedAt: '2026-08-12T09:00:00Z',
                ),
              ]),
            ),
    });
    await pumpAppAt(tester, adapter, AppRoutes.legal);

    await tester.tap(find.text('İzni geri al'));
    await tester.pumpAndSettle();

    // ⚠️ Onay penceresi **neyi** geri aldığını yazar (11.15c kuralı).
    expect(find.textContaining('Ticari İleti İzni'), findsWidgets);
    await tester.tap(find.text('Geri al'));
    await tester.pumpAndSettle();

    final write = adapter.requests
        .where((r) => r.path == '/v1/users/me/consents' && r.method == 'POST')
        .last;
    expect((write.data as Map)['consents'], [
      // 🔑 Karar **onaylanan sürüme** yazılır, kayıt silinmez:
      // "sormadık" ile "sorduk, hayır dedi" ayrı şeylerdir.
      {'versionId': 'bbbbbbbb-2222-2222-2222-222222222222', 'granted': false},
    ]);
  });

  testWidgets('eski sürüm onaylanmışsa "onayladığınız metni oku" çıkar', (
    tester,
  ) async {
    // 12.17 eki: rıza kaydının işaret ettiği metne giden yol. Bu buton
    // olmasaydı kullanıcı **neyi kabul ettiğini bir daha göremezdi**.
    final adapter = routedAdapter(
      baseRoutes(
        documents: [
          legalDocumentBody(
            type: 'acik_riza',
            title: 'Açık Rıza Metni',
            versionId: 'yeni-surum',
            versionNumber: 3,
          ),
        ],
        consents: [
          consentBody(
            type: 'acik_riza',
            title: 'Açık Rıza Metni',
            isMandatory: true,
            currentVersionId: 'yeni-surum',
            currentVersionNumber: 3,
            consentedVersionId: 'eski-surum',
            consentedVersionNumber: 2,
            granted: true,
            decidedAt: '2026-06-01T09:00:00Z',
          ),
        ],
      ),
    );
    await pumpAppAt(tester, adapter, AppRoutes.legal);

    expect(find.textContaining('Onayladığınız metni oku (v2)'), findsOneWidget);
  });

  testWidgets('metin ekranı sürüm + yürürlük tarihini SÖYLER', (tester) async {
    final adapter = routedAdapter({
      ...baseRoutes(),
      '/v1/legal/documents/acik_riza': (_) async => jsonResponse(
        successEnvelope(
          legalDocumentBody(
            type: 'acik_riza',
            title: 'Açık Rıza Metni',
            versionNumber: 4,
            body: '<p>Rıza metninin tam hâli.</p>',
          ),
        ),
      ),
    });
    await pumpAppAt(tester, adapter, AppRoutes.legalDocument('acik_riza'));

    expect(find.byType(LegalDocumentScreen), findsOneWidget);
    // 🔑 Sürüm ve yürürlük tarihi metnin bir parçası: kullanıcının hangi hâle
    // rıza verdiğini bilmesi bu bloğun tamamının sebebi.
    expect(find.textContaining('Sürüm 4'), findsOneWidget);
    expect(find.byType(RichHtmlBody), findsOneWidget);
  });

  testWidgets(
    'yürürlükten kalkmış sürüm ekranı bunu AÇIKÇA söyler',
    (tester) async {
      // 🔴 Söylemezse kullanıcı eski metni güncel sanar — bu bloğun savaştığı
      // hasarın tersten hâli.
      final adapter = routedAdapter({
        ...baseRoutes(),
        '/v1/legal/versions/eski-surum': (_) async => jsonResponse(
          successEnvelope({
            'id': 'eski-surum',
            'documentType': 'acik_riza',
            'documentTitle': 'Açık Rıza Metni',
            'versionNumber': 2,
            'summary': null,
            'body': '<p>Eski metin.</p>',
            'effectiveFrom': '2026-01-01T00:00:00Z',
            'publishedAt': '2026-01-01T00:00:00Z',
            'isLive': false,
            'supersededAt': '2026-08-01T00:00:00Z',
          }),
        ),
      });
      await pumpAppAt(tester, adapter, AppRoutes.legalVersion('eski-surum'));

      expect(find.byType(LegalVersionScreen), findsOneWidget);
      expect(find.text('Bu metin artık yürürlükte değil'), findsOneWidget);
      expect(find.textContaining('Sürüm 2'), findsOneWidget);
    },
  );

  testWidgets('misafir de metinleri okuyabilir (mağaza şartı)', (tester) async {
    // Ekranı oturuma kapatmak, "gizlilik politikasına uygulama içinden
    // erişilebilmeli" şartını kırardı.
    final adapter = routedAdapter(
      baseRoutes(
        documents: [legalDocumentBody(type: 'gizlilik_politikasi', title: 'Gizlilik Politikası')],
      ),
    );
    await pumpAppAt(tester, adapter, AppRoutes.legal, signedIn: false);

    expect(find.byType(LegalDocumentsScreen), findsOneWidget);
    expect(find.text('Gizlilik Politikası'), findsOneWidget);
    expect(find.text('Metni oku'), findsOneWidget);
    // Rıza satırları yok — onlar oturum ister.
    expect(find.textContaining('Onayınız'), findsNothing);
  });
}
