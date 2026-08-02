import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/navigation/app_modules.dart';
import 'package:kadirli_app/core/push/push_messaging.dart';
import 'package:kadirli_app/core/router/app_routes.dart';
import 'package:kadirli_app/features/notifications/data/models/app_notification.dart';

/// 🔑 Push `data` sözleşmesi ve deep-link eşlemesi.
///
/// Bu eşleme bozulursa kimse hata almaz — bildirime dokunan kullanıcı ya
/// **yanlış ekrana** gider ya da hiçbir şey olmaz. Bu yüzden hem sözleşmenin
/// okunması (`PushPayload.fromData`) hem de rota üretimi burada kilitli.
void main() {
  const guid = '3f0d0a1e-6a4c-4c7e-9f1a-2b3c4d5e6f70';

  group('PushPayload.fromData', () {
    test('sunucunun yazdığı dört anahtarı okur', () {
      // SendPushNotificationsJob.BuildData'nın birebir çıktısı.
      final payload = PushPayload.fromData(
        {
          'notificationId': guid,
          'type': 'announcement',
          'relatedId': guid,
          'relatedType': 'announcement',
        },
        title: 'Pazar Yeri Taşınıyor',
        body: 'Cumartesi pazarı…',
      );

      expect(payload.notificationId, guid);
      expect(payload.type, 'announcement');
      expect(payload.relatedId, guid);
      expect(payload.relatedType, 'announcement');
      expect(payload.title, 'Pazar Yeri Taşınıyor');
    });

    test('eksik ve boş anahtarlar null olur, yabancı anahtar yok sayılır', () {
      final payload = PushPayload.fromData({
        'notificationId': guid,
        'relatedType': '   ',
        'bilinmeyen': 'değer',
      });

      expect(payload.notificationId, guid);
      expect(payload.relatedType, isNull, reason: 'yalnız boşluk = yok');
      expect(payload.relatedId, isNull);
      expect(payload.type, isNull);
    });

    test('sayısal değerler metne çevrilir (FCM data her zaman metin taşır)', () {
      final payload = PushPayload.fromData({'notificationId': 42});
      expect(payload.notificationId, '42');
    });
  });

  group('notificationRouteFor', () {
    test('bilinen türler doğru detay rotasına gider', () {
      final expected = <String, String>{
        'announcement': AppRoutes.announcementDetail(guid),
        'ad': AppRoutes.adDetail(guid),
        'event': AppRoutes.eventDetail(guid),
        'campaign': AppRoutes.campaignDetail(guid),
        'death': AppRoutes.deathDetail(guid),
        'power_outage': AppRoutes.powerOutageDetail(guid),
        'pharmacy': AppRoutes.pharmacyDetail(guid),
        'place': AppRoutes.placeDetail(guid),
        'taxi': AppRoutes.taxiDriverDetail(guid),
        'guide': AppRoutes.guideItemDetail(guid),
      };

      for (final entry in expected.entries) {
        expect(
          notificationRouteFor(relatedType: entry.key, relatedId: guid),
          entry.value,
          reason: '${entry.key} yanlış rotaya gidiyor',
        );
      }
    });

    test('tür büyük/küçük harf ve boşluğa duyarsız', () {
      expect(
        notificationRouteFor(relatedType: '  Announcement ', relatedId: guid),
        AppRoutes.announcementDetail(guid),
      );
    });

    test('tanınmayan tür null döner — uydurma rotaya GİDİLMEZ', () {
      expect(
        notificationRouteFor(relatedType: 'gelecekte-eklenecek', relatedId: guid),
        isNull,
      );
    });

    test('bozuk ya da eksik kimlik null döner', () {
      expect(
        notificationRouteFor(relatedType: 'announcement', relatedId: null),
        isNull,
      );
      expect(
        notificationRouteFor(relatedType: 'announcement', relatedId: ''),
        isNull,
      );
      expect(
        notificationRouteFor(relatedType: 'announcement', relatedId: '12'),
        isNull,
        reason: 'GUID olmayan kimlik rotaya konmamalı',
      );
      expect(
        notificationRouteFor(
          relatedType: 'announcement',
          // Yol enjeksiyonu denemesi: rota üretimine sızmamalı.
          relatedId: '../../ayarlar',
        ),
        isNull,
      );
    });

    test('tür yoksa (yalnız kimlik) gezinilmez', () {
      expect(notificationRouteFor(relatedType: null, relatedId: guid), isNull);
    });

    /// 🔑 Doküman/kayıt tutarlılığı: eşlenen her tür gerçek ve **hazır** bir
    /// modüle gitmeli. Bir modül kaldırılır ya da `ready=false` yapılırsa
    /// deep-link kullanıcıyı "yakında" ekranına düşürürdü.
    test('eşlenen her rota, kayıtlı ve hazır bir modülün altında', () {
      const typeToModuleId = {
        'announcement': 'announcements',
        'ad': 'ads',
        'event': 'events',
        'campaign': 'campaigns',
        'death': 'deaths',
        'power_outage': 'power-outages',
        'pharmacy': 'pharmacies',
        'place': 'places',
        'taxi': 'taxis',
        'guide': 'guide',
      };

      for (final entry in typeToModuleId.entries) {
        final route = notificationRouteFor(
          relatedType: entry.key,
          relatedId: guid,
        );
        expect(route, isNotNull, reason: '${entry.key} eşlenmemiş');

        final module = kAppModules.firstWhere(
          (m) => m.id == entry.value,
          orElse: () => throw StateError('Modül kaydı yok: ${entry.value}'),
        );
        expect(
          module.ready,
          isTrue,
          reason: '${module.id} hazır değil — deep-link "yakında" ekranına düşer',
        );
        expect(
          route!.startsWith(module.route),
          isTrue,
          reason: '$route, ${module.route} modülünün altında değil',
        );
      }
    });
  });

  group('AppNotification', () {
    test('sunucu gövdesini ayrıştırır ve hedef rotayı türetir', () {
      final notification = AppNotification.fromJson({
        'id': guid,
        'title': 'Pazar Yeri Taşınıyor',
        'body': 'Cumartesi pazarı bu haftadan itibaren…',
        'type': 'announcement',
        'relatedId': guid,
        'relatedType': 'announcement',
        'isRead': false,
        'readAt': null,
        'createdAt': '2026-08-02T18:00:00Z',
      });

      expect(notification.isRead, isFalse);
      expect(notification.hasTarget, isTrue);
      expect(notification.targetRoute, AppRoutes.announcementDetail(guid));
      expect(notification.createdAt, isNotNull);
    });

    test('ilgili kayıt yoksa hedef rota da yok (bildirim yalnız okunur)', () {
      final notification = AppNotification.fromJson({
        'id': guid,
        'title': 'Bilgilendirme',
        'isRead': true,
      });

      expect(notification.hasTarget, isFalse);
      expect(notification.targetRoute, isNull);
      expect(notification.body, isEmpty, reason: 'eksik alan varsayılana düşer');
    });
  });

  group('NotificationKind', () {
    test('bilinen tür Türkçe etiket ve kendi ikonunu alır', () {
      expect(NotificationKind.of('announcement').label, 'Duyuru');
      expect(NotificationKind.of('ad').label, 'İlan');
      expect(NotificationKind.of('POWER_OUTAGE').label, 'Kesinti');
    });

    test('tanınmayan/boş tür genel bildirime düşer, kaybolmaz', () {
      expect(NotificationKind.of('gelecekte-eklenecek').label, 'Bildirim');
      expect(NotificationKind.of(null).label, 'Bildirim');
      expect(NotificationKind.of('  ').label, 'Bildirim');
    });
  });
}
