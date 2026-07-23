using Hangfire;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Jobs;

public class PublishScheduledAnnouncementsJob
{
    private readonly AppDbContext _context;
    private readonly IAnnouncementNotificationGenerator _notificationGenerator;
    private readonly ILogger<PublishScheduledAnnouncementsJob> _log;

    public PublishScheduledAnnouncementsJob(
        AppDbContext context,
        IAnnouncementNotificationGenerator notificationGenerator,
        ILogger<PublishScheduledAnnouncementsJob> log)
        => (_context, _notificationGenerator, _log) = (context, notificationGenerator, log);

    // Faz 9.4 resiliency korunuyor: DisableConcurrentExecution (dakikalık koşularda üst üste binme yok)
    // + 3 deneme sonrası Fail (dead-letter). Faz 10.10 ile set-tabanlı tek UPDATE'ten duyuru başına
    // "önce bildirim üret, sonra yayına al" akışına geçildi. Sıralama bilinçli: arada çökülürse duyuru
    // scheduled kalır ve sonraki koşu devralır; bildirim üretimi related_id işaretiyle idempotent
    // olduğundan mükerrer satır oluşmaz.
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 55)]
    public async Task RunAsync()
    {
        var now = DateTime.UtcNow;
        var due = await _context.Announcements
            .Where(a => a.Status == "scheduled" && a.ScheduledFor != null && a.ScheduledFor <= now)
            .ToListAsync();
        if (due.Count == 0) return;

        var notified = 0;
        foreach (var announcement in due)
        {
            notified += await _notificationGenerator.GenerateForAnnouncementAsync(announcement);
            announcement.Status = "active";
            announcement.SentAt = now;
        }
        await _context.SaveChangesAsync();

        _log.LogInformation(
            "PublishScheduledAnnouncementsJob: {Count} zamanlanmış duyuru yayınlandı, {Notified} bildirim üretildi",
            due.Count, notified);
    }
}
