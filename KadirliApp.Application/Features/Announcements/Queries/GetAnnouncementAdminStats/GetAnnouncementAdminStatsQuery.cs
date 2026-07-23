using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Announcements.Queries.GetAnnouncementAdminStats;

public record AnnouncementAdminStatsDto(int ViewCount, int ClickCount, int UniqueViewers);

/// <summary>
/// Faz 10.10-A (vizyon turu): AnnouncementsAdmin Index'in görüntülenme/tıklama/tekil-erişim kolonları.
/// Panel-only — public AnnouncementDto'ya bilinçli alan EKLENMEDİ (kontrat donmak üzere; toplam
/// görüntülenme mobil kullanıcıya sızmaz). UniqueViewers = announcement_views satır sayısı (composite PK
/// kullanıcı başına tekil) — "kaç FARKLI kullanıcı gördü"; view_count ise toplam açılış. Parametresiz:
/// panel listesi zaten Limit=200 ile komple çekiliyor, id filtresi gereksiz karmaşıklık. Cache'siz.
/// </summary>
public record GetAnnouncementAdminStatsQuery : IRequest<Dictionary<Guid, AnnouncementAdminStatsDto>>;

public class GetAnnouncementAdminStatsQueryHandler
    : IRequestHandler<GetAnnouncementAdminStatsQuery, Dictionary<Guid, AnnouncementAdminStatsDto>>
{
    private readonly IUnitOfWork _uow;

    public GetAnnouncementAdminStatsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Dictionary<Guid, AnnouncementAdminStatsDto>> Handle(
        GetAnnouncementAdminStatsQuery request, CancellationToken cancellationToken)
    {
        var uniqueViewers = await _uow.SetQuery<AnnouncementView>()
            .GroupBy(v => v.AnnouncementId)
            .Select(g => new { AnnouncementId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AnnouncementId, x => x.Count, cancellationToken);

        return await _uow.Repository<Announcement>().Query()
            .Select(a => new { a.Id, a.ViewCount, a.ClickCount })
            .ToDictionaryAsync(
                x => x.Id,
                x => new AnnouncementAdminStatsDto(
                    x.ViewCount,
                    x.ClickCount,
                    uniqueViewers.TryGetValue(x.Id, out var u) ? u : 0),
                cancellationToken);
    }
}
