/// Rota yolları tek yerde — string kopyalamak yok.
///
/// Faz 11 ilerledikçe burası büyür (11.4 sekme kabuğu, 11.13 push deep-link
/// hedefleri).
abstract final class AppRoutes {
  static const home = '/';

  // --- Kimlik doğrulama (11.3) ---
  /// Açılış: oturum kararı burada verilir, sonra yönlendirilir.
  static const splash = '/acilis';
  static const login = '/giris';
  static const otpVerify = '/giris/kod';
  static const register = '/kayit';

  // --- Geliştirici (yalnız debug) ---
  static const designPreview = '/gelistirici/tasarim';
  static const networkProbe = '/gelistirici/ag';

  /// Giriş akışının ekranları — oturum açıkken buralara girilmez.
  static const authFlow = {login, otpVerify, register};

  /// **Oturum zorunlu** rota önekleri. Anonim kullanıcı bunlara giderse
  /// `/giris`e yönlendirilir.
  ///
  /// Bugün boş: 11.3'te oturum gerektiren bir *ekran* yok — uygulama misafir
  /// olarak gezilebiliyor, korumalı **aksiyonlar** ise `ensureSignedIn` ile
  /// nazikçe girişe yönlendiriyor (MOBILE_UX_PLAN §7). 11.4/11.5'te "Profil"
  /// ve "Bildirimler" sekmeleri eklendiğinde buraya yazılacak.
  static const protectedPrefixes = <String>[];

  static bool requiresAuth(String location) =>
      protectedPrefixes.any(location.startsWith);
}
