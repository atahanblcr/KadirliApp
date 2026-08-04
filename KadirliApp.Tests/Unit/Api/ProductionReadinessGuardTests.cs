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
