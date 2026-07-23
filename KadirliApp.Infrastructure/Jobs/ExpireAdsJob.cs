using Hangfire;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Jobs;

public class ExpireAdsJob
{
    private readonly AppDbContext _context;
    private readonly ILogger<ExpireAdsJob> _log;

    public ExpireAdsJob(AppDbContext context, ILogger<ExpireAdsJob> log)
        => (_context, _log) = (context, log);

    // Faz 9.4: tek set-tabanlı UPDATE — atomik ve idempotent (tekrar çalışırsa koşula uyan satır kalmaz).
    // 3 deneme sonrası Fail → iş Hangfire'ın "Failed" kümesinde kalır (dead-letter; dashboard'dan görünür).
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task RunAsync()
    {
        var now = DateTime.UtcNow;
        var affected = await _context.Ads
            .Where(a => a.Status == "approved" && a.ExpiresAt < now)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, "expired"));

        if (affected > 0)
            _log.LogInformation("ExpireAdsJob: {Count} ilan süresi dolduğu için expired yapıldı", affected);
    }
}
