import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Oturum token'larının deposu (API_CONTRACT §4).
///
/// İki token saklanır: **access** (her istekte `Authorization` header'ı) ve
/// **refresh** (tek kullanımlık; yenilemede rotasyona uğrar → dönen YENİSİ
/// saklanmalı). `tempToken` saklanmaz — yalnız kayıt akışında (11.3) bellekte
/// taşınır.
abstract interface class TokenStore {
  Future<String?> readAccessToken();
  Future<String?> readRefreshToken();
  Future<void> save({required String accessToken, required String refreshToken});
  Future<void> clear();

  /// Cihazda kayıtlı bir oturum var mı (açılış yönlendirmesi — 11.3).
  Future<bool> hasSession();
}

/// `flutter_secure_storage` tabanlı gerçek depo (Keychain / EncryptedSharedPrefs).
///
/// Okumalar bellekte önbelleklenir: her istekte platform kanalına gitmek
/// gereksiz gecikme yaratır. Yazma/temizleme önbelleği de günceller.
class SecureTokenStore implements TokenStore {
  SecureTokenStore({FlutterSecureStorage? storage})
    : _storage =
          storage ??
          const FlutterSecureStorage(
            aOptions: AndroidOptions(encryptedSharedPreferences: true),
            iOptions: IOSOptions(accessibility: KeychainAccessibility.first_unlock),
          );

  static const _accessKey = 'auth.accessToken';
  static const _refreshKey = 'auth.refreshToken';

  final FlutterSecureStorage _storage;

  String? _accessCache;
  String? _refreshCache;
  bool _cacheWarm = false;

  @override
  Future<String?> readAccessToken() async {
    await _warmCache();
    return _accessCache;
  }

  @override
  Future<String?> readRefreshToken() async {
    await _warmCache();
    return _refreshCache;
  }

  @override
  Future<void> save({required String accessToken, required String refreshToken}) async {
    _accessCache = accessToken;
    _refreshCache = refreshToken;
    _cacheWarm = true;
    await _storage.write(key: _accessKey, value: accessToken);
    await _storage.write(key: _refreshKey, value: refreshToken);
  }

  @override
  Future<void> clear() async {
    _accessCache = null;
    _refreshCache = null;
    _cacheWarm = true;
    await _storage.delete(key: _accessKey);
    await _storage.delete(key: _refreshKey);
  }

  @override
  Future<bool> hasSession() async => (await readRefreshToken())?.isNotEmpty ?? false;

  Future<void> _warmCache() async {
    if (_cacheWarm) return;
    _accessCache = await _storage.read(key: _accessKey);
    _refreshCache = await _storage.read(key: _refreshKey);
    _cacheWarm = true;
  }
}

/// Bellek-içi depo — testler ve platform kanalı olmayan ortamlar için.
class InMemoryTokenStore implements TokenStore {
  InMemoryTokenStore({String? accessToken, String? refreshToken})
    : _access = accessToken,
      _refresh = refreshToken;

  String? _access;
  String? _refresh;

  @override
  Future<String?> readAccessToken() async => _access;

  @override
  Future<String?> readRefreshToken() async => _refresh;

  @override
  Future<void> save({required String accessToken, required String refreshToken}) async {
    _access = accessToken;
    _refresh = refreshToken;
  }

  @override
  Future<void> clear() async {
    _access = null;
    _refresh = null;
  }

  @override
  Future<bool> hasSession() async => _refresh?.isNotEmpty ?? false;
}
