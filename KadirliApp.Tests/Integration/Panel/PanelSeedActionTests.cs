extern alias WebPanel;

using System.Net;
using FluentAssertions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 12.19a — görünmez sözleşme <b>#78</b>'in <b>davranış</b> ayağı:
/// <c>/Dashboard/Seed</c> gerçekten kapandı mı?
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Kapatılan şey neydi:</b> aksiyon 14 Ağu 2026 denetimine kadar <c>[HttpGet]</c>,
/// <b>ortam kapısız</b> ve düz bir <c>&lt;a href&gt;</c> ile çağrılıyordu. Üçü birleşince
/// canlıda şu mümkündü: yöneticinin ziyaret ettiği kötü niyetli bir sayfadaki tek bir
/// <c>&lt;img src="https://panel/Dashboard/Seed"&gt;</c>, <b>onun oturum çerezleriyle</b>
/// boş kalan her tabloya sahte içerik bastırırdı — sahte ilan, uydurma telefon,
/// <b>sahte vefat ilanı</b>. Yönetici hiçbir şey tıklamamış olurdu.
/// </para>
/// <para>
/// 🔑 <b>Neden gerçek <c>IMockDataSeeder</c> DEĞİL sahtesi bağlanıyor:</b> panel testlerinin
/// hepsi <b>tek</b> Postgres konteynerini paylaşıyor (<c>PanelCollection</c>). Gerçek
/// seeder burada koşsaydı, bu dosya 400+ testin altındaki veritabanına 20 tablo dolusu
/// sahte kayıt basar ve "boş liste" ya da "kesin sayı" iddiası taşıyan başka bir testi
/// <b>koşum sırasına göre</b> kırardı — yani süit rastgele kırmızıya dönerdi.
/// Burada denetlenen şey zaten <i>kapılar</i>: yönlendirme, antiforgery, rol, ortam ve
/// denetim izi. Seeder'ın <b>kendi</b> davranışı (idempotentlik, satır sayımı)
/// <c>MockDataSeederTests</c>'te, kendi veritabanında ölçülüyor.
/// </para>
/// </remarks>
[Collection(PanelCollection.Name)]
public class PanelSeedActionTests
{
    private readonly WebPanelApplicationFactory _factory;

    public PanelSeedActionTests(WebPanelApplicationFactory factory) => _factory = factory;

    /// <summary>Çağrıldığını kaydeden, veritabanına DOKUNMAYAN seeder.</summary>
    private sealed class SpySeeder : IMockDataSeeder
    {
        public int Calls { get; private set; }

        public Task<MockDataSeedResult> SeedAsync(CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(new MockDataSeedResult(
                new Dictionary<string, int> { ["ads"] = 5, ["announcements"] = 3 }));
        }
    }

