import 'package:flutter/foundation.dart';

import '../../../core/utils/utils.dart';

/// Kalkış saati hesapları — **Flutter'a bağımsız saf mantık** (11.11'in
/// `DeathSubmissionService` deseni: ekransız test edilebilir).
///
/// Sunucu ulaşım saatlerini gün içi "duvar saati" olarak veriyor
/// (`"07:00"` / `"06:30:00"`), tarih taşımıyor → hesaplar **Kadirli gün
/// içi dakikası** üzerinden yapılır, saat dilimi kaydırması yok
/// (11.7 `dutyDate` / 11.10 `eventDate` / 11.11 `funeralDate` dersinin devamı,
/// bu kez "tarihi hiç olmayan" alan).
abstract final class DepartureTimes {
  static const int minutesPerDay = 24 * 60;

  /// `"07:00"` / `"06:30:00"` → gün başından beri geçen dakika.
  /// Bozuk/boş değer `null` döner (ekranda o satır hiç çizilmez).
  static int? minutesOfDay(String? raw) {
    final value = raw?.trim();
    if (value == null || value.isEmpty) return null;
    final parts = value.split(':');
    if (parts.length < 2) return null;
    final hour = int.tryParse(parts[0]);
    final minute = int.tryParse(parts[1]);
    if (hour == null || minute == null) return null;
    if (hour < 0 || hour > 23 || minute < 0 || minute > 59) return null;
    return hour * 60 + minute;
  }

  /// Kadirli duvar saatine göre "şu an"ın gün içi dakikası.
  static int nowMinutes({DateTime? now}) {
    final turkey = now == null ? AppDate.nowInTurkey : AppDate.toTurkey(now);
    return turkey.hour * 60 + turkey.minute;
  }

  /// Verilen kalkış saatleri arasından **sıradakini** bulur.
  ///
  /// Bugün kalan sefer yoksa ilk sefere "yarın" olarak düşer — "Adana otobüsü
  /// kaçta?" sorusunun cevabı akşam 23:00'te de bir şey söylemeli.
  static NextDeparture? next(Iterable<String> times, {DateTime? now}) {
    final minutes = <int>[];
    for (final raw in times) {
      final value = minutesOfDay(raw);
      if (value != null) minutes.add(value);
    }
    if (minutes.isEmpty) return null;
    minutes.sort();

    final current = nowMinutes(now: now);
    for (final value in minutes) {
      if (value >= current) {
        return NextDeparture(
          minutesOfDay: value,
          minutesUntil: value - current,
          isTomorrow: false,
        );
      }
    }

    final first = minutes.first;
    return NextDeparture(
      minutesOfDay: first,
      minutesUntil: minutesPerDay - current + first,
      isTomorrow: true,
    );
  }

  /// Şehir içi hattın o anki durumu (`firstDeparture`/`lastDeparture`/
  /// `frequencyMinutes` üçlüsünden türer).
  ///
  /// ⚠️ Şehir içi hatlarda **sefer listesi yok**, yalnız ilk/son saat ve
  /// sıklık var → sıradaki kalkış **yaklaşık**; ekranda da öyle yazılır
  /// (kesin bilgi gibi sunmak kullanıcıyı durakta bekletir).
  static IntracityStatus intracity({
    String? first,
    String? last,
    int? frequencyMinutes,
    DateTime? now,
  }) {
    final start = minutesOfDay(first);
    final end = minutesOfDay(last);
    if (start == null || end == null) {
      return const IntracityStatus(state: IntracityServiceState.unknown);
    }

    // Gece yarısını aşan servis penceresi (23:00 → 01:00) için sonu bir gün ileri al.
    final finish = end > start ? end : end + minutesPerDay;
    final current = nowMinutes(now: now);

    if (current < start) {
      return IntracityStatus(
        state: IntracityServiceState.beforeFirst,
        nextMinutesOfDay: start,
        minutesUntil: start - current,
      );
    }

    if (current > finish) {
      return IntracityStatus(
        state: IntracityServiceState.finished,
        nextMinutesOfDay: start,
        minutesUntil: minutesPerDay - current + start,
        nextIsTomorrow: true,
      );
    }

    // Sefer aralığı bilinmiyorsa "çalışıyor" demekle yetiniyoruz.
    if (frequencyMinutes == null || frequencyMinutes <= 0) {
      return const IntracityStatus(state: IntracityServiceState.running);
    }

    final elapsed = current - start;
    final steps = (elapsed / frequencyMinutes).ceil();
    final candidate = start + steps * frequencyMinutes;
    if (candidate > finish) {
      // Son sefer geçmiş ama pencere henüz kapanmamış (ör. 21:55'te son 22:00).
      return IntracityStatus(
        state: IntracityServiceState.running,
        nextMinutesOfDay: end,
        minutesUntil: finish - current,
      );
    }

    return IntracityStatus(
      state: IntracityServiceState.running,
      nextMinutesOfDay: candidate % minutesPerDay,
      minutesUntil: candidate - current,
    );
  }

