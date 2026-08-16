using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using KadirliApp.Application;
using KadirliApp.Infrastructure;
using KadirliApp.Infrastructure.Health;
using KadirliApp.Infrastructure.Http;
using KadirliApp.Infrastructure.Persistence;
using Serilog;

// Form verilerinde ondalık ayracı her ortamda '.' olsun (harita lat/lng girişleri)
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// Faz 9.3: Yapılandırılmış loglama — sink'ler (Console, dosya-JSON, Seq) appsettings "Serilog" bölümünden okunur
builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Web ve Api aynı uploads klasörünü paylaşır (../uploads); config'e mutlak yol yazılır
var uploadsDir = Path.GetFullPath(Path.Combine(
    builder.Environment.ContentRootPath,
    builder.Configuration["FileStorage:UploadDirectory"] ?? "wwwroot/uploads"));
Directory.CreateDirectory(uploadsDir);
builder.Configuration["FileStorage:UploadDirectory"] = uploadsDir;

// Faz 12.2 — panel süper admin parolası: secrets/ altında, git'e GİRMEYEN dosyadan.
// Api/Program.cs ile AYNI dosya: iki host farklı kaynaktan okusaydı biri parolayı
// hizalar, diğeri eski değeri geri yazardı ve arıza "bazen giriyorum, bazen giremiyorum"
// biçiminde görünürdü.
builder.Configuration.AddJsonFile(
    Path.Combine(builder.Environment.ContentRootPath, "..", "secrets", "panel-admin.json"),
    optional: true, reloadOnChange: false);

// Add services to the container.
// 10.9 denetimi: antiforgery artık GLOBAL — tüm POST'lar token ister (eski aksiyonların çoğunda
// [ValidateAntiForgeryToken] eksikti; form tag helper'ları token'ı zaten bastığı için görünüm değişmez).
builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
    // Faz 11.18: parolası yönetici tarafından belirlenmiş kullanıcı, kendi parolasını
    // seçene kadar panelin hiçbir ekranını açamaz (bkz. RequirePasswordChangeFilter).
    o.Filters.Add<KadirliApp.Web.Common.RequirePasswordChangeFilter>();
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => {
        o.LoginPath = "/account/login";
        o.AccessDeniedPath = "/account/denied";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        // 🔴 Faz 11.18 (11.15c C grubu): oturum artık HER istekte DB'den tazeleniyor.
        // Bu satır olmadan silinen/banlanan/rolü düşürülen personelin çerezi 8 saat
        // boyunca çalışmaya devam ediyordu ve parola değişimi de onu düşürmüyordu.
        o.Events.OnValidatePrincipal = KadirliApp.Web.Common.PanelPrincipalValidator.ValidateAsync;
    });
