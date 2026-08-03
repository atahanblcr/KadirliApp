extern alias WebPanel;

using System.Net;
using FluentAssertions;
using KadirliApp.Application.Features.Trash;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PanelMenu = WebPanel::KadirliApp.Web.Common.PanelMenu;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.17 — **çöp kutusu / geri alma.**
///
/// Soft delete her modülde vardı, panelde karşılığı yoktu: yanlışlıkla silinen bir
/// duyuru <c>psql</c> olmadan geri gelmiyordu.
///
/// 🔑 Bu testlerin en kritik iddiası "kayıt geri geldi" değil, **"geri gelirken yayına
/// alınmadı"**. Geri getirme <c>status</c>'e dokunmazsa reddedilmiş bir ilan silinip geri
/// getirilerek moderasyonun etrafından dolaşılamaz — ve bu, kod okunarak fark edilmeyecek
/// bir karar olduğu için testle kilitleniyor.
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelTrashTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;
    private readonly string _marker = "TrashTest-" + Guid.NewGuid().ToString("N")[..8];

    public PanelTrashTests(WebPanelApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.Ads.IgnoreQueryFilters().Where(a => a.Title.Contains(_marker)).ExecuteDeleteAsync();
            await db.TaxiDrivers.IgnoreQueryFilters().Where(t => t.Name.Contains(_marker)).ExecuteDeleteAsync();
        });
    }

    private async Task<T?> QueryDbAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        T? result = default;
        await _factory.WithScopeAsync(async sp => result = await query(sp.GetRequiredService<AppDbContext>()));
        return result;
    }

    /// <summary>Doğrudan veritabanına silinmiş bir ilan koyar (panel akışına bağımlı kalmadan).</summary>
    private async Task<Guid> SeedDeletedAdAsync(string status)
    {
        Guid id = Guid.Empty;
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var categoryId = await db.AdCategories.Select(c => c.Id).FirstAsync();
            var userId = await db.Users.Select(u => u.Id).FirstAsync();

            var ad = new Ad
            {
                Title = _marker + " İlanı",
                Description = "Çöp kutusu testi",
                CategoryId = categoryId,
                UserId = userId,
                ContactPhone = "+905550000000",
                Status = status,
                DeletedAt = DateTime.UtcNow.AddMinutes(-5)
            };
            db.Ads.Add(ad);
            await db.SaveChangesAsync();
            id = ad.Id;
        });
        return id;
    }

    // ─────────────────────────── kapsam ───────────────────────────

    /// <summary>
    /// Kapsam tek listede olmalı: sorgu ve komut ayrı <c>switch</c>'ler yazarsa biri
    /// güncellenip diğeri unutulur → "listede görünen ama geri getirilemeyen kayıt".
    /// </summary>
    [Fact]
    public void TrashScope_CoversTheSoftDeletableContentModules()
    {
        TrashModules.Keys.Should().BeEquivalentTo(
            new[] { "ads", "announcements", "deaths", "events", "campaigns", "taxis" });

        // GuideItem ISoftDeletable değil — kapsamda olmamalı (silmesi fiziksel).
        TrashModules.Keys.Should().NotContain("guide");
        // Kullanıcı hesabı bilinçli olarak dışarıda: silme talebi yönetici tarafından geri alınmaz.
        TrashModules.Keys.Should().NotContain("users");
    }

    [Fact]
    public void TrashMenuItem_IsOutsideThePermissionMatrix()
    {
        var item = PanelMenu.Items.SingleOrDefault(i => i.Controller == "TrashAdmin");

        item.Should().NotBeNull();
        item!.Module.Should().BeNull("karşılığı olmayan yetki üretmemeli");
        PanelMenu.AdminOnlyControllers.Should().Contain("TrashAdmin");
    }

    [Fact]
    public async Task Moderator_CannotOpenTheTrash()
    {
        await _factory.EnsureModeratorAsync("trash-moderator-test", "Moderator123!");
        var client = _factory.CreatePanelClient();
        await client.LoginAsync("trash-moderator-test", "Moderator123!");

        var response = await client.GetAsync("/TrashAdmin/Index");

        response.StatusCode.Should().NotBe(HttpStatusCode.OK,
            "geri getirme, moderatörün silme kararını tersine çevirmektir — ayrı bir güven");
    }

    // ─────────────────────────── liste ───────────────────────────

    /// <summary>
    /// 🔑 Global soft-delete süzgeci tam olarak bu satırları gizler. <c>IgnoreQueryFilters()</c>
    /// unutulursa çöp kutusu **her zaman boş** görünür ve kimse hata almaz.
    /// </summary>
    [Fact]
    public async Task DeletedRecord_AppearsInTheTrash()
    {
        var client = await _factory.SuperAdminAsync();
        await SeedDeletedAdAsync("approved");

        var html = await (await client.GetAsync("/TrashAdmin/Index")).ReadDecodedBodyAsync();

        html.Should().Contain(_marker, "silinmiş ilan çöp kutusunda listelenmeli");
        html.Should().Contain("İlanlar", "kaydın hangi modülden geldiği yazmalı");
    }

    [Fact]
    public async Task LiveRecord_DoesNotAppearInTheTrash()
    {
        var client = await _factory.SuperAdminAsync();
        var id = await SeedDeletedAdAsync("approved");

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var ad = await db.Ads.IgnoreQueryFilters().FirstAsync(a => a.Id == id);
            ad.DeletedAt = null;
            await db.SaveChangesAsync();
        });

        var html = await (await client.GetAsync("/TrashAdmin/Index")).ReadDecodedBodyAsync();

        html.Should().NotContain(_marker, "silinmemiş kayıt çöp kutusunda görünmemeli");
    }

    [Fact]
    public async Task ModuleFilter_NarrowsTheList()
    {
        var client = await _factory.SuperAdminAsync();
        await SeedDeletedAdAsync("approved");

        var ads = await (await client.GetAsync("/TrashAdmin/Index?module=ads")).ReadDecodedBodyAsync();
        var taxis = await (await client.GetAsync("/TrashAdmin/Index?module=taxis")).ReadDecodedBodyAsync();

        ads.Should().Contain(_marker);
        taxis.Should().NotContain(_marker, "modül süzgeci gerçekten süzmeli, yoksa filtre bir süstür");
    }

    // ─────────────────────────── geri getirme ───────────────────────────

    [Fact]
    public async Task Restore_BringsTheRecordBack()
    {
        var client = await _factory.SuperAdminAsync();
        var id = await SeedDeletedAdAsync("approved");

        var response = await client.PostFormAsync("/TrashAdmin/Restore",
            new Dictionary<string, string> { ["module"] = "ads", ["id"] = id.ToString() },
            tokenFromPath: "/TrashAdmin/Index");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var ad = await QueryDbAsync(db => db.Ads.IgnoreQueryFilters().FirstAsync(a => a.Id == id));
        ad!.DeletedAt.Should().BeNull("kayıt geri gelmeli");

        var html = await (await client.GetAsync("/TrashAdmin/Index")).ReadDecodedBodyAsync();
        html.Should().NotContain(_marker, "geri getirilen kayıt çöp kutusundan çıkmalı");
    }

    /// <summary>
    /// 🔑 <b>Çöp kutusu moderasyonun arka kapısı olmamalı.</b> Reddedilmiş bir ilan silinip
    /// geri getirildiğinde <c>approved</c> olsaydı, silme+geri getirme ikilisi onay
    /// mekanizmasını tümüyle atlardı — ve panel hiçbir uyarı vermezdi.
    /// </summary>
    [Theory]
    [InlineData("rejected")]
    [InlineData("pending")]
    public async Task Restore_DoesNotPublishTheRecord(string status)
    {
        var client = await _factory.SuperAdminAsync();
        var id = await SeedDeletedAdAsync(status);

        await client.PostFormAsync("/TrashAdmin/Restore",
            new Dictionary<string, string> { ["module"] = "ads", ["id"] = id.ToString() },
            tokenFromPath: "/TrashAdmin/Index");

        var ad = await QueryDbAsync(db => db.Ads.IgnoreQueryFilters().FirstAsync(a => a.Id == id));
        ad!.DeletedAt.Should().BeNull();
        ad.Status.Should().Be(status, "geri getirme yayına alma değildir — durum korunmalı");
    }

    /// <summary>Geri getirme de bir yönetici kararıdır; izsiz kalmamalı.</summary>
    [Fact]
    public async Task Restore_LeavesAnAuditTrail()
    {
        var client = await _factory.SuperAdminAsync();
        var id = await SeedDeletedAdAsync("approved");

        await client.PostFormAsync("/TrashAdmin/Restore",
            new Dictionary<string, string> { ["module"] = "ads", ["id"] = id.ToString() },
            tokenFromPath: "/TrashAdmin/Index");

        var logged = await QueryDbAsync(db => db.AuditLogs
            .AnyAsync(a => a.AffectedId == id && a.Action == "restore" && a.Module == "ads"));

        logged.Should().BeTrue("geri getirme audit_logs'a yazılmalı");

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.AuditLogs.Where(a => a.AffectedId == id).ExecuteDeleteAsync();
        });
    }

    /// <summary>
    /// <c>guide</c> soft-delete tutmuyor; istek geçerli görünse de reddedilmeli. Çökmek yerine
    /// yöneticiye mesajla dönülür.
    /// </summary>
    [Fact]
    public async Task Restore_UnknownModule_IsRejected()
    {
        var client = await _factory.SuperAdminAsync();
        await SeedDeletedAdAsync("approved"); // sayfada en az bir form olsun ki token okunabilsin

        var response = await client.PostFormAsync("/TrashAdmin/Restore",
            new Dictionary<string, string> { ["module"] = "guide", ["id"] = Guid.NewGuid().ToString() },
            tokenFromPath: "/TrashAdmin/Index");

        // Hata kullanıcıya taşınır (TempData), istek 500 olmaz.
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Restore_AlreadyRestoredRecord_ReportsNotFound_WithoutCrashing()
    {
        var client = await _factory.SuperAdminAsync();
        var id = await SeedDeletedAdAsync("approved");

        // ⚠️ Token'ı bir kez al: ilk geri getirmeden SONRA çöp kutusu boşalır, boş sayfada
        // hiç form (dolayısıyla hiç token) olmaz. Token oturum başına geçerlidir.
        var token = await client.GetAntiforgeryTokenAsync("/TrashAdmin/Index");

        Task<HttpResponseMessage> RestoreAsync() => client.PostAsync("/TrashAdmin/Restore",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["module"] = "ads",
                ["id"] = id.ToString(),
                ["__RequestVerificationToken"] = token
            }));

        await RestoreAsync();
        var second = await RestoreAsync();

        second.StatusCode.Should().Be(HttpStatusCode.Redirect, "ikinci geri getirme çökmemeli");

        var auditCount = await QueryDbAsync(db => db.AuditLogs.CountAsync(a => a.AffectedId == id && a.Action == "restore"));
        auditCount.Should().Be(1, "değişiklik olmayan istek iz bırakmamalı — aksi hâlde denetim izi gürültüyle dolar");

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.AuditLogs.Where(a => a.AffectedId == id).ExecuteDeleteAsync();
        });
    }
}
