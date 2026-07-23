using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Announcements.Commands.TrackAnnouncement;

public enum AnnouncementTrackKind
{
    View,
    Click
}

/// <summary>
/// Faz 10.12: duyuru görüntüleme/tıklama sayaçları (masterclass view_count/click_count — anonim olabilir).
/// TrackAdContactCommand deseni: tek atomik ExecuteUpdate — yarışta kayıp artış olmaz, hiçbir cache
/// grubunu invalide etmez (announcements sorguları cache'siz; sayaç değişimi listeyi bozmaz).
/// View + giriş yapmış kullanıcı: announcement_views'a (announcement_id, user_id) satırı — composite PK,
/// kullanıcı başına tek satır ("kim gördü" kümesi); view_count ise her çağrıda artar (toplam açılış).
/// </summary>
public record TrackAnnouncementCommand(Guid AnnouncementId, AnnouncementTrackKind Kind, Guid? UserId = null)
    : IRequest<bool>;

public class TrackAnnouncementCommandHandler : IRequestHandler<TrackAnnouncementCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public TrackAnnouncementCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(TrackAnnouncementCommand request, CancellationToken cancellationToken)
    {
        // Görünürlük kuralı public detayla aynı (GetAnnouncementById OnlyPublished):
        // yalnız active + süresi dolmamış duyurunun sayacı artar, diğerine 404.
        var now = DateTime.UtcNow;
        var query = _uow.Repository<Announcement>().Query()
            .Where(a => a.Id == request.AnnouncementId
                        && a.Status == "active"
                        && (a.VisibleUntil == null || a.VisibleUntil > now));

        var affected = request.Kind == AnnouncementTrackKind.View
            ? await query.ExecuteUpdateAsync(s => s.SetProperty(a => a.ViewCount, a => a.ViewCount + 1), cancellationToken)
            : await query.ExecuteUpdateAsync(s => s.SetProperty(a => a.ClickCount, a => a.ClickCount + 1), cancellationToken);

        if (affected == 0)
            throw new NotFoundException(nameof(Announcement), request.AnnouncementId);

        if (request.Kind == AnnouncementTrackKind.View && request.UserId.HasValue)
        {
            var exists = await _uow.SetQuery<AnnouncementView>()
                .AnyAsync(v => v.AnnouncementId == request.AnnouncementId && v.UserId == request.UserId.Value, cancellationToken);
            if (!exists)
            {
                await _uow.AddToSetAsync(new AnnouncementView
                {
                    AnnouncementId = request.AnnouncementId,
                    UserId = request.UserId.Value
                }, cancellationToken);
                try
                {
                    await _uow.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    // Yarış: iki eşzamanlı istek composite PK'ya takıldı — idempotent davran.
                }
            }
        }

        return true;
    }
}
