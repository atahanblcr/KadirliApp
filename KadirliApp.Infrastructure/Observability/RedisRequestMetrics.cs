using System.Collections.Concurrent;
using KadirliApp.Application.Common.Performance;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace KadirliApp.Infrastructure.Observability;

/// <summary>
/// Faz 12.22a — istek ölçümlerinin <b>süreçler arası</b> toplandığı yer.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Bu sınıfın var olma sebebi tek bir gerçek:</b> API ve panel <b>ayrı süreçlerdir</b>.
/// Ölçüm süreç belleğinde kalsaydı panelin gösterdiği tablo yalnız <i>panelin kendi</i>
/// handler'larını sayardı — ve ekran, doğru görünen ama yanlış bir p95 basardı. Yanlış
/// sayı basan bir ölçüm ekranı, ölçüm olmamasından <b>kötüdür</b>: ilkinde kimse bilmez,
/// ikincisinde herkes yanlış bilir.
/// </para>
/// <para>
/// 🔑 <b>Yazma sıcak yolda, ağ DEĞİL.</b> <see cref="Record"/> yalnız süreç içi histogramı
/// günceller (kilitsiz). Redis'e yazma <see cref="RequestMetricsFlushService"/> tarafından
/// periyodik ve <b>mutlak</b> yapılır: her süreç yalnız <i>kendi</i> alanına, o ana kadarki
/// toplamını yazar. Fark (delta) göndermek daha az veri taşırdı ama kayıp bir turu telafi
/// edilemez hâle getirirdi; mutlak yazımda kaçan tur bir sonrakinde kendiliğinden kapanır.
/// </para>
/// <para>
/// ⚠️ <b>Süreç yeniden başlarsa kendi sayaçları sıfırlanır</b> ve toplam düşer. Bu bilinçli:
/// alternatif, Redis'te sonsuza kadar büyüyen ve hiçbir zaman temizlenmeyen bir toplam
/// tutmaktı. Örnek kaydı 10 dakikalık TTL taşır — ölen bir süreç ölçümden kendiliğinden
/// çekilir, kimsenin temizlik işi yazması gerekmez (12.13'ün "yalnız kilit, kurtarması
/// olmayan kalıcı kilide dönüşür" dersinin tersten uygulanışı).
/// </para>
/// <para>
/// 🔴 <b>Fail-open.</b> Redis erişilemezse ölçüm <b>süreç içinde toplanmaya devam eder</b>
/// ve okuma <c>Degraded</c> bayrağıyla döner. Gözlem altyapısı, gözlediği uygulamayı
/// asla düşürmez (<c>CachingBehavior</c>'ın aynı kararı).
/// </para>
/// </remarks>
public sealed class RedisRequestMetrics : IRequestMetricsRecorder, IRequestMetricsReader
{
    private const string KeyPrefix = "perf:v1:";
    private const string InstanceSetKey = KeyPrefix + "instances";
    private static readonly TimeSpan InstanceTtl = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 🔴 Farklı handler adı sayısının tavanı. Handler adları <b>tipten</b> gelir, yani
    /// küme kapalıdır ve bugün ~200'dür; tavan yine de var, çünkü sınırsız anahtar kabul
    /// eden bir ölçüm deposu <b>bellek sızıntısıdır</b> ve sızıntıyı ölçüm yapar.
    /// </summary>
    private const int MaxTrackedHandlers = 1000;

    private readonly ConcurrentDictionary<string, RequestHistogram> _local = new(StringComparer.Ordinal);
    private readonly IConnectionMultiplexer _redis;
    private readonly IOptions<PerformanceSettings> _settings;
    private readonly ILogger<RedisRequestMetrics> _log;

    /// <summary>Bu sürecin Redis'teki alanı — ör. <c>KadirliApp.Api#3f2a1b8c</c>.</summary>
    public string InstanceKey { get; }

    public RedisRequestMetrics(
        IConnectionMultiplexer redis,
        IHostEnvironment host,
        IOptions<PerformanceSettings> settings,
        ILogger<RedisRequestMetrics> log)
    {
        _redis = redis;
        _settings = settings;
        _log = log;
        InstanceKey = $"{host.ApplicationName}#{Guid.NewGuid().ToString("N")[..8]}";
    }

    // ── Yazma ─────────────────────────────────────────────────────────────────

    public void Record(string handler, double elapsedMs, bool failed, bool slow)
    {
        if (!_local.TryGetValue(handler, out var histogram))
        {
            if (_local.Count >= MaxTrackedHandlers) return;
            histogram = _local.GetOrAdd(handler, _ => new RequestHistogram());
        }

        histogram.Add(elapsedMs, failed, slow);
    }

