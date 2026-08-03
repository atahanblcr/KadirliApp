using FluentAssertions;
using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Lookups;
using KadirliApp.Application.Features.Pharmacies.Dtos;
using KadirliApp.Application.Features.Pharmacies.Queries;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.15b — **önbellek zinciri gerçekten çalışıyor mu?** (gerçek Redis)
///
/// <c>CacheContractTests</c> yapıyı denetler (grup adı sabit mi, her grubun invalidator'ı
/// var mı). Ama yapı doğruyken davranış yanlış olabilir: pipeline'a davranış kaydedilmemiş
/// olabilir, Redis grup set'i yanlış anahtarla yazılıyor olabilir, invalidation komutu
/// çalışsa da anahtarları silmiyor olabilir. Burası o zincirin **uçtan uca** denendiği yer.
///
/// 🔑 Testlerin en kritik adımı, "invalidate çalıştı" iddiasından ÖNCE gelen adım:
/// **önce önbelleğin gerçekten bayat veri döndürdüğü gösterilir.** O adım olmadan
/// "invalidate sonrası taze veri geldi" iddiası, önbellek tümüyle kapalı olsa bile
/// yeşil kalır — yani hiçbir şey kanıtlamaz.
/// </summary>
[Collection(PanelCollection.Name)]
public class CacheInvalidationTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;
    private readonly string _marker = "Cache-" + Guid.NewGuid().ToString("N")[..8];

    public CacheInvalidationTests(WebPanelApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.Neighborhoods.IgnoreQueryFilters().Where(n => n.Name.Contains(_marker)).ExecuteDeleteAsync();
            await db.Pharmacies.IgnoreQueryFilters().Where(p => p.Name.Contains(_marker)).ExecuteDeleteAsync();
            await sp.GetRequiredService<ICacheService>()
                .InvalidateGroupsAsync(new[] { CacheGroups.Lookups, CacheGroups.Pharmacies });
        });
    }

    private async Task<T> SendAsync<T>(IRequest<T> request)
    {
        T result = default!;
        await _factory.WithScopeAsync(async sp => result = await sp.GetRequiredService<ISender>().Send(request));
        return result;
    }

    /// <summary>Önbelleği atlayarak doğrudan veritabanına yazar — "bayat mı?" sorusunu kurar.</summary>
    private async Task InsertNeighborhoodBehindTheCacheAsync(string name)
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.Neighborhoods.Add(new Neighborhood
            {
                Name = name,
                Slug = name.ToLowerInvariant().Replace(' ', '-'),
                DisplayOrder = 999,
                IsActive = true
            });
            await db.SaveChangesAsync();
        });
    }

    /// <summary>
    /// 🔑 Zincirin tamamı: <b>(1)</b> sorgu önbelleğe yazılıyor mu, <b>(2)</b> önbellek
    /// gerçekten bayat veri döndürüyor mu, <b>(3)</b> mutasyon önbelleği temizliyor mu,
    /// <b>(4)</b> temizlik sonrası taze veri geliyor mu.
    /// </summary>
    [Fact]
    public async Task LookupMutation_InvalidatesTheCachedList()
    {
        // (1) Önbelleği ısıt
        var initial = await SendAsync(new GetNeighborhoodsQuery());
        initial.Should().NotBeNull();

        // (2) Önbelleğin ARKASINDAN veritabanına yaz → sorgu hâlâ eski listeyi dönmeli.
        //     Bu adım başarısız olursa önbellek hiç çalışmıyor demektir ve (4) hiçbir şey
        //     kanıtlamaz.
        var sneaked = _marker + " Gizli Mahalle";
        await InsertNeighborhoodBehindTheCacheAsync(sneaked);

        var stale = await SendAsync(new GetNeighborhoodsQuery());
        stale.Select(n => n.Name).Should().NotContain(sneaked,
            "sorgu cache'lenmiş olmalı — bu iddia düşerse cache hiç devrede değildir");
        stale.Should().HaveCount(initial.Count, "bayat liste ilk listeyle aynı olmalı");

        // (3) Invalidate eden komutu çalıştır
        var created = _marker + " Yeni Mahalle";
        await SendAsync(new CreateNeighborhoodCommand(created, "mahalle", 1));

        // (4) Artık ikisi de görünmeli — grup komple temizlendiği için
        var fresh = await SendAsync(new GetNeighborhoodsQuery());
        fresh.Select(n => n.Name).Should().Contain(created, "mutasyondan sonra yeni kayıt görünmeli");
        fresh.Select(n => n.Name).Should().Contain(sneaked,
            "invalidation grup bazlıdır — gruptaki TÜM anahtarlar düşmeli");
    }

    /// <summary>
    /// Aynı gruptaki **farklı** anahtarlar da temizlenmeli. Mahalle listesi ile cami
    /// listesi ayrı anahtarlarda ama aynı <c>lookups</c> grubunda; biri temizlenip
    /// diğeri kalırsa panelde eklenen cami mobilde 15 dakika görünmez.
    /// </summary>
    [Fact]
    public async Task InvalidatingAGroup_ClearsEveryKeyInThatGroup()
    {
        await SendAsync(new GetNeighborhoodsQuery());
        await SendAsync(new GetMosquesQuery());
        await SendAsync(new GetCemeteriesQuery());

        var cache = default(ICacheService);
        await _factory.WithScopeAsync(sp => { cache = sp.GetRequiredService<ICacheService>(); return Task.CompletedTask; });

        // Üç anahtar da yazılmış olmalı
        (await GetRawAsync("lookups:neighborhoods")).Should().NotBeNull();
        (await GetRawAsync("lookups:mosques")).Should().NotBeNull();
        (await GetRawAsync("lookups:cemeteries")).Should().NotBeNull();

        // Tek bir lookup mutasyonu üçünü birden düşürmeli
        await SendAsync(new CreateMosqueCommand(_marker + " Camii", "Kadirli"));

        (await GetRawAsync("lookups:mosques")).Should().BeNull("mutasyonun kendi anahtarı düşmeli");
        (await GetRawAsync("lookups:neighborhoods")).Should().BeNull("aynı gruptaki diğer anahtarlar da düşmeli");
        (await GetRawAsync("lookups:cemeteries")).Should().BeNull("aynı gruptaki diğer anahtarlar da düşmeli");
    }

    /// <summary>
    /// ⚠️ Ters yön de en az o kadar önemli: bir grubu temizlemek **başka grupları
    /// düşürmemeli**. Aksi hâlde her lookup değişikliği tüm önbelleği süpürür ve
    /// önbellek pratikte kapanır (yavaşlık ölçülmeden fark edilmez).
    /// </summary>
    [Fact]
    public async Task InvalidatingOneGroup_DoesNotTouchAnother()
    {
        await SendAsync(new GetPharmaciesQuery(new QueryPharmacyDto(null, null, 1, 20)));
        await SendAsync(new GetNeighborhoodsQuery());

        var pharmacyKey = new GetPharmaciesQuery(new QueryPharmacyDto(null, null, 1, 20)).CacheKey;
        (await GetRawAsync(pharmacyKey)).Should().NotBeNull();

        await SendAsync(new CreateCemeteryCommand(_marker + " Mezarlığı", null));

        (await GetRawAsync("lookups:neighborhoods")).Should().BeNull("lookups grubu temizlenmeli");
        (await GetRawAsync(pharmacyKey)).Should().NotBeNull(
            "eczane grubu lookup mutasyonundan etkilenmemeli — aksi hâlde önbellek pratikte kapalıdır");
    }

    /// <summary>
    /// Eczane, önbelleğin en riskli müşterisi: nöbetçi listesi bayat kalırsa insanlar
    /// gece kapalı eczaneye gider. Panelden yapılan değişiklik anında yansımalı.
    /// </summary>
    [Fact]
    public async Task PharmacyMutation_InvalidatesThePharmacyCache()
    {
        var query = new GetPharmaciesQuery(new QueryPharmacyDto(null, null, 1, 100));
        var before = await SendAsync(query);

        await SendAsync(new KadirliApp.Application.Features.Pharmacies.Commands.CreatePharmacyCommand(
            new CreatePharmacyDto(_marker + " Eczanesi", "Kadirli merkez", "03281112233",
                null, null, null, null, true)));

        var after = await SendAsync(query);
        after.TotalCount.Should().Be(before.TotalCount + 1,
            "panelden eklenen eczane önbellek yüzünden gizlenmemeli");
        after.Items.Select(p => p.Name).Should().Contain(_marker + " Eczanesi");
    }

    /// <summary>
    /// Redis'te grup üyeliği <c>cache-group:{grup}</c> SET'inde tutuluyor. Set temizlenmezse
    /// zamanla sınırsız büyür ve invalidation her seferinde ölü anahtarları siler.
    /// </summary>
    [Fact]
    public async Task GroupSet_IsRemovedAfterInvalidation()
    {
        await SendAsync(new GetNeighborhoodsQuery());
        (await KeyExistsAsync("cache-group:" + CacheGroups.Lookups)).Should().BeTrue(
            "cache yazılırken grup üyeliği de kaydedilmeli");

        await SendAsync(new CreateNeighborhoodCommand(_marker + " Set", null, 1));

        (await KeyExistsAsync("cache-group:" + CacheGroups.Lookups)).Should().BeFalse(
            "invalidation grup set'ini de silmeli, yoksa set sonsuza dek büyür");
    }

    // ─────────────── Redis'e doğrudan bakan yardımcılar ───────────────
    // ⚠️ ICacheService.GetAsync<T> deserialize eder; "anahtar var mı yok mu" sorusunu
    // ham okumadan cevaplamak gerekiyor (null dönen değer ile silinmiş anahtar aynı
    // görünürdü).

    private async Task<string?> GetRawAsync(string key)
    {
        using var redis = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(_factory.RedisConnectionString);
        var value = await redis.GetDatabase().StringGetAsync("cache:" + key);
        return value.HasValue ? value.ToString() : null;
    }

    private async Task<bool> KeyExistsAsync(string fullKey)
    {
        using var redis = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(_factory.RedisConnectionString);
        return await redis.GetDatabase().KeyExistsAsync(fullKey);
    }
}
