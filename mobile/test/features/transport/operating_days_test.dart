import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/features/transport/application/operating_days.dart';

/// Sefer gün maskesi (12.6) — sunucudaki `OperatingDays`'in mobil karşılığı.
///
/// 🔴 Bu dosyanın asıl işi **gün kaymasını** kilitlemek: sunucu maskesi
/// Pazartesi=1'den başlar, Dart'ın `DateTime.weekday`'i de Pazartesi=1 …
/// Pazar=7. İkisi *tesadüfen* hizalı olduğu için `1 << weekday` gibi bir
/// yazım hatası derlenir, çalışır ve yalnız **günü bir kaydırır** — ne
/// derleyici ne de gözle bakan insan yakalar (görünmez sözleşme #46).
void main() {
  group('kod ↔ maske', () {
    test('kodlar kontrat sırasıyla (Pazartesi\'den) çözülür', () {
      expect(OperatingDays.fromCodes(['mon']).mask, OperatingDays.monday);
      expect(OperatingDays.fromCodes(['tue']).mask, OperatingDays.tuesday);
      expect(OperatingDays.fromCodes(['sun']).mask, OperatingDays.sunday);
      expect(
        OperatingDays.fromCodes(['mon', 'wed', 'fri']).mask,
        OperatingDays.monday | OperatingDays.wednesday | OperatingDays.friday,
      );
    });

    test('kodlar gidiş-dönüş yapar ve Pazartesi\'den sıralı çıkar', () {
      // Sunucu sırasız gönderse bile DTO sırası korunmalı (kontrat).
      final days = OperatingDays.fromCodes(['fri', 'mon', 'sun']);
      expect(days.codes, ['mon', 'fri', 'sun']);
    });

    test('tanınmayan kod yok sayılır, bilinenler korunur', () {
      // Bir yazım hatası ("pzt") seferi kaybetmemeli.
      final days = OperatingDays.fromCodes(['mon', 'pzt', 'çarşamba', 'wed']);
      expect(days.codes, ['mon', 'wed']);
    });

    test('🔴 boş/eksik gün listesi "hiç" değil "her gün" sayılır', () {
      // Alan 12.5'te additive eklendi: gelmediğinde ya da hiçbir kodu
      // tanımadığımızda seferi GİZLEMEK, panelde duran bir kaydı vatandaştan
      // sessizce saklamak olurdu. Şüphede kalınca göstermek doğru yön.
      expect(OperatingDays.fromCodes(null).runsDaily, isTrue);
      expect(OperatingDays.fromCodes(const []).runsDaily, isTrue);
      expect(OperatingDays.fromCodes(['bilinmeyen']).runsDaily, isTrue);
    });
  });

  group('🔴 gün kayması', () {
    test('her Dart günü kendi bitine düşer — Pazar 64\'tür, 1 değil', () {
      expect(OperatingDays.bitForWeekday(DateTime.monday), 1);
      expect(OperatingDays.bitForWeekday(DateTime.tuesday), 2);
      expect(OperatingDays.bitForWeekday(DateTime.wednesday), 4);
      expect(OperatingDays.bitForWeekday(DateTime.thursday), 8);
      expect(OperatingDays.bitForWeekday(DateTime.friday), 16);
      expect(OperatingDays.bitForWeekday(DateTime.saturday), 32);
      expect(OperatingDays.bitForWeekday(DateTime.sunday), 64);
    });

    test('Salı seferi Pazartesi görünmez (kaymanın klasik belirtisi)', () {
      final tuesday = OperatingDays.fromCodes(['tue']);
      expect(tuesday.runsOnWeekday(DateTime.monday), isFalse);
      expect(tuesday.runsOnWeekday(DateTime.tuesday), isTrue);
      expect(tuesday.runsOnWeekday(DateTime.wednesday), isFalse);
    });

    test('Pazar seferi yalnız Pazar çalışır (mod 7 tuzağının hedefi)', () {
      final sunday = OperatingDays.fromCodes(['sun']);
      for (var weekday = DateTime.monday; weekday <= DateTime.sunday; weekday++) {
        expect(
          sunday.runsOnWeekday(weekday),
          weekday == DateTime.sunday,
          reason: '$weekday günü için beklenmeyen sonuç',
        );
      }
    });

    test('gerçek bir tarihle de doğrulanır (3 Ağustos 2026 Pazartesi)', () {
      final monday = DateTime.utc(2026, 8, 3);
      expect(monday.weekday, DateTime.monday);
      expect(OperatingDays.fromCodes(['mon']).runsOn(monday), isTrue);
      expect(OperatingDays.fromCodes(['sun']).runsOn(monday), isFalse);
    });

    test('aralık dışı gün hiçbir bite düşmez (uydurma eşleşme yok)', () {
      expect(OperatingDays.bitForWeekday(0), 0);
      expect(OperatingDays.bitForWeekday(8), 0);
      expect(OperatingDays.daily.runsOnWeekday(0), isFalse);
    });
  });

  group('daysUntilNext', () {
    test('bugün çalışıyorsa 0 döner (bugün dâhildir)', () {
      expect(
        OperatingDays.fromCodes(['mon']).daysUntilNext(DateTime.monday),
        0,
      );
    });

    test('hafta içi hattı Cumartesi sorulduğunda 2 gün (Pazartesi) der', () {
      // 12.6'nın bitti kriteri: hafta sonu bakılan hafta içi seferi "yarın"
      // dememeli — Pazar günü sefer yok.
      final weekdays = OperatingDays.fromCodes(['mon', 'tue', 'wed', 'thu', 'fri']);
      expect(weekdays.daysUntilNext(DateTime.saturday), 2);
      expect(weekdays.daysUntilNext(DateTime.sunday), 1);
      expect(weekdays.daysUntilNext(DateTime.friday), 0);
    });

    test('hafta sonu hattı hafta ortasında doğru sarar', () {
      final weekend = OperatingDays.fromCodes(['sat', 'sun']);
      expect(weekend.daysUntilNext(DateTime.wednesday), 3);
      expect(weekend.daysUntilNext(DateTime.sunday), 0);
      expect(weekend.daysUntilNext(DateTime.monday), 5);
    });

    test('hiç çalışmayan maske null döner (uydurma gün yok)', () {
      expect(const OperatingDays(0).daysUntilNext(DateTime.monday), isNull);
    });
  });

  group('etiketler', () {
    test('özel durumlar Türkçe adlandırılır', () {
      expect(OperatingDays.daily.label, 'Her gün');
      expect(
        OperatingDays.fromCodes(['mon', 'tue', 'wed', 'thu', 'fri']).label,
        'Hafta içi',
      );
      expect(OperatingDays.fromCodes(['sat', 'sun']).label, 'Hafta sonu');
    });

    test('serbest seçim kısa adlarla listelenir', () {
      expect(
        OperatingDays.fromCodes(['mon', 'wed', 'fri']).label,
        'Pzt · Çar · Cum',
      );
    });

    test('ekran okuyucu kısaltma değil tam gün adı duyar', () {
      // "Pzt · Çar" sesli okunduğunda anlaşılmaz (11.6 erişilebilirlik kararı).
      expect(
        OperatingDays.fromCodes(['mon', 'wed']).semanticsLabel,
        'Pazartesi, Çarşamba',
      );
      expect(OperatingDays.daily.semanticsLabel, 'her gün');
      expect(
        OperatingDays.fromCodes(['sat', 'sun']).semanticsLabel,
        'hafta sonu',
      );
    });

    test('gün adı Dart gününden üretilir', () {
      expect(OperatingDays.shortNameOfWeekday(DateTime.monday), 'Pzt');
      expect(OperatingDays.shortNameOfWeekday(DateTime.sunday), 'Paz');
    });

    test('ham İngilizce kod arayüze sızmaz (Değişmez Kural #6)', () {
      // Sunucu kodları kontrat; kullanıcıya gösterilen metin Türkçe olmalı.
      for (final code in ['mon', 'tue', 'wed', 'thu', 'fri', 'sat', 'sun']) {
        final label = OperatingDays.fromCodes([code]).label;
        expect(label.toLowerCase(), isNot(contains(code)));
      }
    });
  });
}
