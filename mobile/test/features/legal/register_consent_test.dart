import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/widgets/widgets.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// **Kayıt akışındaki KVKK rıza adımı** (Faz 12.17).
///
/// Bu dosyanın kilitlediği üç şey ve üçü de sessiz hasar sınıfı:
/// 1. **Ön işaretli kutu yok** — bozulduğunda kayıt hızlanır, hiçbir şey
///    hata vermez ve alınan rıza KVKK'ya göre **geçersizdir**.
/// 2. **Belgeler alınamazsa kayıt açılmaz** — projedeki varsayılan yönün
///    (§5 "şüphede kalınca göster") bilinçli tersi: metni gösteremiyorken
///    rıza almak, rıza almamaktır.
/// 3. **Kararlar sunucuya gerçekten gider** — kutu işaretlenir, ekran ilerler
///    ve gövdede `consents` yoksa panelde hiçbir kanıt oluşmaz.
void main() {
  /// Yeni kullanıcı akışı: telefon → kod → kayıt ekranı.
  Future<void> reachRegisterScreen(
    WidgetTester tester,
    FakeHttpAdapter adapter,
  ) async {
    // Kayıt ekranı rıza kartıyla birlikte uzadı; varsayılan 800x600 test
    // yüzeyi butonu ekran dışında bırakıyor. Gerçek bir telefon ölçüsü
    // kullanmak, testin **kaydırma mekaniğiyle** değil kuralla ilgilenmesini
    // sağlıyor (12.14'ün golden ölçü deseni).
    tester.view.physicalSize = const Size(1200, 3200);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    await pumpApp(tester, adapter: adapter);
    await tester.enterText(find.byType(TextField), '5339990001');
    await tester.tap(find.text('Kod Gönder'));
    await settleApp(tester);
    await tester.tap(find.text('Doğrula'));
    await settleApp(tester);
  }

  Map<String, Future<ResponseBody> Function(RequestOptions options)> authRoutes({
    required Map<String, Future<ResponseBody> Function(RequestOptions)> legal,
  }) => {
    ...homeStubs(),
    ...legal,
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
    '/v1/auth/register': (_) async => jsonResponse(
      successEnvelope({'accessToken': 'ACCESS', 'refreshToken': 'REFRESH'}),
    ),
    '/v1/users/me': (_) async => jsonResponse(
      successEnvelope({
        'id': '11111111-1111-1111-1111-111111111111',
        'phone': '+905339990001',
        'username': 'yenikomsu',
        'role': 'user',
        'primaryNeighborhoodName': 'Savrun',
      }),
    ),
    '/v1/neighborhoods': (_) async => jsonResponse(
      successEnvelope([
        {'id': '22222222-2222-2222-2222-222222222222', 'name': 'Savrun', 'slug': 'savrun'},
      ]),
    ),
  };

  final mandatoryDocument = legalDocumentBody(
    type: 'acik_riza',
    title: 'Açık Rıza Metni',
    versionId: 'aaaaaaaa-1111-1111-1111-111111111111',
    summary: 'Kişisel verilerimin işlenmesini kabul ediyorum.',
  );
  final optionalDocument = legalDocumentBody(
    id: '22222222-2222-2222-2222-222222222222',
    type: 'ticari_ileti',
    title: 'Ticari İleti İzni',
    versionId: 'bbbbbbbb-2222-2222-2222-222222222222',
    summary: 'Kampanya bildirimleri almak istiyorum.',
    isMandatory: false,
    sortOrder: 1,
  );

  testWidgets('zorunlu kutu ÖN İŞARETLİ DEĞİL ve buton kapalı + sebebi yazıyor', (
    tester,
  ) async {
    final adapter = routedAdapter(
      authRoutes(legal: legalStubs(documents: [mandatoryDocument])),
    );
    await reachRegisterScreen(tester, adapter);

    // 🔴 Birinci iddia: kutu **boş** geldi.
    final checkbox = tester.widget<Checkbox>(find.byType(Checkbox).first);
    expect(checkbox.value, isFalse);

    // 🔴 İkinci iddia: o boşluk **butonu kapatıyor** — yalnız "kutu boş"
    // demek yetmez, kutular boş ama kayıt yine tamamlanabilir olabilirdi.
    final button = tester.widget<AppButton>(
      find.widgetWithText(AppButton, 'Kaydı Tamamla'),
    );
    expect(button.onPressed, isNull);

    // 🔴 Üçüncü iddia: kapalı buton **sebebini söylüyor** (§7 madde 42).
    expect(find.textContaining('Açık Rıza Metni'), findsWidgets);
  });

  testWidgets('kutu işaretlenince karar gövdede sunucuya gider', (tester) async {
    final adapter = routedAdapter(
      authRoutes(
        legal: legalStubs(documents: [mandatoryDocument, optionalDocument]),
      ),
    );
    await reachRegisterScreen(tester, adapter);

    await tester.enterText(find.byType(TextField).first, 'yenikomsu');
    await tester.tap(find.byType(DropdownButtonFormField<String>));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Savrun').last);
    await tester.pumpAndSettle();

    // Yalnız ZORUNLU kutu işaretleniyor — isteğe bağlı olan bilerek boş
    // bırakılıyor ki `granted:false`'ın da gönderildiği görülsün.
    await tester.tap(find.byType(Checkbox).first);
    await tester.pump();

    await tester.tap(find.text('Kaydı Tamamla'));
    await settleApp(tester);

    final body = adapter.lastOf('/v1/auth/register')!.data as Map;
    expect(body['consents'], [
      {'versionId': 'aaaaaaaa-1111-1111-1111-111111111111', 'granted': true},
      // ⚠️ "Sormadık" ile "sorduk, hayır dedi" farkı: reddedilen karar da gider.
      {'versionId': 'bbbbbbbb-2222-2222-2222-222222222222', 'granted': false},
    ]);
  });

  testWidgets(
    'metinler alınamazsa kayıt AÇILMAZ ve sebebini söyler',
    (tester) async {
      // 🔴 Projedeki varsayılan yönün bilinçli tersi (§5): burada "şüphede
      // kalınca göster" uygulanırsa kullanıcı **hiç okumadığı** bir metne rıza
      // vermiş olur — ve o rıza, hiç alınmamış rızayla aynı kapıya çıkar.
      final adapter = routedAdapter(
        authRoutes(
          legal: {
            '/v1/legal/documents': (_) async => jsonResponse(
              errorEnvelope('INTERNAL_ERROR', 'Sunucu hatası.'),
              statusCode: 500,
            ),
          },
        ),
      );
      await reachRegisterScreen(tester, adapter);

      // ⚠️ 5xx **geçici** sayılıyor (`apiRetry`): iki tekrar daha var
      // (600 ms + 1200 ms). Kalıcı sonucu görmek için beklemek gerekiyor —
      // ve bu bekleyiş bilinçli: tekrar denerken butonun **açılmadığını** da
      // ölçüyoruz (aradaki `AsyncLoading` dalı da kapalı olmalı).
      for (var i = 0; i < 4; i++) {
        final button = tester.widget<AppButton>(
          find.widgetWithText(AppButton, 'Kaydı Tamamla'),
        );
        expect(button.onPressed, isNull, reason: 'yükleme sırasında da kapalı');
        await tester.pump(const Duration(seconds: 1));
      }

      expect(find.text('Onay metinleri yüklenemedi'), findsOneWidget);

      final button = tester.widget<AppButton>(
        find.widgetWithText(AppButton, 'Kaydı Tamamla'),
      );
      expect(button.onPressed, isNull);
      expect(adapter.countOf('/v1/auth/register'), 0);
    },
  );

  testWidgets(
    'yayında belge YOKSA kayıt akışı 12.17 ÖNCESİYLE aynı çalışır',
    (tester) async {
      // 12.16 kararı: metin seed edilmez. Taze kurulumda kayıt akışı hiç
      // değişmemeli — `consents` gerçekten additive olmalı.
      final adapter = routedAdapter(authRoutes(legal: legalStubs()));
      await reachRegisterScreen(tester, adapter);

      expect(find.text('Onaylarınız'), findsNothing);
      expect(find.byType(Checkbox), findsNothing);

      final button = tester.widget<AppButton>(
        find.widgetWithText(AppButton, 'Kaydı Tamamla'),
      );
      expect(button.onPressed, isNotNull);
    },
  );
}
