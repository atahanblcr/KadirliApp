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
    required Duration start,
    required Duration end,
    String? reason,
  }) => PowerOutage(
    id: id,
    neighborhood: neighborhood,
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
  });
}
