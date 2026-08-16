extern alias WebPanel;

using System.Net;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

    /// <summary>
    /// Baştan sona anonim olan controller'lar. <b>Tek bir ad var ve olmalı</b>: giriş
    /// sayfası. (Oturum açmak için oturum istenemez.)
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Faz 12.20a — bu liste 2'den 1'e indi ve indiği için bir delik kapandı.</b>
    /// 16 Ağustos denetimi buradaki ikinci adı (<c>HomeController</c>) bir bulgu olarak
    /// açtı: gerekçesi yalnız <c>Error</c>/<c>StatusCode</c>'u karşılıyordu ama muafiyet
    /// <b>controller granülaritesinde</b> olduğu için dört aksiyonu birden örtüyordu —
    /// iskeleden kalma <c>Index</c> ve <c>Privacy</c> kimliksiz 200 dönüyordu ve
    /// <b>hiçbir test kırılmıyordu.</b> Daha kötüsü: o sınıfa yarın eklenecek beşinci bir
    /// aksiyon da sessizce anonim doğacaktı.
    ///
    /// 🔑 Faz A'nın sorusuna (<i>"kapsam dizinden mi, tipten mi, elden mi?"</i>) bu bulgu
    /// bir soru daha ekledi: <b><i>"muafiyet hangi granülaritede?"</i></b> Kapsam burada
    /// baştan beri doğruydu — assembly'den türetiliyor; delik <b>muafiyetteydi</b>.
    /// </remarks>
    private static readonly HashSet<string> AnonymousControllers = new(StringComparer.Ordinal)
    {
        "AccountController" // giriş sayfasının kendisi — tamamı anonim değil, kapısı anonim
    };

    /// <summary>
    /// <c>[AllowAnonymous]</c> taşıması BEKLENEN tekil aksiyonlar — <b>controller değil,
    /// aksiyon</b>. Her satır bir gerekçe taşımak zorunda.
    /// </summary>
    /// <remarks>
    /// Hata sayfaları anonim olmak <b>zorunda</b>: <c>UseExceptionHandler("/Home/Error")</c>
    /// ve <c>UseStatusCodePagesWithReExecute("/Home/StatusCode")</c> boru hattını yeniden
    /// çalıştırır — kapı kapalı olsaydı 500 alan yönetici hata sayfası yerine giriş
    /// ekranına atılırdı ve <b>gerçek hata hiçbir yerde görünmezdi</b>.
    /// </remarks>
    private static readonly HashSet<string> AnonymousActions = new(StringComparer.Ordinal)
    {
        "HomeController.Error",      // UseExceptionHandler bu adresi yeniden çalıştırır
        "HomeController.StatusCode"  // UseStatusCodePagesWithReExecute — 404/403 markalı kalsın
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
    /// Bilerek muaf tutulmadıkça hiçbir panel aksiyonu <c>[AllowAnonymous]</c> taşımamalı.
    /// Bu test "kapıyı geçici olarak açıp kapatmayı unutma" hatasını yakalar.
    /// </summary>
    /// <remarks>
    /// 🔑 <b>12.20a'dan beri muafiyet AKSİYON granülaritesinde</b> (<c>AnonymousActions</c>).
    /// Eskiden controller adına bakıyordu; o yüzden muaf bir controller'a eklenen <b>her</b>
    /// yeni aksiyon kendiliğinden muaf oluyordu. Bugün <c>HomeController</c>'a
    /// <c>[AllowAnonymous]</c>'lu üçüncü bir aksiyon eklemek bu testi <b>kırmızıya
    /// döndürür</b> — bozma turunda ölçüldü.
    ///
    /// ⚠️ Bu test <c>FallbackPolicy</c>'nin (12.20a) <b>yerine geçmez, tamamlar</b>:
    /// fallback "unutulan aksiyon kapalı doğsun" der, bu test "açıkça açılan aksiyon
    /// gerekçeli olsun" der. İkincisi olmadan biri tek satırlık bir öznitelikle
    /// fallback'i delip geçebilirdi.
    /// </remarks>
    [Fact]
    public void NoAdminPanelController_OptsOutOfAuthorization()
    {
        var optedOut = PanelControllers()
            .Where(t => !AnonymousControllers.Contains(t.Name))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
                .Select(m => $"{t.Name}.{m.Name}"))
            .Where(name => !AnonymousActions.Contains(name))
            .ToList();

        optedOut.Should().BeEmpty(
            "panelde gerekçesiz [AllowAnonymous] taşıyan aksiyonlar: {0}. " +
            "Gerçekten anonim olmalıysa AnonymousActions'a GEREKÇESİYLE yazın.",
            string.Join(", ", optedOut));
    }

    /// <summary>
    /// 🔴 <b>Faz 12.20a — kapının YÖNÜ.</b> Panelde <c>FallbackPolicy</c> kurulu olmak
    /// zorunda: öznitelik taşımayan bir aksiyon <b>anonim doğmamalı, kapalı doğmalı</b>.
    /// </summary>
    /// <remarks>
    /// 16 Ağustos denetiminin B1 bulgusu tam olarak bu satırın yokluğundan doğdu.
    /// Yapısal testler ("sınıfta <c>[Authorize]</c> var mı") bir <b>tarama</b>dır ve
    /// taramanın muafiyeti çürüyebilir — nitekim çürüdü. Fallback policy ise bir tarama
    /// değil, <b>framework davranışı</b>: §7 madde 53'ün dersi
    /// (<i>"korumayı taramanın erişemeyeceği yere taşı"</i>) burada uygulanıyor.
    ///
    /// ⚠️ İddia bilinçli olarak <b>çalışan uygulamanın servislerinden</b> okunuyor,
    /// <c>Program.cs</c>'in kaynağı taranmıyor: politikayı yazıp kaydetmeyi unutmak
    /// mümkündür ve o durumda kaynak taraması yeşil kalırdı (§7 madde 51'in
    /// "kaynak ≠ yanıt" ayrımının aynısı).
    /// </remarks>
    [Fact]
    public void ThePanel_FailsClosed_WhenAnActionForgetsItsAuthorizeAttribute()
    {
        var options = _factory.Services
            .GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        options.FallbackPolicy.Should().NotBeNull(
            "FallbackPolicy yoksa öznitelik taşımayan her panel aksiyonu ANONİM doğar — " +
            "B1 (HomeController) tam olarak böyle yıllarca kimliksiz açılabildi");

        options.FallbackPolicy!.Requirements
            .Should().ContainSingle(r => r is DenyAnonymousAuthorizationRequirement,
                "fallback politikası kimlik doğrulanmış kullanıcı istemeli; " +
                "boş bir politika kurulmuş olsaydı kapı VAR görünür, YOK olurdu");
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
    [InlineData("/TransportAdmin/Intercity")]
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

    /// <summary>
    /// 🔴 <b>Faz 12.20a'nın davranış ayağı.</b> İskele sayfaları gerçekten gitti mi?
    /// </summary>
    /// <remarks>
    /// <para>
    /// Yapısal test bunu <b>göremez</b>: aksiyonu silmeyi unutup yalnız
    /// <c>[Authorize]</c> eklemek de bütün yapısal iddiaları yeşil bırakırdı — ama
    /// <c>/Home/Privacy</c> adresi <b>ayakta kalır</b> ve orada hâlâ İngilizce bir yer
    /// tutucu gizlilik metni dururdu. Bulgunun asıl rahatsız edici kısmı buydu.
    /// </para>
    /// <para>
    /// ⚠️ İddia bilinçli olarak <b>oturumlu</b> istemciyle kuruluyor. Sebebi ölçüldü:
    /// <c>FallbackPolicy</c> (12.20a) <b>hiçbir uca eşleşmeyen</b> isteklere de uygulanır,
    /// yani oturumsuz bir ziyaretçi silinmiş bir adres için 404 değil <b>302 → giriş</b>
    /// alır. Anonim yanıta bakan bir iddia "aksiyon silinmiş" ile "aksiyon duruyor ama
    /// korumalı"yı <b>ayırt edemezdi</b> — ikisi de 302'dir. Silinmişliğin tek dürüst
    /// kanıtı, <b>girmeye hakkı olan</b> birinin de bulamamasıdır.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData("/Home/Index")]
    [InlineData("/Home/Privacy")]
    public async Task TheScaffoldingPages_AreGone(string path)
    {
        var client = await _factory.SuperAdminAsync();

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "{0} `dotnet new mvc` iskelesinden kalmıştı ve 12.20a'da silindi; " +
            "super_admin bile bulamamalı — 200 dönüyorsa aksiyon hâlâ duruyor", path);
    }

    /// <summary>
    /// Ve aynı adres <b>oturumsuz</b> ziyaretçiye 200 dönmemeli. 12.20a öncesinde tam
    /// olarak bunu yapıyordu (İngilizce iskele metniyle birlikte).
    /// </summary>
    [Theory]
    [InlineData("/Home/Index")]
    [InlineData("/Home/Privacy")]
    public async Task TheScaffoldingPages_AreNotServedToAnonymousVisitors(string path)
    {
        var client = _factory.CreatePanelClient();

        var response = await client.GetAsync(path);

        response.StatusCode.Should().NotBe(HttpStatusCode.OK,
            "{0} 12.20a öncesinde kimliksiz 200 dönüyordu — panelin kabuğunu, varlık " +
            "adreslerini ve ortam rozetini oturumsuz bir ziyaretçiye gösteriyordu", path);
    }

    /// <summary>
    /// Ters yön — <b>ve bu yön olmadan yukarıdaki iddia zayıftır</b> (§7 madde 68'in dersi):
    /// "hiçbir /Home adresi açılmıyor" gerçeklemesi de yeşil kalırdı. Hata sayfaları
    /// oturumsuz <b>çalışmaya devam etmek zorunda</b>.
    /// </summary>
    [Fact]
    public async Task TheErrorPage_StaysOpenToAnonymousVisitors()
    {
        var client = _factory.CreatePanelClient();

        var response = await client.GetAsync("/Home/Error");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "UseExceptionHandler bu adresi boru hattını yeniden çalıştırarak açar; " +
            "kapalı olsaydı 500 alan yönetici hata sayfası yerine giriş ekranına atılır " +
            "ve gerçek hata hiçbir yerde görünmezdi");
    }

    /// <summary>
    /// Sağlık probe'ları da fail-closed kapının dışında kalmak zorunda (12.20a).
    /// Orkestratör 302 alırsa konteyner <b>sağlıksız</b> damgası yer ve sebebi
    /// hiçbir logda görünmez.
    /// </summary>
    [Fact]
    public async Task TheLivenessProbe_IsNotRedirectedToLogin()
    {
        var client = _factory.CreatePanelClient();

        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "/health/live kimlik istemez; FallbackPolicy'den [AllowAnonymous] ile muaf");
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
