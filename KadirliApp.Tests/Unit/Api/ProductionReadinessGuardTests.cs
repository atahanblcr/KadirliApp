using FluentAssertions;
using KadirliApp.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace KadirliApp.Tests.Unit.Api;

/// <summary>
/// <see cref="ProductionReadinessGuard"/> birim testleri — Faz 11.16.
/// </summary>
/// <remarks>
/// Bu kapının kendisi <b>bayrakla kapalı bir kod yolu</b> (yalnız Production'da
/// koşuyor) — yani <c>ARCHITECTURE.md</c> §7'nin "kapalı yol = hiç test edilmemiş
/// yol" uyarısının tam hedefi. Kapı ilk kez gerçek bir yayında çalışacaksa,
/// yanlış yazılmış olduğunu tam o an öğrenmek istemeyiz.
/// </remarks>
public class ProductionReadinessGuardTests
{
    /// <summary>Yayına HAZIR bir yapılandırma — testler bunun üstünde tek ayar bozar.</summary>
    private static Dictionary<string, string?> HealthyProductionSettings() => new()
    {
        ["Otp:DevMode"] = "false",
        ["Sms:Provider"] = "Netgsm",
        ["Jwt:AccessSecret"] = "uretimde_ortam_degiskeninden_gelen_uzun_bir_sir_degeri",
        ["Jwt:RefreshSecret"] = "uretimde_ortam_degiskeninden_gelen_baska_uzun_bir_sir",
        ["Hangfire:Dashboard:Username"] = "yonetici",
        ["Hangfire:Dashboard:Password"] = "guclu-parola",
        ["FileStorage:BaseUrl"] = "",
        ["Fcm:Provider"] = "Firebase",
        // Faz 12.2: uyarı e-postası varsayılan olarak AÇIK; açıkken sağlayıcı "Dev"
        // olamaz (uyarılar sessizce kimseye gitmez). Sağlıklı yapılandırma gerçek
        // sağlayıcıyı bağlar.
        ["Email:Provider"] = "Smtp",
        // Ters vekil arkasında gerçek istemci IP'si; güvenilen kaynak BOŞ bırakılamaz.
        ["ForwardedHeaders:Enabled"] = "true",
        ["ForwardedHeaders:KnownNetworks:0"] = "10.0.0.0/8",
    };

    private static void Validate(Dictionary<string, string?> settings, string? environment = null)
    {
        environment ??= Environments.Production;

        var cfg = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var env = new Mock<IHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns(environment);

        ProductionReadinessGuard.Validate(cfg, env.Object, NullLogger.Instance);
    }

    [Fact]
    public void Saglikli_production_yapilandirmasi_gecer()
    {
        var act = () => Validate(HealthyProductionSettings());

        act.Should().NotThrow();
    }

    [Fact]
    public void Development_ortaminda_hicbir_ayar_denetlenmez()
    {
        // Geliştirme ortamı bilerek "güvensiz": DevMode açık, SMS log'a yazıyor,
        // sırlar commit edilmiş. Kapı burada çalışsaydı proje hiç açılmazdı.
        var unsafeSettings = new Dictionary<string, string?>
        {
            ["Otp:DevMode"] = "true",
            ["Sms:Provider"] = "Dev",
            ["Jwt:AccessSecret"] =
                "super_secret_key_which_is_at_least_32_characters_long_for_hmac_sha256",
        };

        var act = () => Validate(unsafeSettings, Environments.Development);

        act.Should().NotThrow();
    }

    [Fact]
    public void DevMode_acik_kalirsa_uygulama_acilmaz()
    {
        // En tehlikeli ayar: OTP yanıtın içinde döner, kimlik doğrulama çöker.
        var settings = HealthyProductionSettings();
        settings["Otp:DevMode"] = "true";

        var act = () => Validate(settings);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Otp:DevMode*");
    }

    [Fact]
    public void Dev_sms_saglayicisi_kalirsa_uygulama_acilmaz()
    {
        var settings = HealthyProductionSettings();
        settings["Sms:Provider"] = "Dev";

        var act = () => Validate(settings);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Sms:Provider*");
    }

    [Theory]
    [InlineData("Jwt:AccessSecret")]
    [InlineData("Jwt:RefreshSecret")]
    public void Commit_edilmis_jwt_sirri_kalirsa_uygulama_acilmaz(string key)
    {
        // ⚠️ Depo herkese açık: appsettings.json'daki sır GitHub'da okunabiliyor.
        // Ezilmezse üçüncü kişiler geçerli jeton üretebilir.
        var settings = HealthyProductionSettings();
        settings[key] = key.Contains("Access")
            ? "super_secret_key_which_is_at_least_32_characters_long_for_hmac_sha256"
            : "super_secret_refresh_key_which_is_at_least_32_characters_long";

        var act = () => Validate(settings);

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{key}*");
    }

