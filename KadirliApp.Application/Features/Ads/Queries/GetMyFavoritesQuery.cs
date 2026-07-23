using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Ads.Queries;

/// <summary>Faz 10.6: kullanıcının favori ilanları, favoriye eklenme sırasına göre (yeni → eski).</summary>
public record GetMyFavoritesQuery(Guid UserId, int Page = 1, int Limit = 20) : IRequest<PagedResult<FavoriteAdDto>>;

/// <summary>
/// Favori listesi satırı. Silinen ilanların favorileri listeden düşer (Ad soft-delete query filter'ı
/// inner join'de satırı eler); yayında olmayanlar (pending'e dönmüş / süresi geçmiş) IsAvailable=false ile
/// döner — mobil bunları soluk gösterip detayına girişi kapatabilir.
/// </summary>
public record FavoriteAdDto(
    Guid AdId,
    string Title,
    decimal? Price,
    string Status,
    bool IsAvailable,
    int ViewCount,
    DateTime FavoritedAt,
    List<string> ImageUrls
);

public class GetMyFavoritesQueryHandler : IRequestHandler<GetMyFavoritesQuery, PagedResult<FavoriteAdDto>>
{
    private readonly IUnitOfWork _uow;

    public GetMyFavoritesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<FavoriteAdDto>> Handle(GetMyFavoritesQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // f.Ad referansı, Ad'ın soft-delete query filter'lı inner join'ini üretir — silinen ilanın favorisi
        // hem listeden hem totalCount'tan düşer (ikisi aynı join'i paylaşsın diye Where'de açıkça geziliyor).
        var query = _uow.Repository<AdFavorite>().Query()
            .Where(f => f.UserId == request.UserId && f.Ad.DeletedAt == null);

        var totalCount = await query.CountAsync(cancellationToken);

        var files = _uow.Repository<Domain.Entities.File>().Query();

        var (page, limit) = Pagination.Clamp(request.Page, request.Limit);

        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(f => new FavoriteAdDto(
                f.AdId,
                f.Ad.Title,
                f.Ad.Price,
                f.Ad.Status,
                f.Ad.Status == "approved" && f.Ad.ExpiresAt > now,
                f.Ad.ViewCount,
                f.CreatedAt,
                f.Ad.Images
                    .OrderByDescending(i => i.IsCover).ThenBy(i => i.DisplayOrder)
                    .Select(i => files.Where(x => x.Id == i.FileId).Select(x => x.CdnUrl).FirstOrDefault())
                    .Where(u => u != null)
                    .Select(u => u!)
                    .ToList()
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<FavoriteAdDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
