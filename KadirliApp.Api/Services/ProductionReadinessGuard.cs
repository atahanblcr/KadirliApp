using KadirliApp.Infrastructure.Notifications;

namespace KadirliApp.Api.Services;

/// <summary>
/// Production'da açılışta çalışan yapılandırma kapısı — Faz 11.16.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden var:</b> yayın öncesi ayarların listesi Progress.md'de bir <i>kontrol
/// listesiydi</i> — yani insanın hatırlamasına bağlıydı. Buradaki ayarların
/// yanlış kalması <b>sessiz</b> hasar veriyor: uygulama sorunsuz açılır, log
/// temiz görünür, uçlar 200 döner ve hata yalnızca sonuçlarından anlaşılır.
/// En kötüsü <c>Otp:DevMode</c>: açık kalırsa <c>POST /v1/auth/login</c>
/// OTP'yi <b>yanıtın içinde</b> döndürür — herkes istediği telefon numarasıyla
/// giriş yapabilir ve bu, hiçbir yerde hata olarak görünmez.
/// </para>
/// <para>
/// Bu yüzden liste bir <b>kapıya</b> çevrildi: Production ortamında güvensiz
/// ayar varsa uygulama <b>açılmaz</b> ve neyin neden yanlış olduğunu yazar.
/// Yanlış yapılandırılmış bir sistemin çalışmaması, sessizce çalışmasından iyidir.
/// </para>
/// <para>
/// ⚠️ Kapı yalnız <c>ASPNETCORE_ENVIRONMENT=Production</c> iken çalışır;
/// geliştirme ve test ortamları etkilenmez.
/// </para>
/// </remarks>
public static class ProductionReadinessGuard
{
    /// <summary>
    /// <c>appsettings.json</c>'a commit edilmiş geliştirme JWT sırları.
    /// Depo <b>herkese açık</b> olduğu için bu değerler yanmış sayılır: prod'da
    /// ezilmezlerse üçüncü kişiler geçerli erişim jetonu üretebilir.
    /// </summary>
    private static readonly string[] CommittedDevSecrets =
    [
        "super_secret_key_which_is_at_least_32_characters_long_for_hmac_sha256",
        "super_secret_refresh_key_which_is_at_least_32_characters_long"
    ];

