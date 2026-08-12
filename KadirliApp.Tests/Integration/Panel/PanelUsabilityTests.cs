extern alias WebPanel;

using System.Net;
using FluentAssertions;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.15c — canlı panel gezisinde bulunan **kullanılabilirlik hatalarının** kilidi.
///
/// 11.15b panelin *yetkisini* test etti; bu dosya panelin *kullanılabilirliğini* test eder.
/// Buradaki her test, gerçek bir yöneticinin gerçekten karşılaştığı bir soruna karşılık
/// gelir — hiçbiri "olsa iyi olur" değil.
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelUsabilityTests
{
    private readonly WebPanelApplicationFactory _factory;

    public PanelUsabilityTests(WebPanelApplicationFactory factory) => _factory = factory;

    // ── A1: dar ekranda gezinme ────────────────────────────────────────────────

    /// <summary>
    /// 🔴 **Dar ekranda panelde HİÇ menü yoktu.** <c>_Layout.cshtml</c>'deki hamburger
    /// butonunda <c>id</c>/<c>onclick</c>/<c>data-*</c> yoktu ve panelde onu bağlayan JS de
    /// yoktu; kenar çubuğu ise <c>hidden lg:flex</c>. Yani &lt;1024 px'de modüller arası tek
    /// geçiş yolu adres çubuğuna URL yazmaktı — mobildeki "işlevsiz buton yok" kuralının
    /// panel ihlali.
    ///
    /// Menü artık JS'siz <c>&lt;details&gt;</c> ile açılıyor; bu sayede "gerçekten açılıyor mu"
    /// sorusu sunucu tarafı render'ıyla denetlenebiliyor: bağlantılar işaretlemede VAR.
    /// </summary>
    [Fact]
    public async Task NarrowScreen_HasWorkingNavigationMenu()
    {
        var client = await _factory.SuperAdminAsync();
        var html = await (await client.GetAsync("/Dashboard/Index")).ReadDecodedBodyAsync();

        html.Should().Contain("id=\"panel-mobile-menu\"",
            "dar ekran menüsü işaretlemede olmalı — hamburger butonu yine bir kabuk olmamalı");

        // Menü kabı `lg:hidden` bir kapsayıcıda, kenar çubuğu ise `hidden lg:flex`.
        // İkisinin de gerçek modül bağlantısı taşıdığını doğruluyoruz: dar ekran menüsü
        // yalnız bir ikon olsaydı bu iddia geçmezdi.
        var menuStart = html.IndexOf("id=\"panel-mobile-menu\"", StringComparison.Ordinal);
        menuStart.Should().BeGreaterThan(0);
        var menuEnd = html.IndexOf("</nav>", menuStart, StringComparison.Ordinal);
        menuEnd.Should().BeGreaterThan(menuStart, "dar ekran menüsünün <nav> kabı kapanmalı");
        var menuHtml = html[menuStart..menuEnd];

        menuHtml.Should().Contain("/AdsAdmin", "dar ekran menüsünde modül bağlantıları olmalı");
        menuHtml.Should().Contain("/Account/Logout",
            "dar ekranda ÇIKIŞ YAPMANIN da bir yolu olmalı — 11.15c öncesi yoktu");
    }

    /// <summary>Ekran okuyucu etiketi de Türkçe olmalı (11.15c öncesi "Open sidebar" idi).</summary>
    [Fact]
    public async Task NarrowScreenMenu_HasTurkishAccessibleLabel()
    {
        var client = await _factory.SuperAdminAsync();
        var html = await (await client.GetAsync("/Dashboard/Index")).ReadDecodedBodyAsync();

        html.Should().NotContain("Open sidebar", "panelde İngilizce erişilebilirlik etiketi kalmamalı");
        html.Should().Contain("Menüyü aç");
    }

    // ── A3: ham İngilizce durum sızıntısı ──────────────────────────────────────

    /// <summary>
    /// 🔴 **Ham İngilizce durum/rol rozeti** (CLAUDE.md Değişmez Kural #6 ihlali).
    ///
    /// Bu test kaynağı değil, **render edilmiş sayfayı** tarar: her listeye o listenin
    /// "unutulan" durumundaki bir kayıt konur ve sayfada ham değerin görünmediği,
    /// Türkçe karşılığının göründüğü doğrulanır. Kaynak taraması, durumun hangi dalda
    /// basıldığını bilemezdi.
    /// </summary>
    [Fact]
    public async Task AdsList_ExpiredAd_ShowsTurkishLabel_NotRawEnglish()
    {
        var adId = await SeedExpiredAdAsync();

        var client = await _factory.SuperAdminAsync();
        var html = await (await client.GetAsync("/AdsAdmin/Index?status=expired")).ReadDecodedBodyAsync();

        html.Should().Contain("Süresi Doldu", "süresi dolmuş ilan Türkçe etiketle görünmeli");
        html.Should().NotContain(">expired<",
            "ham İngilizce durum değeri yöneticiye gösterilmemeli");

        await DeleteAdAsync(adId);
    }

    [Fact]
    public async Task UsersList_ShowsTurkishRoleLabels_NotEnumNames()
    {
        var client = await _factory.SuperAdminAsync();

        // 🐛 Arama ŞART. Süzgeçsiz istek listenin İLK SAYFASINI veriyor (20 satır) ve
        // seed'deki süper admin orada olmak zorunda değil: testler kullanıcı satırı
        // bıraktıkça o satır aşağı kayıyor. 12.15b'de tam bu oldu — dört yeni test
        // kullanıcısı eklendi ve bu test, kendisiyle ilgisiz bir sebeple kırmızıya döndü.
        // İddia "listede süper admin var" değil, "rol TÜRKÇE basılıyor"; arama onu
        // satır sayısından bağımsız hâle getiriyor.
        var html = await (await client.GetAsync("/UsersAdmin/Index?search=admin")).ReadDecodedBodyAsync();

        html.Should().Contain("Süper Yönetici");
        html.Should().NotContain(">SuperAdmin<", "enum adı ham basılmamalı");
    }

    // ── A2: para biçimi ────────────────────────────────────────────────────────

    /// <summary>🐛 Canlıda fiyatlar <c>¤750,000.00</c> görünüyordu (InvariantCulture + "C2").</summary>
    [Fact]
    public async Task AdsList_ShowsTurkishLiraFormat()
    {
        var adId = await SeedExpiredAdAsync(price: 750000m);

        var client = await _factory.SuperAdminAsync();
        var html = await (await client.GetAsync("/AdsAdmin/Index?status=expired")).ReadDecodedBodyAsync();

        html.Should().Contain("₺750.000,00");
        html.Should().NotContain("¤", "jenerik para birimi simgesi panelde görünmemeli");

        await DeleteAdAsync(adId);
    }

    // ── A9: gövdesiz 404 ───────────────────────────────────────────────────────

    /// <summary>
    /// 🐛 Panelde var olmayan bir adres <b>404 + 0 bayt</b>, yani bembeyaz sayfa döndürüyordu.
    /// Durum kodu korunmalı ama gövde markalı ve Türkçe olmalı.
    /// </summary>
    [Fact]
    public async Task UnknownPath_Returns404_WithBrandedTurkishBody()
    {
        var client = await _factory.SuperAdminAsync();

        var response = await client.GetAsync("/BuBirSayfaDegil");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "özgün durum kodu korunmalı");

        var html = await response.ReadDecodedBodyAsync();
        html.Should().NotBeEmpty("404 gövdesi 0 bayt olmamalı");
        html.Should().Contain("Sayfa bulunamadı");
        html.Should().Contain("Panele dön", "kullanıcıya panele dönüş yolu verilmeli");
        html.Should().Contain("/BuBirSayfaDegil", "hangi adresin bulunamadığı yazılmalı");
    }

    // ── B: onay kuyruğu (hesaplanıp çöpe atılan PendingBreakdown) ──────────────

    /// <summary>
    /// <c>PendingBreakdown</c> 10.10'dan beri hesaplanıyor ama <c>Web</c>/<c>Api</c> içinde
    /// hiç okunmuyordu → "Bekleyen Onaylar" tıklanamayan tek bir sayıydı. Artık her
    /// modül satırı ilgili listenin bekleyen filtresine gidiyor.
    /// </summary>
    [Fact]
    public async Task Dashboard_ShowsPendingQueue_LinkedToModuleFilters()
    {
        var adId = await SeedPendingAdAsync();

        // ⚠️ Dashboard istatistikleri Redis'te 60 sn cache'li ve bu grubun bilinçli olarak
        // invalidator'ı yok (CacheContractTests'te açık istisna). Cache temizlenmezse
        // sayfa, testin az önce eklediği ilanı görmeyen BAYAT bir sonuç basar.
        await InvalidateDashboardCacheAsync();

        var client = await _factory.SuperAdminAsync();
        var html = await (await client.GetAsync("/Dashboard/Index")).ReadDecodedBodyAsync();

        html.Should().Contain("Onay Kuyruğu");
        html.Should().Contain("/AdsAdmin?status=pending",
            "kuyruk satırı ilgili modülün bekleyen filtresine gitmeli");

        await DeleteAdAsync(adId);
        await InvalidateDashboardCacheAsync();
    }

    private Task InvalidateDashboardCacheAsync() => _factory.WithScopeAsync(async sp =>
        await sp.GetRequiredService<KadirliApp.Application.Common.Interfaces.ICacheService>()
            .InvalidateGroupsAsync(new[] { KadirliApp.Application.Common.Caching.CacheGroups.Dashboard }));

    /// <summary>Kuyruk satırının gittiği adres gerçekten çalışmalı (işlevsiz buton yok).</summary>
    [Fact]
    public async Task AdsList_StatusFilter_Works()
    {
        var pendingId = await SeedPendingAdAsync(title: "Kuyruk testi bekleyen ilan");
        var expiredId = await SeedExpiredAdAsync(title: "Kuyruk testi suresi dolmus ilan");

        var client = await _factory.SuperAdminAsync();
        var html = await (await client.GetAsync("/AdsAdmin/Index?status=pending")).ReadDecodedBodyAsync();

        html.Should().Contain("Kuyruk testi bekleyen ilan");
        html.Should().NotContain("Kuyruk testi suresi dolmus ilan",
            "status=pending süzgeci diğer durumları elemeli");

        await DeleteAdAsync(pendingId);
        await DeleteAdAsync(expiredId);
    }

    // ── Yardımcılar ────────────────────────────────────────────────────────────

    private Task<Guid> SeedExpiredAdAsync(decimal price = 1234.5m, string title = "Suresi dolmus test ilani")
        => SeedAdAsync(title, price, "expired", DateTime.UtcNow.AddDays(-1));

    private Task<Guid> SeedPendingAdAsync(decimal price = 100m, string title = "Bekleyen test ilani")
        => SeedAdAsync(title, price, "pending", DateTime.UtcNow.AddDays(30));

    private async Task<Guid> SeedAdAsync(string title, decimal price, string status, DateTime expiresAt)
    {
        Guid id = Guid.Empty;
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstAsync();
            var category = await db.AdCategories.FirstAsync();

            var ad = new Ad
            {
                UserId = user.Id,
                CategoryId = category.Id,
                Title = title,
                Description = "11.15c testi tarafından üretildi.",
                Price = price,
                ContactPhone = "+905550000000",
                Status = status,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };
            db.Ads.Add(ad);
            await db.SaveChangesAsync();
            id = ad.Id;
        });
        return id;
    }

    private async Task DeleteAdAsync(Guid id) =>
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.Ads.Where(a => a.Id == id).ExecuteDeleteAsync();
        });
}