    /// <summary>
    /// Sahte seeder bağlanmış panel; istenirse başka bir <b>ortamda</b> ayağa kalkar.
    /// Konteynerler tabandaki factory'den gelir (yeniden başlatılmaz).
    /// </summary>
    private (WebApplicationFactory<WebPanel::Program> Factory, HttpClient Client, SpySeeder Seeder)
        PanelWithSpySeeder(string? environment = null)
    {
        var spy = new SpySeeder();

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            if (environment is not null) builder.UseEnvironment(environment);
            builder.ConfigureServices(services => services.AddScoped<IMockDataSeeder>(_ => spy));
        });

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        return (factory, client, spy);
    }

    /// <summary>
    /// ⚠️ <b>Sıra burada bir tuzak ve testin ilk yazımında tam bu tuzağa düşüldü:</b>
    /// zorunlu parola bayrağı <b>her host açılışında yeniden konuyor</b>
    /// (<c>DbSeeder</c>, varsayılan parolayı hâlâ kullanan super_admin'i işaretler —
    /// 11.18 kuralı). <c>WithWebHostBuilder</c> <b>yeni bir host</b> kurduğu için,
    /// bayrağı taban factory üzerinden önceden temizlemek <b>hiçbir işe yaramaz</b>:
    /// yeni host açılırken onu geri koyar ve panelin her sayfası
    /// <c>/Account/ChangePassword</c>'e döner. Temizlik türetilmiş host <b>ayağa
    /// kalktıktan sonra</b> yapılmalı.
    /// </summary>
    private async Task<(HttpClient Client, SpySeeder Seeder)> LoggedInPanelAsync(string? environment = null)
    {
        var (factory, client, spy) = PanelWithSpySeeder(environment);

        using (var scope = factory.Services.CreateScope())   // host burada kurulur (DbSeeder koşar)
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var admin = await db.Users.FirstOrDefaultAsync(u => u.Username == DbSeeder.AdminUsername);
            if (admin is { MustChangePassword: true })
            {
                admin.MustChangePassword = false;
                await db.SaveChangesAsync();
            }
        }

        await client.LoginAsync(DbSeeder.AdminUsername, DbSeeder.AdminPassword);
        return (client, spy);
    }

    // ── 1) GET artık yok ────────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 <b>Bu fazın bir numaralı iddiası.</b> Aksiyon GET kaldığı sürece antiforgery
    /// koruması onu <b>hiç görmez</b> (<c>AutoValidateAntiforgeryToken</c> yalnız
    /// POST/PUT/DELETE doğrular) — yani CSRF kapısı açık kalır.
    /// </summary>
    [Fact]
    public async Task Seed_IsNotReachableWithGet()
    {
        var (client, spy) = await LoggedInPanelAsync();

        var response = await client.GetAsync("/Dashboard/Seed");

        response.StatusCode.Should().BeOneOf(
            [HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound],
            "GET ile çağrılabilen bir seed aksiyonu, yöneticinin oturumuyla bir <img> " +
            "etiketinden bile tetiklenir");
        spy.Calls.Should().Be(0, "reddedilen istek seeder'a HİÇ ulaşmamalı");
    }

    /// <summary>
    /// İkinci CSRF ayağı: POST doğru ama <b>token'sız</b> istek de reddedilmeli.
    /// (Global filtre zaten yapıyor; bu iddia filtre bir gün kaldırılırsa konuşur.)
    /// </summary>
    [Fact]
    public async Task Seed_RejectsAPostWithoutAnAntiforgeryToken()
    {
        var (client, spy) = await LoggedInPanelAsync();

        var response = await client.PostAsync("/Dashboard/Seed", new FormUrlEncodedContent(new Dictionary<string, string>()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        spy.Calls.Should().Be(0);
    }

    // ── 2) Mutlu yol: geliştirmede admin basabilir ve İZ DÜŞER ──────────────────

    /// <summary>
    /// Ters yön ve şart: kapılar öyle sıkılaştırılabilir ki aksiyon <b>hiç</b> çalışmaz
    /// hâle gelir — o zaman yukarıdaki iddiaların hepsi vakum olur ve yeşil kalır
    /// (§7 madde 68'in dersi).
    /// </summary>
    [Fact]
    public async Task Seed_RunsForAnAdminInDevelopment_AndLeavesAnAuditTrail()
    {
        var (client, spy) = await LoggedInPanelAsync();

        var before = await AuditSeedCountAsync();

        var response = await client.PostFormAsync("/Dashboard/Seed", new Dictionary<string, string>(),
            tokenFromPath: "/Dashboard/Index");

        response.StatusCode.Should().BeOneOf([HttpStatusCode.Redirect, HttpStatusCode.Found],
            "başarılı aksiyon iniş sayfasına döner");
        spy.Calls.Should().Be(1);

        // 🔴 12.19a'nın üçüncü deliği: aksiyon MediatR'ı atladığı için denetim izi HİÇ
        // düşmüyordu. Canlıda sahte içerik basabilen tek aksiyonun "kim çalıştırdı?"
        // sorusunun cevabı hiçbir yerde yazmıyordu.
        (await AuditSeedCountAsync()).Should().Be(before + 1,
            "örnek veri basma artık audit_logs'a düşmeli (module=system, action=seed)");
    }

    private async Task<int> AuditSeedCountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.AuditLogs.CountAsync(a => a.Module == "system" && a.Action == "seed");
    }

    // ── 3) Rol kapısı ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Seed_IsClosedToModerators()
    {
        const string username = "seed-moderator";
        const string password = "Moderator!2026";
        await _factory.EnsureModeratorAsync(username, password);

        var (_, client, spy) = PanelWithSpySeeder();
        await client.LoginAsync(username, password);

        // ⚠️ Token `/Account/ChangePassword`'den alınıyor, Dashboard'dan DEĞİL: iniş
        // sayfasında moderatöre çizilen hiçbir form yok (seed butonu artık forma bağlı ve
        // moderatöre kapalı), yani token oradan alınamaz ve test rol kapısını değil
        // "token bulunamadı"yı ölçerdi. Antiforgery token'ı kullanıcı kimliğine bağlıdır,
        // bu yüzden aynı oturumdan alınmalı.
        var response = await client.PostFormAsync("/Dashboard/Seed", new Dictionary<string, string>(),
            tokenFromPath: "/Account/ChangePassword");

        response.StatusCode.Should().BeOneOf(
            [HttpStatusCode.Forbidden, HttpStatusCode.Redirect, HttpStatusCode.Found],
            "moderatör örnek veri basamaz (rol kapısı)");
        spy.Calls.Should().Be(0);
    }

    // ── 4) Ortam kapısı ─────────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 <b>Fazın var olma sebebi.</b> Production'da adresin kendisi olmamalı.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>404, 403 değil:</b> 403 "burada bir şey var ama sana kapalı" der ve yolun
    /// varlığını doğrular.
    /// </remarks>
    [Fact]
    public async Task Seed_Is404InProduction()
    {
        var (client, spy) = await LoggedInPanelAsync(environment: "Production");

        // Token Dashboard'dan alınamaz — Production'da buton (ve dolayısıyla form) hiç
        // çizilmiyor. Bu, testin kendisinin ikinci bir kanıtı.
        var response = await client.PostFormAsync("/Dashboard/Seed", new Dictionary<string, string>(),
            tokenFromPath: "/Account/ChangePassword");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        spy.Calls.Should().Be(0);
    }

    /// <summary>
    /// Kapının UX ayağı: buton Production'da <b>hiç çizilmemeli</b>. Adres kapalı ama
    /// buton duruyor olsaydı panelde "tıklayınca hata veren buton" kalırdı (§5).
    /// </summary>
    [Fact]
    public async Task TheSeedButton_IsOnlyDrawnInDevelopment()
    {
        var (devClient, _) = await LoggedInPanelAsync();
        var devHtml = await (await devClient.GetAsync("/Dashboard/Index")).ReadDecodedBodyAsync();

        devHtml.Should().Contain("Paneli Test Verileriyle Doldur",
            "geliştirmede buton durmalı — yoksa bu testin Production ayağı vakum olur");

        var (prodClient, _) = await LoggedInPanelAsync(environment: "Production");
        var prodHtml = await (await prodClient.GetAsync("/Dashboard/Index")).ReadDecodedBodyAsync();

        prodHtml.Should().NotContain("Paneli Test Verileriyle Doldur",
            "Production'da örnek veri butonu hiç çizilmemeli");
    }

    // ── 5) Ortam rozeti (plan dışı ek) ──────────────────────────────────────────

    /// <summary>
    /// ➕ <b>12.19a'nın plan dışı eki:</b> panel, canlı ortamda <b>olmadığını</b> söylemeli.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔑 <b>Neden gerekli:</b> panel geri alınamaz ve şehir ölçekli işlerin yapıldığı yer —
    /// bütün şehre push atmak (12.15: gönderim <b>terminal</b>), hukuki metin <b>yayınlamak</b>
    /// (12.16: yayınlanmış sürüm <b>değiştirilemez</b>), vefat ilanı onaylamak. Buna karşılık
    /// 12.19a'ya kadar ekranda "burası hangi kurulum?" sorusuna cevap veren hiçbir şey yoktu:
    /// geliştirme paneli ile canlı panel <b>piksel piksel aynı</b> görünüyordu.
    /// </para>
    /// <para>
    /// 🔴 <b>Rozetin yönü kuralın kendisi ve iddia bu yüzden İKİ YÖNLÜ:</b> "CANLI" yazan bir
    /// rozet, unutulduğu ya da yanlış yapılandırıldığı anda <b>canlıyı güvenli gösterirdi</b>
    /// (rozet yok → "demek ki geliştirme"). Ters yönde en kötü ihtimal, geliştirme panelinin
    /// süslenmemiş kalmasıdır — sessiz hasar üretmeyen tek yön bu.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task ThePanel_SaysWhenItIsNotProduction()
    {
        var (devClient, _) = await LoggedInPanelAsync();
        var devHtml = await (await devClient.GetAsync("/Dashboard/Index")).ReadDecodedBodyAsync();

        devHtml.Should().Contain("Bu panel canlı ortam DEĞİL",
            "canlı olmayan panel bunu SÖYLEMELİ");

        var (prodClient, _) = await LoggedInPanelAsync(environment: "Production");
        var prodHtml = await (await prodClient.GetAsync("/Dashboard/Index")).ReadDecodedBodyAsync();

        prodHtml.Should().NotContain("Bu panel canlı ortam DEĞİL",
            "Production'da rozet çizilmemeli — yoksa uyarı gürültüye dönüşür ve okunmaz olur");
    }
}
