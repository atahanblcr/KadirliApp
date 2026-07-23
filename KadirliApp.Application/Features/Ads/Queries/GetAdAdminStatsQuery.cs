using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Ads.Queries;

public record AdAdminStatsDto(int ViewCount, int PhoneClickCount, int WhatsappClickCount, int FavoriteCount);

/// <summary>
/// Faz 10.10-A (vizyon turu): AdsAdmin Edit'teki salt-okunur "Etkileşim" kartı — panel-only.
/// AdDetailDto telefon/WhatsApp sayaçlarını taşımıyor ve GetMyAdsQuery public "ilanlarım" kontratı
/// olduğundan panelde yeniden kullanılamaz → tek hedefli mini query. FavoriteCount = ad_favorites
/// satır sayısı — yalnız SAYI; kimlerin favorilediği KVKK gereği bilinçli gösterilmez. Cache'siz.
/// </summary>
public record GetAdAdminStatsQuery(Guid AdId) : IRequest<AdAdminStatsDto?>;

public class GetAdAdminStatsQueryHandler : IRequestHandler<GetAdAdminStatsQuery, AdAdminStatsDto?>
{
    private readonly IUnitOfWork _uow;

    public GetAdAdminStatsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<AdAdminStatsDto?> Handle(GetAdAdminStatsQuery request, CancellationToken cancellationToken)
    {
        var counters = await _uow.Repository<Ad>().Query()
            .Where(a => a.Id == request.AdId)
            .Select(a => new { a.ViewCount, a.PhoneClickCount, a.WhatsappClickCount })
            .FirstOrDefaultAsync(cancellationToken);
        if (counters == null)
            return null;

        var favoriteCount = await _uow.Repository<AdFavorite>().Query()
            .CountAsync(f => f.AdId == request.AdId, cancellationToken);

        return new AdAdminStatsDto(counters.ViewCount, counters.PhoneClickCount, counters.WhatsappClickCount, favoriteCount);
    }
}
