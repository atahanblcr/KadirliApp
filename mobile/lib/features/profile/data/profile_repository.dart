import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../auth/data/models/current_user.dart';
import '../../auth/data/models/notification_preferences.dart';

/// `me` uçlarının **yazma** tarafı (okuma `AuthRepository.fetchCurrentUser`).
///
/// ⚠️ **PATCH semantiği:** gövdeye konmayan alan değişmez. Bu yüzden istekler
/// yalnız *gerçekten değişen* alanları taşır — "aynı kullanıcı adını tekrar
/// göndermek" bile sunucuda gereksiz kontrol tetikler (30 gün kuralı, benzersizlik).
class ProfileRepository {
  ProfileRepository(this._api);

  final ApiClient _api;

  /// `PATCH /v1/users/me`.
  ///
  /// [removeProfilePhoto] true iken sunucu [profilePhotoFileId]'yi yok sayar
  /// (backend kuralı) — çağıran ikisini birlikte göndermemeli.
  Future<CurrentUser> updateProfile({
    String? username,
    int? age,
    String? primaryNeighborhoodId,
    String? profilePhotoFileId,
    bool removeProfilePhoto = false,
  }) async {
    final body = <String, dynamic>{
      'username': ?username,
      'age': ?age,
      'primaryNeighborhoodId': ?primaryNeighborhoodId,
      'profilePhotoFileId': ?profilePhotoFileId,
      if (removeProfilePhoto) 'removeProfilePhoto': true,
    };

    final data = await _api.patch('/v1/users/me', body: body);
    if (data is! Map) throw ApiException.unexpectedResponse(cause: data);
    return CurrentUser.fromJson(Map<String, dynamic>.from(data));
  }

  /// `PATCH /v1/users/me/notifications` — yalnız [changes]'teki anahtarlar
  /// değişir, yanıt **güncel tüm tercihleri** döner.
  Future<NotificationPreferences> updateNotificationPreferences(
    Map<NotificationTopic, bool> changes,
  ) async {
    final body = {
      for (final entry in changes.entries) entry.key.key: entry.value,
    };

    final data = await _api.patch('/v1/users/me/notifications', body: body);
    if (data is! Map) throw ApiException.unexpectedResponse(cause: data);
    return NotificationPreferences.fromJson(Map<String, dynamic>.from(data));
  }

  /// `DELETE /v1/users/me` — hesabı siler (soft delete + anonimleştirme).
  ///
  /// [refreshToken] gövdeye konursa sunucu o token'ın jti'sini kara listeye
  /// alır (çıkış deseni). Yalnız `Role=User` silebilir → aksi
  /// `SELF_DELETE_FORBIDDEN` (403).
  Future<void> deleteAccount({String? refreshToken}) =>
      _api.delete('/v1/users/me', body: {'refreshToken': refreshToken});
}

final profileRepositoryProvider = Provider<ProfileRepository>(
  (ref) => ProfileRepository(ref.watch(apiClientProvider)),
);
