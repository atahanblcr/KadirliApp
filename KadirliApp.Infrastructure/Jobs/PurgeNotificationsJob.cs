using Hangfire;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Jobs;

/// <summary>
/// Faz 12.2b — bildirimlerin saklama süresi.
///
/// <c>notifications</c> projenin en hızlı büyüyen tablosu: <b>her gönderim, alıcı sayısı
/// kadar satır</b> yazar. 5.000 kullanıcıya giden tek bir duyuru 5.000 satır demektir ve
/// kullanıcı bildirimi okuduktan sonra o satır hiçbir soruya cevap vermez.
///
/// 🔴 <b>Kampanya satırı SİLİNMEZ.</b> Özet ucuz (gönderim başına tek satır), tarihçe ise
/// değerli: "geçen ay hangi mahalleye ne yolladık, kaçına ulaştı" sorusu bildirimler
/// silindikten sonra da cevaplanabilmeli. Bu bilinçli asimetri
/// <c>Notification.CampaignId</c>'nin <c>SetNull</c> değil, <b>bildirimin</b> silinmesiyle
/// korunur — kampanya ayakta kalır, sayaçları zaten kolonda.
///
/// ⚠️ <b>Yalnız OKUNMUŞ bildirimler silinir.</b> Okunmamış bir bildirimi yaşına bakarak
/// silmek, kullanıcının hiç görmediği bir mesajı yok etmek olurdu — üstelik rozet sayısı
/// (<c>unreadCount</c>) sessizce düşerdi ve kimse sebebini bulamazdı (görünmez sözleşme #17).
/// </summary>
public class PurgeNotificationsJob
{
    /// <summary>Okunmuş bildirim bu kadar gün sonra silinir.</summary>
    public const int ReadRetentionDays = 90;

    private readonly AppDbContext _context;
    private readonly ILogger<PurgeNotificationsJob> _log;

    public PurgeNotificationsJob(AppDbContext context, ILogger<PurgeNotificationsJob> log)
        => (_context, _log) = (context, log);

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task RunAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-ReadRetentionDays);

        // Set tabanlı tek DELETE — idempotent: ikinci koşuda koşula uyan satır kalmaz.
        // ⚠️ Ölçüt ReadAt DEĞİL CreatedAt: "okundu ama 3 yıl önce yazıldı" satırı ReadAt'e
        // bakılsaydı, dün okunduğu için 90 gün daha yaşardı.
        var deleted = await _context.Notifications
            .Where(n => n.IsRead && n.CreatedAt < cutoff)
            .ExecuteDeleteAsync();

        if (deleted > 0)
            _log.LogInformation("PurgeNotificationsJob: {Deleted} okunmuş bildirim silindi.", deleted);
    }
}
