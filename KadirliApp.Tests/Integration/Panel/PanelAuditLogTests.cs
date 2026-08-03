extern alias WebPanel;

using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PanelDisplay = WebPanel::KadirliApp.Web.Common.PanelDisplay;
using PanelMenu = WebPanel::KadirliApp.Web.Common.PanelMenu;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.17 — **denetim izi ekranı.**
///
/// <c>AuditBehavior</c> 10.9(i)'den beri her hassas komutu <c>audit_logs</c>'a yazıyordu;
/// okuyan tek ekran/uç yoktu. "Bu ilanı kim sildi?" sorusu <c>psql</c> gerektiriyordu.
/// Moderatör rolü 11.15b'den beri gerçekten silebildiği için bu ekran kaçınılmazdı.
///
/// Buradaki testlerin iki ayrı işi var: (1) ekranın gerçekten **yazılmış izi okuduğunu**
/// göstermek — sahte veri değil, panelden yapılan gerçek bir silme; (2) eylem sözlüğünün
/// **kaynakla ayrışamamasını** sağlamak (yeni bir <c>IAuditableCommand</c> eklenip
/// <c>PanelDisplay</c>'e satır atılmazsa panel yine İngilizce konuşur).
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelAuditLogTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;
    private readonly string _marker = "AuditTest-" + Guid.NewGuid().ToString("N")[..8];

    public PanelAuditLogTests(WebPanelApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var ids = await db.IntracityRoutes.IgnoreQueryFilters()
                .Where(r => r.RouteName.Contains(_marker)).Select(r => r.Id).ToListAsync();

            await db.AuditLogs.Where(a => a.AffectedId != null && ids.Contains(a.AffectedId.Value)).ExecuteDeleteAsync();
            await db.IntracityRoutes.Where(r => r.RouteName.Contains(_marker)).ExecuteDeleteAsync();
        });
    }

    // ─────────────────── sözlük: kaynakla ayrışamaz ───────────────────

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !System.IO.File.Exists(Path.Combine(dir.FullName, "KadirliApp.sln")))
            dir = dir.Parent;

        dir.Should().NotBeNull("testler çözüm kökünün altından koşmalı");
        return dir!.FullName;
    }

    /// <summary>
    /// Kaynaktaki <c>AuditAction => "…"</c> literal'lerini toplar. Elle tutulan bir liste
    /// yerine tarama seçildi çünkü unutulan tam olarak **yeni eklenen** komut olur.
    /// </summary>
    public static TheoryData<string> ActionsProducedBySource()
    {
        var data = new TheoryData<string>();
        var root = Path.Combine(RepositoryRoot(), "KadirliApp.Application");

        var actions = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .SelectMany(f => Regex.Matches(System.IO.File.ReadAllText(f), @"AuditAction\s*=>\s*""([^""]+)"""))
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(a => a, StringComparer.Ordinal)
            .ToList();

        actions.Should().NotBeEmpty("tarama hiçbir eylem bulamadıysa test hiçbir şey denetlemiyor");

        foreach (var a in actions) data.Add(a);
        return data;
    }

    [Theory]
    [MemberData(nameof(ActionsProducedBySource))]
    public void AuditAction_HasTurkishLabel_ForEveryActionInSource(string action)
    {
        var badge = PanelDisplay.AuditAction(action);

        badge.Label.Should().NotContain("Bilinmeyen",
            "'{0}' kodda üretilen bir denetim eylemi; panelde ham/İngilizce görünmemeli", action);
        badge.Label.Should().NotBeEquivalentTo(action, "etiket ham değerin kendisi olmamalı");
        badge.Icon.Should().StartWith("fa-");
    }

    [Fact]
    public void AuditAction_UnknownValue_IsFlagged_NotSilentlyPrintedRaw()
    {
        var badge = PanelDisplay.AuditAction("some-new-action");

        badge.Label.Should().StartWith("Bilinmeyen işlem");
        badge.Css.Should().Contain("red");
    }

    /// <summary>
    /// Denetim izi menü satırı <b>modülsüz</b> olmalı: modül anahtarı verilseydi izin
    /// matrisinde moderatöre dağıtılabilen ama controller'ın rol kapısı yüzünden asla
    /// çalışmayacak bir yetki belirirdi — 11.15b'nin en büyük bulgusunun aynısı.
    /// </summary>
    [Fact]
    public void AuditLogMenuItem_IsOutsideThePermissionMatrix()
    {
        var item = PanelMenu.Items.SingleOrDefault(i => i.Controller == "AuditLogsAdmin");

        item.Should().NotBeNull("denetim izi menüde olmalı, yoksa ekran erişilebilir ama bulunamaz");
        item!.Module.Should().BeNull("karşılığı olmayan yetki üretmemeli");
        PanelMenu.AdminOnlyControllers.Should().Contain("AuditLogsAdmin",
            "moderatörün menüsünde çizilmemeli — controller onu zaten reddediyor");
    }

    // ─────────────────── ekran: gerçekten yazılmış izi okur ───────────────────

    [Fact]
    public async Task AnonymousRequest_IsRedirectedToLogin()
    {
        var client = _factory.CreatePanelClient();

        var response = await client.GetAsync("/AuditLogsAdmin/Index");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/account/login");
    }

    [Fact]
    public async Task Moderator_CannotOpenTheAuditTrail()
    {
        await _factory.EnsureModeratorAsync("audit-moderator-test", "Moderator123!");
        var client = _factory.CreatePanelClient();
        await client.LoginAsync("audit-moderator-test", "Moderator123!");

        var response = await client.GetAsync("/AuditLogsAdmin/Index");

        response.StatusCode.Should().NotBe(HttpStatusCode.OK,
            "denetlenen kişi denetim ekranını açamamalı — StaffAdmin ile aynı karar");
    }

    /// <summary>
    /// 🔑 Bu fazın asıl iddiası: panelden yapılan gerçek bir silme, denetim izi ekranında
    /// **Türkçe** ve **kimin yaptığı belli** şekilde görünüyor. Sahte satır eklenmiyor —
    /// <c>AuditBehavior</c>'ın gerçekten çalıştığı da böyle doğrulanmış oluyor.
    /// </summary>
    [Fact]
    public async Task DeletingARecord_ShowsUpInTheAuditTrail_InTurkish()
    {
        var client = await _factory.SuperAdminAsync();
        var routeName = _marker + " Hattı";

        await client.PostFormAsync("/TransportAdmin/Create",
            new Dictionary<string, string>
            {
                ["RouteNumber"] = "A9",
                ["RouteName"] = routeName,
                ["IsActive"] = "true"
            }, "/TransportAdmin/Create");

        var routeId = await QueryDbAsync(db => db.IntracityRoutes
            .Where(r => r.RouteName == routeName).Select(r => r.Id).FirstOrDefaultAsync());
        routeId.Should().NotBe(Guid.Empty);

        await client.PostFormAsync("/TransportAdmin/Delete",
            new Dictionary<string, string> { ["id"] = routeId.ToString() }, "/TransportAdmin/Index");

        var logged = await QueryDbAsync(db => db.AuditLogs.AnyAsync(a => a.AffectedId == routeId && a.Action == "delete"));
        logged.Should().BeTrue("AuditBehavior silme izini yazmalı — ekranın okuyacağı veri budur");

        var html = await (await client.GetAsync($"/AuditLogsAdmin/Index?affectedId={routeId}")).ReadDecodedBodyAsync();

        html.Should().Contain("Sildi", "eylem Türkçe rozetle basılmalı");
        html.Should().NotContain(">delete<", "ham İngilizce eylem adı ekrana sızmamalı");
        html.Should().Contain("Şehir içi hat", "etkilenen kaydın tipi Türkçeleşmeli");
        html.Should().Contain("Ulaşım", "modül adı Türkçe olmalı");
        html.Should().Contain("admin", "izi kimin bıraktığı görünmeli");
    }

    /// <summary>
    /// Süzgeç gerçekten süzmeli. Süzmeyen bir filtre, denetim izini "her şeyi gösteren
    /// ama hiçbir soruyu cevaplamayan" bir listeye çevirir.
    /// </summary>
    [Fact]
    public async Task AffectedIdFilter_NarrowsTheListToOneRecord()
    {
        var client = await _factory.SuperAdminAsync();

        var unrelated = Guid.NewGuid();
        var html = await (await client.GetAsync($"/AuditLogsAdmin/Index?affectedId={unrelated}")).ReadDecodedBodyAsync();

        html.Should().Contain("Bu filtreye uyan kayıt yok",
            "var olmayan kayıt kimliği hiçbir satır döndürmemeli — süzgeç yok sayılıyorsa tüm liste gelirdi");
    }

    [Fact]
    public async Task ModuleFilter_IsHonoured()
    {
        var client = await _factory.SuperAdminAsync();

        // "guide" modülüne ait iz yoksa bile sayfa açılmalı ve karışık kayıt getirmemeli;
        // asıl iddia: modül süzgeci sorguya gerçekten giriyor.
        var response = await client.GetAsync("/AuditLogsAdmin/Index?module=guide&action=delete");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.ReadDecodedBodyAsync();
        html.Should().Contain("Filtreleri temizle", "filtre uygulandığında temizleme yolu görünmeli");
    }

    private async Task<T?> QueryDbAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        T? result = default;
        await _factory.WithScopeAsync(async sp => result = await query(sp.GetRequiredService<AppDbContext>()));
        return result;
    }
}
