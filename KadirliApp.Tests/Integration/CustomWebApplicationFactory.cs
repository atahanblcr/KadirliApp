using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace KadirliApp.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private readonly RedisContainer _redisContainer;

    public CustomWebApplicationFactory()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("kadirliapp_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        RedirectUploadsToATempDirectory();
    }

    /// <summary>
    /// 🔴 <b>Faz 12.21c — testler DEPONUN <c>uploads/</c> KLASÖRÜNE YAZMAZ.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🐛 <b>Ölçüldü (16 Ağu 2026, 12.21c):</b> depodaki <c>uploads/</c> klasöründe
    /// <b>1208 dosya</b> vardı ama veritabanındaki <c>files</c> tablosunda yalnız
    /// <b>95 satır</b> — yani dosyaların <b>%92'si (1113 adet) YETİMDİ</b>. Sebep tam
    /// olarak buydu: test fabrikaları bağlantı dizelerini eziyordu ama
    /// <c>FileStorage:UploadDirectory</c>'yi ezmiyordu. Her entegrasyon koşusu
    /// <c>a.png</c>/<c>kapak.webp</c>/<c>govde-601.jpg</c> gibi fixture dosyalarını
    /// <b>gerçek</b> klasöre yazıyor, veritabanı satırları ise atılabilir Testcontainers
    /// veritabanında kalıyordu.
    /// </para>
    /// <para>
    /// 🔑 Hasar bugün küçüktü (4,3 MB) ama 12.21 tam da o klasörü <b>kalıcı bir üretim
    /// volume'üne</b> çeviriyor: temizlenmeseydi her koşunun çöpü, vatandaşın gerçek
    /// görselleriyle aynı kalıcı depoya taşınacaktı. 12.15b'nin *"test kendi satırlarını
    /// silsin"* dersinin dosya sistemi karşılığı.
    /// </para>
    /// <para>
    /// ⚠️ <b>Neden ortam değişkeni, <c>ConfigureAppConfiguration</c> değil:</b>
    /// <c>Program.cs</c> yükleme klasörünü <c>builder.Build()</c>'den <b>ÖNCE</b> okuyor
    /// ve <c>ARCHITECTURE.md</c> §8'in bilinen tuzağı gereği oraya verilen ayar o okumaya
    /// <b>yetişmez</b> (hız sınırıyla aynı sınıf). Ortam değişkeni ise
    /// <c>builder.Configuration</c>'da ilk andan itibaren vardır.
    /// </para>
    /// </remarks>
    private static void RedirectUploadsToATempDirectory()
    {
        var temp = Path.Combine(Path.GetTempPath(), "kadirliapp-tests-uploads", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        Environment.SetEnvironmentVariable("FileStorage__UploadDirectory", temp);
    }


    /// <summary>
    /// Türetilen factory'ler config'i override edebilir (ör. RateLimitingTests: DevMode=false + düşük limit).
    /// Aynı anahtar için buradaki değer varsayılanı ezer.
    /// </summary>
    protected virtual IDictionary<string, string?> ExtraConfiguration => new Dictionary<string, string?>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            var config = new Dictionary<string, string?>
            {
                { "ConnectionStrings:Postgres", _dbContainer.GetConnectionString() },
                { "ConnectionStrings:Redis", _redisContainer.GetConnectionString() },
                { "Otp:DevMode", "true" }, // Ensure dev mode is on so we get predictable OTP
                // Faz 9.2: testler art arda istek attığından auth rate limit'i testte gevşetilir
                { "RateLimiting:Auth:PermitLimit", "1000" },
                { "RateLimiting:Global:PermitLimit", "10000" },
                // Faz 10.7: testler art arda yazma isteği attığından public-write limiti de gevşetilir
                { "RateLimiting:PublicWrite:PermitLimit", "1000" },
                // 🔴 Faz 12.2: `secrets/panel-admin.json` geliştiricinin makinesinde var,
                // CI'da yok. Okunsaydı testler kimin makinesinde koştuğuna göre farklı
                // davranırdı (seed parolayı ezer, DbSeeder.AdminPassword ile giriş kırılır).
                { KadirliApp.Infrastructure.Persistence.DbSeeder.PanelPasswordConfigKey, "" }
            };

            foreach (var (key, value) in ExtraConfiguration)
                config[key] = value;

            configBuilder.AddInMemoryCollection(config);
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
