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

        // Faz 12.2b: kampanya başına delta. Sayaçlar SONRADAN türetilemez —
        // "geçersiz token" işareti FcmToken null'landığı anda kaybolur — bu yüzden
        // tam bu döngüde toplanır.
        var deltas = new Dictionary<Guid, CampaignDelta>();

        for (var i = 0; i < batch.Count; i++)
        {
            var n = batch[i];
            var r = i < results.Count ? results[i] : PushResult.Failed("NO_RESULT");

            var delta = n.CampaignId is { } campaignId
                ? deltas.TryGetValue(campaignId, out var existing) ? existing : deltas[campaignId] = new CampaignDelta()
                : null;

            n.FcmSent = true;
            if (r.Success)
            {
                n.FcmSentAt = now;
                n.FcmError = null;
                sent++;
                if (delta is not null) delta.Sent++;
            }
            else
            {
                n.FcmError = r.Error;
                failed++;
                if (delta is not null) delta.Failed++;
            }

            if (r.TokenInvalid && n.User.FcmToken != null)
            {
                n.User.FcmToken = null; // aynı kullanıcının diğer satırları için de guard sayesinde tek kez sayılır
                invalidTokens++;
                if (delta is not null) delta.InvalidTokens++;
            }
        }

        await ApplyCampaignCountersAsync(deltas, batch, now);

        await _context.SaveChangesAsync();

        _log.LogInformation(
            "SendPushNotificationsJob: {Sent} gönderildi, {Failed} başarısız, {Invalid} geçersiz token temizlendi (batch {Batch})",
            sent, failed, invalidTokens, batch.Count);
    }

    /// <summary>
    /// Faz 12.2b — teslim panosunun sayaçlarını <b>artımlı</b> yazar.
    /// </summary>
    /// <remarks>
    /// 🔑 <b>Neden burada:</b> job bu üç sayıyı zaten hesaplıyordu (yukarıdaki döngü),
    /// yalnız log'a yazıp atıyordu. Yazması bedava; sonradan <c>COUNT</c> ile saymak ise
    /// panelin en hızlı büyüyecek tablosunu her liste açılışında tam taramak demek.
    ///
    /// ⚠️ <b>Sayaçlar artımlı olduğu için "bir kez daha say, düzelir" yolu YOK.</b> Bu
    /// metot atlanırsa pano sonsuza kadar "Kuyrukta" gösterir: bildirimler gider,
    /// <c>fcm_sent</c> dolar, hiçbir hata oluşmaz ve <b>yalnız pano yalan söyler.</b>
    ///
    /// 🔴 <b>Tamamlanma ölçütü "işlenen = alıcı" DEĞİL.</b> Öyle olsaydı kampanyalar asla
    /// tamamlanmazdı: bu job yalnız <c>FcmToken != null</c> satırları alır, token'ı olmayan
    /// alıcılar sonsuza kadar bekleyen görünürdü. Ölçüt "bu kampanyada <b>gönderilebilir</b>
    /// bekleyen satır kalmadı" — token'ı olmayanlar bekler, kampanya yine tamamlanır ve
    /// biri yarın token kaydederse satırı bir sonraki turda gider (sayaç artar, kampanya
    /// yeniden açılmaz — <c>CompletedAt</c> ilk tamamlanma anıdır).
    /// </remarks>
    private async Task ApplyCampaignCountersAsync(
        IReadOnlyDictionary<Guid, CampaignDelta> deltas,
        IReadOnlyList<Domain.Entities.Notification> batch,
        DateTime now)
    {
        if (deltas.Count == 0) return;   // 12.2b öncesi satırlar kampanyasız — normal

        var ids = deltas.Keys.ToList();
        var campaigns = await _context.PushCampaigns
            .Where(c => ids.Contains(c.Id))
            .ToListAsync();

        // ⚠️ Bu batch'teki satırlar HENÜZ KAYDEDİLMEDİ: aşağıdaki sorgu veritabanına
        // gider ve onları hâlâ `fcm_sent = false` görür. Dışlanmasalardı hiçbir kampanya
        // asla tamamlanmazdı — "bekleyen var" cevabını kendi işlediğimiz satırlardan alırdık.
        // (Alternatif iki ayrı SaveChanges'ti; o da sayaçları ayrı bir işleme bırakır ve
        // ikincisi patlarsa artımlı sayaçlar kalıcı olarak eksik kalırdı.)
        var processedIds = batch.Select(n => n.Id).ToList();

        foreach (var campaign in campaigns)
        {
            var delta = deltas[campaign.Id];
            campaign.SentCount += delta.Sent;
            campaign.FailedCount += delta.Failed;
            campaign.InvalidTokenCount += delta.InvalidTokens;

            if (campaign.CompletedAt is not null) continue;

            var stillPending = await _context.Notifications
                .Include(n => n.User)
                .AnyAsync(n => n.CampaignId == campaign.Id
                               && !n.FcmSent
                               && n.User.FcmToken != null
                               && !processedIds.Contains(n.Id));

            if (!stillPending) campaign.CompletedAt = now;
        }
    }

    private sealed class CampaignDelta
    {
        public int Sent;
        public int Failed;
        public int InvalidTokens;
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
