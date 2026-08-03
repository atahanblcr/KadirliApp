using System.Net;
using FluentAssertions;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.15b — **panelin her sayfası açılıyor mu?**
///
/// Kulağa basit geliyor ama panelde bir sayfanın 500 vermesi bugüne kadar ancak birinin
/// o sayfayı elle açmasıyla anlaşılıyordu. Razor görünümleri **derleme zamanında**
/// denetlenmez: bir ViewBag alanı silinince ya da DTO'nun bir alanı yeniden adlandırılınca
/// hata **yalnızca çalışma zamanında** ortaya çıkar. Yönetici bunu içerik girmeye
/// çalışırken keşfeder.
///
/// Bu yüzden buradaki liste tek tek yazılmıştır (tarama değil): her satır, gerçekten
/// **render edilen** bir sayfadır. Bir sayfa kırılırsa hangisi olduğu doğrudan görünür.
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelPagesSmokeTests
{
    private readonly WebPanelApplicationFactory _factory;

    public PanelPagesSmokeTests(WebPanelApplicationFactory factory) => _factory = factory;

    public static TheoryData<string> ListPages() => new()
    {
        "/Dashboard/Index",
        "/AuditLogsAdmin/Index",   // Faz 11.17
        "/TrashAdmin/Index",       // Faz 11.17
        "/AdsAdmin/Index",
        "/AdCategoriesAdmin/Index",
        "/AnnouncementsAdmin/Index",
        "/BusinessesAdmin/Index",
        "/CampaignsAdmin/Index",
        "/ComplaintsAdmin/Index",
        "/DeathsAdmin/Index",
        "/EventsAdmin/Index",
        "/EventsAdmin/Calendar",
        "/GuideAdmin/Index",
        "/GuideAdmin/Categories",
        "/LookupsAdmin/Index",
        "/PharmaciesAdmin/Index",
        "/PharmaciesAdmin/Schedule",
        "/PlacesAdmin/Index",
        "/PowerOutagesAdmin/Index",
        "/StaffAdmin/Index",
        "/TaxiAdmin/Index",
        "/TransportAdmin/Index",
        // Faz 11.17: şehirlerarası taraf artık panelden yönetiliyor (menüde ayrı satır değil,
        // Ulaşım ekranının ikinci sekmesi — bu yüzden SidebarLinks testi onu görmez).
        "/TransportAdmin/Intercity",
        "/UsersAdmin/Index"
    };

    public static TheoryData<string> CreateForms() => new()
    {
        "/AdsAdmin/Create",
        "/AdCategoriesAdmin/Create",
        "/AnnouncementsAdmin/Create",
        "/BusinessesAdmin/Create",
        "/CampaignsAdmin/Create",
        "/DeathsAdmin/Create",
        "/EventsAdmin/Create",
        "/GuideAdmin/Create",
        "/GuideAdmin/CategoryCreate",
        "/PharmaciesAdmin/Create",
        "/PlacesAdmin/Create",
        "/PowerOutagesAdmin/Create",
        "/StaffAdmin/Create",
        "/TaxiAdmin/Create",
        "/TransportAdmin/Create",
        "/TransportAdmin/IntercityCreate",
        "/UsersAdmin/Create"
    };

    [Theory]
    [MemberData(nameof(ListPages))]
    public async Task ListPage_RendersForSuperAdmin(string path)
    {
        var client = await _factory.SuperAdminAsync();

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "{0} açılmıyor", path);
        var html = await response.ReadDecodedBodyAsync();
        html.Should().Contain("<html", "sayfa gerçekten HTML döndürmeli, boş yanıt değil");
    }

    /// <summary>
    /// Ekleme formları listelerden daha kırılgandır: çoğu lookup yükler (mahalle, kategori,
    /// mezarlık…). Bir lookup sorgusu bozulursa form açılmaz ve **o modüle içerik
    /// girilemez hâle gelir** — panelin en ağır arıza biçimi.
    /// </summary>
    [Theory]
    [MemberData(nameof(CreateForms))]
    public async Task CreateForm_RendersForSuperAdmin(string path)
    {
        var client = await _factory.SuperAdminAsync();

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "{0} formu açılmıyor", path);
        (await response.Content.ReadAsStringAsync()).Should().Contain("__RequestVerificationToken",
            "form antiforgery token'ı basmıyorsa gönderim 400 alır — form görünse de işe yaramaz");
    }

    /// <summary>
    /// Panelin sol menüsü tek gezinme aracı. Bir bağlantı 404'e giderse yönetici o modülü
    /// bulamaz. Mobildeki "işlevsiz buton yok" kuralının panel karşılığı.
    /// </summary>
    [Fact]
    public async Task SidebarLinks_AllResolveToRealPages()
    {
        var client = await _factory.SuperAdminAsync();
        var html = await (await client.GetAsync("/Dashboard/Index")).Content.ReadAsStringAsync();

        // Menü bağlantıları tag helper'dan üretiliyor ve çoğu aksiyonsuz ("/AdsAdmin"),
        // varsayılan rota Index'e düşürüyor — desen ikisini de yakalamalı.
        var links = System.Text.RegularExpressions.Regex.Matches(html, "href=\"(/[A-Za-z]+(?:/[A-Za-z]+)?)\"")
            .Select(m => m.Groups[1].Value)
            .Where(h => !h.StartsWith("/Account", StringComparison.OrdinalIgnoreCase) && h != "/")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        links.Should().HaveCountGreaterThan(10, "menüden bağlantılar okunamadıysa test hiçbir şey denetlemiyor");

        var broken = new List<string>();
        foreach (var link in links)
        {
            var status = (await client.GetAsync(link)).StatusCode;
            if (status is not (HttpStatusCode.OK or HttpStatusCode.Redirect))
                broken.Add($"{link} → {(int)status}");
        }

        broken.Should().BeEmpty("panel menüsünde açılmayan bağlantılar: {0}", string.Join(", ", broken));
    }
}
