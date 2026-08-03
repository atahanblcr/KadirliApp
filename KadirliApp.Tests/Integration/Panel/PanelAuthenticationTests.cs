extern alias WebPanel;

using System.Net;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.15b — **panelin kapısı.** Buradaki iddialar tek bir soruyu cevaplıyor:
/// *"Oturum açmamış biri panelin herhangi bir sayfasını açabilir mi?"*
///
/// Denetim bilinçli olarak **yapısal**: elle yazılmış bir controller listesi çürür
/// (yeni controller eklenir, listeye yazılmaz, test yeşil kalır ve kimse fark etmez).
/// Bunun yerine assembly'deki **tüm** panel controller'ları taranır — yeni controller
/// kendiliğinden kapsanır. (Aynı yaklaşım API tarafında
/// <c>EndpointAuthorizationSweepTests</c> ile 11.14'te uygulanmıştı.)
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelAuthenticationTests
{
    private readonly WebPanelApplicationFactory _factory;

    public PanelAuthenticationTests(WebPanelApplicationFactory factory) => _factory = factory;

    /// <summary>Kimlik doğrulaması istemesi BEKLENMEYEN controller'lar — gerekçeleri aşağıda.</summary>
    private static readonly HashSet<string> AnonymousControllers = new(StringComparer.Ordinal)
    {
        "AccountController", // giriş sayfasının kendisi
        "HomeController"     // hata sayfası (/Home/Error) + gizlilik metni
    };

    private static IReadOnlyList<Type> PanelControllers() =>
        typeof(WebPanel::Program).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true }
                        && typeof(Controller).IsAssignableFrom(t)
                        // Derleyici üretimi durum makineleri de GetTypes()'tan döner (11.14 dersi)
                        && t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

    [Fact]
    public void PanelControllerInventory_IsDiscoverable()
    {
        PanelControllers().Should().HaveCountGreaterThan(15,
            "panel 20 civarı controller barındırıyor; sayı çökerse tarama hiçbir şeyi denetlemiyor demektir");
    }

    /// <summary>
    /// Her yönetim controller'ı sınıf seviyesinde <c>[Authorize]</c> taşımalı. Aksiyon
    /// seviyesine bırakılırsa **yeni eklenen bir aksiyon korumasız kalır** ve bunu kimse
    /// görmez — panelin en sessiz sızıntı yolu budur.
    /// </summary>
    [Fact]
    public void EveryAdminPanelController_RequiresAuthorizationAtClassLevel()
    {
        var unprotected = PanelControllers()
            .Where(t => !AnonymousControllers.Contains(t.Name))
            .Where(t => t.GetCustomAttribute<AuthorizeAttribute>(inherit: true) is null)
            .Select(t => t.Name)
            .ToList();

        unprotected.Should().BeEmpty(
            "sınıf seviyesinde [Authorize] taşımayan panel controller'ları: {0}. " +
            "Aksiyon seviyesine bırakmayın — yeni aksiyon korumasız doğar.",
            string.Join(", ", unprotected));
    }

    /// <summary>
    /// Anonim listesine bilerek eklenmedikçe hiçbir controller <c>[AllowAnonymous]</c>
    /// taşımamalı. Bu test "kapıyı geçici olarak açıp kapatmayı unutma" hatasını yakalar.
    /// </summary>
    [Fact]
    public void NoAdminPanelController_OptsOutOfAuthorization()
    {
        var optedOut = PanelControllers()
            .Where(t => !AnonymousControllers.Contains(t.Name))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
                .Select(m => $"{t.Name}.{m.Name}"))
            .ToList();

        optedOut.Should().BeEmpty("panelde [AllowAnonymous] taşıyan aksiyonlar: {0}", string.Join(", ", optedOut));
    }

    /// <summary>
    /// Yapısal iddia yetmez — cookie şeması gerçekten devrede mi? Oturumsuz istek
    /// <b>302 → /account/login</b> almalı; 200 alırsa sayfa sızıyordur, 404 alırsa
    /// rota kaybolmuştur.
    /// </summary>
    [Theory]
    [InlineData("/Dashboard/Index")]
    [InlineData("/AdsAdmin/Index")]
    [InlineData("/AdCategoriesAdmin/Index")]
    [InlineData("/AnnouncementsAdmin/Index")]
    [InlineData("/BusinessesAdmin/Index")]
    [InlineData("/CampaignsAdmin/Index")]
    [InlineData("/ComplaintsAdmin/Index")]
    [InlineData("/DeathsAdmin/Index")]
    [InlineData("/EventsAdmin/Index")]
    [InlineData("/GuideAdmin/Index")]
    [InlineData("/LookupsAdmin/Index")]
    [InlineData("/PharmaciesAdmin/Index")]
    [InlineData("/PlacesAdmin/Index")]
    [InlineData("/PowerOutagesAdmin/Index")]
    [InlineData("/StaffAdmin/Index")]
    [InlineData("/TaxiAdmin/Index")]
    [InlineData("/TransportAdmin/Index")]
    [InlineData("/UsersAdmin/Index")]
    public async Task AnonymousRequest_IsRedirectedToLogin(string path)
    {
        var client = _factory.CreatePanelClient();

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect, "{0} oturumsuz açılabiliyor", path);

        var location = response.Headers.Location!.ToString();
        location.Should().Contain("/account/login", "yönlendirme giriş sayfasına gitmeli");
        // ReturnUrl korunmazsa yönetici girişten sonra Dashboard'a düşer ve aradığı
        // sayfayı elle bulmak zorunda kalır — küçük ama her gün yaşanan bir kayıp.
        location.Should().Contain(Uri.EscapeDataString(path),
            "giriş sonrası kullanıcı istediği sayfaya dönebilmeli (ReturnUrl)");
    }

    [Fact]
    public async Task SuperAdmin_CanOpenTheDashboard()
    {
        var client = await _factory.SuperAdminAsync();

        var response = await client.GetAsync("/Dashboard/Index");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WrongPassword_DoesNotIssueASession()
    {
        var client = _factory.CreatePanelClient();
        var token = await client.GetAntiforgeryTokenAsync("/account/login");

        var login = await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = "admin",
            ["password"] = "kesinlikle-yanlis",
            ["__RequestVerificationToken"] = token
        }));

        login.StatusCode.Should().Be(HttpStatusCode.OK, "hatalı giriş formu geri döndürür, yönlendirmez");
        (await login.ReadDecodedBodyAsync()).Should().Contain("şifre hatalı",
            "kullanıcı neden giremediğini görmeli");

        var afterwards = await client.GetAsync("/Dashboard/Index");
        afterwards.StatusCode.Should().Be(HttpStatusCode.Redirect, "başarısız giriş oturum açmamalı");
    }

    /// <summary>
    /// Antiforgery koruması global (<c>AutoValidateAntiforgeryToken</c>). Token'sız POST
    /// **400** almalı — aksi hâlde panel CSRF'e açıktır ve yöneticinin tarayıcısındaki
    /// oturum üçüncü bir siteden kullanılabilir.
    /// </summary>
    [Fact]
    public async Task PostWithoutAntiforgeryToken_IsRejected()
    {
        var client = await _factory.LoginAsSuperAdminAsync();

        var response = await client.PostAsync("/LookupsAdmin/NeighborhoodCreate",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["name"] = "CSRF Mahallesi" }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "antiforgery token'sız POST kabul edilirse panel CSRF'e açıktır");
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var client = await _factory.LoginAsSuperAdminAsync();
        (await client.GetAsync("/Dashboard/Index")).StatusCode.Should().Be(HttpStatusCode.OK);

        await client.GetAsync("/account/logout");

        (await client.GetAsync("/Dashboard/Index")).StatusCode.Should().Be(HttpStatusCode.Redirect,
            "çıkıştan sonra panel yeniden kapanmalı");
    }
}
