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
    Task<int> GenerateForAnnouncementAsync(Announcement announcement, CancellationToken ct = default);
}
