/// Rota yolları tek yerde — string kopyalamak yok.
///
/// Faz 11 ilerledikçe burası büyür (11.13 push deep-link hedefleri).
abstract final class AppRoutes {
  // --- Alt sekmeler (11.4) ---
  /// Ana Sayfa (Hub) — 1. sekme.
  static const home = '/';
  static const ads = '/ilanlar';
  static const notifications = '/bildirimler';
  static const profile = '/profil';

  /// Sekme sırası — `StatefulShellRoute` dal indeksleriyle birebir.
  static const tabs = [home, ads, notifications, profile];

  // --- Kimlik doğrulama (11.3) ---
  /// Açılış: oturum kararı burada verilir, sonra yönlendirilir.
  static const splash = '/acilis';
  static const login = '/giris';
  static const otpVerify = '/giris/kod';
  static const register = '/kayit';

  // --- Ayarlar / Kontrol + Profil (11.5) ---
  static const settings = '/ayarlar';

  /// Profil düzenleme (`PATCH /v1/users/me`) — oturum zorunlu.
  static const profileEdit = '/profil/duzenle';

  /// Hesabı sil (`DELETE /v1/users/me`) — oturum zorunlu, mağaza şartı.
  static const accountDelete = '/ayarlar/hesabi-sil';

  // --- Modül ekranları (11.6+ dolduracak; bugün "yakında" ekranı) ---
  static const announcements = '/duyurular';
  static const powerOutages = '/kesintiler';

  /// Duyuru detayı — `/duyurular/<id>`. **11.13 push deep-link hedefi**
  /// (bildirimin `data.relatedId`'si doğrudan buraya gider).
  static String announcementDetail(String id) => '$announcements/$id';

  /// Kesinti detayı — `/kesintiler/<id>`.
  static String powerOutageDetail(String id) => '$powerOutages/$id';

  /// İlan detayı — `/ilanlar/<id>` (11.8). İlanlar bir **sekme** olduğu için
  /// detay o dalın alt rotası: alt sekme çubuğu yerinde kalır, geri liste
  /// konumuna döner.
  static String adDetail(String id) => '$ads/$id';

  /// Eczane detayı — `/eczaneler/<id>` (11.7).
  static String pharmacyDetail(String id) => '$pharmacies/$id';

  /// Rehber kaydı detayı — `/rehber/<id>` (11.7).
  static String guideItemDetail(String id) => '$guide/$id';

  static const pharmacies = '/eczaneler';
  static const guide = '/rehber';
  static const deaths = '/vefat';
  static const taxis = '/taksi';
  static const places = '/mekanlar';
  static const events = '/etkinlikler';
  static const campaigns = '/kampanyalar';
  static const transport = '/ulasim';
  static const complaints = '/sikayet';

  // --- Geliştirici (yalnız debug) ---
  static const designPreview = '/gelistirici/tasarim';
  static const networkProbe = '/gelistirici/ag';

  /// Giriş akışının ekranları — oturum açıkken buralara girilmez.
  static const authFlow = {login, otpVerify, register};

  /// **Oturum zorunlu** rota önekleri. Anonim kullanıcı bunlara giderse
  /// `/giris`e yönlendirilir.
  ///
  /// ⚠️ **Sekmeler bilinçli olarak burada DEĞİL.** Bildirimler/Profil sekmesine
  /// dokunan misafir sert yönlendirmeyle sekme kabuğundan atılmaz; ekranın
  /// içinde nazik `SignInPrompt` daveti görür (MOBILE_UX_PLAN §7 + 11.3'ün
  /// "misafir gezinme birinci sınıf" kararı). Bu liste, sekme dışı **gerçek
  /// korumalı ekranlar** içindir (11.5 profil düzenleme / hesap silme,
  /// 11.9 "ilan ver" gibi).
  ///
  /// ⚠️ `startsWith` ile eşleşir → `/profil` (sekme) buraya TAKILMAZ, yalnız
  /// `/profil/duzenle` takılır.
  static const protectedPrefixes = <String>[profileEdit, accountDelete];

  static bool requiresAuth(String location) =>
      protectedPrefixes.any(location.startsWith);
}
