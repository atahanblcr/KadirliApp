using Hangfire;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Jobs;

/// <summary>
/// Faz 12.2 — giriş denemelerinin saklama süresi.
///
/// <c>login_attempts</c> tekilleştirilmiyor (<c>error_logs</c>'un aksine): her deneme bir
/// satır, çünkü "kaç kez" değil "ne zaman, nereden" sorusunu cevaplıyor. Yani tablo
/// <b>tek yönlü ve hızlı</b> büyür — saklama işi olmadan projenin en büyük tablosu olur.
///
/// Süreler bilinçli asimetrik: <b>başarılı</b> giriş 90 gün (rutin bilgi), <b>başarısız</b>
/// deneme 180 gün (saldırı geçmişi; bir kampanya aylar sonra tekrar edebilir ve "bu IP'yi
/// daha önce görmüş müydük" sorusunun cevabı o zaman değerli olur).
///
/// ⚠️ <b>Panoya erişen biri bu işi elle tetikleyerek güvenlik kanıtını silebilir</b> —
/// <c>/hangfire</c> korumasının bu fazda gözden geçirilmesi tesadüf değil
/// (<c>HangfireDashboardAuthorizationFilter</c> + <c>ProductionReadinessGuard</c>).
/// </summary>
public class PurgeLoginAttemptsJob
{
    public const int SucceededRetentionDays = 90;
    public const int FailedRetentionDays = 180;

    private readonly AppDbContext _context;
    private readonly ILogger<PurgeLoginAttemptsJob> _log;

    public PurgeLoginAttemptsJob(AppDbContext context, ILogger<PurgeLoginAttemptsJob> log)
        => (_context, _log) = (context, log);

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task RunAsync()
    {
        var now = DateTime.UtcNow;
        var succeededCutoff = now.AddDays(-SucceededRetentionDays);
        var failedCutoff = now.AddDays(-FailedRetentionDays);

        // Set tabanlı tek DELETE — idempotent: ikinci koşuda koşula uyan satır kalmaz.
        var succeeded = await _context.LoginAttempts
            .Where(x => x.Succeeded && x.CreatedAt < succeededCutoff)
            .ExecuteDeleteAsync();

        var failed = await _context.LoginAttempts
            .Where(x => !x.Succeeded && x.CreatedAt < failedCutoff)
            .ExecuteDeleteAsync();

        if (succeeded + failed > 0)
        {
            _log.LogInformation(
                "PurgeLoginAttemptsJob: {Succeeded} başarılı, {Failed} başarısız giriş denemesi silindi.",
                succeeded, failed);
        }
    }
}
