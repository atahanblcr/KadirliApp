import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/features/announcements/data/models/announcement.dart';
import 'package:kadirli_app/features/pharmacies/data/models/on_duty_pharmacy.dart';
import 'package:kadirli_app/features/power_outages/data/models/power_outage.dart';

/// Ana Sayfa'yı besleyen üç modelin **gerçek sunucu gövdeleriyle** ayrıştırılması
/// ve türetilmiş kuralları (kontrat-model ayrışması burada yakalanır).
void main() {
  group('PowerOutage', () {
    final now = DateTime.utc(2026, 7, 30, 12);

    PowerOutage make({required int startHour, required int endHour}) =>
        PowerOutage.fromJson({
          'id': 'cccccccc-0000-0000-0000-000000000001',
          'neighborhood': 'Yenimahalle',
          'startTime': DateTime.utc(2026, 7, 30, startHour).toIso8601String(),
          'endTime': DateTime.utc(2026, 7, 30, endHour).toIso8601String(),
          'reason': 'Trafo bakımı',
        });

    test('gerçek yanıt gövdesi ayrıştırılır', () {
      final outage = make(startHour: 10, endHour: 14);
      expect(outage.neighborhood, 'Yenimahalle');
      expect(outage.reason, 'Trafo bakımı');
      expect(outage.startTime.toUtc().hour, 10);
    });

    test('süren kesinti aktif, gelecek olan değil', () {
      expect(make(startHour: 10, endHour: 14).isActive(now: now), isTrue);
      expect(make(startHour: 10, endHour: 14).isUpcoming(now: now), isFalse);

      expect(make(startHour: 18, endHour: 20).isActive(now: now), isFalse);
      expect(make(startHour: 18, endHour: 20).isUpcoming(now: now), isTrue);
    });

    test('biten kesinti şeride girmez', () {
      final past = make(startHour: 6, endHour: 9);
      expect(past.isActive(now: now), isFalse);
      expect(past.isUpcoming(now: now), isFalse);
      expect(past.isRelevant(now: now), isFalse);
    });

    test('bitiş anı sınırda: tam bitiş saatinde artık aktif değil', () {
      final justEnded = make(startHour: 9, endHour: 12);
      expect(justEnded.isActive(now: now), isFalse);
    });
  });

  group('Announcement', () {
    Announcement make({int priority = 0, String? sentAt}) => Announcement.fromJson({
      'id': 'dddddddd-0000-0000-0000-000000000001',
      'title': 'Pazar Yeri Taşınıyor',
      'body': 'Cumartesi pazarı kapalı alanda kurulacaktır.',
      'typeId': '5a8b77a4-d0d7-4949-8359-d43a140b8140',
      'typeName': 'Belediye Duyurusu',
      'priority': priority,
      'status': 'active',
      'targetType': null,
      'sentAt': sentAt,
      'visibleUntil': null,
      'sendPushNotification': false,
      'hasLink': false,
      'imageUrl': null,
      'hasLocation': false,
      'createdAt': '2026-07-03T21:19:26.827756Z',
    });

    test('canlı yanıttaki tüm alanlar sorunsuz ayrıştırılır', () {
      final announcement = make(sentAt: '2026-06-30T21:19:26.740368Z');
      expect(announcement.title, 'Pazar Yeri Taşınıyor');
      expect(announcement.typeName, 'Belediye Duyurusu');
      expect(announcement.hasLink, isFalse);
    });

    test('öncelik panel semantiğiyle eşleşir (0 normal / 1 yüksek / 2 acil)', () {
      expect(make().priorityLevel, AnnouncementPriority.normal);
      expect(make(priority: 1).priorityLevel, AnnouncementPriority.high);
      expect(make(priority: 2).priorityLevel, AnnouncementPriority.urgent);
      // Beklenmeyen büyük değer de "acil" sayılır (kırılmaz).
      expect(make(priority: 7).priorityLevel, AnnouncementPriority.urgent);
    });

    test('yayın tarihi sentAt yoksa createdAt\'e düşer', () {
      expect(
        make(sentAt: '2026-06-30T21:19:26.740368Z').publishedAt?.toUtc().month,
        6,
      );
      expect(make().publishedAt?.toUtc().month, 7);
    });
  });

  group('OnDutyPharmacy', () {
    final json = {
      'scheduleId': 'aaaaaaaa-0000-0000-0000-000000000001',
      'dutyDate': '2026-07-30T00:00:00Z',
      'startTime': '08:30',
      'endTime': '08:30',
      'pharmacyId': 'bbbbbbbb-0000-0000-0000-000000000001',
      'name': 'Şifa Eczanesi',
      'address': 'Cumhuriyet Cad. No:12',
      'phone': '+903287141001',
      'latitude': null,
      'longitude': null,
      'pharmacistName': 'Ecz. Zeynep Aslan',
      'workingHours': '08:30 - 19:00',
    };

    test('saat alanları METİN olarak gelir, tarihe çevrilmez', () {
      final pharmacy = OnDutyPharmacy.fromJson(json);
      expect(pharmacy.startTime, '08:30');
      expect(pharmacy.dutyHours, '08:30 - 08:30');
      expect(pharmacy.hasLocation, isFalse);
    });

    test('saat boşsa nöbet aralığı gösterilmez', () {
      final pharmacy = OnDutyPharmacy.fromJson({...json, 'startTime': '', 'endTime': ''});
      expect(pharmacy.dutyHours, isNull);
    });

    test('konumu olan eczane haritada gösterilebilir', () {
      final pharmacy = OnDutyPharmacy.fromJson({
        ...json,
        'latitude': 37.3745,
        'longitude': 36.0967,
      });
      expect(pharmacy.hasLocation, isTrue);
    });
  });
}
