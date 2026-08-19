using FluentAssertions;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Architecture;

/// <summary>
/// Faz 12.22b — görünmez sözleşme <b>#84</b>: <b>her trigram indeksi
/// <c>lower(...)</c> üzerinde olmak ZORUNDA.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Bu testin var olma sebebi ölçülmüş bir hata:</b> Haziran 2026'da konan iki GIN
/// indeksi (<c>ix_ads_title_trgm</c> · <c>ix_places_name_trgm</c>) <b>ham kolon</b>
/// üzerineydi, oysa projedeki <b>her</b> arama <c>x.Kolon.ToLower().Contains(...)</c>
/// yazıyor → Postgres'e <c>lower(kolon) LIKE '%…%'</c> gidiyor. İfade indeksinde ifade
/// <b>birebir</b> eşleşmek zorundadır; <c>title</c> ≠ <c>lower(title)</c>.
/// </para>
/// <para>
/// 🔑 <b>Hasarın biçimi tam olarak bu projenin "sessiz hasar" tanımı:</b> indeks vardı,
/// yer kaplıyordu, her yazmada güncelleniyordu — ve <b>hiç kullanılmıyordu.</b> Ne hata,
/// ne uyarı, ne log. Üstelik "indeks var mı?" sorusunun cevabı <i>"var"</i> olduğu için
/// arama yavaşladığında bakılacak ilk yer <b>yanlış cevap veriyordu</b>. 20.005 satırda
/// ölçüldü: <c>Seq Scan</c>, 29,2 ms → düzeltmeden sonra <c>BitmapOr</c>, 0,75 ms (39×).
/// </para>
/// <para>
/// 🔑 <b>KAPSAM VERİTABANINDAN TÜRER, kaynak taramasından değil.</b> Bu bilinçli ve Faz A
/// denetiminin dersinin (<i>"kapsam dizinden mi, tipten mi, elden mi?"</i>) doğrudan
/// uygulanışı: bir migration'ı okuyan tarama, indeksi <b>başka bir yoldan</b> ekleyen
/// kimseyi yakalayamazdı (elle SQL, ikinci bir migration, seed betiği). <c>pg_indexes</c>'e
/// sormak, indeksin <b>nasıl</b> doğduğunu umursamaz — yalnız <b>ne olduğunu</b> sorar.
/// </para>
/// <para>
/// ⚠️ Bu test <b>indeks eksikliğini yakalamaz</b> (14 sorgu daha trigram indeksi olmadan
/// <c>lower(…) LIKE</c> yapıyor) ve bunu kendisi yazıyor. Yakaladığı şey dar ama gerçek:
/// <i>"var olan bir indeksin ölü olması."</i> Eksik indeks bir <b>ölçüm</b> kararıdır
/// (bkz. <c>Memory_Bank/Performance_Baseline.md</c>), ölü indeks ise bir <b>hatadır</b>.
/// </para>
/// </remarks>
public class TrigramIndexTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TrigramIndexTests(CustomWebApplicationFactory factory) => _factory = factory;

    private sealed record TrigramIndex(string Name, string Definition);

    private async Task<List<TrigramIndex>> TrigramIndexesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var rows = new List<TrigramIndex>();
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "select indexname, indexdef from pg_indexes " +
                "where schemaname = 'public' and indexdef like '%gin_trgm_ops%' order by indexname";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                rows.Add(new TrigramIndex(reader.GetString(0), reader.GetString(1)));
        }
        finally
        {
            await conn.CloseAsync();
        }

        return rows;
    }

    /// <summary>
    /// 🔴 <b>Ters yön ve olmazsa hepsi anlamsız.</b> Aşağıdaki iddia boş küme üzerinde de
    /// <b>yeşil kalır</b>: hiç trigram indeksi bulunamazsa "hepsi doğru" denir. O hâlde
    /// bu test, indeksler tamamen kaybolduğunda bile bir şey söylemezdi.
    /// </summary>
    [Fact]
    public async Task TheDatabase_ActuallyHasTrigramIndexes()
    {
        var indexes = await TrigramIndexesAsync();

        indexes.Should().NotBeEmpty(
            "hiç trigram indeksi bulunamadı. Ya migration'lar uygulanmadı ya da " +
            "12.13/12.22b'nin indeksleri düşürüldü — ikisi de aramayı TAM TARAMAYA " +
            "döndürür ve bunu başka hiçbir test söylemez");
    }

    /// <summary>
    /// Asıl iddia: indeksin ifadesi sorgunun ifadesiyle eşleşmeli.
    /// </summary>
    [Fact]
    public async Task EveryTrigramIndex_IsBuiltOnALoweredExpression()
    {
        var dead = (await TrigramIndexesAsync())
            .Where(i => !i.Definition.Contains("lower(", StringComparison.OrdinalIgnoreCase))
            .ToList();

        dead.Should().BeEmpty(
            "ham kolon üzerine kurulmuş bir trigram indeksi ÖLÜDÜR: projedeki her arama " +
            "`kolon.ToLower().Contains(...)` yazıyor, yani Postgres'e `lower(kolon) LIKE ...` " +
            "gidiyor ve ifade indeksinde ifade BİREBİR eşleşmek zorunda. Ölü indeks yer " +
            "kaplar, her yazmada güncellenir, hiç kullanılmaz — ve 'indeks var mı?' " +
            "sorusuna YANLIŞ bir 'var' cevabı verir. Ölü indeksler: {0}",
            string.Join(", ", dead.Select(d => $"{d.Name} → {d.Definition}")));
    }

    /// <summary>
    /// 🔑 <b>Premisin kendisi de kilitli.</b> Yukarıdaki kuralın tamamı tek bir varsayıma
    /// dayanıyor: <i>"aramalar küçük harfe çeviriyor."</i> O varsayım bir gün değişirse
    /// (ör. <c>ILIKE</c>'a geçilirse) kural yanlış hâle gelir ama <b>yeşil kalır</b> —
    /// yani doğru şeyi ölçen ama artık geçersiz bir kilit olur.
    /// </summary>
    [Fact]
    public void TheSearchQueries_StillLowercaseTheirColumns()
    {
        var featuresRoot = Path.Combine(RepositoryRoot(), "KadirliApp.Application", "Features");

        var lowered = Directory
            .EnumerateFiles(featuresRoot, "*.cs", SearchOption.AllDirectories)
            .Count(f => File.ReadAllText(f).Contains(".ToLower().Contains(", StringComparison.Ordinal));

        lowered.Should().BeGreaterThan(5,
            "bu dosyadaki kuralın premisi 'aramalar kolonu küçük harfe çeviriyor'. " +
            "Premis çöktüyse kural da geçersizdir ve bunu söyleyen tek yer burasıdır");
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "KadirliApp.sln")))
            dir = dir.Parent;

        dir.Should().NotBeNull("testler çözüm kökünün altından koşmalı");
        return dir!.FullName;
    }
}
