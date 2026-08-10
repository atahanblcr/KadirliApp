extern alias WebPanel;

using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

using Csp = WebPanel::KadirliApp.Web.Common.ContentSecurityPolicyMiddleware;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 12.9 — CSP başlığının <b>gerçekten gönderildiğini</b> ve doğru kurulduğunu denetler.
/// </summary>
/// <remarks>
/// <para>
/// <c>PanelExternalOriginTests</c> kaynağı tarar (görünümlerde dış origin var mı);
/// bu dosya <b>canlı yanıta</b> bakar. İkisi ayrı olmak zorunda: politika metnini üreten
/// bir metot yazıp onu pipeline'a <b>bağlamayı unutmak</b> mümkün ve o durumda kaynak
/// taraması yeşil kalır, panel korumasız çalışır ve <b>hiçbir belirti olmaz</b>.
/// </para>
/// </remarks>
[Collection(PanelCollection.Name)]
public class PanelContentSecurityPolicyTests
{
    private readonly WebPanelApplicationFactory _factory;

    public PanelContentSecurityPolicyTests(WebPanelApplicationFactory factory) => _factory = factory;

    private static string HeaderOf(HttpResponseMessage response)
    {
        response.Headers.TryGetValues("Content-Security-Policy", out var values)
            .Should().BeTrue("panelin her yanıtı CSP başlığı taşımalı");

        return values!.Single();
    }

    // ── 1) Başlık var ve script tarafı sıkı ───────────────────────────────────

    /// <summary>
    /// 🔴 <b>`script-src`'ta `'unsafe-inline'` OLMAMALI.</b> Açılırsa koruma fiilen
    /// iptal olur: panelde gösterilen metnin bir kısmı <i>vatandaştan</i> geliyor
    /// (hata kaydı mesajları, şikayet başlıkları) ve depolanmış XSS bu projenin
    /// zaten savaştığı bir sınıf (görünmez sözleşme #33).
    /// </summary>
    [Fact]
    public async Task PanelResponse_CarriesACspWithoutUnsafeInlineScripts()
    {
        var client = await _factory.SuperAdminAsync();
        var policy = HeaderOf(await client.GetAsync("/Dashboard/Index"));

        policy.Should().Contain("default-src 'self'");
        policy.Should().Contain("object-src 'none'");
        policy.Should().Contain("base-uri 'self'");
        policy.Should().Contain("frame-ancestors 'none'");
        policy.Should().Contain("form-action 'self'");

        var scriptSrc = policy
            .Split(';', StringSplitOptions.TrimEntries)
            .Single(d => d.StartsWith("script-src ", StringComparison.Ordinal));

        scriptSrc.Should().NotContain("'unsafe-inline'",
            "açılsaydı 12.9'da 47 satır içi işleyiciyi taşımanın hiçbir anlamı kalmazdı");
        scriptSrc.Should().NotContain("'unsafe-eval'");
        scriptSrc.Should().Contain("'nonce-");

        // Hiçbir yönergede dış origin olmamalı — TEK istisna harita kareleri (img-src).
        var directivesWithForeignOrigins = policy
            .Split(';', StringSplitOptions.TrimEntries)
            .Where(d => d.Contains("http", StringComparison.OrdinalIgnoreCase))
            .ToList();

        directivesWithForeignOrigins.Should().OnlyContain(
            d => d.StartsWith("img-src ", StringComparison.Ordinal) && d.Contains(Csp.MapTileOrigin),
            "dış origin yalnız harita KARELERİ için açık: Leaflet gelmezse seçici tamamen " +
            "ölür, kareler gelmezse harita gri kalır ama koordinat seçimi çalışır");
    }

    // ── 2) Nonce istek başına değişiyor ───────────────────────────────────────

    /// <summary>
    /// 🔴 <b>Sabit bir nonce, CSP'yi görünürde bırakıp fiilen kaldırır:</b> enjekte
    /// edilen betik değeri sayfadan <b>kopyalayarak</b> çalışır. Bu, kırılması en kolay
    /// ve fark edilmesi en zor ayrıntı — panel her iki durumda da sorunsuz görünür.
    /// </summary>
    [Fact]
    public async Task Nonce_IsDifferentOnEveryRequest()
    {
        var client = await _factory.SuperAdminAsync();

        static string NonceOf(string policy) =>
            Regex.Match(policy, "'nonce-(?<n>[^']+)'").Groups["n"].Value;

        var first = NonceOf(HeaderOf(await client.GetAsync("/Dashboard/Index")));
        var second = NonceOf(HeaderOf(await client.GetAsync("/Dashboard/Index")));

        first.Should().NotBeNullOrWhiteSpace();
        second.Should().NotBe(first, "nonce tahmin edilebilir olursa CSP fiilen kalkar");
    }

