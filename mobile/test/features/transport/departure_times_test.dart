import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/features/transport/application/departure_times.dart';
import 'package:kadirli_app/features/transport/application/operating_days.dart';

/// Kalkış saati hesapları (11.12) — ekransız saf mantık.
///
/// ⚠️ Tüm "şimdi" değerleri **UTC** verilir; [DepartureTimes] bunları Kadirli
/// duvar saatine (+03) çevirir. Testin makinenin saat diliminden bağımsız
/// kalması bu yüzden garanti (11.2 kararı).
void main() {
  /// Kadirli saatiyle verilen anı UTC'ye çevirir (13:40 Kadirli = 10:40 UTC).
  DateTime kadirli(int hour, int minute) =>
      DateTime.utc(2026, 8, 2, hour, minute).subtract(const Duration(hours: 3));

  group('minutesOfDay', () {
    test('"HH:mm" ve "HH:mm:ss" biçimlerinin ikisini de çözer', () {
      // Şehirlerarası uç "07:00", şehir içi uç TimeSpan → "06:30:00" gönderiyor.
      expect(DepartureTimes.minutesOfDay('07:00'), 7 * 60);
      expect(DepartureTimes.minutesOfDay('06:30:00'), 6 * 60 + 30);
      expect(DepartureTimes.minutesOfDay(' 22:05 '), 22 * 60 + 5);
    });

    test('bozuk/boş değer null döner (satır hiç çizilmesin)', () {
      expect(DepartureTimes.minutesOfDay(null), isNull);
      expect(DepartureTimes.minutesOfDay(''), isNull);
      expect(DepartureTimes.minutesOfDay('yarın'), isNull);
      expect(DepartureTimes.minutesOfDay('25:00'), isNull);
      expect(DepartureTimes.minutesOfDay('10:71'), isNull);
    });
  });

  group('sıradaki kalkış', () {
    const times = ['07:00', '10:30', '14:00', '17:30'];

    test('gün içinde bir sonraki seferi ve kalan süreyi verir', () {
      final next = DepartureTimes.next(times, now: kadirli(13, 40));
      expect(next!.label, '14:00');
      expect(next.minutesUntil, 20);
      expect(next.isTomorrow, isFalse);
      expect(next.isImminent, isTrue);
    });

    test('tam kalkış dakikasındaki sefer hâlâ "sıradaki"dir', () {
      final next = DepartureTimes.next(times, now: kadirli(14, 0));
      expect(next!.label, '14:00');
      expect(next.minutesUntil, 0);
      expect(DepartureTimes.untilLabel(next.minutesUntil), 'Şimdi');
    });

    test('son sefer geçtiyse yarının ilk seferine düşer', () {
      // 23:10'da "sefer yok" demek yerine ertesi günün ilk saatini söylüyoruz.
      final next = DepartureTimes.next(times, now: kadirli(23, 10));
      expect(next!.label, '07:00');
      expect(next.isTomorrow, isTrue);
      expect(next.minutesUntil, (24 * 60 - (23 * 60 + 10)) + 7 * 60);
      // Yarına kalan sefer "acil" sayılmaz (kart vurgusu çıkmamalı).
      expect(next.isImminent, isFalse);
    });

    test('sırasız gelen saatler sıralanır, bozuklar elenir', () {
      final next = DepartureTimes.next([
        '17:30',
        'bozuk',
        '07:00',
      ], now: kadirli(8, 0));
      expect(next!.label, '17:30');
    });

    test('hiç geçerli saat yoksa null döner', () {
      expect(DepartureTimes.next(const [], now: kadirli(8, 0)), isNull);
      expect(DepartureTimes.next(const ['-'], now: kadirli(8, 0)), isNull);
    });

    test('30 dakikadan uzak sefer vurgulanmaz', () {
      final next = DepartureTimes.next(times, now: kadirli(13, 0));
      expect(next!.minutesUntil, 60);
      expect(next.isImminent, isFalse);
    });
  });

  group('sıradaki kalkış — sefer günleri (12.6)', () {
    // 2026 Ağustos: 3'ü Pazartesi → 8'i Cumartesi, 9'u Pazar.
    DateTime at(int day, int hour, int minute) =>
        DateTime.utc(2026, 8, day, hour, minute).subtract(
          const Duration(hours: 3),
        );

    DepartureOption option(String time, List<String> days) => DepartureOption(
      minutesOfDay: DepartureTimes.minutesOfDay(time)!,
      days: OperatingDays.fromCodes(days),
    );

    const weekdayCodes = ['mon', 'tue', 'wed', 'thu', 'fri'];

    test('fixture günleri doğru (test kendi varsayımını denetler)', () {
      expect(at(3, 12, 0).toUtc().add(const Duration(hours: 3)).weekday,
          DateTime.monday);
      expect(at(8, 12, 0).toUtc().add(const Duration(hours: 3)).weekday,
          DateTime.saturday);
      expect(at(9, 12, 0).toUtc().add(const Duration(hours: 3)).weekday,
          DateTime.sunday);
    });

    test('bugün çalışan seferde davranış eskisiyle aynı', () {
      final next = DepartureTimes.nextAmong(
        [option('07:00', weekdayCodes), option('14:00', weekdayCodes)],
        now: at(3, 13, 40), // Pazartesi 13:40
      );
      expect(next!.label, '14:00');
      expect(next.daysAhead, 0);
      expect(next.isToday, isTrue);
      expect(next.minutesUntil, 20);
      expect(next.dayLabel, isNull);
    });

    test('🔴 hafta içi seferi CUMARTESİ sorulduğunda "Pzt" der, "yarın" DEMEZ', () {
      // 12.6'nın bitti kriteri. "Yarın" deseydi vatandaş Pazar günü durakta
      // beklerdi — hata vermeyen yanlış cevap.
      final next = DepartureTimes.nextAmong(
        [option('07:00', weekdayCodes)],
        now: at(8, 12, 0), // Cumartesi öğlen
      );
      expect(next!.label, '07:00');
      expect(next.daysAhead, 2);
      expect(next.isTomorrow, isFalse);
      expect(next.isLaterThanTomorrow, isTrue);
      expect(next.dayLabel, 'Pzt');
      expect(next.weekday, DateTime.monday);
      // 2 gün + (07:00 - 12:00)
      expect(next.minutesUntil, 2 * 24 * 60 - 5 * 60);
    });

    test('hafta içi seferi PAZAR sorulduğunda "yarın" der', () {
      final next = DepartureTimes.nextAmong(
        [option('07:00', weekdayCodes)],
        now: at(9, 12, 0), // Pazar öğlen
      );
      expect(next!.daysAhead, 1);
      expect(next.isTomorrow, isTrue);
      expect(next.dayLabel, 'Yarın');
    });

    test('bugünün geçmiş seferi elenir, aynı günün sonraki seferi kalır', () {
      final next = DepartureTimes.nextAmong(
        [option('07:00', weekdayCodes), option('17:30', weekdayCodes)],
        now: at(3, 10, 0),
      );
      expect(next!.label, '17:30');
      expect(next.daysAhead, 0);
    });

    test('gün maskeleri karışıkken en erken ULAŞILABİLİR sefer seçilir', () {
      // Cumartesi 12:00: hafta içi 06:00 seferi daha erken görünse de
      // Cumartesi çalışan 20:00 seferi bugün ulaşılabilir olan.
      final next = DepartureTimes.nextAmong(
        [option('06:00', weekdayCodes), option('20:00', ['sat'])],
        now: at(8, 12, 0),
      );
      expect(next!.label, '20:00');
      expect(next.daysAhead, 0);
    });

    test('🔴 yalnız bugün çalışan hattın saatleri geçtiyse bir hafta sonrası', () {
      // Ofset döngüsü 6'da bitseydi null dönerdi ve kart "kalkış saati
      // girilmemiş" derdi — oysa saatler duruyor.
      final next = DepartureTimes.nextAmong(
        [option('07:00', ['mon'])],
        now: at(3, 20, 0), // Pazartesi akşam, sefer geçmiş
      );
      expect(next!.label, '07:00');
      expect(next.daysAhead, 7);
      expect(next.dayLabel, 'Pzt');
      expect(next.weekday, DateTime.monday);
    });

    test('hiçbir gün çalışmayan bozuk sefer elenir (olmayan sefer gösterilmez)', () {
      final next = DepartureTimes.nextAmong(
        [
          DepartureOption(minutesOfDay: 7 * 60, days: const OperatingDays(0)),
        ],
        now: at(3, 6, 0),
      );
      expect(next, isNull);
    });

    test('yarına kalan sefer "acil" sayılmaz', () {
      final next = DepartureTimes.nextAmong(
        [option('07:00', weekdayCodes)],
        now: at(9, 23, 50), // Pazar gece, yarın 07:00
      );
      expect(next!.isTomorrow, isTrue);
      expect(next.isImminent, isFalse);
    });

    test('saat listesi alan eski API her gün varsayar (davranış korundu)', () {
      final next = DepartureTimes.next(
        const ['07:00', '14:00'],
        now: at(8, 13, 0), // Cumartesi
      );
      expect(next!.label, '14:00');
      expect(next.daysAhead, 0);
    });
  });

  group('şehir içi servis durumu', () {
    IntracityStatus status(int hour, int minute) => DepartureTimes.intracity(
      first: '06:30:00',
      last: '22:00:00',
      frequencyMinutes: 20,
      now: kadirli(hour, minute),
    );

    test('servis saatleri içinde çalışıyor ve yaklaşık sefer verir', () {
      final result = status(9, 5);
      expect(result.state, IntracityServiceState.running);
      // 06:30 + 8×20 = 09:10
      expect(result.nextLabel, '09:10');
      expect(result.minutesUntil, 5);
    });

    test('ilk seferden önce "ilk sefer" durumu', () {
      final result = status(5, 30);
      expect(result.state, IntracityServiceState.beforeFirst);
      expect(result.nextLabel, '06:30');
      expect(result.minutesUntil, 60);
    });

    test('son seferden sonra bugünkü seferler biter, yarına düşer', () {
      final result = status(23, 0);
      expect(result.state, IntracityServiceState.finished);
      expect(result.nextLabel, '06:30');
      expect(result.nextIsTomorrow, isTrue);
    });

    test('pencere içinde ama son seferi aşan hesap son saate kilitlenir', () {
      // 21:55: bir sonraki 20 dk adımı 22:10 olurdu — son sefer 22:00.
      final result = status(21, 55);
      expect(result.state, IntracityServiceState.running);
      expect(result.nextLabel, '22:00');
      expect(result.minutesUntil, 5);
    });

    test('sıklık girilmemişse yalnız "çalışıyor" denir, saat uydurulmaz', () {
      final result = DepartureTimes.intracity(
        first: '06:30:00',
        last: '22:00:00',
        frequencyMinutes: null,
        now: kadirli(9, 5),
      );
      expect(result.state, IntracityServiceState.running);
      expect(result.nextLabel, isNull);
    });

    test('saatler girilmemişse durum "bilinmiyor" (uydurma yok)', () {
      final result = DepartureTimes.intracity(
        first: null,
        last: '22:00:00',
        now: kadirli(9, 5),
      );
      expect(result.state, IntracityServiceState.unknown);
    });

    test('gece yarısını aşan servis penceresi doğru okunur', () {
      // 23:00 – 01:00 arası çalışan bir gece hattı.
      final result = DepartureTimes.intracity(
        first: '23:00:00',
        last: '01:00:00',
        frequencyMinutes: 30,
        now: kadirli(23, 40),
      );
      expect(result.state, IntracityServiceState.running);
      expect(result.nextLabel, '00:00');
    });
  });

  group('etiketler', () {
    test('gün içi dakika "HH:mm" olur, gün taşması sarar', () {
      expect(DepartureTimes.label(7 * 60), '07:00');
      expect(DepartureTimes.label(23 * 60 + 5), '23:05');
      expect(DepartureTimes.label(24 * 60), '00:00');
    });

    test('kalan süre etiketi', () {
      expect(DepartureTimes.untilLabel(0), 'Şimdi');
      expect(DepartureTimes.untilLabel(20), '20 dakika sonra');
      expect(DepartureTimes.untilLabel(130), '2 sa 10 dk sonra');
    });
  });
}
