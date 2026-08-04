extern alias WebPanel;

using System.Net;
using System.Reflection;
using FluentAssertions;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PanelPermissionFilter = WebPanel::KadirliApp.Web.Authorization.PanelPermissionFilter;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.18 — **toplu işlem** (11.15c B grubu).
///
/// Önceki durum: hiçbir listede satır seçimi yoktu; onay kuyruğundaki 40 ilan tek tek,
/// her biri ayrı sayfa yüklemesiyle onaylanıyordu.
///
/// 🔑 Bu sınıfın en değerli testi "42 kayıt onaylandı" değil,
/// <see cref="BulkActionNames_DeriveSamePermissionAsSingleRecordAction"/>: toplu aksiyonlar
/// <c>Bulk…</c> diye adlandırılsaydı panelin izin türetmesi (görünmez sözleşme #19) onları
/// hiçbir moderasyon önekiyle eşleştiremez ve sessizce <c>update</c> iznine düşürürdü —
/// yani **yalnız düzenleme yetkisi olan bir moderatör toplu ONAY yapabilir hâle gelirdi**
/// ve bunu kimse fark etmezdi. Ad kuralı bu yüzden testle kilitli.
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelBulkActionTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;
    private readonly string _marker = "BulkTest-" + Guid.NewGuid().ToString("N")[..8];

    public PanelBulkActionTests(WebPanelApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.Ads.IgnoreQueryFilters().Where(a => a.Title.Contains(_marker)).ExecuteDeleteAsync();
        });
    }

    // ————————————————————————————————————————————————————————————————
    // Yapısal: ad kuralı ↔ izin türetmesi
    // ————————————————————————————————————————————————————————————————

    /// <summary>
    /// 🔴 Her toplu aksiyon, tek-kayıt karşılığıyla **aynı izni** gerektirmeli.
    /// Ad değiştirildiği anda (örn. <c>ApproveSelected</c> → <c>BulkApprove</c>) bu test
    /// kırmızıya döner.
    /// </summary>
    [Theory]
    [InlineData("ApproveSelected", "Approve")]
    [InlineData("RejectSelected", "Reject")]
    [InlineData("DeleteSelected", "Delete")]
    public void BulkActionNames_DeriveSamePermissionAsSingleRecordAction(string bulkAction, string singleAction)
    {
        var bulkPermission = InvokeActionFor(bulkAction, "POST");
        var singlePermission = InvokeActionFor(singleAction, "POST");

        bulkPermission.Should().Be(singlePermission,
            $"{bulkAction}, {singleAction} ile aynı yetkiyi istemeli — aksi hâlde toplu yol " +
            "tek-kayıt yolundan daha zayıf bir izinle açılır ve kimse fark etmez");
    }

    /// <summary>
    /// ⚠️ Reddedilen adlandırmanın kendisi: <c>Bulk…</c> öneki moderasyon olarak
    /// türetilmez. Bu test kuralın **neden** var olduğunu belgeliyor — biri "Bulk daha
    /// okunaklı" diye adı değiştirmek isterse burada ne kaybedeceğini görür.
    /// </summary>
    [Fact]
    public void BulkPrefixNaming_WouldSilentlyDowngradePermission()
    {
        InvokeActionFor("BulkApprove", "POST").Should().Be("update",
            "‘BulkApprove’ hiçbir moderasyon önekiyle eşleşmez ve update'e düşer — " +
            "bu yüzden aksiyonlar ‘…Selected’ diye adlandırıldı");

        InvokeActionFor("ApproveSelected", "POST").Should().Be("approve");
    }

    /// <summary>Toplu aksiyonların hepsi gerçekten var mı (ad değişirse view'ler sessizce 404 alır).</summary>
    [Theory]
    [InlineData("AdsAdminController", "ApproveSelected")]
    [InlineData("AdsAdminController", "RejectSelected")]
    [InlineData("AdsAdminController", "DeleteSelected")]
    [InlineData("EventsAdminController", "ApproveSelected")]
    [InlineData("EventsAdminController", "RejectSelected")]
    [InlineData("EventsAdminController", "DeleteSelected")]
    [InlineData("CampaignsAdminController", "ApproveSelected")]
    [InlineData("CampaignsAdminController", "RejectSelected")]
    [InlineData("CampaignsAdminController", "DeleteSelected")]
    [InlineData("DeathsAdminController", "ApproveSelected")]
    [InlineData("DeathsAdminController", "DeleteSelected")]
    [InlineData("AnnouncementsAdminController", "DeleteSelected")]
    public void BulkAction_ExistsAndIsHttpPost(string controllerName, string actionName)
    {
        var controllerType = typeof(WebPanel::KadirliApp.Web.Controllers.AdsAdminController).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == controllerName);

        controllerType.Should().NotBeNull($"{controllerName} bulunmalı");

        var method = controllerType!.GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance);
        method.Should().NotBeNull($"{controllerName}.{actionName} bulunmalı");
        method!.GetCustomAttribute<HttpPostAttribute>().Should().NotBeNull(
            "toplu işlem durum değiştirir — GET ile açılabilir olmamalı");
    }

    // ————————————————————————————————————————————————————————————————
    // Davranış: gerçekten toplu onaylıyor mu
    // ————————————————————————————————————————————————————————————————

    private async Task<Guid> SeedPendingAdAsync()
    {
        Guid id = Guid.Empty;
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var categoryId = await db.AdCategories.Select(c => c.Id).FirstAsync();
            var userId = await db.Users.Select(u => u.Id).FirstAsync();

            var ad = new Ad
            {
                Title = $"{_marker} toplu onay denemesi",
                Description = "Toplu işlem testi için üretildi.",
                Price = 1000,
                CategoryId = categoryId,
                UserId = userId,
                ContactPhone = "+905000000000",
                Status = "pending",
                // ⚠️ Süresi GEÇMİŞ bilerek: toplu onayın da tek-kayıt onayı gibi taze
                // pencere vermesi gerekiyor (görünmez sözleşme #25). Toplu bir SQL UPDATE
                // yazılsaydı bu kural atlanır, panel "onaylandı" der, mobil hiçbir şey göstermezdi.
                ExpiresAt = DateTime.UtcNow.AddDays(-3)
            };
            db.Ads.Add(ad);
            await db.SaveChangesAsync();
            id = ad.Id;
        });
        return id;
    }

    private async Task<Ad> ReadAdAsync(Guid id)
    {
        Ad result = null!;
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            result = await db.Ads.IgnoreQueryFilters().AsNoTracking().FirstAsync(a => a.Id == id);
        });
        return result;
    }

    [Fact]
    public async Task ApproveSelected_ApprovesEverySelectedRecord()
    {
        var first = await SeedPendingAdAsync();
        var second = await SeedPendingAdAsync();
        var client = await _factory.SuperAdminAsync();

        var response = await client.PostFormAsync("/AdsAdmin/ApproveSelected",
            new Dictionary<string, string> { ["ids"] = first.ToString() },
            tokenFromPath: "/AdsAdmin/Index");

        // Tek istekle iki kayıt göndermek için elle form gövdesi kurulur (sözlük tek değer taşır).
        var token = await client.GetAntiforgeryTokenAsync("/AdsAdmin/Index");
        response = await client.PostAsync("/AdsAdmin/ApproveSelected", new FormUrlEncodedContent(
            new[]
            {
                new KeyValuePair<string, string>("ids", first.ToString()),
                new KeyValuePair<string, string>("ids", second.ToString()),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        (await ReadAdAsync(first)).Status.Should().Be("approved");
        (await ReadAdAsync(second)).Status.Should().Be("approved");
    }

    /// <summary>
    /// 🔑 Toplu onay, tek-kayıt onayının **iş kurallarını da** çalıştırmalı: süresi geçmiş
    /// ilana taze pencere verilmeli (görünmez sözleşme #25). Bu test, toplu yolun bir gün
    /// "daha hızlı olsun" diye toplu SQL'e çevrilmesini engelliyor.
    /// </summary>
    [Fact]
    public async Task ApproveSelected_AppliesBusinessRules_NotJustStatusUpdate()
    {
        var id = await SeedPendingAdAsync(); // ExpiresAt geçmişte
        var client = await _factory.SuperAdminAsync();

        await client.PostFormAsync("/AdsAdmin/ApproveSelected",
            new Dictionary<string, string> { ["ids"] = id.ToString() },
            tokenFromPath: "/AdsAdmin/Index");

        var ad = await ReadAdAsync(id);
        ad.Status.Should().Be("approved");
        ad.ExpiresAt.Should().BeAfter(DateTime.UtcNow,
            "toplu onay da tek-kayıt onayının penceresini vermeli — yoksa panel 'onaylandı' " +
            "der, mobil hiçbir şey göstermez ve ExpireAdsJob durumu bir saat içinde geri alır");
    }

    /// <summary>Toplu işlem denetim izi bırakmalı — komut başına bir satır.</summary>
    [Fact]
    public async Task BulkApproval_LeavesOneAuditRecordPerItem()
    {
        var first = await SeedPendingAdAsync();
        var second = await SeedPendingAdAsync();
        var client = await _factory.SuperAdminAsync();

        var token = await client.GetAntiforgeryTokenAsync("/AdsAdmin/Index");
        await client.PostAsync("/AdsAdmin/ApproveSelected", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("ids", first.ToString()),
            new KeyValuePair<string, string>("ids", second.ToString()),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        }));

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var count = await db.AuditLogs
                .CountAsync(a => a.AffectedId == first || a.AffectedId == second);

            count.Should().BeGreaterThanOrEqualTo(2,
                "her kayıt kendi komutundan geçmeli — toplu SQL yazılsaydı denetim izi hiç düşmezdi");
        });
    }

    /// <summary>Boş seçim sessizce bir şey yapmamalı, kullanıcıya söylemeli.</summary>
    [Fact]
    public async Task EmptySelection_ReportsNothingSelected()
    {
        var client = await _factory.SuperAdminAsync();

        var response = await client.PostFormAsync("/AdsAdmin/ApproveSelected",
            new Dictionary<string, string>(), tokenFromPath: "/AdsAdmin/Index");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var followUp = await client.GetAsync("/AdsAdmin/Index");
        (await followUp.ReadDecodedBodyAsync()).Should().Contain("Hiçbir kayıt seçilmedi");
    }

    /// <summary>
    /// ⚠️ Bilinmeyen kimlik partiyi **durdurmamalı**: 41 kaydı 1 tanesi yüzünden geri
    /// çevirmek, yöneticiyi "hangisiydi?" diye aramaya bırakır.
    /// </summary>
    [Fact]
    public async Task UnknownId_DoesNotAbortTheBatch()
    {
        var valid = await SeedPendingAdAsync();
        var client = await _factory.SuperAdminAsync();

        var token = await client.GetAntiforgeryTokenAsync("/AdsAdmin/Index");
        await client.PostAsync("/AdsAdmin/ApproveSelected", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("ids", Guid.NewGuid().ToString()), // yok
            new KeyValuePair<string, string>("ids", valid.ToString()),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        }));

        (await ReadAdAsync(valid)).Status.Should().Be("approved",
            "geçerli kayıt, geçersiz olanın yanında işlenmeye devam etmeli");
    }

    /// <summary>Liste sayfası toplu işlem arayüzünü gerçekten çiziyor mu.</summary>
    [Theory]
    [InlineData("/AdsAdmin/Index")]
    [InlineData("/EventsAdmin/Index")]
    [InlineData("/CampaignsAdmin/Index")]
    [InlineData("/DeathsAdmin/Index")]
    [InlineData("/AnnouncementsAdmin/Index")]
    public async Task ListPage_RendersBulkToolbarAndCheckboxes(string path)
    {
        var client = await _factory.SuperAdminAsync();
        var body = await (await client.GetAsync(path)).ReadDecodedBodyAsync();

        body.Should().Contain("data-bulk-scope", "toplu işlem kapsamı çizilmeli");
        body.Should().Contain("data-bulk-select-all", "başlıkta 'tümünü seç' kutusu olmalı");
        body.Should().Contain("data-bulk-submit", "toplu eylem butonu olmalı");
        body.Should().Contain("Toplu işlem için satır seçin",
            "hiçbir şey seçilmemişken çubuk ne yapılacağını söylemeli");
    }

    /// <summary>
    /// ⚠️ Toplu işlem kutuları, satırlardaki tek-kayıt formlarıyla **iç içe geçmemeli**.
    /// Kutular <c>form="…"</c> ile dışarıdaki hedef forma bağlanır; bağlanmazsa seçim
    /// POST'a hiç girmez ve panel her seferinde "Hiçbir kayıt seçilmedi" der.
    /// </summary>
    [Fact]
    public async Task BulkCheckboxes_AreBoundToTargetFormByAttribute()
    {
        var client = await _factory.SuperAdminAsync();
        var body = await (await client.GetAsync("/AdsAdmin/Index")).ReadDecodedBodyAsync();

        body.Should().Contain("id=\"bulk-adsadmin\"", "hedef form kimliğiyle çizilmeli");
        body.Should().Contain("form=\"bulk-adsadmin\"",
            "kutular ve butonlar hedef forma öznitelikle bağlanmalı (iç içe form olmaması için)");
    }

    private static string InvokeActionFor(string actionName, string httpMethod)
    {
        var method = typeof(PanelPermissionFilter).GetMethod(
            "ActionFor", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, new object[] { actionName, httpMethod })!;
    }
}
