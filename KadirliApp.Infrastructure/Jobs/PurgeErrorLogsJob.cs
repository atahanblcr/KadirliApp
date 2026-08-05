using Hangfire;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Jobs;

/// <summary>
/// Faz 12.1 — hata kayıtlarının saklama süresi.
///
/// Tekilleştirme satır sayısını çok düşürüyor ama <b>sıfırlamıyor</b>: her yeni hata
/// şekli yeni bir satır. Saklama işi olmadan <c>error_logs</c> tek yönlü büyür ve
/// (10.14/(3)'ün <c>uploads</c> için söylediği gibi) sorun ancak yıllar sonra fark edilir.
///
/// Süreler bilinçli asimetrik: **çözülmüş** kayıt 30 gün (işi bitmiş, tarihçesi denetim
/// izinde zaten var), **çözülmemiş** kayıt 90 gün (hâlâ birinin bakması gereken şey —
/// erken silmek "hata kendiliğinden kayboldu" yanılsaması üretir).
///
/// ⚠️ Ölçüt <c>LastSeenAt</c>, <c>CreatedAt</c> DEĞİL: bir yıl önce açılıp bugün hâlâ
/// tekrar eden kayıt <c>CreatedAt</c>'e bakılsaydı **hâlâ patlarken** silinirdi.
/// </summary>
public class PurgeErrorLogsJob
{
    public const int ResolvedRetentionDays = 30;
    public const int UnresolvedRetentionDays = 90;

    private readonly AppDbContext _context;
    private readonly ILogger<PurgeErrorLogsJob> _log;

    public PurgeErrorLogsJob(AppDbContext context, ILogger<PurgeErrorLogsJob> log)
        => (_context, _log) = (context, log);

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task RunAsync()
    {
        var now = DateTime.UtcNow;
        var resolvedCutoff = now.AddDays(-ResolvedRetentionDays);
        var unresolvedCutoff = now.AddDays(-UnresolvedRetentionDays);

        // Set tabanlı tek DELETE — idempotent: ikinci koşuda koşula uyan satır kalmaz.
        var resolved = await _context.ErrorLogs
            .Where(x => x.IsResolved && x.LastSeenAt < resolvedCutoff)
            .ExecuteDeleteAsync();

        var unresolved = await _context.ErrorLogs
            .Where(x => !x.IsResolved && x.LastSeenAt < unresolvedCutoff)
            .ExecuteDeleteAsync();

        if (resolved + unresolved > 0)
        {
            _log.LogInformation(
                "PurgeErrorLogsJob: {Resolved} çözülmüş, {Unresolved} çözülmemiş hata kaydı silindi.",
                resolved, unresolved);
        }
    }
}
