import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/features/legal/data/legal_repository.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// KVKK uçlarının **gerçek** yanıt gövdeleriyle eşleşmesi (12.16 kontratı).
void main() {
  LegalRepository repositoryWith(FakeHttpAdapter adapter) =>
      LegalRepository(testApiClient(adapter));

  group('GET /v1/legal/documents', () {
    test('gövdeyi ayrıştırır ve versionId TAŞINIR (rızanın çıpası)', () async {
      final adapter = routedAdapter({
        '/v1/legal/documents': (_) async =>
            jsonResponse(successEnvelope([legalDocumentBody()])),
      });

      final documents = await repositoryWith(adapter).documents();

      expect(documents, hasLength(1));
      expect(documents.single.type, 'acik_riza');
      expect(documents.single.versionId, 'aaaaaaaa-1111-1111-1111-111111111111');
      expect(documents.single.versionNumber, 1);
      expect(documents.single.isMandatory, isTrue);
      expect(documents.single.body, '<p>Rıza metninin tam hâli.</p>');
    });

    test('registrationOnly YALNIZ istendiğinde sorgu dizesine düşer', () async {
      // ⚠️ Varsayılan `false`: ayarlar ekranı yayında olan **her** belgeyi
      // okuyabilmeli. Parametre her zaman gönderilseydi ayarlar ekranı da
      // kayıt ekranının dar listesini görürdü.
      final adapter = routedAdapter({
        '/v1/legal/documents': (_) async => jsonResponse(successEnvelope([])),
      });
      final repository = repositoryWith(adapter);

      await repository.documents();
      expect(
        adapter.lastOf('/v1/legal/documents')!.queryParameters,
        isNot(contains('registrationOnly')),
      );

      await repository.documents(registrationOnly: true);
      expect(
        adapter.lastOf('/v1/legal/documents')!.queryParameters['registrationOnly'],
        isTrue,
      );
    });
  });

  group('GET /v1/legal/documents/{type}', () {
    test('tanınmayan tür 404 → NOT_FOUND (varsayılana DÜŞMEZ)', () async {
      // 🔴 Sunucu bilinmeyen türü varsayılana düşürmüyor: yanlış hukuki metni
      // göstermek, kullanıcıya okumadığı bir belgeyi onaylatmanın en sessiz
      // yoludur. İstemci de o 404'ü **hata olarak** taşımalı.
      final adapter = routedAdapter({
        '/v1/legal/documents/olmayan_tur': (_) async => jsonResponse(
          errorEnvelope('NOT_FOUND', 'Belge bulunamadı.'),
          statusCode: 404,
        ),
      });

      expect(
        () => repositoryWith(adapter).documentByType('olmayan_tur'),
        throwsA(
          isA<ApiException>().having((e) => e.code, 'code', ApiErrorCodes.notFound),
        ),
      );
    });
  });

  group('GET /v1/legal/versions/{id} (12.17 eki)', () {
    test('yürürlükten kalkmış sürüm DÖNER ve isLive false gelir', () async {
      // 🔑 Ucun bütün amacı bu: kullanıcının onayladığı metin, yerini yeni bir
      // sürüme bırakmış olsa bile okunabilmeli. `isLive` sunucudan geliyor;
      // istemci onu `supersededAt`'ten türetmiyor (iki sahip olurdu).
      final adapter = routedAdapter({
        '/v1/legal/versions/old-version': (_) async => jsonResponse(
          successEnvelope({
            'id': 'old-version',
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

      final version = await repositoryWith(adapter).version('old-version');

      expect(version.versionNumber, 2);
      expect(version.isLive, isFalse);
      expect(version.supersededAt, isNotNull);
      expect(version.body, '<p>Eski metin.</p>');
    });
  });

  group('POST /v1/users/me/consents', () {
    test('gövde kontrata uygun gider (reddedilen karar DA gider)', () async {
      final adapter = routedAdapter({
        '/v1/users/me/consents': (_) async => jsonResponse(successEnvelope(true)),
      });

      await repositoryWith(adapter).saveConsents(const [
        ConsentDecision(versionId: 'v-1', granted: true),
        ConsentDecision(versionId: 'v-2', granted: false),
      ]);

      expect(adapter.lastOf('/v1/users/me/consents')!.data, {
        'consents': [
          {'versionId': 'v-1', 'granted': true},
          {'versionId': 'v-2', 'granted': false},
        ],
        'isReconsent': false,
      });
    });

    test('yeniden onay akışı isReconsent:true SÖYLER', () async {
      // ⚠️ Değeri `ConsentSources`'a çeviren **sunucudur**; istemci yalnız
      // "yeniden onay akışından geliyorum" der. Söylemeseydi defterdeki
      // "nasıl alındı" sütunu bu rızaları `settings` sanırdı.
      final adapter = routedAdapter({
        '/v1/users/me/consents': (_) async => jsonResponse(successEnvelope(true)),
      });

      await repositoryWith(adapter).saveConsents(const [
        ConsentDecision(versionId: 'v-1', granted: true),
      ], isReconsent: true);

      expect(
        (adapter.lastOf('/v1/users/me/consents')!.data as Map)['isReconsent'],
        isTrue,
      );
    });

    test('zorunlu rızayı geri alma reddi MANDATORY_CONSENT olarak gelir', () async {
      final adapter = routedAdapter({
        '/v1/users/me/consents': (_) async => jsonResponse(
          errorEnvelope(
            'MANDATORY_CONSENT',
            '"Açık Rıza Metni" zorunludur ve buradan geri alınamaz.',
          ),
          statusCode: 400,
        ),
      });

      expect(
        () => repositoryWith(adapter).saveConsents(const [
          ConsentDecision(versionId: 'v-1', granted: false),
        ]),
        throwsA(
          isA<ApiException>()
              .having((e) => e.code, 'code', ApiErrorCodes.mandatoryConsent)
              // Sunucunun mesajı **hangi belge** olduğunu söylüyor ve istemci
              // onu ezmemeli (sözlükteki genel karşılık yalnız yedek).
              .having((e) => e.message, 'message', contains('Açık Rıza Metni')),
        ),
      );
    });
  });

  group('GET /v1/users/me/consents', () {
    test('hiç karar verilmemiş belge de listede durur', () async {
      // ⚠️ Yalnız karar verilenler gelseydi ayarlar ekranı, hiç sorulmamış bir
      // izni (ör. ticari ileti) göstermez ve kullanıcının onu verme yolu
      // **hiç var olmazdı** — "işlevsiz buton yok" kuralının tersi.
      final adapter = routedAdapter({
        '/v1/users/me/consents': (_) async => jsonResponse(
          successEnvelope([
            {
              'type': 'ticari_ileti',
              'title': 'Ticari İleti İzni',
              'isMandatory': false,
              'currentVersionId': 'v-1',
              'currentVersionNumber': 1,
              'consentedVersionId': null,
              'consentedVersionNumber': null,
              'granted': false,
              'decidedAt': null,
              'revokedAt': null,
              'needsReconsent': false,
            },
          ]),
        ),
      });

      final consents = await repositoryWith(adapter).myConsents();

      expect(consents.single.hasDecision, isFalse);
      expect(consents.single.granted, isFalse);
      // 🔑 "Hayır dedi" DEĞİL: `granted:false` + `decidedAt:null` = hiç sorulmadı.
      expect(consents.single.canRevoke, isFalse);
    });
  });
}
