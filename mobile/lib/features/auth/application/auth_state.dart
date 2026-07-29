import '../data/models/current_user.dart';

/// Oturum durumu (MOBILE_UX_PLAN §8.3).
///
/// **Freezed kullanılmadı, bilinçli:** JSON'a çevrilmeyen, dört haldik bir
/// durum makinesi için Dart 3'ün `sealed class`'ı yeterli — kod üretimine
/// bağımlılık eklemeden `switch` ile tam kapsamlı eşleştirme yapılabiliyor.
/// (Freezed yalnız kontrat modellerinde: orada `fromJson` üretimi gerçek kazanç.)
sealed class AuthState {
  const AuthState();

  /// Açılışta henüz karar verilmedi (splash) — yönlendirme yapılmaz.
  const factory AuthState.unknown() = AuthUnknown;

  /// Oturum yok. Uygulama misafir olarak gezilebilir; korumalı aksiyonlar
  /// nazikçe girişe yönlendirir.
  const factory AuthState.anonymous() = AuthAnonymous;

  /// OTP doğrulandı ama hesap **henüz yok** — kayıt ekranı tamamlanmalı.
  /// [tempToken] yalnız bellekte taşınır (güvenli depoya YAZILMAZ).
  const factory AuthState.registering({
    required String phone,
    required String tempToken,
  }) = AuthRegistering;

  /// Oturum açık.
  const factory AuthState.authenticated(CurrentUser user) = AuthAuthenticated;

  bool get isUnknown => this is AuthUnknown;
  bool get isAuthenticated => this is AuthAuthenticated;
  bool get isRegistering => this is AuthRegistering;
  bool get isAnonymous => this is AuthAnonymous;

  /// Oturum açıksa kullanıcı, değilse null.
  CurrentUser? get user => switch (this) {
    AuthAuthenticated(:final user) => user,
    _ => null,
  };
}

final class AuthUnknown extends AuthState {
  const AuthUnknown();
}

final class AuthAnonymous extends AuthState {
  const AuthAnonymous();
}

final class AuthRegistering extends AuthState {
  const AuthRegistering({required this.phone, required this.tempToken});

  final String phone;
  final String tempToken;
}

final class AuthAuthenticated extends AuthState {
  const AuthAuthenticated(this.user);

  @override
  final CurrentUser user;
}
