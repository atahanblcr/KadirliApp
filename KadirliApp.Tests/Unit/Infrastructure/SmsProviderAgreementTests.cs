using FluentAssertions;
using KadirliApp.Api.Services;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Infrastructure;
using KadirliApp.Infrastructure.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace KadirliApp.Tests.Unit.Infrastructure;

/// <summary>
/// Faz 12.21b — <b>iki kapının aynı gerçeği anlatması.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Bu dosya ölçülmüş bir kilitlenmeden doğdu.</b> 12.21'in paketleme adımında API
/// Production'da başlatılmak istendi ve <b>hiçbir <c>Sms:Provider</c> değeriyle
/// açılmadığı</b> görüldü:
/// <list type="bullet">
/// <item><c>Dev</c> → <c>ProductionReadinessGuard</c> durdurur (haklı: SMS gitmezse
/// hiç kimse giriş yapamaz),</item>
/// <item>başka bir ad → <c>AddInfrastructure</c> *"Bilinmeyen SMS sağlayıcısı"* fırlatır
/// (haklı: gerçeklenmiş başka sağlayıcı yok).</item>
/// </list>
/// İkisi de tek başına doğru; birlikte <b>geçilemez</b>. Blokaj gerçek ve doğrudur —
/// eksik olan, bunun <b>hiçbir yerde yazmıyor</b> olmasıydı.
/// </para>
/// <para>
/// 🐛 <b>Ve bunu hiçbir test söylemiyordu, çünkü testin kendisi de aynı hatayı yapıyordu:</b>
/// <c>ProductionReadinessGuardTests.HealthyProductionSettings()</c> *"sağlıklı üretim
/// yapılandırması"* olarak <c>Sms:Provider = "Netgsm"</c> veriyordu. Yani kapı yıllarca
/// <b>hiçbir zaman var olamayacak</b> bir yapılandırmayla doğrulandı: test yeşildi,
/// iddia doğruydu, senaryo <b>hayaliydi</b>.
/// 🔑 Ders — bu projenin *"bir alanı test ederken o alana GERÇEKTE ne geldiğini ölç"*
/// (12.17) kuralının kardeşi: <b>bir yapılandırmayı test ederken o değerin GERÇEKTEN
/// seçilebilir olduğunu ölç.</b>
/// </para>
/// <para>
/// ⚠️ <b>Kapsam elle tutulmuyor:</b> sağlayıcı listesi <c>DependencyInjection</c>'ın
/// haritasından türetiliyor (<see cref="SmsProviders.Implemented"/>), yani yarın yazılacak
/// gerçek bir sağlayıcı kendiliğinden kapsanır ve bu testlerin hiçbiri elle güncellenmez.
/// </para>
/// </remarks>
public class SmsProviderAgreementTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => s.Value))
            .Build();

    private static IServiceProvider BuildWith(string smsProvider)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(Config(
            ("Sms:Provider", smsProvider),
            ("ConnectionStrings:Postgres", "Host=localhost;Database=x;Username=x;Password=x"),
            ("ConnectionStrings:Redis", "localhost:6379")));
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Haritada yazan her ad gerçekten <b>çözülebilir</b> olmalı. Bir ad eklenip
    /// gerçeklemesi yazılmazsa (ya da tipi yanlış yazılırsa) liste yalan söylerdi.
    /// </summary>
    [Fact]
    public void EveryImplementedProvider_CanActuallyBeResolved()
    {
        SmsProviders.Implemented.Should().NotBeEmpty(
            "en az bir SMS adaptörü olmalı; liste boşalırsa hiçbir ortam açılamaz");

        foreach (var provider in SmsProviders.Implemented)
        {
            var sp = BuildWith(provider);

            sp.GetService<ISmsService>().Should().NotBeNull(
                "'{0}' gerçeklenmiş sağlayıcı listesinde ama çözülemiyor", provider);
        }
    }

    /// <summary>
    /// Ters yön — <b>ve bu yön olmadan yukarıdaki iddia zayıftır</b> (§7 madde 68'in dersi):
    /// "her şeyi kabul et" gerçeklemesi de yeşil kalırdı.
    /// </summary>
    [Fact]
    public void AnUnimplementedProvider_IsRejected_AndTheMessageSaysWhatIsAvailable()
    {
        var act = () => BuildWith("BoyleBirSaglayiciYok");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Bilinmeyen SMS sağlayıcısı*")
            // 🔑 Asıl iddia bu: reddetmek yetmez, NE YAZILABİLECEĞİNİ söylemek zorunda.
            // 12.21b öncesinde mesaj yalnız "implementasyon ekleyin" diyordu ve operatör
            // hangi değerin geçerli olduğunu hiçbir yerden öğrenemiyordu.
            .Which.Message.Should().Contain(SmsProviders.Dev);
    }

    /// <summary>
    /// 🔴 <b>Kilidin kalbi: readiness kapısı, DI'nin GERÇEKTEN kabul edeceği bir değer
    /// önermek zorunda.</b>
    /// </summary>
    /// <remarks>
    /// İki durum var ve test <b>ikisini de</b> tutuyor — yani bugün de doğru, gerçek bir
    /// sağlayıcı yazıldığı gün de doğru:
    /// <list type="bullet">
    /// <item>Üretime uygun sağlayıcı <b>yoksa</b> (bugün): kapı bunu <b>açıkça söylemek</b>
    /// zorunda. Aksi hâlde operatör mesajı okur, uydurma bir ad yazar ve
    /// <b>bambaşka görünen</b> ikinci bir hataya düşer — ki 12.21'de birebir yaşandı.</item>
    /// <item>Bir gün <b>varsa</b>: kapı onları <b>adıyla</b> saymak zorunda ve o adların
    /// hepsi DI tarafından çözülebilir olmalı (ilk test bunu ayrıca denetliyor).</item>
    /// </list>
    /// </remarks>
    [Fact]
    public void TheReadinessGate_OffersOnlyProvidersThatDependencyInjectionAccepts()
    {
        var message = ProductionBlockerMessageFor(SmsProviders.Dev);

        if (SmsProviders.ProductionCapable.Count == 0)
        {
            message.Should().Contain("BUGÜN SEÇEBİLECEĞİNİZ BAŞKA BİR DEĞER YOK",
                "üretime uygun tek bir sağlayıcı bile yokken kapı bunu SÖYLEMELİ — " +
                "yoksa operatör var olmayan bir ayar değeri arar ve ikinci, ilgisiz " +
                "görünen bir hataya düşer (12.21'de ölçüldü)");
            return;
        }

        foreach (var provider in SmsProviders.ProductionCapable)
        {
            message.Should().Contain(provider,
                "kapı önerdiği sağlayıcıyı adıyla saymalı");

            BuildWith(provider).GetService<ISmsService>().Should().NotBeNull(
                "kapının önerdiği '{0}' DI tarafından çözülebilmeli — " +
                "önerilen ama çözülemeyen bir değer, kapıyı bir sonraki hataya yönlendiren " +
                "bir tabelaya çevirir", provider);
        }
    }

    /// <summary>
    /// <c>ProductionReadinessGuard</c>'ı Production'da koşturup <c>Sms</c> maddesinin
    /// engelleyici metnini döndürür.
    /// </summary>
    private static string ProductionBlockerMessageFor(string smsProvider)
    {
        var cfg = Config(
            ("Otp:DevMode", "false"),
            ("Sms:Provider", smsProvider),
            ("Jwt:AccessSecret", "uretimde_ortam_degiskeninden_gelen_uzun_bir_sir_degeri"),
            ("Jwt:RefreshSecret", "uretimde_ortam_degiskeninden_gelen_baska_uzun_bir_sir"),
            ("Hangfire:Dashboard:Username", "yonetici"),
            ("Hangfire:Dashboard:Password", "guclu-parola"),
            ("Email:Provider", "Smtp"),
            ("ForwardedHeaders:Enabled", "true"),
            ("ForwardedHeaders:KnownNetworks:0", "10.0.0.0/8"));

        var env = new Mock<IHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);

        var act = () => ProductionReadinessGuard.Validate(cfg, env.Object, NullLogger.Instance);

        return act.Should().Throw<InvalidOperationException>(
                "Sms:Provider={0} üretimde engelleyici olmalı", smsProvider)
            .Which.Message;
    }
}