    /// <summary>
    /// Yapılandırmayı denetler; Production'da engelleyici sorun varsa fırlatır.
    /// </summary>
    public static void Validate(IConfiguration cfg, IHostEnvironment env, ILogger logger)
    {
        if (!env.IsProduction())
        {
            return;
        }

        var blockers = new List<string>();

        // 1) OTP'nin yanıtta dönmesi — en tehlikelisi. Kimlik doğrulama tamamen çöker.
        if (cfg.GetValue("Otp:DevMode", false))
        {
            blockers.Add(
                "Otp:DevMode=true → POST /v1/auth/login OTP kodunu YANITTA döndürür. " +
                "Herkes istediği numarayla giriş yapabilir. Production'da 'false' olmalı.");
        }

        // 2) SMS sağlayıcısı — dev sağlayıcı kodu yalnız log'a yazar.
        //
        // 🔴 Faz 12.21b: mesaj artık NE YAPILACAĞINI da söylüyor ve bunu tahmin etmiyor,
        // gerçeklenmiş sağlayıcı listesinden TÜRETİYOR (`SmsProviders`). Sebebi ölçülmüş
        // bir kilitlenme: bu kapı `Dev`'i reddediyor, `AddInfrastructure` ise `Dev` dışında
        // hiçbir adı tanımıyor — yani API Production'da HİÇBİR değerle açılamıyordu.
        // Eski mesaj bunu söylemediği için operatör "Netgsm" yazıp tamamen ilgisiz görünen
        // ikinci bir hataya düşüyordu. Blokaj gerçek ve doğru; eksik olan DÜRÜSTLÜKTÜ.
        var smsProvider = cfg["Sms:Provider"] ?? SmsProviders.Dev;
        if (string.Equals(smsProvider, SmsProviders.Dev, StringComparison.OrdinalIgnoreCase))
        {
            blockers.Add(
                $"Sms:Provider={SmsProviders.Dev} → OTP kodu SMS ile gönderilmez, yalnız log'a yazılır. " +
                "Uç yine 'OTP gönderildi' der; HİÇBİR kullanıcı giriş yapamaz. " +
                SmsProviders.AvailabilitySentence());
        }

        // 3) Commit edilmiş JWT sırları — depo herkese açık.
        foreach (var (key, label) in new[]
                 {
                     ("Jwt:AccessSecret", "erişim"),
                     ("Jwt:RefreshSecret", "yenileme")
                 })
        {
            var value = cfg[key];
            if (value is not null && CommittedDevSecrets.Contains(value))
            {
                blockers.Add(
                    $"{key} hâlâ depoya commit edilmiş geliştirme değerinde. " +
                    $"Depo herkese açık olduğu için bu sır yanmıştır: üçüncü kişiler " +
                    $"geçerli {label} jetonu üretebilir. Ortam değişkeniyle ezin.");
            }
        }

        // 4) Hangfire panosu — reverse-proxy arkasında "yerel istek" kontrolü çöker.
        var hangfireUser = cfg["Hangfire:Dashboard:Username"];
        var hangfirePass = cfg["Hangfire:Dashboard:Password"];
        if (string.IsNullOrWhiteSpace(hangfireUser) || string.IsNullOrWhiteSpace(hangfirePass))
        {
            blockers.Add(
                "Hangfire:Dashboard:Username/Password boş → pano yalnız 'yerel isteklere' " +
                "açık sayılır, ama reverse-proxy arkasında HER istek yerel görünür. " +
                "/hangfire fiilen herkese açık olur.");
        }

        // 5) FileStorage:BaseUrl — DOLU olmamalı.
        // ⚠️ Bu, sezgiye ters olduğu için ayrıca yazıldı: görünmez sözleşme #9
        // görsel URL'lerinin GÖRELİ dönmesini şart koşuyor, origin'i istemci
        // ekliyor. BaseUrl doldurulursa sunucu mutlak URL döndürür ve mobil
        // istemci başına bir origin daha ekleyip "http://…http://…" üretir.
        if (!string.IsNullOrWhiteSpace(cfg["FileStorage:BaseUrl"]))
        {
            blockers.Add(
                "FileStorage:BaseUrl dolu → görsel URL'leri MUTLAK döner. " +
                "Görünmez sözleşme #9 göreli URL bekliyor (origin'i istemci ekler); " +
                "dolu bırakılırsa mobil 'http://…http://…' üretir ve hiçbir görsel açılmaz. " +
                "Boş bırakın.");
        }

        // 6) Faz 12.2 — uyarı e-postası açık ama sağlayıcı "Dev".
        // 🔴 Sessiz başarısızlığın ders kitabı örneği: şüpheli girişler işaretlenir,
        // SecurityAlertJob koşar, e-posta "gönderildi" sayılır ve yalnız log'a yazılır —
        // yani uyarı sistemi tam ihtiyaç duyulduğu anda kimseye ulaşmaz ve bunu gösteren
        // hiçbir belirti olmaz.
        var emailProvider = cfg["Email:Provider"] ?? "Dev";
        if (cfg.GetValue("Security:AlertEmailEnabled", true) &&
            string.Equals(emailProvider, "Dev", StringComparison.OrdinalIgnoreCase))
        {
            blockers.Add(
                "Security:AlertEmailEnabled=true iken Email:Provider=Dev → şüpheli giriş " +
                "uyarıları HİÇ KİMSEYE gitmez, yalnız log'a yazılır. Gerçek sağlayıcı " +
                "bağlayın (Email:Provider=Smtp) ya da uyarıyı bilinçli olarak kapatın.");
        }

        // 7) Faz 12.2 — ForwardedHeaders açık ama güvenilen proxy/ağ yok.
        // 🔴 Bu bir "eksik ayar" değil, açık bir güvenlik açığı: istemci kendi
        // X-Forwarded-For başlığını uydurup güvenlik kaydını ZEHİRLER — kendi IP'sini
        // gizler, başkasınınkini yazdırır ve login_attempts masum bir kullanıcıyı işaret eder.
        if (KadirliApp.Infrastructure.Http.ForwardedHeadersSetup.IsEnabledWithoutTrustedSources(cfg))
        {
            blockers.Add(
                "ForwardedHeaders:Enabled=true ama KnownProxies/KnownNetworks BOŞ → " +
                "istemci kendi X-Forwarded-For başlığını uydurabilir ve giriş denemesi " +
                "kaydını zehirleyebilir (kendi IP'sini gizler, başkasınınkini yazdırır). " +
                "Ters vekilin IP'sini ya da ağını yazın.");
        }

        // 8) Faz 12.2 — proxy arkasında ForwardedHeaders KAPALI olmasın.
        // Engelleyici değil (tek makinede doğrudan servis edilen bir kurulum meşru),
        // ama sonuçları sessiz: R2 herkeste yanar, R3 hiç yanmaz, IP bazlı hız sınırı
        // tek partition'a düşer.
        if (!KadirliApp.Infrastructure.Http.ForwardedHeadersSetup.IsEnabled(cfg))
        {
            logger.LogWarning(
                "ForwardedHeaders:Enabled=false → istemci IP'si ters vekilin IP'si olarak " +
                "kaydedilir. Ters vekil arkasındaysanız giriş denemesi kayıtları ve IP bazlı " +
                "hız sınırı YANLIŞ çalışır. Doğrudan servis ediyorsanız sorun yok.");
        }

        // 9) Faz 12.7 — sosyal giriş "açık" işaretlenmiş ama hiçbir client id yok.
        // 🔴 Sessiz başarısızlık: mobilde "Google ile giriş" butonu ÇİZİLİR, kullanıcı basar,
        // uç "bu giriş yöntemi şu anda kullanılamıyor" der ve bu, kullanıcının bakış açısından
        // ayırt edilemez bir arıza olur. Ayrıca bayrak açıkken client id boş bırakmak,
        // "doğrulama yapılamıyor" durumunun ta kendisidir — sosyal girişin bir numaralı
        // zafiyeti (`aud`) tam da burada başlar (§7 madde 68).
        if (cfg.GetValue("Auth:Social:Enabled", false))
        {
            var configured = new[] { "Auth:Social:Google:ClientIds", "Auth:Social:Apple:ClientIds" }
                .Where(key => cfg.GetSection(key).GetChildren().Any(c => !string.IsNullOrWhiteSpace(c.Value))
                              || !string.IsNullOrWhiteSpace(cfg.GetSection(key).Value))
                .ToList();

            if (configured.Count == 0)
            {
                blockers.Add(
                    "Auth:Social:Enabled=true ama Auth:Social:Google:ClientIds ve " +
                    "Auth:Social:Apple:ClientIds BOŞ → hiçbir sağlayıcı doğrulanamaz. " +
                    "Mobilde sosyal giriş butonu çizilir, basan herkes 'kullanılamıyor' hatası " +
                    "alır. Client id'leri ortam değişkeniyle verin ya da bayrağı kapatın.");
            }
        }

        if (blockers.Count > 0)
        {
            var message = "Production yapılandırması yayına uygun değil — " +
                          $"{blockers.Count} engelleyici ayar bulundu:" +
                          Environment.NewLine +
                          string.Join(
                              Environment.NewLine,
                              blockers.Select((b, i) => $"  {i + 1}. {b}"));
            throw new InvalidOperationException(message);
        }

        // Engelleyici değil ama sessiz: push kapalıysa bildirim hiç gitmez ve
        // bunu kimse fark etmez (iş kuyruğu boş koşar, hata düşmez).
        var fcmProvider = cfg["Fcm:Provider"] ?? "None";
        if (string.Equals(fcmProvider, "None", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Fcm:Provider=None → push bildirimleri GÖNDERİLMEYECEK. " +
                "Bildirim satırları veritabanına yazılır, cihazlara hiçbir şey düşmez. " +
                "Bilinçli bir tercihse sorun yok.");
        }
    }
}
