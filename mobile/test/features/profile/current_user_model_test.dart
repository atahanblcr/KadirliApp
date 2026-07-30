import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/features/auth/data/models/current_user.dart';
import 'package:kadirli_app/features/auth/data/models/notification_preferences.dart';

import '../../helpers/profile_fixtures.dart';

/// `MyProfileDto` ↔ `CurrentUser` eşleşmesi + 30 günlük değişiklik kısıtı.
void main() {
  group('ayrıştırma', () {
    test('tam gövde (11.5 alanları dahil) okunur', () {
      final user = CurrentUser.fromJson(
        profileBody(
          profilePhotoUrl: '/uploads/profile/a.jpg',
          usernameLastChangedAt: '2026-07-01T10:00:00.0000000Z',
          neighborhoodLastChangedAt: '2026-06-01T10:00:00.0000000Z',
        ),
      );

      expect(user.username, 'ahmetk');
      expect(user.age, 30);
      expect(user.primaryNeighborhoodName, 'Savrun');
      expect(user.profilePhotoUrl, '/uploads/profile/a.jpg');
      expect(user.notificationPreferences.announcements, isTrue);
      expect(user.notificationPreferences.ads, isFalse);
      expect(user.usernameLastChangedAt, DateTime.utc(2026, 7, 1, 10));
      expect(user.neighborhoodLastChangedAt, DateTime.utc(2026, 6, 1, 10));
      expect(user.createdAt, DateTime.utc(2026, 1, 15, 9));
    });

    test('tercih bloğu hiç gelmezse sunucu varsayılanları kullanılır', () {
      final body = profileBody()..remove('notificationPreferences');
      final user = CurrentUser.fromJson(body);

      // Sunucudaki NotificationPreferences entity varsayılanları.
      expect(user.notificationPreferences.announcements, isTrue);
      expect(user.notificationPreferences.deaths, isTrue);
      expect(user.notificationPreferences.pharmacy, isTrue);
      expect(user.notificationPreferences.events, isTrue);
      expect(user.notificationPreferences.ads, isFalse);
      expect(user.notificationPreferences.campaigns, isFalse);
    });

    test('11.3 döneminden kalan kısmi önbellek gövdesi de okunur', () {
      // Bayat önbellek (eski model) yeni alanlar olmadan gelirse patlamamalı.
      final user = CurrentUser.fromJson(const {
        'id': 'x',
        'phone': '+905321110001',
        'username': 'eski',
        'role': 'user',
      });

      expect(user.displayName, 'eski');
      expect(user.canChangeUsername(), isTrue);
    });
  });

  group('görüntüleme', () {
    test('kullanıcı adı yoksa nötr hitap', () {
      final user = CurrentUser.fromJson(profileBody(username: null));
      expect(user.displayName, 'Komşu');
      expect(user.initial, 'K');
    });

    test('baş harf Türkçe kuralına uyar (i → İ)', () {
      final user = CurrentUser.fromJson(profileBody(username: 'ismail'));
      expect(user.initial, 'İ');
    });

    test('hesap silme yalnız normal kullanıcıda mümkün', () {
      expect(CurrentUser.fromJson(profileBody()).isStandardUser, isTrue);
      expect(CurrentUser.fromJson(profileBody(role: 'admin')).isStandardUser, isFalse);
    });
  });

  group('30 günlük değişiklik kısıtı', () {
    final now = DateTime.utc(2026, 7, 30, 12);

    test('hiç değiştirilmemişse serbest (kayıt anı sayaç başlatmaz)', () {
      final user = CurrentUser.fromJson(profileBody());

      expect(user.canChangeUsername(now: now), isTrue);
      expect(user.canChangeNeighborhood(now: now), isTrue);
      expect(user.usernameChangeDaysLeft(now: now), 0);
    });

    test('20 gün önce değiştiyse kilitli ve kalan gün 10', () {
      final user = CurrentUser.fromJson(
        profileBody(usernameLastChangedAt: '2026-07-10T12:00:00.0000000Z'),
      );

      expect(user.canChangeUsername(now: now), isFalse);
      expect(user.usernameChangeDaysLeft(now: now), 10);
    });

    test('kalan süre gün cinsine yukarı yuvarlanır (3 saat → 1 gün)', () {
      final user = CurrentUser.fromJson(
        // Kısıt bugün 15:00'te bitiyor, şu an 12:00 → 3 saat kaldı.
        profileBody(usernameLastChangedAt: '2026-06-30T15:00:00.0000000Z'),
      );

      expect(user.canChangeUsername(now: now), isFalse);
      expect(user.usernameChangeDaysLeft(now: now), 1);
    });

    test('30 gün dolduğunda tekrar serbest', () {
      final user = CurrentUser.fromJson(
        profileBody(neighborhoodLastChangedAt: '2026-06-30T12:00:00.0000000Z'),
      );

      expect(user.canChangeNeighborhood(now: now), isTrue);
      expect(user.neighborhoodChangeDaysLeft(now: now), 0);
    });
  });

  group('NotificationPreferences', () {
    test('valueOf/withValue altı anahtarı da kapsar', () {
      var prefs = const NotificationPreferences();

      for (final topic in NotificationTopic.values) {
        final flipped = !prefs.valueOf(topic);
        prefs = prefs.withValue(topic, flipped);
        expect(prefs.valueOf(topic), flipped, reason: topic.key);
      }
    });

    test('enum anahtarları sunucu gövdesindeki adlarla birebir', () {
      expect(
        NotificationTopic.values.map((topic) => topic.key).toList(),
        ['announcements', 'deaths', 'pharmacy', 'events', 'ads', 'campaigns'],
      );
    });
  });
}