    // ── 3) Başlıktaki nonce, sayfadaki nonce ile AYNI ─────────────────────────

    /// <summary>
    /// İkisi ayrışırsa panelin bütün satır içi blokları engellenir: silme onayı,
    /// toplu işlem sayacı ve harita seçici <b>sessizce</b> çalışmaz.
    /// Bu testin varlık sebebi, üretilen değerin görünüme <i>ulaştığını</i> kanıtlamak.
    /// </summary>
    [Fact]
    public async Task NonceInHeader_MatchesTheNonceRenderedIntoThePage()
    {
        var client = await _factory.SuperAdminAsync();

        // Harita seçicisi taşıyan bir form: satır içi blokların gerçekten olduğu sayfa.
        var response = await client.GetAsync("/EventsAdmin/Create");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var headerNonce = Regex.Match(HeaderOf(response), "'nonce-(?<n>[^']+)'").Groups["n"].Value;
        var html = await response.ReadDecodedBodyAsync();

        html.Should().Contain($"nonce=\"{headerNonce}\"",
            "başlıktaki nonce ile sayfadaki nonce aynı olmalı — ayrışırsa satır içi " +
            "bloklar engellenir ve panel sessizce işlevsizleşir");
    }

    // ── 4) Giriş ekranı da korunuyor ──────────────────────────────────────────

    /// <summary>
    /// Giriş ekranı <c>Layout = null</c> olduğu için ortak <c>&lt;head&gt;</c>'i
    /// paylaşmıyor — 12.9 öncesinde Tailwind'in <b>ikinci bir CDN kopyasını</b> tam
    /// bu yüzden taşıyordu. Oturum <b>açılmadan</b> görülen tek sayfa burası; korumanın
    /// dışında kalması, korunmayan tek yerin en çok saldırıya açık yer olması demekti.
    /// </summary>
    [Fact]
    public async Task LoginPage_IsAlsoCovered_AndLoadsNoExternalResource()
    {
        var client = _factory.CreatePanelClient();
        var response = await client.GetAsync("/account/login");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        HeaderOf(response).Should().Contain("default-src 'self'");

        var html = await response.ReadDecodedBodyAsync();
        html.Should().NotContain("cdn.tailwindcss.com");
        html.Should().NotContain("fonts.googleapis.com");
        html.Should().Contain("/css/panel.css", "stil artık yerelden geliyor");
        html.Should().Contain("/lib/inter/inter.css", "yazı tipi artık yerelden geliyor");
    }

    // ── 5) Harita seçici taşıyan formlar yerel Leaflet kullanıyor ─────────────

    /// <summary>
    /// 🔴 12.9'un <b>işlevsel</b> çekirdeği. <c>_LocationPickerScripts</c> beş modülün
    /// Create+Edit görünümlerinde (10 form) kullanılıyor; <c>unpkg</c> erişilemediğinde
    /// yönetici <b>boş bir kutu</b> görüyor ve koordinat seçemiyordu.
    /// </summary>
    [Theory]
    [InlineData("/AnnouncementsAdmin/Create")]
    [InlineData("/DeathsAdmin/Create")]
    [InlineData("/EventsAdmin/Create")]
    [InlineData("/PlacesAdmin/Create")]
    [InlineData("/GuideAdmin/Create")]
    public async Task LocationPickerForms_LoadLeafletFromTheLocalCopy(string path)
    {
        var client = await _factory.SuperAdminAsync();
        var html = await (await client.GetAsync(path)).ReadDecodedBodyAsync();

        html.Should().Contain("/lib/leaflet/leaflet.js");
        html.Should().NotContain("unpkg.com",
            "Leaflet dış origin'den gelirse ağ kesildiğinde harita seçici SESSİZCE ölür");
    }
}
