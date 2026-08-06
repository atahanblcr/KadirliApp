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
