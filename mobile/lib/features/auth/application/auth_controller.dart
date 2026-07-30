import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../../core/network/network.dart';
import '../../../core/theme/theme_controller.dart' show sharedPreferencesProvider;
import '../../notifications/data/fcm_token_service.dart';
import '../data/auth_repository.dart';
import '../data/models/auth_tokens.dart';
import '../data/models/current_user.dart';
import '../data/models/verify_otp_result.dart';
import 'auth_state.dart';

/// Oturumun tek sahibi: token depolama, profil okuma, çıkış, kayıt geçişi.
///
/// **Token yenilemeyi burası YAPMAZ** — 401 → refresh → tekrar akışı
/// `AuthInterceptor`'da (11.2). Buranın işi yenileme *başarısız olduğunda*
/// (`sessionExpiredProvider` sinyali) durumu anonime düşürmek.
final authControllerProvider = NotifierProvider<AuthController, AuthState>(
  AuthController.new,
);

/// Oturum açıksa kullanıcı — ekranların `ref.watch(currentUserProvider)` ile
/// kullandığı kısayol (11.4 selamlama, 11.5 profil).
final currentUserProvider = Provider<CurrentUser?>(
  (ref) => ref.watch(authControllerProvider).user,
);

/// Giriş ekranında bir kez gösterilip temizlenen bilgi mesajı
/// ("oturum süresi doldu" gibi). Hata değil — bilgilendirme.
final authNoticeProvider = NotifierProvider<AuthNoticeController, String?>(
  AuthNoticeController.new,
);

class AuthNoticeController extends Notifier<String?> {
  @override
  String? build() => null;

  void set(String message) => state = message;

  /// Gösterildikten sonra çağrılır (mesaj bir kereliktir).
  void clear() => state = null;
}

class AuthController extends Notifier<AuthState> {
  /// Kullanıcı "misafir olarak devam et" dediyse bir daha giriş ekranıyla
  /// karşılaşmaz (açılış doğrudan Ana Sayfa).
  static const _guestPrefsKey = 'auth.guestChoice';

  /// Son bilinen profil — çevrimdışı açılışta oturumun düşmüş görünmemesi ve
  /// ilk karede selamlamanın dolu olması için.
  static const _cachedUserPrefsKey = 'auth.cachedUser';

  var _lastSessionExpirySignal = 0;

  @override
  AuthState build() {
    // Ağ katmanı yenilemeyi denedi ve sunucu reddetti → oturumu düşür.
    // (Çevrimdışı hatada sinyal GELMEZ — bkz. AuthInterceptor.)
    _lastSessionExpirySignal = ref.read(sessionExpiredProvider);
    ref.listen(sessionExpiredProvider, (previous, next) {
      if (next > _lastSessionExpirySignal) {
        _lastSessionExpirySignal = next;
        _onSessionExpired();
      }
    });

    return const AuthState.unknown();
  }

  SharedPreferences get _prefs => ref.read(sharedPreferencesProvider);
  AuthRepository get _repository => ref.read(authRepositoryProvider);
  TokenStore get _tokens => ref.read(tokenStoreProvider);

  /// Kullanıcı daha önce "misafir devam" dedi mi (açılış yönlendirmesi).
  bool get hasChosenGuest => _prefs.getBool(_guestPrefsKey) ?? false;

  // --- Açılış ---

  /// Splash ekranının çağırdığı açılış akışı: token var mı → profil taze mi.
  ///
  /// Sıra önemli: önbellekteki profille **hemen** oturumu açar (beyaz ekran /
  /// giriş ekranı yanıp sönmesi olmaz), ardından sunucudan tazeler.
  Future<void> bootstrap() async {
    if (!await _tokens.hasSession()) {
      _clearCachedUser();
      state = const AuthState.anonymous();
      return;
    }

    final cached = _readCachedUser();
    if (cached != null) state = AuthState.authenticated(cached);

    try {
      final user = await _repository.fetchCurrentUser();
      _writeCachedUser(user);
      state = AuthState.authenticated(user);
    } on ApiException catch (error) {
      // Çevrimdışı: token'lara dokunulmaz (11.2 kararı). Önbellek varsa oturum
      // açık kalır; yoksa anonim gösterilir ama token SİLİNMEZ — sonraki
      // açılışta tekrar denenir.
      if (error.isConnectionProblem) {
        if (cached == null) state = const AuthState.anonymous();
        return;
      }
      // Sunucu reddetti (401/403: iptal edilmiş refresh, banlı/pasif hesap).
      await _endSession(revokeOnServer: false);
      if (error.isForbidden) {
        ref.read(authNoticeProvider.notifier).set(error.message);
      }
    }
  }

  // --- Giriş / kayıt ---

  /// `verify-otp` sonucunu oturuma dönüştürür.
  ///
  /// Yeni kullanıcıda kayıt ekranına geçilir (`tempToken` bellekte);
  /// kayıtlı kullanıcıda token'lar saklanıp profil çekilir.
  Future<void> completeOtpVerification({
    required String phoneE164,
    required VerifyOtpResult result,
  }) async {
    if (result.isNewUser) {
      final tempToken = result.tempToken;
      if (tempToken == null || tempToken.isEmpty) {
        throw ApiException.unexpectedResponse(cause: 'tempToken boş geldi');
      }
      state = AuthState.registering(phone: phoneE164, tempToken: tempToken);
      return;
    }

    final tokens = result.tokens;
    if (tokens == null) throw ApiException.unexpectedResponse(cause: 'token çifti boş geldi');
    await _startSession(tokens);
  }

