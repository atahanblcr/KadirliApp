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

  // --- İlanlar Bölüm 2 (11.9) ---

  /// İlan verme formu (`POST /v1/ads`) — oturum zorunlu.
  ///
  /// Kabuğun **dışında** tam ekran: uzun ve odaklanmış bir görev, alt sekme
  /// çubuğu kullanıcıyı formun ortasında başka yere davet etmemeli.
  static const adCreate = '/ilan-ver';

  /// İlan düzenleme (`PUT /v1/ads/{id}`) — aynı form, kategori kilitli.
  ///
  /// ⚠️ Bilinçli olarak `/ilan-ver`in **alt rotası değil**: go_router iç içe
  /// rotada üst ekranı da kurar (11.7 tuzağı) → düzenlemeye girerken arkada
  /// "yeni ilan" formu açılıyor, kategori isteği atıyor ve taslak diyaloğunu
  /// düzenleme ekranının üstüne fırlatıyordu.
  static const adEditPrefix = '/ilan-duzenle';

  static String adEdit(String id) => '$adEditPrefix/$id';

  /// "İlanlarım" (`GET /v1/users/me/ads`) — Profil sekmesinin alt rotası.
  static const myAds = '/profil/ilanlarim';

  /// "Favorilerim" (`GET /v1/users/me/favorites`) — Profil sekmesinin alt rotası.
  static const myFavorites = '/profil/favorilerim';

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

  /// Etkinlik detayı — `/etkinlikler/<id>` (11.10).
  static String eventDetail(String id) => '$events/$id';

  /// Kampanya detayı — `/kampanyalar/<id>` (11.10).
  static String campaignDetail(String id) => '$campaigns/$id';

  /// Vefat ilanı detayı — `/vefat/<id>` (11.11).
  static String deathDetail(String id) => '$deaths/$id';

  /// Taksi sürücüsü detayı — `/taksi/<id>` (11.11).
  static String taxiDriverDetail(String id) => '$taxis/$id';

  /// Mekan detayı — `/mekanlar/<id>` (11.11).
  static String placeDetail(String id) => '$places/$id';

  /// Vefat bildirimi formu (`POST /v1/deaths`) — oturum zorunlu.
  ///
  /// ⚠️ `/vefat`ın **alt rotası değil, kardeşi**: go_router iç içe rotada üst
  /// ekranı da kurar (11.7/11.9 tuzağı) → arkada liste açılıp gereksiz istek
  /// atardı. Ayrıca form kabuğun dışında, odaklanmış bir görev.
  static const deathReport = '/vefat-bildir';

  /// Haber detayı — `/haberler/<id>` (12.14). **12.15 push deep-link hedefi**
  /// (`relatedType = "news"`).
  static String newsDetail(String id) => '$news/$id';

  /// "Kaydedilenler" — yerel yer imi listesi (12.14 eki).
  ///
  /// ⚠️ `/haberler`in **alt rotası değil, kardeşi**. İki sebep: go_router iç
  /// içe rotada üst ekranı da kurar (arka planda haber/kategori isteği atardı)
  /// ve `/haberler/:id` deseni `kaydedilenler` sözcüğünü de **kimlik sanardı**.
  static const savedNews = '/kaydedilen-haberler';

  static const pharmacies = '/eczaneler';
  static const guide = '/rehber';
  static const deaths = '/vefat';
  static const taxis = '/taksi';
  static const places = '/mekanlar';
  static const events = '/etkinlikler';
  static const campaigns = '/kampanyalar';
  static const transport = '/ulasim';
  static const complaints = '/sikayet';

  /// Haberler (12.14) — 13. modül.
  static const news = '/haberler';

  /// Şikayet/istek gönderme formu (`POST /v1/complaints`).
  ///
  /// ⚠️ `/sikayet`in **alt rotası değil, kardeşi** (11.9/11.11 dersi: go_router
  /// iç içe rotada üst ekranı da kurar → arkada "Bildirimlerim" açılıp
  /// gereksiz istek atardı).
  ///
  /// ⚠️ `protectedPrefixes`'e de **yazılmadı**: uç anonim gönderime açık
  /// (`[AllowAnonymous]`), kullanıcıyı Giriş'e atmak bildirimi engellerdi.
  static const complaintCreate = '/sikayet-bildir';

  /// Bir içeriği şikayet etme kısayolu (ilan detayı → "Şikayet et").
  /// Form türü/ilgili kaydı sorgu parametreleriyle önden doldurur.
  static String complaintForContent({
    required String module,
    required String id,
    String? title,
  }) {
    final query = <String, String>{
      'tur': 'content',
      'modul': module,
      'kayit': id,
      if (title != null && title.trim().isNotEmpty) 'baslik': title.trim(),
    };
    final encoded = query.entries
        .map(
          (entry) =>
              '${entry.key}=${Uri.encodeQueryComponent(entry.value)}',
        )
        .join('&');
    return '$complaintCreate?$encoded';
  }

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
  static const protectedPrefixes = <String>[
    profileEdit,
    accountDelete,
    // 11.9: ilan verme/düzenleme ve me-scoped listeler.
    adCreate,
    adEditPrefix,
    myAds,
    myFavorites,
    // 11.11: vefat bildirimi (`POST /v1/deaths` `[A]`).
    deathReport,
  ];

  static bool requiresAuth(String location) =>
      protectedPrefixes.any(location.startsWith);
}
