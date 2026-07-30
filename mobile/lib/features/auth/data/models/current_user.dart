import 'package:freezed_annotation/freezed_annotation.dart';

import 'notification_preferences.dart';

part 'current_user.freezed.dart';
part 'current_user.g.dart';

/// `GET|PATCH /v1/users/me` yanıtı (`MyProfileDto`).
///
/// 11.3'te oturum için gereken alanlarla başladı; **11.5'te tamamlandı**
/// (bildirim tercihleri + değişiklik tarihleri). Bilinmeyen JSON alanları
/// `json_serializable`'da sessizce yok sayılır.
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

    /// Altı bildirim anahtarı (11.5 — Ayarlar ekranı).
    @Default(NotificationPreferences()) NotificationPreferences notificationPreferences,

    /// Kullanıcı adının **en son** değiştirildiği an (kayıt anı sayılmaz →
    /// ilk değişiklik serbest). 30 günlük kısıt bundan hesaplanır.
    DateTime? usernameLastChangedAt,

    /// Birincil mahallenin en son değiştirildiği an (aynı kural).
    DateTime? neighborhoodLastChangedAt,
    DateTime? createdAt,
  }) = _CurrentUser;

  const CurrentUser._();

  factory CurrentUser.fromJson(Map<String, dynamic> json) => _$CurrentUserFromJson(json);

  /// Sunucudaki `UpdateMyProfileCommandHandler.UsernameChangeDays` /
  /// `NeighborhoodChangeDays` sabitiyle aynı (masterclass kuralı).
  static const changeCooldownDays = 30;

  /// Selamlamada kullanılacak ad — kullanıcı adı yoksa numaranın son hanesi
  /// yerine nötr bir hitap ("Komşu") kullanılır.
  String get displayName {
    final name = username?.trim();
    return (name == null || name.isEmpty) ? 'Komşu' : name;
  }

  /// Avatar harfi (fotoğraf yokken). Türkçe kuralı: `i` → `İ`
  /// (Dart'ın varsayılan `toUpperCase`'i `I` üretir).
  String get initial {
    final first = displayName.substring(0, 1);
    return first == 'i' ? 'İ' : first.toUpperCase();
  }

  /// Normal kullanıcı mı (hesap silme yalnız `user` rolünde mümkün —
  /// `SELF_DELETE_FORBIDDEN`).
  bool get isStandardUser => role == 'user';

  /// Kullanıcı adı bir daha ne zaman değiştirilebilir (hiç değişmediyse null).
  DateTime? get usernameChangeAllowedAt =>
      usernameLastChangedAt?.add(const Duration(days: changeCooldownDays));

  DateTime? get neighborhoodChangeAllowedAt =>
      neighborhoodLastChangedAt?.add(const Duration(days: changeCooldownDays));

  bool canChangeUsername({DateTime? now}) =>
      _cooldownPassed(usernameChangeAllowedAt, now);

  bool canChangeNeighborhood({DateTime? now}) =>
      _cooldownPassed(neighborhoodChangeAllowedAt, now);

  /// Kısıt bitene kaç gün kaldı (bugün biterse 1 — "yarın" demek yerine
  /// "1 gün" göstermek kullanıcıya daha güvenli bir beklenti verir).
  int usernameChangeDaysLeft({DateTime? now}) =>
      _daysLeft(usernameChangeAllowedAt, now);

  int neighborhoodChangeDaysLeft({DateTime? now}) =>
      _daysLeft(neighborhoodChangeAllowedAt, now);

  static bool _cooldownPassed(DateTime? allowedAt, DateTime? now) {
    if (allowedAt == null) return true;
    return !(now ?? DateTime.now()).toUtc().isBefore(allowedAt.toUtc());
  }

  static int _daysLeft(DateTime? allowedAt, DateTime? now) {
    if (allowedAt == null) return 0;
    final remaining = allowedAt.toUtc().difference((now ?? DateTime.now()).toUtc());
    if (remaining <= Duration.zero) return 0;
    // Kalan süre gün cinsine YUKARI yuvarlanır: 3 saat kaldıysa "1 gün".
    return (remaining.inMinutes / Duration.minutesPerDay).ceil();
  }
}
