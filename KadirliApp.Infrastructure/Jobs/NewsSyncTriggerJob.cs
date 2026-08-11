using Hangfire;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.News.Commands;
using KadirliApp.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Jobs;

/// <summary>
/// Faz 12.13 — panelden <b>elle</b> tetiklenen senkron koşusu.
/// </summary>
/// <remarks>
/// 🔑 <b>Neden ayrı bir iş sınıfı:</b> zamanlanmış <c>SyncNewsJob</c>
/// <c>[DisableConcurrentExecution]</c> taşıyor ve o öznitelik <b>yalnız kendi işini</b>
/// serileştirir. Elle tetikleme aynı sınıfa bindirilseydi, zamanlanmış koşu sürerken
/// basılan buton Hangfire kuyruğunda <b>bekler</b> ve yönetici "hiçbir şey olmadı" derdi.
/// Ayrı iş + veritabanı kilidi (kısmi unique indeks) daha dürüst: koşu ya hemen başlar
/// ya da <b>sebebiyle</b> reddedilir.
///
/// ⚠️ İş <b>hiçbir mantık taşımaz</b>: alımın tek sahibi <see cref="INewsSyncService"/>.
/// ⚠️ <c>AutomaticRetry = 0</c>: kilide takılan bir koşuyu yeniden denemek, tam da
/// kilidin engellediği şeyi biraz sonra tekrar denemektir.
/// </remarks>
public class NewsSyncTriggerJob
{
    private readonly INewsSyncService _sync;
    private readonly ILogger<NewsSyncTriggerJob> _log;

    public NewsSyncTriggerJob(INewsSyncService sync, ILogger<NewsSyncTriggerJob> log)
        => (_sync, _log) = (sync, log);

    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync(NewsSyncRequestMode mode, Guid? adminId)
    {
        var outcome = mode switch
        {
            NewsSyncRequestMode.Archive =>
                await _sync.RunArchiveBackfillAsync(NewsSyncTriggers.Manual, adminId, CancellationToken.None),
            NewsSyncRequestMode.Reconcile =>
                await _sync.ReconcileAsync(NewsSyncTriggers.Manual, adminId, CancellationToken.None),
            _ =>
                await _sync.RunIncrementalAsync(NewsSyncTriggers.Manual, adminId, CancellationToken.None)
        };

        if (outcome.Blocked)
        {
            // Hata DEĞİL: koruma çalıştı. Uyarı olarak loglamak, gerçek arızaları arayan
            // insanı gürültüye boğardı.
            _log.LogInformation("Elle tetiklenen haber senkronu atlandı: {Error}", outcome.ErrorMessage);
            return;
        }

        if (!outcome.Succeeded)
            _log.LogWarning("Elle tetiklenen haber senkronu tamamlanamadı: {Error}", outcome.ErrorMessage);
    }
}

/// <summary>Faz 12.13 — <see cref="INewsSyncQueue"/>'nun Hangfire gerçeklemesi.</summary>
public class HangfireNewsSyncQueue : INewsSyncQueue
{
    private readonly IBackgroundJobClient _jobs;

    public HangfireNewsSyncQueue(IBackgroundJobClient jobs) => _jobs = jobs;

    public string Enqueue(NewsSyncRequestMode mode, Guid? adminId)
        => _jobs.Enqueue<NewsSyncTriggerJob>(job => job.RunAsync(mode, adminId));
}
