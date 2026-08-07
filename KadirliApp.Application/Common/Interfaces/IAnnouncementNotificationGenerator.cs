using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Common.Interfaces;

/// <summary>
/// Faz 10.10: duyuru YAYINLANDIĞI anda hedef kullanıcılara notifications satırı üretir.
/// Üç çağıran var: CreateAnnouncement (anında yayın), UpdateAnnouncement (scheduled→active geçişi)
/// ve PublishScheduledAnnouncementsJob (zamanı gelen duyurular).
/// </summary>
public interface IAnnouncementNotificationGenerator
{
    /// <summary>Üretilen bildirim sayısını döner (idempotent — duyuru için satır zaten varsa 0).</summary>
    /// <param name="announcement">Yayınlanan duyuru.</param>
    /// <param name="campaignSource">
    /// Faz 12.3 — gönderimi <b>doğuran olay</b> (<c>PushCampaignSources</c>). Varsayılan
    /// <c>announcement</c>; kesintiden doğan duyuru <c>power_outage</c> geçer.
    /// <para>
    /// 🔑 Neden parametre: kesinti bildirimi <b>bir duyurudur</b> (faz kararı — deep-link ve
    /// mobil tarafı böylece hiç değişmiyor, görünmez sözleşme #18 korunuyor), ama teslim
    /// panosunda "bu push nereden çıktı?" sorusunun cevabı "duyuru" olsaydı yönetici kesinti
    /// gönderimlerini <b>hiçbir şekilde ayıramazdı</b>. Değeri dispatcher'a ikinci bir
    /// hedefleme yolu açmadan, yalnız etiket olarak taşıyor.
    /// </para>
    /// </param>
    /// <param name="ct">İptal belirteci.</param>
    Task<int> GenerateForAnnouncementAsync(
        Announcement announcement, string? campaignSource = null, CancellationToken ct = default);
}
