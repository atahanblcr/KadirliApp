using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using KadirliApp.Application;
using KadirliApp.Infrastructure;
using KadirliApp.Infrastructure.Health;
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

// Add services to the container.
// 10.9 denetimi: antiforgery artık GLOBAL — tüm POST'lar token ister (eski aksiyonların çoğunda
// [ValidateAntiForgeryToken] eksikti; form tag helper'ları token'ı zaten bastığı için görünüm değişmez).
builder.Services.AddControllersWithViews(o =>
    o.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => {
        o.LoginPath = "/account/login";
        o.AccessDeniedPath = "/account/denied";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization(o => {
    o.AddPolicy("AdminPanel", p => p.RequireRole("admin","super_admin","moderator"));
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<KadirliApp.Application.Common.Auditing.IAuditContext, KadirliApp.Web.Common.HttpAuditContext>();

// Faz 9.2: panel login'ine IP bazlı Brute-Force koruması (AccountController POST Login'de
// [EnableRateLimiting("panel-login")]). Limitler appsettings "RateLimiting:PanelLogin" bölümünden.
var panelLoginPermit = builder.Configuration.GetValue("RateLimiting:PanelLogin:PermitLimit", 5);
var panelLoginWindow = builder.Configuration.GetValue("RateLimiting:PanelLogin:WindowSeconds", 60);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = async (context, ct) =>
    {
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

// Migration + idempotent başlangıç verisi (super_admin ve lookup tabloları)
await DbSeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

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
