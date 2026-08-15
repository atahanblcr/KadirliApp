import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/features/legal/application/consent_selection.dart';
import 'package:kadirli_app/features/legal/data/legal_repository.dart';
import 'package:kadirli_app/features/legal/data/models/legal_document.dart';

/// **Rıza kutularının saf mantığı** (Faz 12.17) — üç ekranın (kayıt · yeniden
/// onay · ayarlar) ortak karar sahibi.
///
/// 🔴 Buradaki birinci iddia bir **KVKK kuralıdır, bir UI tercihi değil**:
/// ön işaretli kutu rıza sayılmaz. Kural tek bir karakterle bozulabilir
/// (`grantedVersionIds: {...documents.map(...)}`) ve bozulduğunda uygulama
/// çalışmaya devam eder, hiçbir test kırılmaz, hiçbir log düşmez — kayıt
/// hızlanır bile. Bu test onu **yakalayan tek şey**.
void main() {
  LegalDocument doc({
    required String id,
    required String type,
    required String title,
    bool mandatory = false,
  }) => LegalDocument(
    id: id,
    type: type,
    title: title,
    versionId: 'version-$id',
    isMandatory: mandatory,
  );

  final kvkk = doc(
    id: '1',
    type: 'kvkk_aydinlatma',
    title: 'KVKK Aydınlatma Metni',
    mandatory: true,
  );
  final consent = doc(
    id: '2',
    type: 'acik_riza',
    title: 'Açık Rıza Metni',
    mandatory: true,
  );
  final marketing = doc(id: '3', type: 'ticari_ileti', title: 'Ticari İleti İzni');

  group('ön işaretli kutu YOK', () {
    test('initial hiçbir kutuyu işaretlemez — zorunlular dâhil', () {
      final selection = ConsentSelection.initial([kvkk, consent, marketing]);

      expect(selection.isGranted(kvkk), isFalse);
      expect(selection.isGranted(consent), isFalse);
      expect(selection.isGranted(marketing), isFalse);
      expect(selection.grantedVersionIds, isEmpty);
    });

    test('initial ile "devam" KAPALI başlar (zorunlu belge varken)', () {
      // 🔑 İkinci yön: yalnız "kutular boş" demek yetmez — o boşluğun
      // **butonu kapattığını** da ölçmek gerekir, yoksa kutular boş ama kayıt
      // yine de tamamlanabilir olurdu.
      expect(ConsentSelection.initial([kvkk]).canSubmit, isFalse);
    });
  });

  group('zorunlu kutu kapıyı tutar', () {
    test('zorunlu işaretlenmeden devam edilemez ve SEBEBİ söylenir', () {
      final selection = ConsentSelection.initial([kvkk, consent]);

      expect(selection.canSubmit, isFalse);
      expect(selection.blockingDocument, kvkk);
      expect(selection.blockingReason, contains('KVKK Aydınlatma Metni'));
    });

    test('ikinci zorunlu kalınca sebep ONA döner (ilk kutuya takılıp kalmaz)', () {
      final selection = ConsentSelection.initial([
        kvkk,
        consent,
      ]).toggle(kvkk, true);

      expect(selection.canSubmit, isFalse);
      expect(selection.blockingDocument, consent);
      expect(selection.blockingReason, contains('Açık Rıza Metni'));
    });

    test('zorunluların hepsi işaretlenince devam AÇILIR', () {
      final selection = ConsentSelection.initial([kvkk, consent, marketing])
          .toggle(kvkk, true)
          .toggle(consent, true);

      expect(selection.canSubmit, isTrue);
      expect(selection.blockingReason, isNull);
    });

    test('isteğe bağlı kutu devam etmeyi ENGELLEMEZ', () {
      final selection = ConsentSelection.initial([marketing]);

      expect(selection.canSubmit, isTrue);
      expect(selection.isGranted(marketing), isFalse);
    });

    test('hiç belge yoksa devam açıktır (taze kurulumun gerçek hâli)', () {
      // 12.16 kararı: metin seed edilmez → yayında belge yok → kayıt akışı
      // birebir 12.17 öncesi gibi çalışmalı.
      expect(ConsentSelection.initial(const []).canSubmit, isTrue);
    });

    test('işareti geri almak kapıyı yeniden kapatır', () {
      final selection = ConsentSelection.initial([kvkk])
          .toggle(kvkk, true)
          .toggle(kvkk, false);

      expect(selection.canSubmit, isFalse);
    });
  });

  group('sunucuya giden kararlar', () {
    test('HER belge için bir satır gider — reddedilenler dâhil', () {
      // ⚠️ Yalnız işaretlenenler gönderilseydi *"sormadık"* ile *"sorduk,
      // hayır dedi"* farkı hiçbir yerde durmazdı: isteğe bağlı bir izni
      // bilerek reddeden kullanıcı sunucuda **hiç sorulmamış** görünürdü.
      final selection = ConsentSelection.initial([kvkk, marketing])
          .toggle(kvkk, true);

      expect(selection.decisions, [
        const ConsentDecision(versionId: 'version-1', granted: true),
        const ConsentDecision(versionId: 'version-3', granted: false),
      ]);
    });

    test('karar BELGEYE değil SÜRÜME bağlanır (§7 madde 71)', () {
      final selection = ConsentSelection.initial([kvkk]).toggle(kvkk, true);

      expect(selection.decisions.single.versionId, kvkk.versionId);
      expect(selection.decisions.single.versionId, isNot(kvkk.id));
    });
  });

  group('onay etiketi', () {
    test('özet varsa özet, yoksa BAŞLIK kullanılır', () {
      // Boş bir onay satırı, kullanıcının neyi kabul ettiğini söylemeyen bir
      // kutu demektir — o yüzden etiket hiçbir zaman boş olamaz.
      const withSummary = LegalDocument(
        id: '1',
        type: 'acik_riza',
        title: 'Açık Rıza Metni',
        versionId: 'v1',
        summary: 'Verilerimin işlenmesini kabul ediyorum.',
      );
      const withoutSummary = LegalDocument(
        id: '2',
        type: 'kvkk_aydinlatma',
        title: 'KVKK Aydınlatma Metni',
        versionId: 'v2',
        summary: '   ',
      );

      expect(withSummary.consentLabel, 'Verilerimin işlenmesini kabul ediyorum.');
      expect(withoutSummary.consentLabel, 'KVKK Aydınlatma Metni');
    });
  });
}
