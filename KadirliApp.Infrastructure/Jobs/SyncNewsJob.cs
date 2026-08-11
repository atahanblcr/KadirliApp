using Hangfire;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Jobs;

/// <summary>
/// Faz 12.12 — <b>artımlı haber senkronu</b> (15 dakikada bir).
///
/// 📊 Sıklığın gerekçesi ölçüm: kaynak günde <b>~5</b> yeni/güncel haber üretiyor. 15 dakika
/// fazlasıyla yeter; daha sık koşmak kaynağa gereksiz yük, daha seyrek koşmak "akşam çıkan
/// haber sabah görünür" demek olurdu.
///
/// ⚠️ İş <b>hiçbir şeyi kendisi bilmez</b>: bütün mantık <see cref="INewsSyncService"/>'te.
/// İkinci bir alım gerçeklemesi (ör. burada "hızlıca bir istek atalım") yazılırsa iki yol
/// farklı kayıt üretir ve ikisi de hata vermez.
/// </summary>
public class SyncNewsJob
{
    private readonly INewsSyncService _sync;
    private readonly ILogger<SyncNewsJob> _log;

    public SyncNewsJob(INewsSyncService sync, ILogger<SyncNewsJob> log) => (_sync, _log) = (sync, log);

    /// <remarks>
    /// <c>DisableConcurrentExecution</c> şart: iki koşu aynı anda aynı imleçten başlarsa aynı
    /// haberleri iki kez işler (upsert idempotent olduğu için veri bozulmaz) ama görselleri
    /// iki kez indirir ve koşu defteri okunamaz hâle gelir.
    /// ⚠️ <c>AutomaticRetry = 0</c> bilinçli: kısmi hatalar zaten <c>Failed</c> sayacına
    /// yazılıyor ve <b>15 dakika sonra</b> yeni bir koşu geliyor. Hangfire'ın kendi yeniden
    /// denemesi, kaynak çökmüşken üstüne yığılırdı.
    /// </remarks>
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public async Task RunAsync()
    {
        var outcome = await _sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None);

        // 🔑 "Atlandı" ile "tamamlanamadı" ayrı loglanır (12.13): elle tetiklenmiş bir koşu
        // sürerken zamanlanmış koşu kilide takılır ve bu **korumanın çalışması**dır. Uyarı
        // olarak yazılsaydı gerçek arızaları arayan insan, olağan bir olayı hata sanardı.
        if (outcome.Blocked)
            _log.LogInformation("Zamanlanmış haber senkronu atlandı: {Error}", outcome.ErrorMessage);
        else if (!outcome.Succeeded)
            _log.LogWarning("Haber senkronu tamamlanamadı: {Error}", outcome.ErrorMessage);
    }
}
