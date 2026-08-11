using Hangfire;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Jobs;

/// <summary>
/// Faz 12.13 — senkron koşu defterinin saklama süresi.
///
/// 📊 Ölçek: artımlı iş 15 dakikada bir koşuyor → günde <b>96</b> satır, yılda ~35.000.
/// Saklama işi olmadan bu tablo tek yönlü büyür ve (10.14/(3)'ün <c>uploads</c> için
/// söylediği gibi) sorun ancak yıllar sonra fark edilir. <c>PurgeErrorLogsJob</c> deseni.
///
/// 🔑 <b>Ölçüt <c>StartedAt</c>, <c>CreatedAt</c> değil:</b> ikisi bugün aynı ama koşu
/// kaydının anlamı "ne zaman <i>koştu</i>" — satırın ne zaman <i>eklendiği</i> değil.
/// Ayrım bir gün (koşu önceden planlanırsa) fark edecek.
///
/// ⚠️ <b>Bitmemiş koşu SİLİNMEZ</b>, yaşı ne olursa olsun: <c>completed_at IS NULL</c> satırı
/// silmek, kısmi unique indeksin (eşzamanlılık kilidi) tuttuğu kaydı arkadan kaldırmak
/// demektir — yani kilidin "temizlenme" yolu <b>iki farklı yerde</b> olurdu.
/// O işin tek sahibi <c>NewsSyncService.ReapStuckRunsAsync</c>: kaydı silmez, <b>kapatır</b>.
/// </summary>
public class PurgeNewsSyncRunsJob
{
    public const int RetentionDays = 30;

    private readonly AppDbContext _context;
    private readonly ILogger<PurgeNewsSyncRunsJob> _log;

    public PurgeNewsSyncRunsJob(AppDbContext context, ILogger<PurgeNewsSyncRunsJob> log)
        => (_context, _log) = (context, log);

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task RunAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);

        // Set tabanlı tek DELETE — idempotent: ikinci koşuda koşula uyan satır kalmaz.
        var deleted = await _context.NewsSyncRuns
            .Where(x => x.CompletedAt != null && x.StartedAt < cutoff)
            .ExecuteDeleteAsync();

        if (deleted > 0)
            _log.LogInformation("PurgeNewsSyncRunsJob: {Count} haber senkron koşusu silindi.", deleted);
    }
}
