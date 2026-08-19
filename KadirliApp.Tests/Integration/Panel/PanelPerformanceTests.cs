extern alias WebPanel;

using System.Net;
using FluentAssertions;
using KadirliApp.Application.Common.Performance;
using KadirliApp.Application.Features.Performance;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PanelDisplay = WebPanel::KadirliApp.Web.Common.PanelDisplay;
using PanelMenu = WebPanel::KadirliApp.Web.Common.PanelMenu;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 12.22a — <b>performans panosu.</b> Görünmez sözleşme <b>#83</b>'ün panel ayağı.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Ekranın var olma sebebi bir hasar sınıfı:</b> bir uç yavaşladığında kimse hata
/// almaz. Bu testler ekranın <i>açıldığını</i> değil, <b>söylediği şeyin doğru</b>
/// olduğunu kilitliyor — özellikle ölçümün gerçekten toplandığını ve "ölçüm yok" ile
/// "istek yok"un ayırt edildiğini.
/// </para>
/// <para>
/// ⚠️ Ekran <b>yalnız-admin</b> desenine tabi (<c>ARCHITECTURE.md</c> §3): matris dışında,
/// menü satırının <c>Module</c>'ü <c>null</c>. Bu desenin kendisi
/// <c>PanelModeratorPermissionTests</c> tarafından <b>türetilerek</b> denetlendiği için
/// burada tekrar edilmiyor; burada yalnız desenin <i>bu ekrana ait</i> yüzü var.
/// </para>
/// </remarks>
[Collection(PanelCollection.Name)]
public class PanelPerformanceTests
{
    private readonly WebPanelApplicationFactory _factory;

