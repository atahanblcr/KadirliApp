using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Ads.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Ads.Queries;

public class GetAdsQueryHandler : IRequestHandler<GetAdsQuery, PagedResult<AdResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetAdsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<AdResponseDto>> Handle(GetAdsQuery request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var query = _uow.Repository<Ad>().Query()
            .Where(x => x.DeletedAt == null);

        // Faz 10.5 düzeltmesi: public liste bugüne dek status filtrelemiyordu — pending/rejected
        // ilanlar (iletişim telefonlarıyla) herkese dönüyordu. Public uç OnlyPublished=true geçer.
        if (request.OnlyPublished)
            query = query.Where(x => x.Status == "approved" && x.ExpiresAt > DateTime.UtcNow);

        if (dto.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == dto.CategoryId.Value);

        if (dto.MinPrice.HasValue)
            query = query.Where(x => x.Price >= dto.MinPrice.Value);

        if (dto.MaxPrice.HasValue)
            query = query.Where(x => x.Price <= dto.MaxPrice.Value);

        // Faz 10.8 KARAR: arama başlık + açıklamada (pazaryeri beklentisi; pg_trgm GIN indeksi yalnız
        // başlıkta — açıklama araması seq scan, tablo büyürse indeks eklenmeli, not: 10.13).
        if (!string.IsNullOrWhiteSpace(dto.Search))
        {
            var search = dto.Search.ToLower();
            query = query.Where(x =>
                x.Title.ToLower().Contains(search) ||
                x.Description.ToLower().Contains(search));
        }

        // Faz 10.8: sıralama whitelist'i (MyAds status whitelist emsali — bilinmeyen değer 400).
        query = dto.Sort switch
        {
            null or "" or "newest" => query.OrderByDescending(x => x.CreatedAt),
            "oldest" => query.OrderBy(x => x.CreatedAt),
            "price_asc" => query.OrderBy(x => x.Price).ThenByDescending(x => x.CreatedAt),
            "price_desc" => query.OrderByDescending(x => x.Price).ThenByDescending(x => x.CreatedAt),
            _ => throw new Common.Exceptions.AppException(
                "Geçersiz sort değeri. Kullanılabilir: newest, oldest, price_asc, price_desc.", "VALIDATION_ERROR")
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit,
            request.OnlyPublished ? Pagination.MaxLimit : Pagination.AdminMaxLimit);

        // AdImage → File navigation'ı yok; URL'ler correlated subquery ile files tablosundan çekiliyor.
        var files = _uow.Repository<Domain.Entities.File>().Query();

        var items = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new AdResponseDto(
                x.Id,
                x.Title,
                x.Description,
                x.Price,
                x.Status,
                x.ContactPhone,
                x.ViewCount,
                x.CreatedAt,
                x.Images
                    .OrderByDescending(i => i.IsCover).ThenBy(i => i.DisplayOrder)
                    .Select(i => files.Where(f => f.Id == i.FileId).Select(f => f.CdnUrl).FirstOrDefault())
                    .Where(u => u != null)
                    .Select(u => u!)
                    .ToList()
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
