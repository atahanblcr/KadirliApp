import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:kadirli_app/core/utils/utils.dart';

void main() {
  setUpAll(() async => initializeDateFormatting('tr_TR'));

  group('Türkiye saati (sabit +03)', () {
    test('UTC değer Türkiye duvar saatine kayar', () {
      final utc = DateTime.utc(2026, 7, 29, 6, 30);

      expect(AppDate.time(utc), '09:30');
      expect(AppDate.date(utc), '29 Temmuz 2026');
      expect(AppDate.dateTime(utc), '29 Temmuz 2026, 09:30');
    });

    test('gece yarısına yakın UTC değer ERTESİ güne geçer', () {
      // 22:30 UTC = 01:30 TR (ertesi gün) — cihaz saat dilimi ne olursa olsun.
      final utc = DateTime.utc(2026, 7, 29, 22, 30);

      expect(AppDate.date(utc), '30 Temmuz 2026');
      expect(AppDate.isoDay(utc), '2026-07-30');
    });

    test('Türkçe gün adı', () {
      expect(AppDate.dayWithWeekday(DateTime.utc(2026, 7, 29, 9)), '29 Temmuz Çarşamba');
    });
  });

  group('göreli zaman', () {
    final now = DateTime.utc(2026, 7, 29, 12);

    test('bir dakikadan yeni → az önce', () {
      expect(AppDate.relative(now.subtract(const Duration(seconds: 30)), now: now), 'az önce');
    });

    test('dakika / saat', () {
      expect(AppDate.relative(now.subtract(const Duration(minutes: 5)), now: now), '5 dakika önce');
      expect(AppDate.relative(now.subtract(const Duration(hours: 3)), now: now), '3 saat önce');
    });

    test('takvim günü farkı: dün ve n gün önce', () {
      expect(AppDate.relative(now.subtract(const Duration(hours: 25)), now: now), 'dün');
      expect(AppDate.relative(now.subtract(const Duration(days: 4)), now: now), '4 gün önce');
    });

    test('bir haftadan eski → tam tarih', () {
      expect(AppDate.relative(now.subtract(const Duration(days: 20)), now: now), '9 Temmuz 2026');
    });

    test('gelecek tarih → tam tarih-saat (negatif fark yok)', () {
      expect(
        AppDate.relative(now.add(const Duration(days: 2)), now: now),
        '31 Temmuz 2026, 15:00',
      );
    });
  });

  test('ulaşım saat etiketi normalize edilir', () {
    expect(AppDate.clockLabel('7:05'), '07:05');
    expect(AppDate.clockLabel('14:30:00'), '14:30');
    expect(AppDate.clockLabel('bozuk'), 'bozuk');
  });

  group('tarih aralığı (11.6 kesinti kartı)', () {
    test('aynı güne düşen aralık tek tarihle yazılır', () {
      expect(
        AppDate.range(
          DateTime.utc(2026, 8, 12, 6),
          DateTime.utc(2026, 8, 12, 12),
        ),
        '12 Ağustos 2026, 09:00 – 15:00',
      );
    });

    test('güne yayılan aralık iki tam tarihle yazılır', () {
      // 12 Ağu 20:00 UTC = 23:00 TR → 13 Ağu 01:00 UTC = 04:00 TR
      expect(
        AppDate.range(
          DateTime.utc(2026, 8, 12, 20),
          DateTime.utc(2026, 8, 13, 1),
        ),
        '12 Ağustos 2026, 23:00 → 13 Ağustos 2026, 04:00',
      );
    });

    test('gün ayrımı UTC\'ye değil KADİRLİ saatine göre yapılır', () {
      // İkisi de UTC'de farklı günde ama Türkiye saatinde 13 Ağustos.
      expect(
        AppDate.range(
          DateTime.utc(2026, 8, 12, 22),
          DateTime.utc(2026, 8, 13, 4),
        ),
        '13 Ağustos 2026, 01:00 – 07:00',
      );
    });
  });

  group('süre etiketi (geri sayım)', () {
    test('bir saatin altı dakika ile yazılır', () {
      expect(AppDate.duration(const Duration(minutes: 45)), '45 dakika');
    });

    test('tam saat kısaca "n saat"', () {
      expect(AppDate.duration(const Duration(hours: 6)), '6 saat');
    });

    test('saat + dakika kısaltmayla', () {
      expect(
        AppDate.duration(const Duration(hours: 2, minutes: 30)),
        '2 sa 30 dk',
      );
    });

    test('bir günden uzun süre gün cinsinden', () {
      expect(AppDate.duration(const Duration(days: 1, hours: 4)), '1 gün 4 saat');
      expect(AppDate.duration(const Duration(days: 2)), '2 gün');
    });

    test('saniyeler yukarı yuvarlanır — "0 dakika kaldı" yazmaz', () {
      expect(AppDate.duration(const Duration(seconds: 5)), '1 dakika');
      expect(AppDate.duration(const Duration(seconds: 61)), '2 dakika');
    });

    test('negatif süre sıfır kabul edilir (saat kayması)', () {
      expect(AppDate.duration(const Duration(minutes: -10)), '0 dakika');
    });
  });
}