    [Fact]
    public void Commit_edilmis_sirlar_appsettings_ile_AYNI_olmali()
    {
        // 🔑 Bu test kapının "hangi sır yanmış" listesini gerçek dosyayla eşitler.
        // appsettings.json'daki geliştirme sırrı değiştirilir de kapının listesi
        // güncellenmezse, kapı yanmış sırrı ARTIK TANIMAZ ve sessizce geçirir —
        // yani koruma kaybolur ama hiçbir test kırılmaz. Bu test onu engelliyor.
        var appsettingsPath = Path.Combine(
            SolutionRoot(), "KadirliApp.Api", "appsettings.json");
        var cfg = new ConfigurationBuilder()
            .AddJsonFile(appsettingsPath)
            .Build();

        foreach (var key in new[] { "Jwt:AccessSecret", "Jwt:RefreshSecret" })
        {
            var committed = cfg[key];
            committed.Should().NotBeNullOrWhiteSpace();

            var settings = HealthyProductionSettings();
            settings[key] = committed;

            var act = () => Validate(settings);
            act.Should().Throw<InvalidOperationException>(
                because: $"appsettings.json'daki {key} değeri kapının yanmış-sır " +
                         "listesinde yok — liste dosyayla ayrışmış");
        }
    }

    [Fact]
    public void Hangfire_panosu_kimliksiz_kalirsa_uygulama_acilmaz()
    {
        var settings = HealthyProductionSettings();
        settings["Hangfire:Dashboard:Username"] = "";
        settings["Hangfire:Dashboard:Password"] = "";

        var act = () => Validate(settings);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Hangfire*");
    }

    [Fact]
    public void FileStorage_BaseUrl_DOLU_ise_uygulama_acilmaz()
    {
        // ⚠️ Sezgiye ters olan madde: "prod domain'i yaz" demek YANLIŞ.
        // Görünmez sözleşme #9 göreli URL bekliyor; origin'i istemci ekliyor.
        // Doldurulursa mobil "http://…http://…" üretir ve hiçbir görsel açılmaz.
        var settings = HealthyProductionSettings();
        settings["FileStorage:BaseUrl"] = "https://api.kadirli.app";

        var act = () => Validate(settings);

        act.Should().Throw<InvalidOperationException>().WithMessage("*FileStorage:BaseUrl*");
    }

    /// <summary>
    /// Faz 12.7 — sosyal giriş "açık" işaretlenmiş ama hiçbir client id yok.
    /// </summary>
    /// <remarks>
    /// 🔴 Sessiz başarısızlık: mobilde "Google ile giriş" butonu <b>çizilir</b>, kullanıcı
    /// basar ve "bu giriş yöntemi kullanılamıyor" hatası alır — kullanıcının bakış açısından
    /// ayırt edilemez bir arıza. Ayrıca bayrak açıkken client id boş bırakmak, doğrulamanın
    /// hiç yapılamaması demektir: sosyal girişin bir numaralı zafiyeti (<c>aud</c>,
    /// §7 madde 68) tam burada başlar.
    /// </remarks>
    [Fact]
    public void Sosyal_giris_acikken_client_id_yoksa_uygulama_acilmaz()
    {
        var settings = HealthyProductionSettings();
        settings["Auth:Social:Enabled"] = "true";

        var act = () => Validate(settings);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Auth:Social*");
    }

    [Fact]
    public void Sosyal_giris_acik_ve_client_id_varsa_uygulama_acilir()
    {
        var settings = HealthyProductionSettings();
        settings["Auth:Social:Enabled"] = "true";
        settings["Auth:Social:Google:ClientIds"] = "111-kadirli.apps.googleusercontent.com";

        var act = () => Validate(settings);

        act.Should().NotThrow();
    }

    /// <summary>
    /// Bayrak kapalıyken client id aranmaz — Apple aboneliği beklenirken (12.8) canlıdaki
    /// durum bu olacak ve <b>meşru</b>: sosyal giriş kapalı, telefon + OTP çalışıyor.
    /// </summary>
    [Fact]
    public void Sosyal_giris_kapaliysa_client_id_aranmaz()
    {
        var settings = HealthyProductionSettings();
        settings["Auth:Social:Enabled"] = "false";

        var act = () => Validate(settings);

        act.Should().NotThrow();
    }

