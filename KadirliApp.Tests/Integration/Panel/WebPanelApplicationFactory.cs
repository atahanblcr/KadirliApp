extern alias WebPanel;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.15b — **panelin (KadirliApp.Web) ilk otomatik test altyapısı.**
///
/// 20 panel controller'ı bugüne kadar yalnız elle <c>curl</c> ile denenmişti: yönetici
/// arayüzü, mobil uygulamanın tüm içeriğinin girildiği yer olduğu hâlde emniyet ağının
/// dışındaydı. Bu factory paneli gerçek Postgres + Redis ile ayağa kaldırır.
///
/// ⚠️ <c>extern alias WebPanel</c> zorunlu: <c>KadirliApp.Api</c> ve <c>KadirliApp.Web</c>
/// projelerinin ikisi de üst düzey deyimlerle yazıldığı için global namespace'te
/// <c>Program</c> tipi üretirler. Alias olmadan bu dosya derlenmez.
/// </summary>
public class WebPanelApplicationFactory : WebApplicationFactory<WebPanel::Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private readonly RedisContainer _redisContainer;

    public WebPanelApplicationFactory()
    {
        // ⚠️ Panel girişi IP başına 5 denemeye kilitli (Faz 9.2) ve TestServer'da
        // RemoteIpAddress **null**'dır → tüm testler "unknown" adlı TEK partisyonu paylaşır;
        // altıncı giriş 429 alır ve süit çöker.
        //
        // Limit `Program.cs`'te `builder.Configuration.GetValue(...)` ile **erkenden**
        // (host kurulmadan) okunuyor; WebApplicationFactory'nin ConfigureAppConfiguration'ı
        // ise ancak Build() sırasında uygulanır — yani oradan verilen değer bu satıra
        // YETİŞMEZ. (Bağlantı dizeleri yetişiyor çünkü onlar DbContext çözülürken,
        // yani Build() sonrasında okunuyor.) Ortam değişkeni ise `CreateBuilder`'ın
        // varsayılan sağlayıcılarından biri olduğu için zamanında görülür.
        //
        // 📌 Kilidin KENDİSİ ayrı bir testte, gerçek limitle denenir — burada gevşetilmesi
        // onu test kapsamı dışına çıkarmaz.
        Environment.SetEnvironmentVariable("RateLimiting__PanelLogin__PermitLimit", "100000");

        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("kadirliapp_panel_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
    }

    /// <summary>Testler kendi verilerini kurarken doğrudan Redis'e bakabilsin diye.</summary>
    public string RedisConnectionString => _redisContainer.GetConnectionString();

    private HttpClient? _superAdmin;
    private readonly SemaphoreSlim _loginLock = new(1, 1);

    /// <summary>
    /// Tüm panel testlerinin paylaştığı, oturum açmış süper admin istemcisi. Panel oturumu
    /// çerezde durduğu için tek istemci onlarca testi taşıyabilir; her testte yeniden giriş
    /// yapmak süiti yavaşlatır ve giriş hız sınırını tüketir.
    /// </summary>
    public async Task<HttpClient> SuperAdminAsync()
    {
        if (_superAdmin is not null) return _superAdmin;

        await _loginLock.WaitAsync();
        try
        {
            if (_superAdmin is not null) return _superAdmin;

            // 🔑 Faz 11.18: seed'lenen süper admin artık `MustChangePassword = true` ile doğuyor
            // (varsayılan parola kaynakta yazılı olduğu için). O bayrak açıkken panelin HİÇBİR
            // sayfası açılmaz — paylaşılan oturum bayrağı burada temizler.
            //
            // ⚠️ Bu, davranışı test kapsamı dışına çıkarmaz: zorunlu değişim akışının kendisi
            // `PanelPasswordSecurityTests`te, bayrağı bilerek AÇARAK denenir. Buradaki temizlik
            // yalnızca "ilk giriş" ile hiç ilgisi olmayan 400+ testin, her istekte parola
            // ekranına yönlendirilmemesi içindir.
            await this.ClearMustChangePasswordAsync(KadirliApp.Infrastructure.Persistence.DbSeeder.AdminUsername);

            return _superAdmin = await this.LoginAsSuperAdminAsync();
        }
        finally
        {
            _loginLock.Release();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:Postgres", _dbContainer.GetConnectionString() },
                { "ConnectionStrings:Redis", _redisContainer.GetConnectionString() },
                // Panel girişi IP başına 5 denemeye kilitli (Faz 9.2). Testler tek IP'den
                // onlarca kez giriş yaptığı için limit gevşetilir — kilidin KENDİSİ ayrı
                // bir testte, kendi factory'siyle denenmeli.
                { "RateLimiting:PanelLogin:PermitLimit", "10000" },
                // 🔴 Faz 12.2: panel parolası artık `secrets/panel-admin.json`'dan
                // hizalanabiliyor. O dosya GELİŞTİRİCİNİN MAKİNESİNDE var, CI'da yok —
                // yani testler onu okusaydı **kimin makinesinde koştuğuna göre** farklı
                // davranırdı: seed parolayı geliştiricinin değeriyle ezer ve
                // `DbSeeder.AdminPassword` ile giriş yapan onlarca test kırılırdı.
                // Boş değer dosyayı bilinçli olarak devre dışı bırakır.
                { KadirliApp.Infrastructure.Persistence.DbSeeder.PanelPasswordConfigKey, "" }
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _redisContainer.StopAsync();
    }
}

/// <summary>
/// Panel testlerinin tamamı **tek** container çiftini paylaşır. Her test sınıfına ayrı
/// <c>IClassFixture</c> verilseydi 6-7 Postgres+Redis konteyneri açılır, süit dakikalarca
/// uzardı.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PanelCollection : ICollectionFixture<WebPanelApplicationFactory>
{
    public const string Name = "panel";
}
