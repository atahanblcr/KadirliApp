import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/features/taxis/data/models/taxi_driver.dart';
import 'package:kadirli_app/features/taxis/data/recent_taxi_calls_store.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Faz 12.23 — "son aranan taksiciler" deposunun ilk birim testleri.
///
/// 11.11'de yazıldı, hiç test edilmemişti. En kritik iddia **telefonun
/// saklanmadığı**: numara her zaman sunucudan taze gelmeli, yoksa çağrı sayacı
/// işlemez ve şoför numarasını değiştirdiğinde vatandaş **eski numarayı** arar.
void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  Future<RecentTaxiCallsStore> storeWith(Map<String, Object> seed) async {
    SharedPreferences.setMockInitialValues(seed);
    return RecentTaxiCallsStore(await SharedPreferences.getInstance());
  }

  TaxiDriver driver(String id) => TaxiDriver(
    id: id,
    name: '$id Şoför',
    phone: '5321110001',
    plaka: '80 ABC $id',
  );

  group('hatırlama', () {
    test('en son aranan BAŞA gelir', () async {
      final store = await storeWith({});

      await store.remember(driver('a'));
      final after = await store.remember(driver('b'));

      expect(after.map((item) => item.id), ['b', 'a']);
    });

    test('aynı şoför MÜKERRER girmez, yalnız öne alınır', () async {
      final store = await storeWith({});

      await store.remember(driver('a'));
      await store.remember(driver('b'));
      final after = await store.remember(driver('a'));

      expect(after.map((item) => item.id), ['a', 'b']);
    });

    test('tavan ${RecentTaxiCallsStore.maxItems} — aşınca en eski düşer', () async {
      final store = await storeWith({});

      for (final id in ['a', 'b', 'c', 'd']) {
        await store.remember(driver(id));
      }

      // Üçten fazlası liste ekranının üstünü kaplar ve "son" olma anlamını yitirir.
      final saved = store.read();
      expect(saved.length, RecentTaxiCallsStore.maxItems);
      expect(saved.map((item) => item.id), ['d', 'c', 'b']);
    });
  });

  group('ne saklanıyor', () {
    test('TELEFON SAKLANMAZ (numara her zaman sunucudan taze gelmeli)', () async {
      SharedPreferences.setMockInitialValues({});
      final prefs = await SharedPreferences.getInstance();
      final store = RecentTaxiCallsStore(prefs);

      await store.remember(driver('a'));

      // Ham depoya bakılıyor: modelde alan olmaması yetmez, JSON'a da
      // sızmamalı. Sızsaydı arama `POST /drivers/{id}/call` yerine yerel
      // numaradan yapılabilir hâle gelir ve çağrı sayacı sessizce ölürdü.
      expect(prefs.getString('taxis.recentCalls'), isNot(contains('5321110001')));
    });

    test('ad ve plaka anlık görüntü olarak saklanır (doğrudan çizilebilsin)', () async {
      final store = await storeWith({});

      await store.remember(driver('a'));

      final saved = store.read().single;
      expect(saved.name, 'a Şoför');
      expect(saved.plaka, isNotNull);
    });
  });

  group('dayanıklılık', () {
    test('bozuk JSON listeyi düşürmez', () async {
      final store = await storeWith({'taxis.recentCalls': '}{ bozuk'});
      expect(store.read(), isEmpty);
    });

    test('kimliksiz/adsız satır ELENİR, diğerleri kalır', () async {
      final store = await storeWith({
        'taxis.recentCalls': jsonEncode([
          {'id': '', 'name': 'Adsız'},
          {'id': 'b', 'name': 'b Şoför', 'calledAt': '2026-08-01T10:00:00Z'},
        ]),
      });

      expect(store.read().map((item) => item.id), ['b']);
    });

    test('clear listeyi temizler', () async {
      final store = await storeWith({});
      await store.remember(driver('a'));

      await store.clear();

      expect(store.read(), isEmpty);
    });
  });
}
