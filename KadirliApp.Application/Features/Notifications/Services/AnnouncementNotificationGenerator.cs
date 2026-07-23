using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Notifications.Services;

/// <summary>
/// Faz 10.10 kararları:
/// - SendPushNotification=false ise bildirim SATIRI DA yazılmaz (plandaki iki seçenekten ilki —
///   böylece 10.11 FCM job'ı "fcm_sent=false olan her satır push'lanabilir" varsayımıyla sade kalır).
/// - Hedefleme: targetType=neighborhood → PrimaryNeighborhoodId VEYA UserNeighborhood eşleşmesi;
///   diğer tüm targetType değerleri "all" gibi davranır (panel yalnız all/neighborhood üretiyor).
/// - Bildirim tercihi: NotificationPreferences.Announcements=false olan kullanıcıya satır yazılmaz
///   (tercih üretim anında uygulanır; aksi hâlde 10.3'teki tercih ekranı ölü kalırdı).
/// - İdempotency işareti: duyuru başına related_type+related_id kontrolü — satır varsa üretim atlanır
///   (job iki kez koşarsa veya update tekrar tetiklerse mükerrer üretilmez).
/// - Body 500 karaktere kırpılır (bildirim listesi için özet yeterli; tam metin duyuru detayında).
/// </summary>
public class AnnouncementNotificationGenerator : IAnnouncementNotificationGenerator
{
    public const string RelatedTypeAnnouncement = "announcement";
    private const int MaxBodyLength = 500;

    private readonly IUnitOfWork _uow;

    public AnnouncementNotificationGenerator(IUnitOfWork uow) => _uow = uow;

    public async Task<int> GenerateForAnnouncementAsync(Announcement announcement, CancellationToken ct = default)
    {
        if (!announcement.SendPushNotification)
            return 0;

        var notifications = _uow.Repository<Notification>();

        var alreadyGenerated = await notifications.Query()
            .AnyAsync(n => n.RelatedType == RelatedTypeAnnouncement && n.RelatedId == announcement.Id, ct);
        if (alreadyGenerated)
            return 0;

        var users = _uow.Repository<User>().Query()
            .Where(u => u.IsActive && !u.IsBanned && u.NotificationPreferences.Announcements);

        if (announcement.TargetType == "neighborhood" && !string.IsNullOrWhiteSpace(announcement.TargetNeighborhoods))
        {
            var neighborhoodIds = JsonSerializer.Deserialize<List<Guid>>(announcement.TargetNeighborhoods) ?? new List<Guid>();
            users = users.Where(u =>
                (u.PrimaryNeighborhoodId != null && neighborhoodIds.Contains(u.PrimaryNeighborhoodId.Value)) ||
                u.Neighborhoods.Any(un => neighborhoodIds.Contains(un.NeighborhoodId)));
        }

        var targetUserIds = await users.Select(u => u.Id).ToListAsync(ct);
        if (targetUserIds.Count == 0)
            return 0;

        var body = announcement.Body.Length > MaxBodyLength
            ? announcement.Body[..MaxBodyLength] + "…"
            : announcement.Body;

        foreach (var userId in targetUserIds)
        {
            await notifications.AddAsync(new Notification
            {
                UserId = userId,
                Title = announcement.Title,
                Body = body,
                Type = RelatedTypeAnnouncement,
                RelatedId = announcement.Id,
                RelatedType = RelatedTypeAnnouncement
            }, ct);
        }

        await _uow.SaveChangesAsync(ct);
        return targetUserIds.Count;
    }
}
