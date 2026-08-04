extern alias WebPanel;

using System.Net;
using FluentAssertions;
using KadirliApp.Application.Features.Search.Queries;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PanelMenu = WebPanel::KadirliApp.Web.Common.PanelMenu;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.16b — **global arama** (11.18'den kalan madde).
/// </summary>
/// <remarks>
/// <para>
/// 🔑 Bu dosyanın en kritik iddiası "arama sonuç buluyor" değil,
/// <b>"moderatör yetkisi olmayan modülden tek sonuç bile alamıyor"</b>.
/// </para>
/// <para>
/// Sebep: global arama, panelin geri kalanından farklı bir yetki deseni kullanıyor.
/// Tek modüle ait olmadığı için <c>[PanelPermission]</c> takamıyor; izin, ekranın
/// kapısında değil <b>sorgunun içinde</b> uygulanıyor. Böyle bir istisna yalnız
/// kanıtlanabildiği sürece güvenlidir — <c>PanelMenu.PermissionFilteredControllers</c>'a
/// ad yazmak testi susturmaya yetmesin diye süzmenin kendisi burada denetleniyor.
/// </para>
/// </remarks>
[Collection(PanelCollection.Name)]
public class GlobalSearchTests : IAsyncLifetime
{
    private const string Username = "search_mod";
    private const string Password = "SearchMod123!";

    private readonly WebPanelApplicationFactory _factory;

    /// <summary>Her koşuya özgü, iki modülde de geçen benzersiz bir terim.</summary>
    private readonly string _marker = "Zzsearch" + Guid.NewGuid().ToString("N")[..6];

    private Guid _moderatorId;

    public GlobalSearchTests(WebPanelApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        var moderator = await _factory.EnsureModeratorAsync(Username, Password);
        _moderatorId = moderator.Id;
        await _factory.ClearMustChangePasswordAsync(Username);

        // Aynı terim HEM duyuruda HEM ilanda geçiyor; moderatöre yalnız duyuru izni verilecek.
        // Böylece "izin süzgeci çalışıyor mu" sorusu tek aramayla cevaplanabiliyor.
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();

            var typeId = await db.AnnouncementTypes.Select(t => t.Id).FirstAsync();
            db.Announcements.Add(new Announcement
            {
                Title = $"{_marker} duyurusu",
                Body = "test",
                TypeId = typeId,
                Status = "published"
            });

            var categoryId = await db.AdCategories.Select(c => c.Id).FirstAsync();
            var userId = await db.Users.Select(u => u.Id).FirstAsync();
            db.Ads.Add(new Ad
            {
                Title = $"{_marker} ilanı",
                CategoryId = categoryId,
                UserId = userId,
                Description = "test",
                Status = "approved",
                ContactPhone = "+905550000000",
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            });

            await db.SaveChangesAsync();
        });
    }

    public async Task DisposeAsync()
    {
        await SetPermissionsAsync();
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.Announcements.IgnoreQueryFilters().Where(a => a.Title.Contains(_marker)).ExecuteDeleteAsync();
            await db.Ads.IgnoreQueryFilters().Where(a => a.Title.Contains(_marker)).ExecuteDeleteAsync();
        });
    }

    private async Task SetPermissionsAsync(params AdminPermission[] permissions)
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.Set<AdminPermission>().Where(p => p.UserId == _moderatorId).ExecuteDeleteAsync();
            if (permissions.Length > 0) db.Set<AdminPermission>().AddRange(permissions);
            await db.SaveChangesAsync();
        });
    }

    private async Task<HttpClient> ModeratorClientAsync()
    {
        var client = _factory.CreatePanelClient();
        await client.LoginAsync(Username, Password);
        return client;
    }

    private async Task<string> SearchAsync(HttpClient client, string term)
    {
        var response = await client.GetAsync($"/GlobalSearch/Index?q={Uri.EscapeDataString(term)}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.ReadDecodedBodyAsync();
    }

    // ─────────────────────── Yetki süzgeci (bu dosyanın asıl işi) ───────────────────────

    /// <summary>
    /// 🔴 Moderatör yalnız izinli olduğu modülün sonuçlarını görür.
    /// Aynı terim iki modülde de geçtiği için bu, "hiç sonuç yok" ile karıştırılamaz.
    /// </summary>
    [Fact]
    public async Task Moderator_SeesResultsOnlyFromModulesTheyCanRead()
    {
        await SetPermissionsAsync(new AdminPermission
        {
            UserId = _moderatorId,
            Module = "announcements",
            CanRead = true
        });

        var client = await ModeratorClientAsync();
        var body = await SearchAsync(client, _marker);

        body.Should().Contain($"{_marker} duyurusu",
            "duyurular modülünde okuma izni var — sonuç görünmeli");
        body.Should().NotContain($"{_marker} ilanı",
            "ilanlar modülünde izni YOK; global aramanın izin süzgeci ekranın kapısında " +
            "değil sorgunun içinde çalışıyor ve tek sonuç bile sızdırmamalı");
    }

    /// <summary>
    /// Hiç izni olmayan moderatör arama ekranını açabilir ama <b>hiçbir</b> sonuç almaz.
    /// ⚠️ Ekranın 403 vermemesi bilinçli: kutu her sayfada duruyor, kapıyı çarpmak yerine
    /// boş sonuç göstermek doğru davranış — ama boş olması ŞART.
    /// </summary>
    [Fact]
    public async Task ModeratorWithNoPermissions_GetsEmptyResults()
    {
        await SetPermissionsAsync();

        var client = await ModeratorClientAsync();
        var body = await SearchAsync(client, _marker);

        body.Should().NotContain($"{_marker} duyurusu");
        body.Should().NotContain($"{_marker} ilanı");
    }

    /// <summary>Admin her modülde arar (izin matrisi admin için atlanır).</summary>
    [Fact]
    public async Task SuperAdmin_SearchesEveryModule()
    {
        var client = await _factory.SuperAdminAsync();
        var body = await SearchAsync(client, _marker);

        body.Should().Contain($"{_marker} duyurusu");
        body.Should().Contain($"{_marker} ilanı");
    }

    // ─────────────────────── Davranış ───────────────────────

    /// <summary>
    /// Çok kısa terimde arama <b>koşmaz</b> ve bunu söyler.
    /// ⚠️ Sessizce "sonuç bulunamadı" demek yanıltıcı olurdu: kullanıcı aramanın
    /// çalıştığını ama kaydın olmadığını sanırdı.
    /// </summary>
    [Fact]
    public async Task ShortTerm_IsRejectedWithAnExplanation()
    {
        var client = await _factory.SuperAdminAsync();
        var body = await SearchAsync(client, "a");

        body.Should().Contain($"en az {GlobalSearchQueryHandler.MinTermLength} karakter");
    }

    /// <summary>
    /// Arama büyük/küçük harfe duyarsız. Türkçe'de bu, "sonuç yok" sanılan
    /// hataların en sık sebebi.
    /// </summary>
    [Fact]
    public async Task Search_IsCaseInsensitive()
    {
        var client = await _factory.SuperAdminAsync();
        var body = await SearchAsync(client, _marker.ToUpperInvariant());

        body.Should().Contain($"{_marker} duyurusu");
    }

    /// <summary>
    /// 🔑 Silinen kayıt aramada <b>görünmez</b> — onun yeri Çöp Kutusu (Faz 11.17).
    /// Görünseydi "silmiştim ama hâlâ çıkıyor" karmaşası doğardı; ayrıca sonuçtan
    /// düzenleme ekranına giden bağlantı boş sayfaya götürürdü.
    /// </summary>
    [Fact]
    public async Task DeletedRecords_DoNotAppearInSearch()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var announcement = await db.Announcements.FirstAsync(a => a.Title.Contains(_marker));
            announcement.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        });

        var client = await _factory.SuperAdminAsync();
        var body = await SearchAsync(client, _marker);

        body.Should().NotContain($"{_marker} duyurusu");
    }

    // ─────────────────────── Yapısal ───────────────────────

    /// <summary>
    /// 🔑 Aranabilir modül anahtarları menüdekilerle <b>birebir</b> olmalı.
    /// Ayrışırlarsa yetkili olduğu bir modülde arama yapan yönetici hiç sonuç alamaz
    /// ve sebebini hiçbir yerde göremez (görünmez sözleşme #20'nin arama biçimi).
    /// </summary>
    [Fact]
    public void SearchableModules_AllExistInPanelMenu()
    {
        var menuModules = PanelMenu.Items
            .Where(i => i.RequiresPermission)
            .Select(i => i.Module!)
            .ToHashSet(StringComparer.Ordinal);

        var unknown = GlobalSearchQueryHandler.SearchableModules
            .Where(m => !menuModules.Contains(m))
            .ToList();

        unknown.Should().BeEmpty(
            "menüde/izin matrisinde karşılığı olmayan aranabilir modül anahtarları: {0}",
            string.Join(", ", unknown));
    }
}
