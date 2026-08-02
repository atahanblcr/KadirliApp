import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/router/app_router.dart';
import 'package:kadirli_app/features/complaints/data/models/complaint.dart';
import 'package:kadirli_app/features/complaints/presentation/widgets/complaint_card.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// Şikayet / İstek: liste (durum takibi) + gönderme formu (11.12).
void main() {
  const guest = {'auth.guestChoice': true};

  Map<String, dynamic> complaint({
    String id = 'c1',
    String? type = 'complaint',
    String subject = 'Sokak lambası yanmıyor',
    String message = 'Yenimahalle 1234. sokaktaki lamba bir haftadır yanmıyor.',
    String status = 'pending',
    String? adminNotes,
    String? resolvedAt,
    String createdAt = '2026-08-01T09:00:00Z',
    String? relatedModule,
  }) => {
    'id': id,
    'userId': 'u1',
    'type': type,
    'relatedModule': relatedModule,
    'relatedId': null,
    'subject': subject,
    'message': message,
    'status': status,
    'adminNotes': adminNotes,
    'resolvedBy': null,
    'resolvedAt': resolvedAt,
    'createdAt': createdAt,
  };

  Map<String, dynamic> pagedBody(List<Map<String, dynamic>> items) =>
      successEnvelope({
        'items': items,
        'totalCount': items.length,
        'pageSize': 20,
        'currentPage': 1,
        'totalPages': items.isEmpty ? 0 : 1,
      });

  Map<String, dynamic> me() => successEnvelope({
    'id': 'u1',
    'phone': '+905321110001',
    'username': 'aysedmr',
    'fullName': 'Ayşe Demir',
    'profilePhotoUrl': null,
    'age': 30,
    'neighborhoodId': null,
    'neighborhoodName': null,
    'isVerified': true,
    'usernameChangedAt': null,
    'createdAt': '2026-01-01T00:00:00Z',
    'notificationPreferences': null,
  });

  Future<FakeHttpAdapter> openComplaints(
    WidgetTester tester, {
    bool signedIn = true,
    List<Map<String, dynamic>>? items,
    Map<String, Future<ResponseBody> Function(RequestOptions)> routes =
        const {},
    String location = '/sikayet',
  }) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/users/me': (_) async => jsonResponse(me()),
      '/v1/complaints/my': (_) async =>
          jsonResponse(pagedBody(items ?? [complaint()])),
      ...routes,
    });

    final container = await pumpApp(
      tester,
      prefs: signedIn ? const {} : guest,
      tokenStore: signedIn
          ? InMemoryTokenStore(accessToken: 'A', refreshToken: 'R')
          : null,
      adapter: adapter,
    );
    container.read(routerProvider).go(location);
    await tester.pumpAndSettle();
    return adapter;
  }

  testWidgets('bildirimlerim durum rozeti ve türle listelenir', (tester) async {
    await openComplaints(tester);

    expect(find.text('Sokak lambası yanmıyor'), findsOneWidget);
    // Durum HEM renk HEM metin (renk körü kullanıcı için renk yetmez).
    expect(find.text('Bekliyor'), findsOneWidget);
    expect(find.text('Şikayet'), findsOneWidget);
    expect(find.text('Toplam 1 bildirim'), findsOneWidget);
  });

  testWidgets('yönetici notu "Yetkili yanıtı" olarak öne çıkar', (tester) async {
    await openComplaints(
      tester,
      items: [
        complaint(
          status: 'resolved',
          adminNotes: 'Ekip yönlendirildi, lamba değiştirildi.',
          resolvedAt: '2026-08-02T07:00:00Z',
        ),
      ],
    );

    expect(find.text('Çözüldü'), findsOneWidget);
    expect(find.text('Yetkili yanıtı'), findsOneWidget);
    expect(
      find.text('Ekip yönlendirildi, lamba değiştirildi.'),
      findsOneWidget,
    );
    expect(find.textContaining('Sonuçlandırıldı:'), findsOneWidget);
  });

  testWidgets('sonuçlanmamış bildirimde durum açıklaması yazılır', (
    tester,
  ) async {
    await openComplaints(
      tester,
      items: [complaint(status: 'in_progress')],
    );

    expect(find.text('İşlemde'), findsOneWidget);
    expect(
      find.text('Bildiriminiz ilgili birim tarafından inceleniyor.'),
      findsOneWidget,
    );
  });

  testWidgets('tanınmayan tür ham hâliyle gösterilir (bilgi kaybolmaz)', (
    tester,
  ) async {
    await openComplaints(tester, items: [complaint(type: 'altyapi')]);
    expect(find.text('altyapi'), findsOneWidget);
  });

  testWidgets('hiç bildirim yoksa açıklayıcı boş durum çıkar', (tester) async {
    await openComplaints(tester, items: const []);

    expect(find.text('Henüz bildiriminiz yok'), findsOneWidget);
    expect(find.byType(ComplaintCard), findsNothing);
    // Gönderme yolu boş durumda da açık kalmalı.
    expect(find.text('Bildirim gönder'), findsOneWidget);
  });

  testWidgets('misafir Giriş daveti görür ama "Bildirim gönder" yine açıktır', (
    tester,
  ) async {
    final adapter = await openComplaints(tester, signedIn: false);

    expect(find.text('Bildirimlerinizi takip edin'), findsOneWidget);
    expect(find.text('Bildirim gönder'), findsOneWidget);
    // Anonimde korumalı uca hiç istek gitmez (11.4/11.10 kararı).
    expect(adapter.lastOf('/v1/complaints/my'), isNull);
  });

  testWidgets('liste alınamazsa hata ekranı çıkar', (tester) async {
    await openComplaints(
      tester,
      routes: {
        // Kalıcı hata (404) — 5xx `apiRetry` yüzünden "pending timer" verir.
        '/v1/complaints/my': (_) async => jsonResponse(
          errorEnvelope('NOT_FOUND', 'Kayıt bulunamadı.'),
          statusCode: 404,
        ),
      },
    );

    expect(find.text('Tekrar dene'), findsOneWidget);
  });

  // ------------------------------------------------------------------ form

  testWidgets('zorunlu alanlar boşken uca istek GİTMEZ', (tester) async {
    final adapter = await openComplaints(
      tester,
      location: '/sikayet-bildir',
    );

    await tester.tap(find.text('Bildirimi gönder'));
    await tester.pumpAndSettle();

    expect(find.text('Konu zorunlu.'), findsOneWidget);
    expect(find.text('Mesaj zorunlu.'), findsOneWidget);
    expect(adapter.requests.where((r) => r.method == 'POST'), isEmpty);
  });

  testWidgets('çok kısa mesaj reddedilir (sunucuda doğrulayıcı yok)', (
    tester,
  ) async {
    await openComplaints(tester, location: '/sikayet-bildir');

    await tester.enterText(find.byType(TextField).at(0), 'Konu başlığı');
    await tester.enterText(find.byType(TextField).at(1), 'kısa');
    await tester.tap(find.text('Bildirimi gönder'));
    await tester.pumpAndSettle();

    expect(
      find.textContaining('Mesajı biraz daha açıklayın'),
      findsOneWidget,
    );
  });

  testWidgets('form gövdesi seçilen türle birlikte gider ve başarı diyaloğu çıkar', (
    tester,
  ) async {
    final adapter = await openComplaints(
      tester,
      location: '/sikayet-bildir',
      routes: {
        '/v1/complaints': (_) async =>
            jsonResponse(successEnvelope('new-complaint-id')),
      },
    );

    await tester.tap(find.text('Öneri'));
    await tester.pumpAndSettle();

    await tester.enterText(find.byType(TextField).at(0), 'Pazar günleri');
    await tester.enterText(
      find.byType(TextField).at(1),
      'Uygulamaya pazar yeri günlerinin eklenmesini öneriyorum.',
    );
    await tester.tap(find.text('Bildirimi gönder'));
    await tester.pumpAndSettle();

    final body = Map<String, dynamic>.from(
        adapter.lastOf('/v1/complaints')!.data as Map);
    expect(body['type'], 'suggestion');
    expect(body['subject'], 'Pazar günleri');
    expect(body['relatedModule'], isNull);

    expect(find.text('Bildiriminiz alındı'), findsOneWidget);
    expect(
      find.textContaining('"Bildirimlerim" listesinden takip'),
      findsOneWidget,
    );
  });

  testWidgets('misafir formda uyarı görür ve başarı metni takip vaat etmez', (
    tester,
  ) async {
    await openComplaints(
      tester,
      signedIn: false,
      location: '/sikayet-bildir',
      routes: {
        '/v1/complaints': (_) async =>
            jsonResponse(successEnvelope('new-complaint-id')),
      },
    );

    expect(
      find.text('Giriş yapmadan da gönderebilirsiniz'),
      findsOneWidget,
    );

    await tester.enterText(find.byType(TextField).at(0), 'Çöp toplanmıyor');
    await tester.enterText(
      find.byType(TextField).at(1),
      'Üç gündür çöpler alınmıyor, koku çok rahatsız edici.',
    );
    // Misafirde formun başına uyarı şeridi + "Giriş yap" ekleniyor → gönder
    // butonu ekran dışında kalıyor ve `ListView` tembel olduğu için hiç
    // kurulmuyor (11.9 tuzağı).
    await tester.drag(find.byType(ListView).last, const Offset(0, -300));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Bildirimi gönder'));
    await tester.pumpAndSettle();

    // Anonim kayıtta user_id NULL kalır → "takip edebilirsiniz" demek yalan olurdu.
    expect(
      find.textContaining('"Bildirimlerim" listesinde görünmeyecek'),
      findsOneWidget,
    );
  });

  testWidgets('içerik şikayeti bağlantılı açılınca modül ve kimlik gider', (
    tester,
  ) async {
    final adapter = await openComplaints(
      tester,
      location:
          '/sikayet-bildir?tur=content&modul=ads&kayit=ad-42&baslik=Sat%C4%B1l%C4%B1k+Ford',
      routes: {
        '/v1/complaints': (_) async =>
            jsonResponse(successEnvelope('new-complaint-id')),
      },
    );

    // İlgili içerik ham kimlikle değil adıyla gösterilir.
    expect(find.text('Satılık Ford'), findsOneWidget);
    expect(find.text('ad-42'), findsNothing);

    await tester.enterText(find.byType(TextField).at(0), 'Yanıltıcı ilan');
    await tester.enterText(
      find.byType(TextField).at(1),
      'İlandaki fotoğraflar başka bir araca ait görünüyor.',
    );
    // Formun başında "İlgili içerik" kartı var; ayrıca 11.15'te filtre chip'i
    // 48 dp dokunma hedefine çıkarıldı (erişilebilirlik) → tür şeridi bir tık
    // yükseldi ve gönder butonu ekran dışında kalıyor. `ListView` tembel
    // olduğu için buton hiç kurulmuyor (11.9 tuzağı) → önce kaydır.
    await tester.drag(find.byType(ListView).last, const Offset(0, -300));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Bildirimi gönder'));
    await tester.pumpAndSettle();

    final body = Map<String, dynamic>.from(
        adapter.lastOf('/v1/complaints')!.data as Map);
    expect(body['type'], 'content');
    expect(body['relatedModule'], 'ads');
    expect(body['relatedId'], 'ad-42');
  });

  testWidgets('tür "içerik şikayeti"nden çıkınca modül gövdeden düşer', (
    tester,
  ) async {
    final adapter = await openComplaints(
      tester,
      location: '/sikayet-bildir?tur=content&modul=ads&kayit=ad-42',
      routes: {
        '/v1/complaints': (_) async =>
            jsonResponse(successEnvelope('new-complaint-id')),
      },
    );

    await tester.tap(find.text('Şikayet'));
    await tester.pumpAndSettle();

    await tester.enterText(find.byType(TextField).at(0), 'Başka bir konu');
    await tester.enterText(
      find.byType(TextField).at(1),
      'Bu bildirim artık bir ilanla ilgili değil.',
    );
    await tester.tap(find.text('Bildirimi gönder'));
    await tester.pumpAndSettle();

    final body = Map<String, dynamic>.from(
        adapter.lastOf('/v1/complaints')!.data as Map);
    expect(body['type'], 'complaint');
    expect(body['relatedModule'], isNull);
    expect(body['relatedId'], isNull);
  });

  testWidgets('sunucu reddederse hata şeridi çıkar, ekran kapanmaz', (
    tester,
  ) async {
    await openComplaints(
      tester,
      location: '/sikayet-bildir',
      routes: {
        '/v1/complaints': (_) async => jsonResponse(
          errorEnvelope('VALIDATION_ERROR', 'Mesaj çok uzun.'),
          statusCode: 400,
        ),
      },
    );

    await tester.enterText(find.byType(TextField).at(0), 'Bir konu');
    await tester.enterText(
      find.byType(TextField).at(1),
      'Yeterince uzun bir mesaj metni burada duruyor.',
    );
    await tester.tap(find.text('Bildirimi gönder'));
    await tester.pumpAndSettle();

    expect(find.text('Mesaj çok uzun.'), findsOneWidget);
    expect(find.text('Bildirimi gönder'), findsOneWidget);
  });

  group('model', () {
    test('durum değerleri panelin kullandığı sabitlerle birebir', () {
      expect(ComplaintStatus.parse('pending'), ComplaintStatus.pending);
      expect(ComplaintStatus.parse('in_progress'), ComplaintStatus.inProgress);
      expect(ComplaintStatus.parse('resolved'), ComplaintStatus.resolved);
      expect(ComplaintStatus.parse('rejected'), ComplaintStatus.rejected);
      expect(ComplaintStatus.parse('bilinmeyen'), ComplaintStatus.unknown);
      expect(ComplaintStatus.parse(null), ComplaintStatus.unknown);
    });

    test('sonuçlanan durumlar isClosed', () {
      expect(ComplaintStatus.resolved.isClosed, isTrue);
      expect(ComplaintStatus.rejected.isClosed, isTrue);
      expect(ComplaintStatus.pending.isClosed, isFalse);
      expect(ComplaintStatus.inProgress.isClosed, isFalse);
    });

    test('boş yönetici notu "yanıt var" saymaz', () {
      final withBlank = Complaint.fromJson(complaint(adminNotes: '   '));
      expect(withBlank.hasAnswer, isFalse);
      final withNote = Complaint.fromJson(complaint(adminNotes: 'Tamam.'));
      expect(withNote.answer, 'Tamam.');
    });
  });
}
