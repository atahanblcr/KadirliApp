using KadirliApp.Infrastructure.Persistence;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Infrastructure.Identity;
using KadirliApp.Infrastructure.Persistence.Repositories;
using KadirliApp.Infrastructure.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Hangfire;
using Hangfire.PostgreSql;
using StackExchange.Redis;

namespace KadirliApp.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Bağlantı dizeleri kayıt anında DEĞİL, kullanım anında DI'daki IConfiguration'dan okunur.
    /// Neden (Faz 10.2 keşfi): WebApplicationFactory (minimal hosting) test override'larını host
    /// BUILD edilirken ekler — buradaki eager `cfg.GetConnectionString(...)` okuması override'ı
    /// göremediğinden entegrasyon testleri Testcontainers yerine dev veritabanına yazıyordu.
    /// </summary>
    private static string PostgresConn(IServiceProvider sp) =>
        sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres yapılandırılmamış");

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<AppDbContext>((sp, opt) =>
            opt.UseNpgsql(PostgresConn(sp), npg =>
            {
                npg.EnableRetryOnFailure(3);
                npg.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            })
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IPermissionService, PermissionService>();

        // Faz 12.1 — hata günlüğü yazıcısı. Tek örnek iki rolü birden üstlenir: istek
        // yolundaki IErrorLogSink (kuyruğa bırakır) ve arka plandaki BackgroundService
        // (kuyruğu boşaltır). ⚠️ Üçü de AYNI örneği çözmeli — ayrı örnekler olsaydı
        // yazılan kuyruk hiç okunmaz ve hata kayıtları sessizce kaybolurdu.
        services.AddSingleton<Observability.ChannelErrorLogSink>();
        services.AddSingleton<IErrorLogSink>(sp => sp.GetRequiredService<Observability.ChannelErrorLogSink>());
        services.AddHostedService(sp => sp.GetRequiredService<Observability.ChannelErrorLogSink>());

        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(
                sp.GetRequiredService<IConfiguration>().GetConnectionString("Redis") ?? "localhost:6379"));
        services.AddScoped<IOtpService, RedisOtpService>();
        // Faz 10.2: refresh token rotasyonu/logout iptal listesi (jti → Redis, TTL'li)
        services.AddSingleton<ITokenBlacklistService, RedisTokenBlacklistService>();

        // Faz 9.2: SMS/Mail sağlayıcı seçimi config'ten. Henüz gerçek sağlayıcı anlaşması yok —
        // "Dev" adaptörleri log'a yazar. Sağlayıcı bağlanınca (ör. NetGSM/SMTP) yeni implementasyon
        // Notifications/ altına eklenir, buradaki switch'e kaydedilir ve appsettings'te Provider değiştirilir.
        var smsProvider = cfg["Sms:Provider"] ?? "Dev";
        switch (smsProvider.ToLowerInvariant())
        {
            case "dev":
                services.AddSingleton<ISmsService, Notifications.DevLogSmsService>();
                break;
            default:
                throw new InvalidOperationException(
                    $"Bilinmeyen SMS sağlayıcısı: '{smsProvider}'. ISmsService implementasyonu ekleyip Infrastructure/DependencyInjection.cs'e kaydedin.");
        }

        var emailProvider = cfg["Email:Provider"] ?? "Dev";
        switch (emailProvider.ToLowerInvariant())
        {
            case "dev":
                services.AddSingleton<IEmailService, Notifications.DevLogEmailService>();
                break;
            // Faz 12.2: 9.2'nin "sağlayıcı bağlama talimatı" birebir uygulandı —
            // yeni sınıf Notifications/ altına, buraya bir case, çağıran kod DEĞİŞMEDİ.
            case "smtp":
                services.AddSingleton<IEmailService, Notifications.SmtpEmailService>();
                break;
            default:
                throw new InvalidOperationException(
                    $"Bilinmeyen e-posta sağlayıcısı: '{emailProvider}'. IEmailService implementasyonu ekleyip Infrastructure/DependencyInjection.cs'e kaydedin.");
        }
        // Faz 10.11: FCM push sağlayıcı seçimi (SMS/e-posta deseninin aynısı). Varsayılan "None" → NoOpPushService:
        // Firebase'siz ortamda sistem çalışmaya devam eder, SendPushNotificationsJob IsConfigured=false görünce
        // hiç göndermez. Gerçek gönderim: Fcm:Provider=Firebase + Fcm:ServiceAccountKeyPath — başka kod değişmez.
        var fcmProvider = cfg["Fcm:Provider"] ?? "None";
        switch (fcmProvider.ToLowerInvariant())
        {
            case "none":
                services.AddSingleton<IPushService, Notifications.NoOpPushService>();
                break;
            case "firebase":
                services.AddSingleton<IPushService, Notifications.FcmPushService>();
                break;
            default:
                throw new InvalidOperationException(
                    $"Bilinmeyen FCM sağlayıcısı: '{fcmProvider}'. Geçerli değerler: None | Firebase.");
        }

        // Faz 9.4: distributed cache (CachingBehavior/CacheInvalidationBehavior bunu kullanır)
        services.AddSingleton<ICacheService, Caching.RedisCacheService>();

        // Faz 12.2 — giriş denemesi kaydı. Scoped: istek başına AppDbContext ve
        // IHttpContextAccessor ile çalışır (IP'yi YALNIZ bu sınıf okur).
        services.AddScoped<ILoginAttemptRecorder, Security.LoginAttemptRecorder>();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        services.AddHangfire((sp, c) => c.UsePostgreSqlStorage(o => o.UseNpgsqlConnection(PostgresConn(sp))));
        services.AddHangfireServer();

        // Faz 9.3: /health uçlarının kontrol ettiği kritik bağımlılıklar.
        // Redis kontrolü uygulamanın mevcut singleton multiplexer'ını kullanır (ikinci bağlantı açılmaz).
        services.AddHealthChecks()
            .AddNpgSql(PostgresConn, name: "postgres", tags: new[] { "ready" })
            .AddRedis(sp => sp.GetRequiredService<IConnectionMultiplexer>(), name: "redis", tags: new[] { "ready" })
            .AddHangfire(o => o.MinimumAvailableServers = 1, name: "hangfire", tags: new[] { "ready" });

        return services;
    }

    public static void UseInfrastructureJobs(this IServiceProvider serviceProvider)
    {
        RecurringJob.AddOrUpdate<KadirliApp.Infrastructure.Jobs.ExpireAdsJob>("expire-ads", j => j.RunAsync(), Cron.Hourly);
        RecurringJob.AddOrUpdate<KadirliApp.Infrastructure.Jobs.ArchiveDeathsJob>("archive-deaths", j => j.RunAsync(), Cron.Daily);
        RecurringJob.AddOrUpdate<KadirliApp.Infrastructure.Jobs.PublishScheduledAnnouncementsJob>("publish-scheduled-announcements", j => j.RunAsync(), Cron.Minutely);
        // Faz 10.11: fcm_sent=false bildirimleri gönderir. Fcm:Provider=None iken IsConfigured=false → no-op (boş koşar).
        RecurringJob.AddOrUpdate<KadirliApp.Infrastructure.Jobs.SendPushNotificationsJob>("send-push-notifications", j => j.RunAsync(), Cron.Minutely);
        // Faz 12.1: hata kayıtlarının saklama süresi (çözülmüş 30 gün, çözülmemiş 90 gün).
        RecurringJob.AddOrUpdate<KadirliApp.Infrastructure.Jobs.PurgeErrorLogsJob>("purge-error-logs", j => j.RunAsync(), Cron.Daily);
        // Faz 12.2: şüpheli giriş uyarısı. 5 dakika bilinçli — "anında" bildirim tek tek
        // olayları yollamak demek olurdu ve gruplama (dolayısıyla kısma) anlamsızlaşırdı.
        RecurringJob.AddOrUpdate<KadirliApp.Infrastructure.Jobs.SecurityAlertJob>("security-alerts", j => j.RunAsync(), "*/5 * * * *");
        // Faz 12.2: giriş denemelerinin saklama süresi (başarılı 90, başarısız 180 gün).
        RecurringJob.AddOrUpdate<KadirliApp.Infrastructure.Jobs.PurgeLoginAttemptsJob>("purge-login-attempts", j => j.RunAsync(), Cron.Daily);
        // Faz 12.2b: okunmuş bildirimlerin saklama süresi (90 gün). Kampanya satırı KALIR.
        RecurringJob.AddOrUpdate<KadirliApp.Infrastructure.Jobs.PurgeNotificationsJob>("purge-notifications", j => j.RunAsync(), Cron.Daily);
    }
}
