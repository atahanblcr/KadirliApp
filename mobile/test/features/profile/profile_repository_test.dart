import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/features/auth/data/models/notification_preferences.dart';
import 'package:kadirli_app/features/profile/data/profile_repository.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/profile_fixtures.dart';
import '../../helpers/pump_app.dart';

/// `me` uçlarının yazma tarafı — özellikle **PATCH gövdesinin şekli**
/// (gönderilmeyen alan sunucuda değişmez).
void main() {
  ProfileRepository repositoryFor(FakeHttpAdapter adapter) =>
      ProfileRepository(testApiClient(adapter));

  group('PATCH /v1/users/me', () {
    test('yalnız verilen alanlar gövdeye girer', () async {
      final adapter = routedAdapter({
        '/v1/users/me': (_) async => jsonResponse(successEnvelope(profileBody())),
      });

      await repositoryFor(adapter).updateProfile(username: 'yeniad');

      final body = adapter.lastOf('/v1/users/me')!.data as Map;
      expect(body, {'username': 'yeniad'});
      expect(body.containsKey('age'), isFalse);
      expect(body.containsKey('primaryNeighborhoodId'), isFalse);
    });

    test('fotoğraf kaldırma bayrağı yalnız istendiğinde gider', () async {
      final adapter = routedAdapter({
        '/v1/users/me': (_) async => jsonResponse(successEnvelope(profileBody())),
      });

      await repositoryFor(adapter).updateProfile(age: 41, removeProfilePhoto: true);

      expect(adapter.lastOf('/v1/users/me')!.data, {
        'age': 41,
        'removeProfilePhoto': true,
      });
    });

    test('hiçbir alan verilmezse boş gövde (sunucu hiçbir şeyi değiştirmez)', () async {
      final adapter = routedAdapter({
        '/v1/users/me': (_) async => jsonResponse(successEnvelope(profileBody())),
      });

      await repositoryFor(adapter).updateProfile();

      expect(adapter.lastOf('/v1/users/me')!.data, isEmpty);
    });

    test('yanıt güncel profili döner (ek GET gerekmez)', () async {
      final adapter = routedAdapter({
        '/v1/users/me': (_) async => jsonResponse(
          successEnvelope(
            profileBody(username: 'guncel', profilePhotoUrl: '/uploads/x.jpg'),
          ),
        ),
      });

      final user = await repositoryFor(adapter).updateProfile(username: 'guncel');

      expect(user.username, 'guncel');
      expect(user.profilePhotoUrl, '/uploads/x.jpg');
      expect(adapter.countOf('/v1/users/me'), 1);
    });

    test('30 gün kısıtı ApiException olarak yükselir', () async {
      final adapter = routedAdapter({
        '/v1/users/me': (_) async => jsonResponse(
          errorEnvelope(
            ApiErrorCodes.usernameChangeLimit,
            'Kullanıcı adı 30 günde bir değiştirilebilir. Son değişiklik: 2026-07-10.',
          ),
          statusCode: 400,
        ),
      });

      await expectLater(
        repositoryFor(adapter).updateProfile(username: 'cokhizli'),
        throwsA(
          isA<ApiException>().having(
            (error) => error.code,
            'code',
            ApiErrorCodes.usernameChangeLimit,
          ),
        ),
      );
    });
  });

  group('PATCH /v1/users/me/notifications', () {
    test('yalnız değişen anahtar gönderilir, yanıt tüm tercihleri döner', () async {
      final adapter = routedAdapter({
        '/v1/users/me/notifications': (_) async => jsonResponse(
          successEnvelope(const {
            'announcements': true,
            'deaths': true,
            'pharmacy': true,
            'events': true,
            'ads': true,
            'campaigns': false,
          }),
        ),
      });

      final prefs = await repositoryFor(adapter).updateNotificationPreferences({
        NotificationTopic.ads: true,
      });

      expect(adapter.lastOf('/v1/users/me/notifications')!.data, {'ads': true});
      expect(prefs.ads, isTrue);
      expect(prefs.campaigns, isFalse);
    });
  });

  group('DELETE /v1/users/me', () {
    test('refresh token gövdede gider (sunucu jti iptal eder)', () async {
      final adapter = routedAdapter({
        '/v1/users/me': (_) async => jsonResponse(successEnvelope(true)),
      });

      await repositoryFor(adapter).deleteAccount(refreshToken: 'REFRESH');

      final request = adapter.lastOf('/v1/users/me')!;
      expect(request.method, 'DELETE');
      expect(request.data, {'refreshToken': 'REFRESH'});
    });

    test('admin hesabında SELF_DELETE_FORBIDDEN yükselir', () async {
      final adapter = routedAdapter({
        '/v1/users/me': (_) async => jsonResponse(
          errorEnvelope(
            ApiErrorCodes.selfDeleteForbidden,
            'Yönetici/personel hesapları bu uçtan silinemez.',
          ),
          statusCode: 403,
        ),
      });

      await expectLater(
        repositoryFor(adapter).deleteAccount(),
        throwsA(
          isA<ApiException>().having(
            (error) => error.code,
            'code',
            ApiErrorCodes.selfDeleteForbidden,
          ),
        ),
      );
    });
  });
}