builder.Services.AddAuthorization(o => {
    o.AddPolicy("AdminPanel", p => p.RequireRole("admin","super_admin","moderator"));

    // 🔴 Faz 12.20a — KAPININ YÖNÜ: öznitelik yoksa REDDET.
    //
    // 16 Ağustos denetiminin B1 bulgusu tek bir cümleye dayanıyordu: "FallbackPolicy YOK,
    // yani öznitelik taşımayan aksiyon ANONİMDİR." O varsayılan yüzünden `HomeController`
    // yıllarca kimliksiz açılabildi ve bunu söyleyen tek şey elle tutulan bir muafiyet
    // listesiydi. Varsayılan artık ters: unutulan bir [Authorize] paneli açmaz, KAPATIR.
    //
    // 🔑 12.19'un "sessizce kapanan kapı fark edilir, sessizce açılan fark edilmez" kuralının
    // panel karşılığı — ve korumayı bir TARAMADAN framework'e taşıyor (§7 madde 53'ün dersi:
    // "korumayı taramanın erişemeyeceği yere taşıyabilir miyim?"). PanelAuthenticationTests
    // ikinci hat olarak kalır: fallback "giriş iste" der, o test "sınıfta [Authorize] VAR mı,
    // yani ROL kapısı da kuruldu mu" der — ikisi farklı sorulardır.
    //
    // ⚠️ Bedeli: gerçekten anonim olması gereken üç yer artık bunu AÇIKÇA söylemek zorunda —
    // giriş akışı (AccountController.Login/Logout), hata sayfaları (HomeController) ve
    // /health/* probe'ları (MapInfrastructureHealthEndpoints). Üçü de [AllowAnonymous].
    o.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<KadirliApp.Application.Common.Auditing.IAuditContext, KadirliApp.Web.Common.HttpAuditContext>();
// Faz 11.15b: menüyü kullanıcının izinlerine göre süzer (scoped — istek başına tek sorgu).
builder.Services.AddScoped<KadirliApp.Web.Common.IPanelMenuProvider, KadirliApp.Web.Common.PanelMenuProvider>();

// Faz 9.2: panel login'ine IP bazlı Brute-Force koruması (AccountController POST Login'de
// [EnableRateLimiting("panel-login")]). Limitler appsettings "RateLimiting:PanelLogin" bölümünden.
var panelLoginPermit = builder.Configuration.GetValue("RateLimiting:PanelLogin:PermitLimit", 5);
var panelLoginWindow = builder.Configuration.GetValue("RateLimiting:PanelLogin:WindowSeconds", 60);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = async (context, ct) =>
    {
        // 🐛 Faz 12.2 (canlı doğrulamada bulundu): hız sınırı ara katmanı controller'dan
        // ÖNCE çalışıyor, yani kısılan denemeler AccountController'a hiç ulaşmıyor ve
        // login_attempts'e TEK SATIR bile düşmüyordu. Sonuç, bu fazın savaştığı sınıfın
        // ta kendisiydi: saldırgan dakikada 500 deneme yapar, panel "5 deneme" gösterir.
        // ⚠️ Kısma ne kadar iyi çalışırsa tablo o kadar çok yalan söylüyordu.
        await RecordRateLimitedAttemptAsync(context.HttpContext, ct);

        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "Çok fazla deneme yaptınız. Lütfen 1 dakika sonra tekrar deneyin.", ct);
    };
    o.AddPolicy("panel-login", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = panelLoginPermit,
                Window = TimeSpan.FromSeconds(panelLoginWindow),
                QueueLimit = 0
            }));
});

var app = builder.Build();

// 🔴 Faz 12.2 — ForwardedHeaders EN BAŞTA (Api/Program.cs ile AYNI sınıftan, aynı ayarlardan).
// İki host iki ayrı gerçekleme yazsaydı biri güncellenip diğeri unutulurdu ve panelin
// gördüğü IP ile API'nin gördüğü IP ayrışırdı — aynı saldırı iki tabloda iki farklı
// kaynaktan gelmiş görünürdü.
app.UseConfiguredForwardedHeaders(
    app.Configuration,
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ForwardedHeaders"));

// 🔴 Faz 12.9 — Production'da panelin yerelleştirilmiş varlıkları eksikse AÇILMAZ.
// Veritabanına dokunmadan ÖNCE: eksik CSS/JS ile açılan bir panel "çalışıyor" görünür
// (uçlar 200 döner, log temizdir) ama yönetici stilsiz bir sayfada harita seçemez.
KadirliApp.Web.Common.PanelAssetGuard.Validate(
    app.Environment,
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("PanelAssets"));

// Migration + idempotent başlangıç verisi (super_admin ve lookup tabloları)
await DbSeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// 🔴 Faz 12.9 — CSP başlığı + istek başına nonce.
// ⚠️ Sıra: statik dosyalardan ve durum kodu yeniden çalıştırmasından ÖNCE olmalı ki
// panelin ÜRETTİĞİ HER yanıt (hata sayfaları ve 404 dâhil) başlığı taşısın. Yalnız
// "normal" sayfalarda korunmak, saldırganın hedefleyeceği yanıtı korumasız bırakmaktır.
// Nonce görünümlere Context.Items["csp-nonce"] ile ulaşır.
app.UseMiddleware<KadirliApp.Web.Common.ContentSecurityPolicyMiddleware>();

// Faz 12.1: panelde oluşan işlenmemiş hataları error_logs'a yazar.
// ⚠️ Sıra kritik ve sezgiye ters: bu satır UseExceptionHandler'dan **SONRA** gelmeli.
// Ara katmanlarda önce kaydedilen DIŞTA kalır; istisnayı ilk gören ise İÇTEKİdir.
// Önce yazsaydık UseExceptionHandler istisnayı bizden önce yutar ve buraya hiç ulaşmazdı —
// yani hata sayfası çalışır, kayıt sessizce hiç oluşmazdı.
// Yanıt üretmez, yalnız kaydedip yeniden fırlatır (bkz. PanelErrorLoggingMiddleware).
app.UseMiddleware<KadirliApp.Web.Common.PanelErrorLoggingMiddleware>();

// Faz 11.15c: gövdesiz durum sayfalarının sonu. Bu satır olmadan panelde var olmayan
// bir adres 404 + 0 BAYT (bembeyaz sayfa) dönüyordu; yönetici ne olduğunu anlamıyor ve
// panele dönecek bir bağlantı bulamıyordu. UseExceptionHandler yalnız 500'ü ve yalnız
// Development dışında karşılar — durum kodları için ayrı katman gerekiyor.
// Statik dosyalardan ÖNCE: yeniden çalıştırma (re-execute) tüm pipeline'dan geçmeli.
app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");

// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsDir),
    RequestPath = "/uploads"
});