    public PanelPerformanceTests(WebPanelApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task TheScreen_Opens_AndExplainsWhyItExists()
    {
        var client = await _factory.SuperAdminAsync();

        var response = await client.GetAsync("/PerformanceAdmin/Index");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.ReadDecodedBodyAsync();
        body.Should().Contain("Performans");
        body.Should().Contain("p95", "ekranın tamamı bu sayının etrafında kuruldu");
    }

    /// <summary>
    /// 🔑 <b>Asıl iddia bu:</b> ekran açılıyor olması bir şey söylemez — <b>ölçümün
    /// gerçekten toplandığını</b> göstermek gerekir. Panelin kendi isteği de boru
    /// hattından geçtiği için, sayfayı açmak <c>GetRequestMetricsQuery</c>'yi ölçüme
    /// düşürür: yani ekran <b>kendi ölçümünü</b> göstermek zorunda.
    /// </summary>
    [Fact]
    public async Task TheScreen_ShowsRealMeasurements_NotAnEmptyShell()
    {
        var client = await _factory.SuperAdminAsync();

        // 🐛 Sıfırlama bir SÜS DEĞİL, testin belirleyiciliğinin kendisi. Bu test ilk
        //    yazımında tek başına YEŞİL, tam süitte KIRMIZI koştu: panel testleri tek
        //    uygulamayı paylaşıyor, süit boyunca yüzlerce handler ölçülüyor ve tablo
        //    en fazla 40 satır (p95'i en yüksek olanlar) çiziyor — aranan hızlı sorgu
        //    o kesiğin altında kalıyordu. 🔑 Ders: paylaşılan durum üzerine kurulan bir
        //    iddia, o durumu ÖNCE kendisi kurmalı.
        await client.PostFormAsync(
            "/PerformanceAdmin/DeleteMeasurements",
            new Dictionary<string, string>(),
            tokenFromPath: "/PerformanceAdmin/Index");

        // İlk istek ölçümü doğurur, ikincisi onu GÖRÜR.
        await client.GetAsync("/PerformanceAdmin/Index");
        var body = await (await client.GetAsync("/PerformanceAdmin/Index")).ReadDecodedBodyAsync();

        body.Should().Contain(nameof(KadirliApp.Application.Features.Performance.Queries.GetRequestMetricsQuery),
            "panelin kendi sorgusu ölçülmüş olmalı — görünmüyorsa ya halka boru hattında " +
            "değil ya da okuma yazmayla aynı sayaçlara bakmıyor (iki ayrı singleton hatası)");
    }

    /// <summary>
    /// 🔴 <b>Boş tablo İKİ ayrı şey demek olabilir</b> ve ikisi karışırsa ekran sessizce
    /// güven verir: <i>"hiç istek gelmedi"</i> (iyi haber) ile <i>"ölçüm çalışmıyor"</i>
    /// (kötü haber). Ekran ayrımı yazmak zorunda.
    /// </summary>
    [Fact]
    public async Task TheScreen_NamesItsSources_SoAnEmptyTableIsNotAmbiguous()
    {
        var client = await _factory.SuperAdminAsync();

        var body = await (await client.GetAsync("/PerformanceAdmin/Index")).ReadDecodedBodyAsync();

        body.Should().Contain("Ölçümün kaynağı",
            "hangi süreçlerin sayıldığı yazılmazsa okuyucu API'nin ölçümlerinin dahil " +
            "olup olmadığını BİLEMEZ — panel ve API ayrı süreçlerdir");
    }

    /// <summary>
    /// Sıfırlama <b>geri alınamaz</b> ve denetim izine düşmek zorunda: "dün p95 şuydu"
    /// diyen birine "kim sıfırladı?" sorusunun bir cevabı olmalı.
    /// </summary>
    [Fact]
    public async Task Resetting_ClearsTheCounters_AndLeavesAnAuditTrail()
    {
        var client = await _factory.SuperAdminAsync();

        await client.GetAsync("/PerformanceAdmin/Index");

        var response = await client.PostFormAsync(
            "/PerformanceAdmin/DeleteMeasurements",
            new Dictionary<string, string>(),
            tokenFromPath: "/PerformanceAdmin/Index");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var written = await db.AuditLogs
                .AnyAsync(a => a.Module == PerformanceAudit.Module && a.Action == "reset");

            written.Should().BeTrue(
                "ölçüm silmek geri alınamaz bir yönetici eylemidir — izi düşmezse " +
                "taban çizgisinin neden değiştiği hiçbir yerde yazmaz");
        });
    }

    /// <summary>
    /// ⚠️ Denetim izi ekranı modül sütununa <b>ham İngilizce</b> basamaz (Değişmez Kural #6).
    /// Ekran matris dışında olduğu için karşılığı <c>PanelDisplay.NonMatrixModules</c>'ta
    /// olmak zorunda — bu bağ görünmezdir ve yalnız denetim ekranına bakan biri fark eder.
    /// </summary>
    [Fact]
    public void TheAuditModule_HasATurkishLabel()
    {
        PanelDisplay.ModuleLabel(PerformanceAudit.Module)
            .Should().NotBe(PerformanceAudit.Module)
            .And.Be("Performans");
    }

    /// <summary>
    /// Menü satırı var ve <c>Module</c>'ü <c>null</c> — yalnız-admin deseninin bu ekrana
    /// ait yüzü. (Desenin genel kilidi <c>PanelModeratorPermissionTests</c>'te ve kapsamını
    /// <c>AdminOnlyControllers</c>'tan <b>türetir</b>.)
    /// </summary>
    [Fact]
    public void TheMenuRow_ExistsAndStaysOutsideThePermissionMatrix()
    {
        var row = PanelMenu.Items.SingleOrDefault(i => i.Controller == "PerformanceAdmin");

        row.Should().NotBeNull("ekran menüde olmalı — erişilemeyen bir ekran yoktur");
        row!.Module.Should().BeNull(
            "modül anahtarı verilseydi izin matrisinde KARŞILIĞI OLMAYAN bir yetki belirirdi");
        PanelMenu.AdminOnlyControllers.Should().Contain("PerformanceAdmin");
    }

    /// <summary>
    /// 📌 <b>Yaklaşıklığın yönü ekranda YAZILI olmalı.</b> "25 ms" yazan bir hücreyi kesin
    /// bir ölçüm sanan okuyucu, 19 ms'lik gerçeği %30 yüksek okur ve buna göre karar verir.
    /// </summary>
    [Fact]
    public async Task TheScreen_AdmitsThatPercentilesAreApproximate()
    {
        var client = await _factory.SuperAdminAsync();

        var body = await (await client.GetAsync("/PerformanceAdmin/Index")).ReadDecodedBodyAsync();

        body.Should().Contain("yaklaşık",
            "kovalı histogram gerçeğin ÜSTÜNÜ söyler; bunu yazmayan bir tablo, kesinlik iddia eder");
    }

    /// <summary>
    /// Eşik ekranda yazmalı: "yavaş" sütunu bir sayı, ama <i>neye göre</i> yavaş olduğu
    /// yazılmazsa sayı yorumlanamaz.
    /// </summary>
    [Fact]
    public async Task TheScreen_StatesTheSlowThreshold()
    {
        var client = await _factory.SuperAdminAsync();
        var body = await (await client.GetAsync("/PerformanceAdmin/Index")).ReadDecodedBodyAsync();

        body.Should().MatchRegex(@"eşik:\s*\d+\s*ms");
    }
}