  /// Kayıt ekranının çağırdığı son adım. Hata (kullanıcı adı çakışması vb.)
  /// çağırana fırlatılır — alan altında gösterilir.
  Future<void> completeRegistration({
    required String username,
    required String neighborhoodId,
    int? age,
  }) async {
    final current = state;
    if (current is! AuthRegistering) {
      throw ApiException.unexpectedResponse(cause: 'kayıt akışı dışında register çağrısı');
    }

    final tokens = await _repository.register(
      tempToken: current.tempToken,
      username: username,
      primaryNeighborhoodId: neighborhoodId,
      age: age,
    );
    await _startSession(tokens);
  }

  /// Kayıt ekranından geri dönüş (numarayı değiştir / vazgeç).
  void cancelRegistration() {
    if (state is AuthRegistering) state = const AuthState.anonymous();
  }

  // --- Oturum sonu ---

  /// Çıkış: sunucuda refresh iptal + FCM temizliği, ardından yerel temizlik.
  /// Sunucu hatası çıkışı engellemez (kullanıcı yine çıkmış olmalı).
  Future<void> logout() => _endSession(revokeOnServer: true);

  /// Misafir olarak devam — bir daha açılışta giriş ekranı gösterilmez.
  Future<void> continueAsGuest() async {
    state = const AuthState.anonymous();
    await _prefs.setBool(_guestPrefsKey, true);
  }

  /// Hesap silindikten sonra (11.5) yerel oturumu kapatır.
  ///
  /// Sunucu tarafı iş `DELETE /v1/users/me` ile bitti (refresh iptal + hesap
  /// anonimleştirildi) → burada `logout` çağrılmaz, yalnız yerel temizlik
  /// yapılır. "Misafir devam" tercihi de sıfırlanır: kullanıcı Giriş ekranına
  /// döner, sessizce misafir moduna düşmez.
  Future<void> completeAccountDeletion() async {
    await _prefs.remove(_guestPrefsKey);
    await _tokens.clear();
    _clearCachedUser();
    state = const AuthState.anonymous();
  }

  /// Sunucudan **taze dönen** profili oturuma yazar (11.5).
  ///
  /// `PATCH /v1/users/me` zaten güncel `MyProfileDto` döndürdüğü için ek bir
  /// `GET` atmaya gerek yok. Bildirim anahtarlarında iyimser güncelleme için de
  /// kullanılır (istek başarısız olursa çağıran eski kullanıcıyı geri yazar).
  void applyProfile(CurrentUser user) {
    if (state is! AuthAuthenticated) return;
    _writeCachedUser(user);
    state = AuthState.authenticated(user);
  }

  /// Profil güncellemesinden sonra durumu tazelemek için (11.5).
  Future<void> refreshCurrentUser() async {
    if (state is! AuthAuthenticated) return;
    try {
      final user = await _repository.fetchCurrentUser();
      _writeCachedUser(user);
      state = AuthState.authenticated(user);
    } on ApiException catch (error) {
      debugPrint('Profil tazelenemedi: ${error.code}');
    }
  }

  // --- İç akış ---

  Future<void> _startSession(AuthTokens tokens) async {
    await _tokens.save(
      accessToken: tokens.accessToken,
      refreshToken: tokens.refreshToken,
    );

    // Giriş sonrası cihaz push token'ı bağlanır (11.3'te no-op — 11.13'te gerçek).
    await ref.read(fcmTokenServiceProvider).registerAfterLogin();

    final user = await _repository.fetchCurrentUser();
    _writeCachedUser(user);
    ref.read(authNoticeProvider.notifier).clear();
    state = AuthState.authenticated(user);
  }

  Future<void> _endSession({required bool revokeOnServer}) async {
    if (revokeOnServer) {
      try {
        await _repository.logout(refreshToken: await _tokens.readRefreshToken());
      } on ApiException catch (error) {
        debugPrint('Sunucu tarafı çıkış atlandı: ${error.code}');
      }
      // Bilinçli çıkışta "misafir devam" tercihi de sıfırlanır → kullanıcı
      // Giriş ekranına döner (router kuralı), sessizce misafire düşmez.
      await _prefs.remove(_guestPrefsKey);
    }
    await _tokens.clear();
    _clearCachedUser();
    state = const AuthState.anonymous();
  }

  void _onSessionExpired() {
    // Token'ları AuthInterceptor zaten sildi; burada durum + önbellek temizlenir.
    _clearCachedUser();
    ref.read(authNoticeProvider.notifier).set(
      'Oturumunuzun süresi doldu. Lütfen tekrar giriş yapın.',
    );
    state = const AuthState.anonymous();
  }

  // --- Profil önbelleği ---

  CurrentUser? _readCachedUser() {
    final raw = _prefs.getString(_cachedUserPrefsKey);
    if (raw == null || raw.isEmpty) return null;
    try {
      return CurrentUser.fromJson(jsonDecode(raw) as Map<String, dynamic>);
    } catch (error) {
      // Model değiştiyse (11.5 alan ekleyecek) bayat önbellek atılır.
      debugPrint('Profil önbelleği okunamadı, atılıyor: $error');
      _clearCachedUser();
      return null;
    }
  }

  void _writeCachedUser(CurrentUser user) {
    _prefs.setString(_cachedUserPrefsKey, jsonEncode(user.toJson()));
  }

  void _clearCachedUser() {
    _prefs.remove(_cachedUserPrefsKey);
  }
}