// Statik dosyalardan SONRA: her paneli isteğini tek satır yapılandırılmış olayla loglar (css/js gürültüsü olmadan)
app.UseSerilogRequestLogging();

app.UseRouting();

// Faz 9.2: rate limiter routing'den SONRA — endpoint'e bağlı "panel-login" policy'si çözülebilsin
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

// Faz 9.3: /health (detay), /health/live, /health/ready
app.MapInfrastructureHealthEndpoints();

app.Run();

/// <summary>
/// Faz 12.2 — hız sınırının reddettiği giriş denemesini kaydeder.
/// </summary>
/// <remarks>
/// 🔴 <b>Hiçbir koşulda fırlatmaz.</b> Burası zaten bir reddetme yolu; buradaki bir istisna
/// istemciye 429 yerine ham 500 verdirirdi. Gövde okunamazsa kimlik "(boş)" olur ve satır
/// yine yazılır — <b>IP ve zaman</b>, "bu adres kısıldı" bilgisinin asıl taşıyıcısı.
///
/// ⚠️ Gövde yalnız form POST'unda ve <c>ReadFormAsync</c> ile okunur; istek zaten
/// reddedildiği için aşağı akışta kimseyi etkilemez.
/// </remarks>
static async Task RecordRateLimitedAttemptAsync(HttpContext http, CancellationToken ct)
{
    try
    {
        if (!HttpMethods.IsPost(http.Request.Method)) return;

        var recorder = http.RequestServices
            .GetService<KadirliApp.Application.Common.Interfaces.ILoginAttemptRecorder>();
        if (recorder is null) return;

        var identifier = string.Empty;
        if (http.Request.HasFormContentType)
        {
            var form = await http.Request.ReadFormAsync(ct);
            identifier = form["username"].ToString();
        }

        await recorder.RecordAsync(new KadirliApp.Application.Common.Interfaces.LoginAttemptRecord(
            Channel: KadirliApp.Application.Common.Interfaces.LoginChannels.Panel,
            RawIdentifier: identifier,
            UserId: null,
            Succeeded: false,
            FailureReason: KadirliApp.Application.Common.Interfaces.LoginFailureReasons.RateLimited), ct);
    }
    catch
    {
        // Yutulur: reddetme yanıtı her hâlükârda yazılmalı.
    }
}

// Faz 11.15b: paneli WebApplicationFactory ile ayağa kaldırabilmek için. Üst düzey
// deyimlerin ürettiği Program sınıfı varsayılan olarak `internal`'dır; test projesinin
// generic tip argümanı olarak kullanabilmesi için public'e açılıyor
// (KadirliApp.Api/Program.cs'te de aynı satır var).
public partial class Program { }
