using FluentAssertions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Dashboard;

/// <summary>
/// Faz 12.19a — örnek veri basmanın <b>kendi</b> davranışı: idempotentlik ve satır sayımı.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b>Neden ayrı bir veritabanı (kendi <c>IClassFixture</c>'ı):</b> bu dosyanın işi
/// <i>gerçekten sahte veri basmak</i>. Panel süitinin paylaşılan konteynerinde koşsaydı
/// 400+ testin altındaki veriyi değiştirir ve "boş liste"/"kesin sayı" iddiası taşıyan
/// başka bir testi <b>koşum sırasına göre</b> kırardı.
/// </para>
/// <para>
/// 🔴 <b>Denetimin "hafifletici" notunun kilidi burada:</b> analiz <c>/Dashboard/Seed</c>'i
/// "veritabanını mahveder" diye okumuştu; ölçüm daha dar bir gerçek gösterdi —
/// <c>MockDataSeeder</c>'ın 20 bloğunun hepsi <c>if (!await db.X.AnyAsync())</c> ile
/// korunuyor, yani <b>dolu bir tabloya dokunmaz</b>. Gerçek risk canlıda <i>henüz boş</i>
/// modüllerdi. O hafifletici bugün <b>bir varsayım</b>: yarın eklenen 21. blok kapıyı
/// yazmayı unutabilir ve <b>hiçbir şey hata vermez</b>. Aşağıdaki ikinci koşu, varsayımı
/// bir ölçüme çevirir.
/// </para>
/// </remarks>
public class MockDataSeederTests : IClassFixture<KadirliApp.Tests.Integration.CustomWebApplicationFactory>
{
    private readonly KadirliApp.Tests.Integration.CustomWebApplicationFactory _factory;

    public MockDataSeederTests(KadirliApp.Tests.Integration.CustomWebApplicationFactory factory)
    {
        _factory = factory;
        // WebApplicationFactory host'u tembel kurar; Seed/migration açılışta koşsun diye
        // bir istemci üretilir (DbSeeder lookup verilerini MockDataSeeder ŞART koşuyor).
        _factory.CreateClient();
    }

    private async Task<T> WithSeederAsync<T>(Func<IMockDataSeeder, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<IMockDataSeeder>());
    }

    /// <summary>
    /// İlk koşu yazar <b>ve ne yazdığını söyler</b>; ikinci koşu <b>hiçbir şeye dokunmaz</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔑 <b>İki koşu neden TEK testte:</b> xUnit aynı sınıfın testlerini paylaşılan
    /// fixture üzerinde <b>belirsiz sırayla</b> çalıştırır. "İlk koşu" ile "ikinci koşu"
    /// ayrı testler olsaydı, "ilk"in gerçekten ilk olduğu <b>hiçbir yerde garanti
    /// edilmezdi</b> — ve garanti edilmediği anda iddia sessizce tersine döner
    /// (ölçüldü: ayrı yazıldığında <c>TheFirstRun</c> boş bir sonuç gördü ve kırıldı).
    /// Sıra iddianın parçasıysa, sıra da testin içinde olmalı.
    /// </para>
    /// <para>
    /// 🔴 İkinci koşunun iddiası iki şeyi birden kilitliyor: <c>MockDataSeeder</c>'ın tablo
    /// bazında idempotent olduğunu <b>ve</b> sayım sarmalayıcısının gerçekten <i>fark</i>
    /// ölçtüğünü (mutlak sayı değil).
    /// </para>
    /// </remarks>
    [Fact]
    public async Task SeedingWritesOnce_AndReportsExactlyWhatItWrote()
    {
        var first = await WithSeederAsync(s => s.SeedAsync());

        first.TotalRows.Should().BeGreaterThan(0, "boş bir veritabanında seeder yazmalı");
        first.Tables.Should().ContainKey("ads");
        first.Tables.Should().ContainKey("death_notices");
        first.Tables.Values.Should().OnlyContain(v => v > 0,
            "rapor yalnız GERÇEKTEN değişen tabloları içermeli — sıfırlar gürültüdür");

        // Kapsam EF modelinden türer, elle tablo listesinden değil.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var modelTables = db.Model.GetEntityTypes()
                .Where(t => !t.IsOwned())
                .Select(t => t.GetTableName())
                .Where(n => n is not null)
                .ToHashSet(StringComparer.Ordinal);

            first.Tables.Keys.Should().OnlyContain(t => modelTables.Contains(t));
            (await db.Ads.CountAsync()).Should().BeGreaterThan(0,
                "seeder gerçekten yazmış olmalı — yoksa aşağıdaki 'ikinci koşu' iddiası vakum olur");
        }

        var second = await WithSeederAsync(s => s.SeedAsync());

        second.TotalRows.Should().Be(0,
            "MockDataSeeder tablo bazında idempotent — dolu bir tabloya İKİNCİ kez yazması, " +
            "canlıda çalıştırılması hâlinde gerçek verinin yanına sahte kayıt koyması demektir");
        second.Tables.Should().BeEmpty();
    }
}
