using KadirliApp.Application;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Api.Filters;
using KadirliApp.Api.Middleware;
using KadirliApp.Api.Services;
using KadirliApp.Infrastructure;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Hangfire;
using KadirliApp.Infrastructure.Health;
using Microsoft.Extensions.FileProviders;
using Serilog;
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

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<KadirliApp.Application.Common.Auditing.IAuditContext, HttpAuditContext>();
// Flutter kontratı: tüm yanıtlar {success, data, meta} zarfıyla döner
builder.Services.AddControllers(o => o.Filters.Add<ApiResponseWrapperFilter>());

var jwtConfig = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtConfig["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtConfig["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // Secret yoksa tahmin edilebilir fallback'le AYAKTA KALMAK yerine startup'ta patla (Faz 10.2)
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["AccessSecret"]
                ?? throw new InvalidOperationException("Jwt:AccessSecret yapılandırılmamış"))),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("AdminPanel", p => p.RequireRole("admin", "super_admin", "moderator"));
    o.AddPolicy("SuperAdmin", p => p.RequireRole("super_admin"));
});

// [RequirePermission(module, action)] — dinamik "perm:*" policy'leri (masterclass 12.5)
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, KadirliApp.Api.Authorization.PermissionPolicyProvider>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, KadirliApp.Api.Authorization.PermissionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Faz 9.2: .NET 8 yerleşik rate limiting — Brute-Force/DDoS koruması. Limitler appsettings
// "RateLimiting" bölümünden. IP bazlı fixed-window; reverse proxy arkasında çalıştırılacaksa
// RemoteIpAddress'in gerçek istemci IP'si olması için ForwardedHeaders middleware'i eklenmelidir.
var rateLimitCfg = builder.Configuration.GetSection("RateLimiting");
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = async (context, ct) =>
    {
        var http = context.HttpContext;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            http.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

        // Flutter kontratı: hata zarfı {success:false, error:{code,message}, meta} (ExceptionMiddleware ile aynı şema)
        http.Response.ContentType = "application/json";
        await http.Response.WriteAsJsonAsync(new
        {
            success = false,
            error = new { code = "RATE_LIMITED", message = "Çok fazla istek. Lütfen daha sonra tekrar deneyin." },
            meta = new
            {
                timestamp = DateTime.UtcNow.ToString("o"),
                path = http.Request.Path.Value,
                traceId = System.Diagnostics.Activity.Current?.Id ?? http.TraceIdentifier
            }
        }, ct);
    };

    // Genel taban limit: IP başına tüm uçlar (statik /uploads dosyaları middleware sırası gereği hariç)
    var globalPermit = rateLimitCfg.GetValue("Global:PermitLimit", 300);
    var globalWindow = rateLimitCfg.GetValue("Global:WindowSeconds", 60);
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = globalPermit,
                Window = TimeSpan.FromSeconds(globalWindow),
                QueueLimit = 0
            }));

    // Sıkı limit: login/verify-otp (AuthController [EnableRateLimiting("auth")])
    var authPermit = rateLimitCfg.GetValue("Auth:PermitLimit", 5);
    var authWindow = rateLimitCfg.GetValue("Auth:WindowSeconds", 60);
    o.AddPolicy("auth", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPermit,
                Window = TimeSpan.FromSeconds(authWindow),
                QueueLimit = 0
            }));

    // Faz 10.7: anonim/kullanıcı yazma uçları için hedefli limit ([EnableRateLimiting("public-write")]) —
    // deaths/complaints/ads POST'ları, track-phone/whatsapp ve files upload. Global 300/dk tek başına
    // pending kuyruğu doldurmayı / sayaç şişirmeyi engellemiyordu.
    var publicWritePermit = rateLimitCfg.GetValue("PublicWrite:PermitLimit", 15);
    var publicWriteWindow = rateLimitCfg.GetValue("PublicWrite:WindowSeconds", 60);
    o.AddPolicy("public-write", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = publicWritePermit,
                Window = TimeSpan.FromSeconds(publicWriteWindow),
                QueueLimit = 0
            }));
});

var app = builder.Build();

// Hataları {success:false, error:{code,message}, meta} kontratına çevirir — pipeline'ın en başında olmalı
app.UseMiddleware<ExceptionMiddleware>();

// Her isteği tek satır yapılandırılmış olayla loglar (method, path, status, süre)
app.UseSerilogRequestLogging();

// Migration + idempotent başlangıç verisi (super_admin ve lookup tabloları)
await DbSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

// Mobil istemci yüklenen görselleri /uploads/... yolundan çeker
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsDir),
    RequestPath = "/uploads"
});

// Faz 9.2: statik dosyalardan SONRA — /uploads görselleri limite dahil edilmez
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "Hello KadirliApp .NET 8 API!");

// Faz 9.3: /health (detay), /health/live, /health/ready
app.MapInfrastructureHealthEndpoints();

app.UseHangfireDashboard("/hangfire");
app.Services.UseInfrastructureJobs();

app.Run();

public partial class Program { }
