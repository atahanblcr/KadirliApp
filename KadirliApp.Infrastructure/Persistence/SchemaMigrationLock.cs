using Microsoft.Extensions.Logging;
using Npgsql;

namespace KadirliApp.Infrastructure.Persistence;

/// <summary>
/// Faz 12.21b — <b>açılışta koşan şema göçünün ve başlangıç verisinin tek seferde
/// koşmasını garanti eden Postgres advisory kilidi.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Neden gerekli:</b> hem <c>KadirliApp.Api</c> hem <c>KadirliApp.Web</c> açılışta
/// <c>DbSeeder.SeedAsync</c> çağırıyor ve o metot <c>MigrateAsync()</c> ile şemayı göç
/// ettiriyor. Bugün risk yok (iki süreç, elle sırayla başlıyor) ama konteynerleşen bir
/// dağıtımda <b>iki replika aynı anda</b> açılır. İki eşzamanlı <c>Migrate()</c>'in
/// belirtisi bir hata mesajı değil, <b>bozuk bir şemadır</b>: aynı migration iki kez
/// uygulanmaya çalışılır, biri yarıda düşer ve <c>__EFMigrationsHistory</c> ile gerçek
/// şema ayrışır. Seed tarafı da yarışır — iki süreç birden *"süper admin yok"* görüp
/// ikisini birden yazmaya kalkar.
/// </para>
/// <para>
/// 🔑 <b>Neden ADVISORY LOCK, diğer iki seçenek değil.</b> Plan üç seçenek sayıyordu:
/// <list type="bullet">
/// <item><b>(a) tek seferlik <c>migrate</c> job'ı</b> — doğru ama dağıtım hattına bir
/// sıralama borcu yazar: job unutulursa/atlanırsa uygulamalar <b>eski şemayla</b> açılır
/// ve arıza ancak o kolona dokunan ilk istekte görünür. Ayrıca yerel geliştirmede
/// <c>dotnet run</c>'ın bugünkü "çalıştır ve çalışsın" davranışını bozar.</item>
/// <item><b>(b) yalnız Api koşsun</b> — panel Api'den önce açıldığında <b>göç edilmemiş</b>
/// bir şemaya bakar; hata verir ama sebebi hiçbir yerde yazmaz, üstelik bu bir
/// <i>başlatma sırası</i> bağımlılığıdır ve o sıranın korunduğunu hiçbir şey denetlemez.</item>
/// <item><b>(c) advisory lock</b> — kilit <b>veritabanında</b>. Bu projenin §7 madde 60'ta
/// verdiği kararın aynısı: eşzamanlılık kilidi Redis'te olamaz, çünkü Redis burada bilinçli
/// olarak <b>fail-open</b>'dır (§7 madde 36) ve tam yarış anında kilidi açar.</item>
/// </list>
/// </para>
/// <para>
/// 🔑 <b>Ve advisory lock'un burada üçüncü bir üstünlüğü var:</b> 12.13'ün
/// <i>"koruma ile KURTARMA birlikte yazılır"</i> dersi bu kilitte <b>yapısı gereği</b>
/// karşılanıyor. Haber senkronunun kısmi unique indeksi, süreç öldüğünde sonsuza kadar
/// duran bir satır bırakıyordu ve ayrı bir <c>ReapStuckRuns</c> adımı gerekmişti. Advisory
/// lock <b>oturuma bağlıdır</b>: konteyner OOM ile ölürse bağlantı düşer ve Postgres kilidi
/// <b>kendiliğinden</b> bırakır. Yani "takılmış kilit" durumu yoktur.
/// </para>
/// <para>
/// ⚠️ <b>Kilit KENDİ bağlantısında alınır</b>, <c>DbContext</c>'in bağlantısında değil.
/// Sebep: advisory lock <i>oturum</i> kapsamlıdır ve EF havuzdan aldığı bağlantıyı
/// komutlar arasında bırakabilir — kilit o an sessizce düşer ve koruma <b>var görünür,
/// yok olur</b>. Bağlantı iş bitene kadar açık tutulur.
/// </para>
/// <para>
/// ⚠️ <b>Sonsuza kadar beklemez.</b> <c>lock_timeout</c> ile beklemenin bir tavanı var:
/// patolojik bir durumda konteyner <b>gürültüyle düşer</b> (orkestratör yeniden dener),
/// sessizce asılı kalmaz. Tavan bilinçli olarak cömert — ilk kurulumdaki gerçek bir göç
/// dakikalar sürebilir ve o sırada bekleyen replika haklı olarak bekliyordur.
/// </para>
/// </remarks>
public static class SchemaMigrationLock
{
    /// <summary>
    /// Advisory kilidin anahtarı. <b>Bütün host'larda AYNI olmak zorunda</b> — farklı sayı
    /// yazmak "kilit yok" demektir ve bunu hiçbir şey söylemez.
    /// </summary>
    /// <remarks>
    /// Sayı keyfi ama <b>sabit</b>: Postgres advisory kilitleri tek bir global uzayı
    /// paylaşır, yani başka bir uygulamanın aynı sayıyı kullanması teorik olarak mümkündür.
    /// Bu veritabanını yalnız biz kullanıyoruz; çakışma olsaydı belirtisi "açılış bekliyor"
    /// olurdu — sessiz değil.
    /// </remarks>
    public const long AdvisoryKey = 20260816_1221;

