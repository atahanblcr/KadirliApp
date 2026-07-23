using Hangfire;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Jobs;

public class ArchiveDeathsJob
{
    private readonly AppDbContext _context;
    private readonly ILogger<ArchiveDeathsJob> _log;

    public ArchiveDeathsJob(AppDbContext context, ILogger<ArchiveDeathsJob> log)
        => (_context, _log) = (context, log);

    // Faz 9.4: tek set-tabanlı UPDATE — atomik ve idempotent (tekrar çalışırsa koşula uyan satır kalmaz).
    // 3 deneme sonrası Fail → iş Hangfire'ın "Failed" kümesinde kalır (dead-letter; dashboard'dan görünür).
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task RunAsync()
    {
        var now = DateTime.UtcNow;
        var affected = await _context.DeathNotices
            .Where(d => d.Status == "approved" && d.AutoArchiveAt < now)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.Status, "archived"));

        if (affected > 0)
            _log.LogInformation("ArchiveDeathsJob: {Count} vefat ilanı arşivlendi", affected);
    }
}
