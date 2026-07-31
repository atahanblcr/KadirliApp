import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/features/pharmacies/application/pharmacies_providers.dart';
import 'package:kadirli_app/features/pharmacies/data/models/duty_schedule.dart';

/// Nöbet kaydı modeli (11.7).
///
/// İki kontrat inceliği burada kilitleniyor: `dutyDate` "Türkiye gününün UTC
/// gece yarısı" konvansiyonu ve `startTime/endTime`'ın **metin** olması.
void main() {
  DutySchedule entry({
    String id = '1',
    String date = '2026-07-03T00:00:00Z',
    String start = '19:00',
    String end = '09:00',
    String pharmacyId = 'p1',
    String pharmacyName = 'Merkez Eczanesi',
  }) => DutySchedule(
    id: id,
    dutyDate: DateTime.parse(date),
    startTime: start,
    endTime: end,
    pharmacyId: pharmacyId,
    pharmacyName: pharmacyName,
  );

  group('dayKey', () {
    test('UTC gece yarısı kaydı doğru günü verir', () {
      expect(entry(date: '2026-07-03T00:00:00Z').dayKey, '2026-07-03');
    });

    test('tek haneli ay ve gün sıfırla doldurulur', () {
      expect(entry(date: '2026-01-05T00:00:00Z').dayKey, '2026-01-05');
    });

    test('ay sonu kaydı bir sonraki aya kaymaz', () {
      expect(entry(date: '2026-07-31T00:00:00Z').dayKey, '2026-07-31');
    });
  });

  group('saatler', () {
    test('aralık metni birleştirilir', () {
      expect(entry().hours, '19:00 - 09:00');
    });

    test('saatlerden biri boşsa aralık yok', () {
      expect(entry(start: '').hours, isNull);
      expect(entry(end: '').hours, isNull);
    });

    test('gece yarısını aşan nöbet tespit edilir', () {
      expect(entry(start: '19:00', end: '09:00').crossesMidnight, isTrue);
      expect(entry(start: '08:30', end: '19:00').crossesMidnight, isFalse);
    });

    test('aynı saatte başlayıp biten nöbet 24 saat sayılır (aşan)', () {
      expect(entry(start: '08:30', end: '08:30').crossesMidnight, isTrue);
    });

    test('bozuk saat metni patlatmaz', () {
      expect(entry(start: 'abc', end: '09:00').crossesMidnight, isFalse);
    });
  });

  test('JSON ayrıştırma: canlı uçtan gelen gövde', () {
    final parsed = DutySchedule.fromJson(const {
      'id': '78c44865-ea49-478a-8dff-c1ab08d57412',
      'dutyDate': '2026-07-03T00:00:00Z',
      'startTime': '19:00',
      'endTime': '09:00',
      'pharmacyId': '251c0aa5-6593-4d19-bc48-a73b8001f0fb',
      'pharmacyName': 'Merkez Eczanesi',
      'source': 'mock',
    });

    expect(parsed.pharmacyName, 'Merkez Eczanesi');
    expect(parsed.dayKey, '2026-07-03');
    expect(parsed.hours, '19:00 - 09:00');
    expect(parsed.source, 'mock');
  });

  group('ay gezinme', () {
    test('bir ay ileri/geri', () {
      expect(shiftDutyMonth((year: 2026, month: 7), 1), (year: 2026, month: 8));
      expect(shiftDutyMonth((year: 2026, month: 7), -1), (year: 2026, month: 6));
    });

    test('yıl sınırında sarmalanır', () {
      expect(shiftDutyMonth((year: 2026, month: 12), 1), (year: 2027, month: 1));
      expect(shiftDutyMonth((year: 2026, month: 1), -1), (year: 2025, month: 12));
    });
  });

  test('dutyDaysOf yalnız o eczanenin günlerini tarih sırasıyla verir', () {
    final schedule = [
      entry(id: '3', date: '2026-07-20T00:00:00Z', pharmacyId: 'p1'),
      entry(id: '1', date: '2026-07-05T00:00:00Z', pharmacyId: 'p1'),
      entry(id: '2', date: '2026-07-10T00:00:00Z', pharmacyId: 'p2'),
    ];

    final days = dutyDaysOf(schedule, 'p1');

    expect(days.map((d) => d.id), ['1', '3']);
  });
}