    /// <summary>Kilidi beklemenin tavanı. Aşılırsa açılış <b>hata ile</b> durur.</summary>
    public static readonly TimeSpan WaitTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// <paramref name="work"/>'ü, aynı veritabanına bakan bütün süreçler arasında
    /// <b>tek seferde bir tanesi</b> koşacak biçimde çalıştırır.
    /// </summary>
    public static async Task RunExclusivelyAsync(
        string connectionString,
        Func<Task> work,
        ILogger? logger = null,
        CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        // lock_timeout YALNIZ bu oturumu bağlar; havuzdaki diğer bağlantılara sızmaz.
        await ExecuteAsync(connection, $"SET lock_timeout = '{(int)WaitTimeout.TotalMilliseconds}ms'", ct);

        var waited = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await ExecuteAsync(connection, $"SELECT pg_advisory_lock({AdvisoryKey})", ct);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.LockNotAvailable)
        {
            throw new InvalidOperationException(
                $"Şema göçü kilidi {WaitTimeout.TotalMinutes:0} dakikada alınamadı. " +
                "Aynı veritabanına bakan başka bir örnek hâlâ göç ediyor ya da takılmış olabilir. " +
                "Bu bilinçli olarak ÖLÜMCÜLDÜR: kilitsiz devam etmek iki eşzamanlı Migrate() " +
                "demektir ve onun belirtisi bir hata değil, BOZUK BİR ŞEMADIR.", ex);
        }

        if (waited.ElapsedMilliseconds > 1000)
        {
            logger?.LogInformation(
                "Şema göçü kilidi {Seconds:0.0} sn beklendikten sonra alındı — başka bir örnek göç ediyordu.",
                waited.Elapsed.TotalSeconds);
        }

        try
        {
            await work();
        }
        finally
        {
            // ⚠️ Bu satır olmasa bile kilit bağlantı kapanınca düşer (oturum kapsamlı).
            // Yine de açıkça bırakılıyor: bekleyen replikanın boşuna beklediği süre,
            // "connection dispose oldu mu" gibi görünmeyen bir ayrıntıya bağlı kalmasın.
            try
            {
                await ExecuteAsync(connection, $"SELECT pg_advisory_unlock({AdvisoryKey})", CancellationToken.None);
            }
            catch (Exception ex)
            {
                // Bırakma başarısızlığı açılışı düşürmemeli: bağlantı kapanınca kilit
                // zaten gidecek. Sessiz de kalmamalı.
                logger?.LogWarning(ex, "Şema göçü kilidi açıkça bırakılamadı; bağlantı kapanınca düşecek.");
            }
        }
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 0; // bekleme sınırını lock_timeout belirler, komut zaman aşımı değil
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
