using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Ads.Queries;

/// <summary>Faz 10.6: kullanıcının kendi ilanları — her statüyü görür (pending/rejected dahil), status ile filtrelenebilir.</summary>
public record GetMyAdsQuery(Guid UserId, string? Status = null, int Page = 1, int Limit = 20) : IRequest<PagedResult<MyAdDto>>;

/// <summary>
/// "Benim ilanlarım" satırı: public AdResponseDto'dan farklı olarak sahibin görmesi gereken alanları da taşır —
/// red gerekçesi, uzatma hakkı ve iletişim tıklama sayaçları (ilan performansı).
/// </summary>
public record MyAdDto(
    Guid Id,
    string Title,
    string? Description,
    decimal? Price,
    string Status,
    Guid CategoryId,
    string CategoryName,
    string ContactPhone,
    int ViewCount,
    int PhoneClickCount,
    int WhatsappClickCount,
    int FavoriteCount,
    int ExtensionCount,
    int MaxExtensions,
    string? RejectedReason,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    List<string> ImageUrls
);

public class GetMyAdsQueryHandler : IRequestHandler<GetMyAdsQuery, PagedResult<MyAdDto>>
{
    private static readonly string[] ValidStatuses = { "pending", "approved", "rejected", "expired" };

    private readonly IUnitOfWork _uow;

    public GetMyAdsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<MyAdDto>> Handle(GetMyAdsQuery request, CancellationToken cancellationToken)
    {
        if (request.Status != null && !ValidStatuses.Contains(request.Status))
            throw new ValidationException($"Geçersiz status filtresi. Geçerli değerler: {string.Join(", ", ValidStatuses)}");

        var query = _uow.Repository<Ad>().Query()
            .Where(x => x.UserId == request.UserId);

        if (request.Status != null)
            query = query.Where(x => x.Status == request.Status);

        var totalCount = await query.CountAsync(cancellationToken);

        // AdImage → File navigation'ı yok; URL'ler correlated subquery ile files tablosundan çekiliyor (GetAds deseni).
        var files = _uow.Repository<Domain.Entities.File>().Query();

        var (page, limit) = Pagination.Clamp(request.Page, request.Limit);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new MyAdDto(
                x.Id,
                x.Title,
                x.Description,
                x.Price,
                x.Status,
                x.CategoryId,
                x.Category.Name,
                x.ContactPhone,
                x.ViewCount,
                x.PhoneClickCount,
                x.WhatsappClickCount,
                x.Favorites.Count,
                x.ExtensionCount,
                x.MaxExtensions,
                x.RejectedReason,
                x.CreatedAt,
                x.ExpiresAt,
                x.Images
                    .OrderByDescending(i => i.IsCover).ThenBy(i => i.DisplayOrder)
                    .Select(i => files.Where(f => f.Id == i.FileId).Select(f => f.CdnUrl).FirstOrDefault())
                    .Where(u => u != null)
                    .Select(u => u!)
                    .ToList()
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<MyAdDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
