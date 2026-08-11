using Hangfire;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Jobs;

/// <summary>
/// Faz 12.12 — <b>mutabakat</b> (gecelik 03:00): kaynakta artık olmayan haberleri işaretler.
///
/// 🔴 <b>Bu işin var olma sebebi tek cümle: <c>modified_after</c> SİLMEYİ HİÇ BİLDİRMEZ.</b>
/// Artımlı senkron yalnız <i>değişenleri</i> görür; kaldırılan bir haber hiçbir akışta
/// görünmez. Bu iş olmadan WordPress'te yayından kaldırılan bir haber uygulamada
/// <b>sonsuza kadar</b> durur — uçlar 200 döner, kimse hata almaz.
///
/// 🔑 Kayıt <b>silinmez</b>, <c>SourceState = "gone"</c> olur: silinseydi
/// <i>"haber neden gitti?"</i> sorusunun cevabı hiçbir yerde olmazdı. Ters yön de var —
/// kaynağa geri dönen haber yeniden yayına girer (idempotent).
///
/// ⚠️ Tarama penceresi <b>arşiv derinliğimizle aynı</b> olmak zorunda: 50 haber çekiyorsak
/// 27 bin kimlik taramak anlamsız, üstelik tehlikeli — pencere derinlikten genişse
/// "bizde yok" ile "kaynakta yok" karışır ve <b>her eski haber <c>gone</c> işaretlenir</b>.
/// </summary>
public class ReconcileNewsJob
{
    private readonly INewsSyncService _sync;
    private readonly ILogger<ReconcileNewsJob> _log;

    public ReconcileNewsJob(INewsSyncService sync, ILogger<ReconcileNewsJob> log) => (_sync, _log) = (sync, log);

    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 900)]
    public async Task RunAsync()
    {
        var outcome = await _sync.ReconcileAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None);

        if (outcome.MarkedGone + outcome.Restored > 0)
            _log.LogInformation(
                "Haber mutabakatı: {Gone} kayıt kaynakta yok, {Restored} kayıt geri döndü.",
                outcome.MarkedGone, outcome.Restored);
    }
}