    /// <summary>Süreç içi toplamları Redis'e yazar (mutlak). Hatada <b>fırlatmaz</b>.</summary>
    internal async Task FlushAsync(CancellationToken ct)
    {
        if (_local.IsEmpty) return;

        try
        {
            var db = _redis.GetDatabase();
            var entries = _local
                .Select(kv => new HashEntry(kv.Key, kv.Value.Serialize()))
                .ToArray();

            var key = (RedisKey)(KeyPrefix + InstanceKey);
            await db.HashSetAsync(key, entries);
            await db.KeyExpireAsync(key, InstanceTtl);
            await db.SetAddAsync(InstanceSetKey, InstanceKey);
            await db.KeyExpireAsync(InstanceSetKey, InstanceTtl);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "İstek ölçümleri Redis'e yazılamadı; süreç içi toplama sürüyor.");
        }
    }

    // ── Okuma ─────────────────────────────────────────────────────────────────

    public async Task<RequestMetricsSnapshot> ReadAsync(CancellationToken ct = default)
    {
        var threshold = _settings.Value.SlowRequestThresholdMs;
        var merged = new Dictionary<string, RequestHistogram>(StringComparer.Ordinal);
        var sources = new SortedSet<string>(StringComparer.Ordinal);
        var degraded = false;
        var rejected = 0;

        // 1) Kendi sürecimiz — her zaman EN TAZE kaynak. Redis kopyası en fazla bir tur
        //    eskidir; ikisini birden saymak sayıları ikiye katlardı, bu yüzden aşağıda
        //    kendi örnek anahtarımız atlanır.
        foreach (var (handler, histogram) in _local)
            MergeInto(merged, handler, histogram);

        if (!_local.IsEmpty) sources.Add(InstanceKey);

        // 2) Diğer süreçler.
        try
        {
            var db = _redis.GetDatabase();
            foreach (var member in await db.SetMembersAsync(InstanceSetKey))
            {
                var instance = member.ToString();
                if (instance == InstanceKey) continue;

                var hash = await db.HashGetAllAsync(KeyPrefix + instance);
                if (hash.Length == 0)
                {
                    // Örnek öldü ve TTL'i doldu — üyeliği burada temizlenir. Kendi kendini
                    // onaran kayıt: ayrı bir bakım işi gerekmiyor.
                    await db.SetRemoveAsync(InstanceSetKey, instance);
                    continue;
                }

                sources.Add(instance);
                foreach (var entry in hash)
                {
                    var histogram = RequestHistogram.TryParse(entry.Value.ToString());
                    if (histogram is not null)
                        MergeInto(merged, entry.Name.ToString(), histogram);
                    else
                        rejected++;
                }
            }
        }
        catch (Exception ex)
        {
            degraded = true;
            _log.LogWarning(ex, "İstek ölçümleri okunamadı; yalnız bu sürecin sayaçları gösteriliyor.");
        }

        // 🔴 Canlı doğrulamada bulundu ve sessizdi: iki süreç FARKLI kova sürümleriyle
        // koşarsa (biri dağıtıldı, diğeri henüz değil) TryParse karşı tarafın bütün
        // kayıtlarını reddeder — tablo eksilir ama "eksik" olduğunu SÖYLEMEZ. Reddetmek
        // doğru karardı (bayat sayıları yanlış kovalara dağıtmak veriyi kaybetmekten
        // kötüdür); yanlış olan, reddi sessizce yapmaktı.
        if (rejected > 0)
        {
            degraded = true;
            _log.LogWarning(
                "{Count} ölçüm kaydı tanınmayan biçimde olduğu için yok sayıldı — " +
                "süreçler farklı sürümlerle koşuyor olabilir (kova tanımı değişti mi?).",
                rejected);
        }

        var handlers = merged
            .Select(kv => new HandlerMetrics(
                Handler: kv.Key,
                Kind: RequestKind.FromName(kv.Key),
                Count: kv.Value.Count,
                Failures: kv.Value.Failures,
                SlowCount: kv.Value.SlowCount,
                AverageMs: kv.Value.AverageMs,
                P50Ms: kv.Value.Percentile(0.50),
                P95Ms: kv.Value.Percentile(0.95),
                P99Ms: kv.Value.Percentile(0.99),
                MaxMs: kv.Value.MaxMs))
            .OrderByDescending(m => m.P95Ms)
            .ThenByDescending(m => m.Count)
            .ThenBy(m => m.Handler, StringComparer.Ordinal)
            .ToList();

        return new RequestMetricsSnapshot(handlers, sources.ToList(), degraded, threshold);
    }

    public async Task ResetAsync(CancellationToken ct = default)
    {
        _local.Clear();

        var db = _redis.GetDatabase();
        foreach (var member in await db.SetMembersAsync(InstanceSetKey))
            await db.KeyDeleteAsync(KeyPrefix + member.ToString());

        await db.KeyDeleteAsync(InstanceSetKey);
    }

    private static void MergeInto(Dictionary<string, RequestHistogram> target, string handler, RequestHistogram source)
    {
        if (!target.TryGetValue(handler, out var existing))
            target[handler] = existing = new RequestHistogram();

        existing.Merge(source);
    }
}

/// <summary>
/// Faz 12.22a — süreç içi ölçümleri belirli aralıklarla Redis'e taşıyan arka plan işi.
/// </summary>
/// <remarks>
/// 🔑 Ayrı bir servis, çünkü <b>yazma sıcak yolda ağa çıkmamalı</b>: her isteğin sonunda
/// bir Redis çağrısı yapan bir ölçüm, ölçtüğü gecikmeyi kendisi üretirdi
/// (<c>ChannelErrorLogSink</c>'in "isteği bloklamayan yazıcı" kararının aynısı).
/// </remarks>
public sealed class RequestMetricsFlushService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    private readonly RedisRequestMetrics _metrics;

    public RequestMetricsFlushService(RedisRequestMetrics metrics) => _metrics = metrics;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
                await _metrics.FlushAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        // Kapanışta son bir tur: süreç düzgün kapanıyorsa son 15 saniyelik ölçüm kaybolmasın.
        await _metrics.FlushAsync(CancellationToken.None);
    }
}
