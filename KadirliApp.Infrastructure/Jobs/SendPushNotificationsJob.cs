using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Jobs;

/// <summary>
/// Faz 10.11: fcm_sent=false bildirimleri FCM'e batch'ler halinde gönderir (≤500). Faz 10.10 kararı gereği
/// yazılan HER bildirim satırı push'lanabilir (send_push=false ise satır zaten yazılmaz), o yüzden ekstra filtre yok.
///
/// İşaretleme semantiği (fcm_sent/fcm_sent_at/fcm_error):
/// - fcm_sent=true → satır işlendi (terminal; tekrar denenmez).
/// - fcm_sent_at dolu → FCM'e gerçekten iletildi. fcm_error dolu → iletilemedi (sebep). İkisi bir arada olmaz.
/// - Mesaj bazlı hatalar terminal sayılır (bad token vb. kalıcıdır); yalnız BATCH düzeyi exception Hangfire retry'ına gider.
/// - UNREGISTERED (kalıcı geçersiz token) → kullanıcının FcmToken'ı temizlenir.
///
/// Token'ı olmayan kullanıcının bildirimi sorguya HİÇ girmez (fcm_sent=false kalır, gönderilebilir hedef yok):
/// mobil yayından önce token olmadığından job doğal olarak boş çalışır. IsConfigured=false (Fcm:Provider=None) ise
/// hiç sorgu atmadan döner — sağlayıcı sonradan bağlanınca token'lı bekleyen bildirimler gönderilir.
/// </summary>
public class SendPushNotificationsJob
{
    private const int BatchSize = 500;

    private readonly AppDbContext _context;
    private readonly IPushService _push;
    private readonly ILogger<SendPushNotificationsJob> _log;

    public SendPushNotificationsJob(AppDbContext context, IPushService push, ILogger<SendPushNotificationsJob> log)
        => (_context, _push, _log) = (context, push, log);

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task RunAsync()
    {
        if (!_push.IsConfigured)
            return; // Fcm:Provider=None → gönderim devre dışı; DB'ye hiç dokunma.

        var batch = await _context.Notifications
            .Include(n => n.User)
            .Where(n => !n.FcmSent && n.User.FcmToken != null)
            .OrderBy(n => n.CreatedAt)
            .Take(BatchSize)
            .ToListAsync();
        if (batch.Count == 0) return;

        var messages = batch
            .Select(n => new PushMessage(n.User.FcmToken!, n.Title, n.Body, BuildData(n)))
            .ToList();

        var results = await _push.SendAsync(messages);

        var now = DateTime.UtcNow;
        int sent = 0, failed = 0, invalidTokens = 0;
        for (var i = 0; i < batch.Count; i++)
        {
            var n = batch[i];
            var r = i < results.Count ? results[i] : PushResult.Failed("NO_RESULT");

            n.FcmSent = true;
            if (r.Success)
            {
                n.FcmSentAt = now;
                n.FcmError = null;
                sent++;
            }
            else
            {
                n.FcmError = r.Error;
                failed++;
            }

            if (r.TokenInvalid && n.User.FcmToken != null)
            {
                n.User.FcmToken = null; // aynı kullanıcının diğer satırları için de guard sayesinde tek kez sayılır
                invalidTokens++;
            }
        }

        await _context.SaveChangesAsync();

        _log.LogInformation(
            "SendPushNotificationsJob: {Sent} gönderildi, {Failed} başarısız, {Invalid} geçersiz token temizlendi (batch {Batch})",
            sent, failed, invalidTokens, batch.Count);
    }

    /// <summary>Mobil istemcinin push'tan ilgili kayda deep-link yapıp bildirimi okundu işaretleyebilmesi için veri yükü.</summary>
    private static IReadOnlyDictionary<string, string>? BuildData(Domain.Entities.Notification n)
    {
        var data = new Dictionary<string, string> { ["notificationId"] = n.Id.ToString() };
        if (n.Type is not null) data["type"] = n.Type;
        if (n.RelatedId is not null) data["relatedId"] = n.RelatedId.Value.ToString();
        if (n.RelatedType is not null) data["relatedType"] = n.RelatedType;
        return data;
    }
}
