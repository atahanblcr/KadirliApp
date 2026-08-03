using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Notifications.DTOs;
using KadirliApp.Application.Features.Notifications.Services;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Notifications.Queries.GetMyNotifications;

/// <summary>
/// Faz 10.10: GET /v1/notifications — kullanıcının bildirimleri (paged, ?unreadOnly=).
/// Cache'siz: kullanıcıya özel ve is_read ile sık değişen veri.
/// </summary>
public record GetMyNotificationsQuery(Guid UserId, int Page = 1, int Limit = 20, bool UnreadOnly = false)
    : IRequest<NotificationListDto>;

public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, NotificationListDto>
{
    private readonly IUnitOfWork _uow;

    public GetMyNotificationsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<NotificationListDto> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // 🔴 Faz 11.15c — "HEDEFİ YAŞAYAN" SÜZGECİ (ölü bildirim emniyet ağı).
        //
        // Canlı kanıt: panelden push'lu duyuru oluşturuldu → 9 notifications satırı üretildi →
        // duyuru panelden silindi → 9 satır AYNEN DURDU. Kullanıcı bildirimi görüyor,
        // dokunuyor ve GET /v1/announcements/{id} NOT_FOUND döndüğü için boş sayfaya düşüyordu.
        //
        // İki katmanlı düzeltme yapıldı:
        //   1) Kaynakta: DeleteAnnouncementCommand artık ilgili bildirimleri de siler.
        //   2) Burada (emniyet ağı): silme dışındaki yollarla da hedef görünmez olabilir —
        //      duyuru "draft"a çekilir ya da VisibleUntil geçer. O durumda da bildirim
        //      ölü bağlantıdır. Süzgeç, public sorgunun görünürlük tanımıyla birebir aynı:
        //      GetAnnouncementsQuery:46 → Status == "active" && (VisibleUntil == null || > now)
        //
        // ⚠️ Süzgeç unreadCount'a da uygulanmalı: baseQuery'den TÜREDİĞİ için otomatik uygulanır.
        //    Ayrılırsa rozet "3 okunmamış" derken liste 1 satır gösterir (sessiz tutarsızlık).
        //
        // 📌 Bugün bildirim üreten TEK modül duyurular (AnnouncementNotificationGenerator).
        //    Vefat/etkinlik/kampanya bildirimi eklendiği gün buraya o modülün de dalı yazılmalı;
        //    RelatedType'ı bilinmeyen bildirim (else dalı) elenmez — bilerek: yeni bir modül
        //    eklendiğinde bildirimleri "sessizce kaybolmaz", yalnız bu süzgeçten muaf kalır.
        var liveAnnouncements = _uow.Repository<Announcement>().Query()
            .Where(a => a.Status == "active" && (a.VisibleUntil == null || a.VisibleUntil > now));

        var baseQuery = _uow.Repository<Notification>().Query()
            .Where(x => x.UserId == request.UserId)
            .Where(x => x.RelatedType != AnnouncementNotificationGenerator.RelatedTypeAnnouncement
                        || (x.RelatedId != null && liveAnnouncements.Any(a => a.Id == x.RelatedId)));

        var unreadCount = await baseQuery.CountAsync(x => !x.IsRead, cancellationToken);

        var query = request.UnreadOnly ? baseQuery.Where(x => !x.IsRead) : baseQuery;
        var totalCount = await query.CountAsync(cancellationToken);
        var (page, limit) = Pagination.Clamp(request.Page, request.Limit);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new NotificationDto
            {
                Id = x.Id,
                Title = x.Title,
                Body = x.Body,
                Type = x.Type,
                RelatedId = x.RelatedId,
                RelatedType = x.RelatedType,
                IsRead = x.IsRead,
                ReadAt = x.ReadAt,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new NotificationListDto
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit,
            UnreadCount = unreadCount
        };
    }
}