    [Fact]
    public void Push_kapaliysa_uygulama_acilir_ama_bu_engelleyici_degildir()
    {
        // Push'suz yayın meşru bir tercih: bildirim satırları yazılmaya devam eder.
        // Bu yüzden yalnız uyarı loglanır, kapı kapanmaz.
        var settings = HealthyProductionSettings();
        settings["Fcm:Provider"] = "None";

        var act = () => Validate(settings);

        act.Should().NotThrow();
    }

    [Fact]
    public void Birden_cok_sorun_varsa_HEPSI_tek_seferde_bildirilir()
    {
        // Tek tek düzeltip yeniden dağıtmak yerine listenin tamamı bir kerede
        // görünmeli — yayın gecesi her tur birkaç dakika demek.
        var settings = HealthyProductionSettings();
        settings["Otp:DevMode"] = "true";
        settings["Sms:Provider"] = "Dev";
        settings["Hangfire:Dashboard:Username"] = "";

        var act = () => Validate(settings);

        var message = act.Should().Throw<InvalidOperationException>().Which.Message;
        message.Should().Contain("Otp:DevMode");
        message.Should().Contain("Sms:Provider");
        message.Should().Contain("Hangfire");
        message.Should().Contain("3 engelleyici");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Faz 12.2 — gözlem katmanının iki sessiz başarısızlığı
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 Sessiz başarısızlığın ders kitabı örneği: şüpheli girişler işaretlenir,
    /// <c>SecurityAlertJob</c> koşar, e-posta "gönderildi" sayılır ve yalnız log'a yazılır.
    /// Yani uyarı sistemi tam ihtiyaç duyulduğu anda kimseye ulaşmaz ve bunu gösteren
    /// hiçbir belirti olmaz — uçlar 200 döner, loglar temiz görünür.
    /// </summary>
    [Fact]
    public void Uyari_epostasi_acikken_Dev_saglayicisi_uygulamayi_actirmaz()
    {
        var settings = HealthyProductionSettings();
        settings["Email:Provider"] = "Dev";

        var act = () => Validate(settings);

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("Security:AlertEmailEnabled");
    }

    /// <summary>
    /// Uyarıyı <b>bilinçli olarak</b> kapatmak meşru bir tercihtir; kapı yalnız
    /// "açık ama çalışmıyor" hâlini engeller.
    /// </summary>
    [Fact]
    public void Uyari_epostasi_bilincli_kapatilmissa_Dev_saglayicisi_engel_degildir()
    {
        var settings = HealthyProductionSettings();
        settings["Email:Provider"] = "Dev";
        settings["Security:AlertEmailEnabled"] = "false";

        var act = () => Validate(settings);

        act.Should().NotThrow();
    }

    /// <summary>
    /// 🔴 Bu bir "eksik ayar" değil, açık bir güvenlik açığı: güvenilen kaynak
    /// tanımlanmadan <c>X-Forwarded-For</c> okumak, istemcinin kendi IP'sini gizleyip
    /// <b>başkasınınkini</b> yazdırmasına izin verir — <c>login_attempts</c> masum bir
    /// kullanıcıyı işaret eder ve kayıt bir kanıt olmaktan çıkar.
    /// </summary>
    [Fact]
    public void ForwardedHeaders_acikken_guvenilen_kaynak_yoksa_uygulama_acilmaz()
    {
        var settings = HealthyProductionSettings();
        settings.Remove("ForwardedHeaders:KnownNetworks:0");

        var act = () => Validate(settings);

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("X-Forwarded-For");
    }

    /// <summary>
    /// Tek makinede doğrudan servis edilen bir kurulum meşrudur: kapı orada durmaz,
    /// yalnız uyarı loglar (aksi hâlde ters vekilsiz her yayın engellenirdi).
    /// </summary>
    [Fact]
    public void ForwardedHeaders_kapaliysa_engelleyici_degildir()
    {
        var settings = HealthyProductionSettings();
        settings["ForwardedHeaders:Enabled"] = "false";
        settings.Remove("ForwardedHeaders:KnownNetworks:0");

        var act = () => Validate(settings);

        act.Should().NotThrow();
    }

    /// <summary>Test derlemesinden çözüm köküne çıkar (appsettings.json'u okumak için).</summary>
    private static string SolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "KadirliApp.sln")))
        {
            dir = dir.Parent;
        }

        dir.Should().NotBeNull("çözüm kökü bulunamazsa test hiçbir şey denetlemez");
        return dir!.FullName;
    }
}
