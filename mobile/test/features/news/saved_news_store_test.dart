import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/features/news/data/models/news_article.dart';
import 'package:kadirli_app/features/news/data/saved_news_store.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Faz 12.23 — §7 madde **62**'nin eksik kalan **davranış ayağı**.
///
/// 62 ("Kaydedilenler kaydın anlık görüntüsünü saklar, gövde saklanmaz, liste
/// tavanlıdır, bozuk satır listeyi düşürmez") 12.14'te yazıldı ama deponun
/// KENDİSİNİN hiçbir birim testi yoktu: tek dolaylı kapsam
/// `news_screen_test.dart`'ın prefs'e **ham string** ile beslediği iki
/// senaryoydu. Yani tavan, yaş, gövde düşürme ve bozuk satır toleransı —
/// maddenin dört yüzünün dördü de — ölçülmüyordu.
///
/// ⚠️ Bu dosya anahtarı `SavedNewsStore.prefsKey`'den okur, elle yazmaz:
/// elle yazılsaydı anahtar değiştiğinde test **yeşil kalır**, kod bozulurdu.
void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  Future<SavedNewsStore> storeWith(Map<String, Object> seed) async {
    SharedPreferences.setMockInitialValues(seed);
    return SavedNewsStore(await SharedPreferences.getInstance());
  }

  NewsArticle article(String id, {String? body}) => NewsArticle(
    id: id,
    title: '$id başlığı',
    excerpt: '$id özeti',
    contentHtml: body,
    sourceUrl: 'https://silagazetesi.com.tr/$id',
  );

  group('anlık görüntü', () {
    test('gövde (contentHtml) DEPOYA YAZILMAZ', () async {
      final store = await storeWith({});

      await store.toggle(article('a', body: '<p>11 KB gövde</p>'));

      // Gövde saklansaydı tek haber 11 KB'a çıkıyor (12.12 ölçümü) ve
      // SharedPreferences dosyanın TAMAMINI açılışta belleğe alıyor.
      expect(store.read().single.contentHtml, isNull);
    });

    test('başlık/özet/kaynak adresi KALIR (detay 404 verse bile okunabilsin)', () async {
      final store = await storeWith({});

      await store.toggle(article('a'));

      final saved = store.read().single;
      expect(saved.title, 'a başlığı');
      expect(saved.excerpt, 'a özeti');
      expect(saved.sourceUrl, 'https://silagazetesi.com.tr/a');
    });
  });

  group('toggle', () {
    test('ikinci dokunuş kaydı KALDIRIR', () async {
      final store = await storeWith({});

      await store.toggle(article('a'));
      final after = await store.toggle(article('a'));

      expect(after, isEmpty);
      expect(store.read(), isEmpty);
    });

    test('en yeni kayıt BAŞA gelir', () async {
      final store = await storeWith({});

      await store.toggle(article('a'));
      await store.toggle(article('b'));

      expect(store.read().map((item) => item.id), ['b', 'a']);
    });

    test('aynı haber MÜKERRER girmez (yeniden kaydedilse de tek satır)', () async {
      final store = await storeWith({});

      await store.toggle(article('a'));
      await store.toggle(article('b'));
      await store.toggle(article('a')); // kaldırır
      await store.toggle(article('a')); // yeniden ekler

      expect(store.read().map((item) => item.id), ['a', 'b']);
    });
  });

  group('tavan', () {
    test('tavan aşılınca EN ESKİ düşer, liste tavanda kalır', () async {
      final store = await storeWith({});

      for (var i = 0; i <= SavedNewsStore.maxItems; i++) {
        await store.toggle(article('haber-$i'));
      }

      final saved = store.read();
      // Tavan olmasaydı depo tek yönlü büyür ve sorun yıllar sonra fark
      // edilirdi — üstelik dosya her açılışta belleğe alınıyor.
      expect(saved.length, SavedNewsStore.maxItems);
      expect(saved.first.id, 'haber-${SavedNewsStore.maxItems}');
      expect(
        saved.map((item) => item.id),
        isNot(contains('haber-0')),
        reason: 'Tavan aşılınca düşmesi gereken EN ESKİ kayıttır.',
      );
    });
  });

  group('dayanıklılık', () {
    test('BOZUK TEK SATIR bütün listeyi düşürmez', () async {
      final good = jsonEncode(article('a').toJson());
      final store = await storeWith({
        SavedNewsStore.prefsKey: <String>[good, '}{ bozuk json', good],
      });

      // Sürüm geçişinde bozulan tek bir JSON, kullanıcının 40 kaydını
      // birden götürmemeli.
      expect(store.read().length, 2);
    });

    test('alan TİPİ değişmiş eski kayıt sessizce atlanır', () async {
      final store = await storeWith({
        SavedNewsStore.prefsKey: <String>[
          jsonEncode({'id': 'a', 'title': 42}), // title bir gün int olsaydı
          jsonEncode(article('b').toJson()),
        ],
      });

      expect(store.read().map((item) => item.id), ['b']);
    });

    test('hiç anahtar yoksa BOŞ liste döner (ilk açılış)', () async {
      final store = await storeWith({});
      expect(store.read(), isEmpty);
    });

    test('clear listeyi ve anahtarı temizler', () async {
      final store = await storeWith({});
      await store.toggle(article('a'));

      expect(await store.clear(), isEmpty);
      expect(store.read(), isEmpty);
    });
  });
}
