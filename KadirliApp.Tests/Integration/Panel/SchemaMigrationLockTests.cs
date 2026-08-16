extern alias WebPanel;

using FluentAssertions;
using KadirliApp.Infrastructure.Persistence;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 12.21b — <b>açılıştaki şema göçünün ve seed'in aynı anda İKİ KEZ koşamayacağı.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Neden gerçek Postgres şart:</b> kilidin kendisi bir <c>if</c> değil, veritabanının
/// bir özelliği (<c>pg_advisory_lock</c>). Sahte bir bağlantıyla yazılmış bir test, kilit
/// hiç çalışmasa da yeşil kalırdı — yani tam olarak bu projenin *"iddiası zayıf test"*
/// dediği şey olurdu.
/// </para>
/// <para>
/// ⚠️ İddia <b>eşzamanlılığın kendisini</b> ölçüyor, bir bayrağı değil: iki iş birlikte
/// başlatılıyor ve <b>kesişmedikleri</b> gösteriliyor. "Kilit alındı mı" diye sormak
/// yetmezdi — kilit alınıp <i>bırakılmasa</i> da o soru yeşil dönerdi.
/// </para>
/// </remarks>
[Collection(PanelCollection.Name)]
public class SchemaMigrationLockTests
{
    private readonly WebPanelApplicationFactory _factory;

    public SchemaMigrationLockTests(WebPanelApplicationFactory factory) => _factory = factory;

    /// <summary>
    /// İki "replika" aynı anda açılırsa göç/seed <b>sırayla</b> koşmalı.
    /// </summary>
    [Fact]
    public async Task TwoStartupsAtOnce_DoNotOverlap()
    {
        var conn = _factory.PostgresConnectionString;
        var log = new List<string>();
        var gate = new object();

        async Task ReplicaAsync(string name)
        {
            await SchemaMigrationLock.RunExclusivelyAsync(conn, async () =>
            {
                lock (gate) log.Add($"{name}-basladi");
                await Task.Delay(300);
                lock (gate) log.Add($"{name}-bitti");
            });
        }

        await Task.WhenAll(ReplicaAsync("A"), ReplicaAsync("B"));

        // İç içe geçmiş bir çalıştırma "A-basladi, B-basladi, …" üretirdi.
        log.Should().HaveCount(4);
        log[1].Should().Be(log[0].Replace("-basladi", "-bitti"),
            "bir replikanın işi bitmeden diğeri başlayamamalı — iki eşzamanlı Migrate() " +
            "bozuk bir şema bırakır ve belirtisi bir hata mesajı DEĞİLDİR");
        log[3].Should().Be(log[2].Replace("-basladi", "-bitti"));
    }

    /// <summary>
    /// Ters yön — <b>ve bu yön olmadan yukarıdaki iddia zayıftır</b> (§7 madde 68'in dersi):
    /// kilit alınıp <b>hiç bırakılmasaydı</b> birinci test yine yeşil kalırdı (işler yine
    /// sırayla koşardı — ikincisi zaman aşımına düşene kadar). Bu test kilidin
    /// <b>bırakıldığını</b> gösteriyor: art arda iki çalıştırma beklemeden geçmeli.
    /// </summary>
    [Fact]
    public async Task TheLockIsReleased_SoTheNextStartupIsNotBlocked()
    {
        var conn = _factory.PostgresConnectionString;

        await SchemaMigrationLock.RunExclusivelyAsync(conn, () => Task.CompletedTask);

        var second = System.Diagnostics.Stopwatch.StartNew();
        await SchemaMigrationLock.RunExclusivelyAsync(conn, () => Task.CompletedTask);
        second.Stop();

        second.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5),
            "kilit bırakılmadıysa ikinci açılış lock_timeout dolana kadar bekler ve " +
            "konteyner dakikalarca 'başlatılıyor' görünür");
    }

    /// <summary>
    /// İş <b>fırlatsa bile</b> kilit bırakılmalı. Bırakılmasaydı bir kez başarısız olan
    /// göç, sonraki bütün açılışları da bloklardı — yani koruma, önlemeye çalıştığı
    /// arızanın sebebine dönerdi (12.13'ün *"kurtarma da yazılır"* dersi).
    /// </summary>
    [Fact]
    public async Task AFailedStartup_StillReleasesTheLock()
    {
        var conn = _factory.PostgresConnectionString;

        var boom = async () => await SchemaMigrationLock.RunExclusivelyAsync(
            conn, () => throw new InvalidOperationException("göç düştü"));

        await boom.Should().ThrowAsync<InvalidOperationException>();

        var after = System.Diagnostics.Stopwatch.StartNew();
        await SchemaMigrationLock.RunExclusivelyAsync(conn, () => Task.CompletedTask);
        after.Stop();

        after.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5),
            "düşen bir göç kilidi elinde tutarsa sonraki her açılış bloklanır");
    }
}
