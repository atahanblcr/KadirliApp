import 'package:freezed_annotation/freezed_annotation.dart';

part 'current_user.freezed.dart';
part 'current_user.g.dart';

/// `GET /v1/users/me` yanıtının oturum için gereken kısmı (`MyProfileDto`).
///
/// ⚠️ Bilinçli olarak **kısmi**: bildirim tercihleri ve değişiklik-tarihi
/// alanları (`usernameLastChangedAt` / `neighborhoodLastChangedAt`) profil
/// düzenleme ekranının işi → 11.5 bu modeli genişletir. Bilinmeyen JSON
/// alanları `json_serializable`'da sessizce yok sayılır, o yüzden kısmi model
/// güvenli.
@freezed
abstract class CurrentUser with _$CurrentUser {
  const factory CurrentUser({
    required String id,
    required String phone,
    String? username,
    String? email,
    int? age,

    /// `user` | `moderator` | `admin` | `super_admin`.
    @Default('user') String role,
    String? primaryNeighborhoodId,
    String? primaryNeighborhoodName,

    /// Göreli URL (`/uploads/...`) — gösterirken `AppImage.url` ile origin eklenir.
    String? profilePhotoUrl,
    DateTime? createdAt,
  }) = _CurrentUser;

  const CurrentUser._();

  factory CurrentUser.fromJson(Map<String, dynamic> json) => _$CurrentUserFromJson(json);

  /// Selamlamada kullanılacak ad — kullanıcı adı yoksa numaranın son hanesi
  /// yerine nötr bir hitap ("Komşu") kullanılır.
  String get displayName {
    final name = username?.trim();
    return (name == null || name.isEmpty) ? 'Komşu' : name;
  }

  /// Normal kullanıcı mı (hesap silme yalnız `user` rolünde mümkün —
  /// `SELF_DELETE_FORBIDDEN`, 11.5).
  bool get isStandardUser => role == 'user';
}
