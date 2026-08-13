import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/features/power_outages/application/power_outages_providers.dart';
import 'package:kadirli_app/features/power_outages/data/models/power_outage.dart';

/// Kesinti modeli + gruplama (11.6).
///
/// Uç tarih filtrelemiyor ve sayfalamıyor → süren/planlı/geçmiş ayrımı,
/// mahalle filtresi ve geri sayım tamamen burada. Bu yüzden mantığın testi
/// ekran testinden daha kritik.
void main() {
  final now = DateTime.utc(2026, 7, 31, 12);

  PowerOutage outage({
    String id = '1',
    String? neighborhood = 'Yenimahalle',
    String? neighborhoodId,
    String? areaDetail,
    required Duration start,
    required Duration end,
    String? reason,
  }) => PowerOutage(
    id: id,
    neighborhood: neighborhood,
    neighborhoodId: neighborhoodId,
    areaDetail: areaDetail,
    startTime: now.add(start),
    endTime: now.add(end),
    reason: reason,
  );

  group('durum', () {
    test('başlamış ve bitmemiş kesinti sürüyor sayılır', () {
      final item = outage(
        start: const Duration(hours: -1),
        end: const Duration(hours: 2),
      );
      expect(item.status(now: now), PowerOutageStatus.active);
      expect(item.isActive(now: now), isTrue);
      expect(item.isPast(now: now), isFalse);
    });

    test('henüz başlamamış kesinti planlıdır', () {
      final item = outage(
        start: const Duration(hours: 5),
        end: const Duration(hours: 8),
      );
      expect(item.status(now: now), PowerOutageStatus.upcoming);
      expect(item.isRelevant(now: now), isTrue);
    });

    test('bitmiş kesinti geçmiştedir ve şeride girmez', () {
      final item = outage(
        start: const Duration(hours: -8),
        end: const Duration(hours: -2),
      );
      expect(item.status(now: now), PowerOutageStatus.past);
      expect(item.isRelevant(now: now), isFalse);
    });

    test('tam bitiş anında kesinti artık sürmüyor (sınır durumu)', () {
      final item = outage(
        start: const Duration(hours: -2),
        end: Duration.zero,
      );
      expect(item.isActive(now: now), isFalse);
      expect(item.status(now: now), PowerOutageStatus.past);
    });

    // 🔴 Faz A bozma turu (13 Ağu 2026): BAŞLANGIÇ sınırı burada **hiç iddia
    // edilmiyordu.** `isActive`'i "başlangıç anı hariç" yapan bozma tüm mobil
    // süiti YEŞİL bıraktı — oysa panel tarafı bunu ayrıca kilitliyor
    // (`PanelPowerOutageFilterTests.StartMoment_IsInclusive_OutageCountsAsOngoing`).
    // §7 madde 27 iki tanımın **birebir** aynı olmasını şart koşuyor; ayna tek
    // taraflı kilitliyken panel "sürüyor" derken vatandaş "planlı" görür ve
    // **kimse hata almaz** (madde 23'ün sınıfı).
    test('tam BAŞLANGIÇ anında kesinti sürüyor sayılır (sınır DÂHİL)', () {
      final item = outage(start: Duration.zero, end: const Duration(hours: 2));

      expect(
        item.isActive(now: now),
        isTrue,
        reason:
            'başlangıç anı DÂHİL — panel tarafındaki PowerOutagePhaseRules ile '
            'birebir aynı olmak zorunda (§7 madde 27)',
      );
      expect(item.isUpcoming(now: now), isFalse);
      expect(item.status(now: now), PowerOutageStatus.active);
    });
  });

  group('süre ve geri sayım', () {
    test('duration planlanan toplam süredir', () {
      final item = outage(
        start: const Duration(hours: 1),
        end: const Duration(hours: 4, minutes: 30),
      );
      expect(item.duration, const Duration(hours: 3, minutes: 30));
    });

    test('sürerken bitişe, planlıyken başlangıca kalan süre döner', () {
      expect(
        outage(start: const Duration(hours: -1), end: const Duration(hours: 2))
            .remaining(now: now),
        const Duration(hours: 2),
      );
      expect(
        outage(start: const Duration(hours: 3), end: const Duration(hours: 5))
            .remaining(now: now),
        const Duration(hours: 3),
      );
    });

    test('geçmiş kesintide geri sayım yok', () {
      expect(
        outage(start: const Duration(hours: -5), end: const Duration(hours: -1))
            .remaining(now: now),
        isNull,
      );
    });
  });

  group('mahalle', () {
    test('boş/null mahalle şehir geneli olarak etiketlenir', () {
      expect(
        outage(neighborhood: null, start: Duration.zero, end: Duration.zero)
            .placeLabel,
        'Kadirli geneli',
      );
      expect(
        outage(neighborhood: '   ', start: Duration.zero, end: Duration.zero)
            .placeLabel,
        'Kadirli geneli',
      );
    });

    test('eşleşme büyük/küçük harf ve boşluğa duyarsız', () {
      final item = outage(
        neighborhood: ' Yenimahalle ',
        start: Duration.zero,
        end: Duration.zero,
      );
      expect(item.matchesNeighborhood('yenimahalle'), isTrue);
      expect(item.matchesNeighborhood('Karataş'), isFalse);
      expect(item.matchesNeighborhood(null), isFalse);
      expect(item.matchesNeighborhood(''), isFalse);
    });

    // ─────────────── Faz 12.3: kimlik öncelikli eşleşme ───────────────

    test('kimlik varsa AD karşılaştırması hiç yapılmaz', () {
      // 🔴 Asıl kazanç bu: sunucu 12.3'ten önce "Yenimahalle Mah." yazıyordu ve
      // kullanıcının profilindeki "Yenimahalle" ile eşleşmiyordu — süzgeç sessizce
      // boş kalıyordu. Kimlik varken yazım farkı artık hiç önemli değil.
      final item = outage(
        neighborhood: 'Yenimahalle Mah.',
        neighborhoodId: 'n-1',
        start: Duration.zero,
        end: Duration.zero,
      );

      expect(
        item.matchesNeighborhood('Yenimahalle', userNeighborhoodId: 'n-1'),
        isTrue,
      );
      expect(
        item.matchesNeighborhood('Yenimahalle Mah.', userNeighborhoodId: 'n-2'),
        isFalse,
        reason: 'kimlikler tutmuyorsa ad tutsa bile eşleşme YOK',
      );
    });

    test('kimliklerden biri boşsa ad karşılaştırmasına düşülür', () {
      // Geri doldurmada eşleşmemiş eski kayıtta `neighborhoodId` boş gelir;
      // elimizdeki tek şey ad olduğu için o yol kapatılmadı.
      final legacy = outage(
        neighborhood: 'Yenimahalle',
        start: Duration.zero,
        end: Duration.zero,
      );

      expect(
        legacy.matchesNeighborhood('yenimahalle', userNeighborhoodId: 'n-1'),
        isTrue,
      );
      expect(legacy.hasNeighborhoodRef, isFalse);
    });

    test('sözlüğe bağlı kayıt hasNeighborhoodRef ile ayırt edilir', () {
      final linked = outage(
        neighborhoodId: 'n-9',
        start: Duration.zero,
        end: Duration.zero,
      );
      expect(linked.hasNeighborhoodRef, isTrue);
    });

    test('mahalle bilgisi olmayan kayıt şehir geneli sayılır', () {
      expect(
        outage(neighborhood: null, start: Duration.zero, end: Duration.zero)
            .isCityWide,
        isTrue,
      );
      expect(
        outage(neighborhood: '  ', start: Duration.zero, end: Duration.zero)
            .isCityWide,
        isTrue,
      );
    });
  });

  group('gruplama', () {
    final items = [
      outage(id: 'gecmis', start: const Duration(hours: -9), end: const Duration(hours: -6)),
      outage(id: 'suren', start: const Duration(hours: -1), end: const Duration(hours: 1)),
      outage(id: 'yakin', start: const Duration(hours: 2), end: const Duration(hours: 4)),
      outage(id: 'uzak', start: const Duration(hours: 20), end: const Duration(hours: 22)),
      outage(
        id: 'baska-mahalle',
        neighborhood: 'Karataş',
        start: const Duration(hours: 6),
        end: const Duration(hours: 7),
      ),
      outage(
        id: 'sehir-geneli',
        neighborhood: null,
        start: const Duration(hours: 8),
        end: const Duration(hours: 9),
      ),
    ];

    test('üç gruba doğru ayrılır ve sıralanır', () {
      final groups = PowerOutageGroups.from(items, now: now);

      expect(groups.active.map((o) => o.id), ['suren']);
      expect(groups.upcoming.map((o) => o.id), [
        'yakin',
        'baska-mahalle',
        'sehir-geneli',
        'uzak',
      ], reason: 'planlananlar en yakın başlangıç önce');
      expect(groups.past.map((o) => o.id), ['gecmis']);
      expect(groups.currentCount, 5);
      expect(groups.pastCount, 1);
    });

    test('mahalle filtresi başka mahalleyi eler, ŞEHİR GENELİNİ elemez', () {
      final groups = PowerOutageGroups.from(
        items,
        now: now,
        neighborhood: 'Yenimahalle',
      );

      final ids = [
        ...groups.active.map((o) => o.id),
        ...groups.upcoming.map((o) => o.id),
        ...groups.past.map((o) => o.id),
      ];
      expect(ids, contains('sehir-geneli'));
      expect(ids, isNot(contains('baska-mahalle')));
      expect(
        groups.hiddenByNeighborhood,
        1,
        reason: 'gizlenen kayıt sayısı kullanıcıya yazılıyor',
      );
    });

    test('filtre yokken hiçbir kayıt gizlenmez', () {
      expect(PowerOutageGroups.from(items, now: now).hiddenByNeighborhood, 0);
    });

    test('boş listede gruplar boş ama patlamaz', () {
      final groups = PowerOutageGroups.from(const [], now: now);
      expect(groups.hasCurrent, isFalse);
      expect(groups.pastCount, 0);
    });

    // ─────────────── Faz 12.3: kimlik bazlı süzgeç ───────────────

    test('kimlik süzgeci yazım farkına rağmen doğru mahalleyi tutar', () {
      final byId = [
        outage(
          id: 'benim',
          neighborhood: 'Yenimahalle Mah.',
          neighborhoodId: 'n-1',
          start: const Duration(hours: 1),
          end: const Duration(hours: 2),
        ),
        outage(
          id: 'baska',
          neighborhood: 'Karataş',
          neighborhoodId: 'n-2',
          start: const Duration(hours: 3),
          end: const Duration(hours: 4),
        ),
        outage(
          id: 'sehir',
          neighborhood: null,
          start: const Duration(hours: 5),
          end: const Duration(hours: 6),
        ),
      ];

      final groups = PowerOutageGroups.from(
        byId,
        now: now,
        neighborhood: 'Yenimahalle',
        neighborhoodId: 'n-1',
      );

      expect(groups.upcoming.map((o) => o.id), ['benim', 'sehir']);
      expect(groups.hiddenByNeighborhood, 1);
    });

    test('yalnız kimlik verilse de süzgeç çalışır (ad gerekmez)', () {
      final byId = [
        outage(
          id: 'benim',
          neighborhood: 'Adı Hiç Tutmayan Bir Metin',
          neighborhoodId: 'n-1',
          start: const Duration(hours: 1),
          end: const Duration(hours: 2),
        ),
        outage(
          id: 'baska',
          neighborhood: 'Yenimahalle',
          neighborhoodId: 'n-2',
          start: const Duration(hours: 3),
          end: const Duration(hours: 4),
        ),
      ];

      final groups = PowerOutageGroups.from(byId, now: now, neighborhoodId: 'n-1');

      expect(groups.upcoming.map((o) => o.id), ['benim']);
      expect(groups.hiddenByNeighborhood, 1);
    });
  });
}
