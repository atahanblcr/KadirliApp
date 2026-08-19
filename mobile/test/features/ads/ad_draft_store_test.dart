import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/features/ads/data/ad_draft_store.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Faz 12.23 — ilan taslağı deposunun ilk birim testleri.
///
/// Depo 11.9'da yazıldı, dört yıl… değil, dört fazdır kullanılıyor ve **hiç
/// test edilmemişti**: yaş sınırı (7 gün), "anlamlı taslak" kuralı ve bozuk
/// kayıt toleransı ölçülmüyordu. Üçü de kullanıcının **yazdığı metni**
/// koruyan/atan kararlar.
void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  Future<AdDraftStore> storeWith(Map<String, Object> seed) async {
    SharedPreferences.setMockInitialValues(seed);
    return AdDraftStore(await SharedPreferences.getInstance());
  }

  /// Deponun içine, yaşı elle ayarlanmış bir taslak yazar.
  String encodedDraft({required Duration age, String title = 'Satılık daire'}) {
    return jsonEncode({
      'categoryId': 'kat-1',
      'title': title,
      'description': 'Merkezde, 3+1',
      'price': '1500000',
      'propertyValues': {'oda': '3+1'},
      'savedAt': DateTime.now().toUtc().subtract(age).toIso8601String(),
    });
  }

  group('gidiş-dönüş', () {
    test('kaydedilen taslak alanlarıyla birlikte geri okunur', () async {
      final store = await storeWith({});

      await store.save(const AdDraft(
        categoryId: 'kat-1',
        categoryName: 'Emlak',
        title: 'Satılık daire',
        description: 'Merkezde',
        price: '1500000',
        propertyValues: {'oda': '3+1'},
      ));

      final draft = store.read()!;
      expect(draft.title, 'Satılık daire');
      expect(draft.categoryName, 'Emlak');
      expect(draft.propertyValues, {'oda': '3+1'});
    });

    test('clear taslağı siler', () async {
      final store = await storeWith({});
      await store.save(const AdDraft(title: 'Satılık daire'));

      await store.clear();

      expect(store.read(), isNull);
    });
  });

  group('yaş sınırı', () {
    test('tavanın ALTINDAKİ taslak teklif edilir', () async {
      final store = await storeWith({
        'ads.draft': encodedDraft(age: AdDraftStore.maxAge - const Duration(hours: 1)),
      });

      expect(store.read(), isNotNull);
    });

    test('tavanı AŞAN taslak teklif EDİLMEZ', () async {
      final store = await storeWith({
        'ads.draft': encodedDraft(age: AdDraftStore.maxAge + const Duration(hours: 1)),
      });

      // Kullanıcı çoktan unutmuştur; kategori/fiyat da bayatlamıştır.
      // Ölmüş bir taslağı teklif etmek, hiç teklif etmemekten kötü.
      expect(store.read(), isNull);
    });
  });

  group('"anlamlı taslak" kuralı', () {
    test('boş taslak teklif EDİLMEZ (kullanıcı ekranı açıp kapatmış olabilir)', () async {
      final store = await storeWith({});
      await store.save(const AdDraft(sellerName: 'Ahmet', contactPhone: '5321110001'));

      // Ön doldurulan satıcı adı/telefonu tek başına "yarım kalmış iş" değildir.
      expect(store.read(), isNull);
    });

    test('yalnız kategori seçilmiş olması BİLE anlamlıdır', () async {
      final store = await storeWith({});
      await store.save(const AdDraft(categoryId: 'kat-1'));

      expect(store.read(), isNotNull);
    });

    test('yalnız açıklama yazılmış olması anlamlıdır', () async {
      final store = await storeWith({});
      await store.save(const AdDraft(description: 'Merkezde 3+1'));

      expect(store.read(), isNotNull);
    });
  });

  group('dayanıklılık', () {
    test('bozuk JSON kullanıcının işini ENGELLEMEZ', () async {
      final store = await storeWith({'ads.draft': '}{ bozuk'});
      expect(store.read(), isNull);
    });

    test('JSON ama nesne değilse (dizi) yok sayılır', () async {
      final store = await storeWith({'ads.draft': '[1,2,3]'});
      expect(store.read(), isNull);
    });

    test('savedAt yoksa taslak YİNE DE okunur (yaş bilinmiyor ≠ bayat)', () async {
      final store = await storeWith({
        'ads.draft': jsonEncode({'title': 'Satılık daire'}),
      });

      // Şüphede kalınca göster (§5): yaşı bilinmeyen bir taslağı atmak,
      // kullanıcının yazdığını sebepsiz silmek olurdu.
      expect(store.read(), isNotNull);
    });
  });
}