  /// Gün içi dakika → `"HH:mm"`.
  static String label(int minutesOfDay) {
    final normalized = minutesOfDay % minutesPerDay;
    final hour = (normalized ~/ 60).toString().padLeft(2, '0');
    final minute = (normalized % 60).toString().padLeft(2, '0');
    return '$hour:$minute';
  }

  /// "12 dakika sonra" / "2 sa 10 dk sonra" / "Şimdi".
  static String untilLabel(int minutesUntil) {
    if (minutesUntil <= 0) return 'Şimdi';
    return '${AppDate.duration(Duration(minutes: minutesUntil))} sonra';
  }
}

/// [DepartureTimes.next] sonucu.
@immutable
class NextDeparture {
  const NextDeparture({
    required this.minutesOfDay,
    required this.minutesUntil,
    required this.isTomorrow,
  });

  final int minutesOfDay;
  final int minutesUntil;
  final bool isTomorrow;

  String get label => DepartureTimes.label(minutesOfDay);

  /// Kalkışa 30 dakikadan az kaldıysa kart bunu vurgular — "yetişebilir miyim"
  /// sorusu bu eşikte anlam kazanıyor.
  bool get isImminent => !isTomorrow && minutesUntil <= 30;

  @override
  bool operator ==(Object other) =>
      other is NextDeparture &&
      other.minutesOfDay == minutesOfDay &&
      other.minutesUntil == minutesUntil &&
      other.isTomorrow == isTomorrow;

  @override
  int get hashCode => Object.hash(minutesOfDay, minutesUntil, isTomorrow);
}

enum IntracityServiceState {
  /// İlk sefer henüz kalkmadı.
  beforeFirst,

  /// Servis saatleri içinde.
  running,

  /// Bugünkü seferler bitti.
  finished,

  /// İlk/son saat girilmemiş → durum söylenemez.
  unknown,
}

/// [DepartureTimes.intracity] sonucu.
@immutable
class IntracityStatus {
  const IntracityStatus({
    required this.state,
    this.nextMinutesOfDay,
    this.minutesUntil,
    this.nextIsTomorrow = false,
  });

  final IntracityServiceState state;
  final int? nextMinutesOfDay;
  final int? minutesUntil;
  final bool nextIsTomorrow;

  String? get nextLabel =>
      nextMinutesOfDay == null ? null : DepartureTimes.label(nextMinutesOfDay!);

  @override
  bool operator ==(Object other) =>
      other is IntracityStatus &&
      other.state == state &&
      other.nextMinutesOfDay == nextMinutesOfDay &&
      other.minutesUntil == minutesUntil &&
      other.nextIsTomorrow == nextIsTomorrow;

  @override
  int get hashCode =>
      Object.hash(state, nextMinutesOfDay, minutesUntil, nextIsTomorrow);
}
