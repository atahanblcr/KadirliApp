using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Businesses.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Businesses.Queries;

/// <summary>
/// Faz 10.9(b): işletme sorguları — admin API + panel kullanır (public işletme ucu yok; işletmeler
/// mobilde yalnız kampanya yanıtı içinde businessName olarak görünür). Cache'siz (admin verisi, sık değişir).
/// </summary>
public sealed record GetBusinessesQuery(QueryBusinessDto Dto) : IRequest<PagedResult<BusinessResponseDto>>;

public sealed class GetBusinessesQueryHandler : IRequestHandler<GetBusinessesQuery, PagedResult<BusinessResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetBusinessesQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<BusinessResponseDto>> Handle(GetBusinessesQuery request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var query = _uow.Repository<Business>().Query();

        if (!string.IsNullOrWhiteSpace(dto.Search))
        {
            var term = dto.Search.ToLower();
            query = query.Where(x => x.BusinessName.ToLower().Contains(term));
        }

        if (dto.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == dto.CategoryId.Value);

        if (dto.IsVerified.HasValue)
            query = query.Where(x => x.IsVerified == dto.IsVerified.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit, Pagination.AdminMaxLimit);

        var items = await query
            .OrderBy(x => x.BusinessName)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new BusinessResponseDto(
                x.Id, x.BusinessName, x.CategoryId, x.Category.Name,
                x.TaxNumber, x.Address, x.Phone, x.Email,
                x.WebsiteUrl, x.InstagramHandle,
                x.LogoFileId, x.LogoFile != null ? x.LogoFile.CdnUrl : null,
                x.IsVerified, x.VerifiedAt,
                x.Campaigns.Count, // Campaign global soft-delete filtresi projeksiyonda da uygulanır — yalnız aktif kayıtlar
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<BusinessResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageSize = limit,
            CurrentPage = page
        };
    }
}

public sealed record GetBusinessByIdQuery(Guid Id) : IRequest<BusinessResponseDto?>;

public sealed class GetBusinessByIdQueryHandler : IRequestHandler<GetBusinessByIdQuery, BusinessResponseDto?>
{
    private readonly IUnitOfWork _uow;

    public GetBusinessByIdQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<BusinessResponseDto?> Handle(GetBusinessByIdQuery request, CancellationToken cancellationToken)
    {
        return await _uow.Repository<Business>().Query()
            .Where(x => x.Id == request.Id)
            .Select(x => new BusinessResponseDto(
                x.Id, x.BusinessName, x.CategoryId, x.Category.Name,
                x.TaxNumber, x.Address, x.Phone, x.Email,
                x.WebsiteUrl, x.InstagramHandle,
                x.LogoFileId, x.LogoFile != null ? x.LogoFile.CdnUrl : null,
                x.IsVerified, x.VerifiedAt,
                x.Campaigns.Count,
                x.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public sealed record GetBusinessCategoriesQuery : IRequest<IReadOnlyList<BusinessCategoryDto>>;

public sealed class GetBusinessCategoriesQueryHandler
    : IRequestHandler<GetBusinessCategoriesQuery, IReadOnlyList<BusinessCategoryDto>>
{
    private readonly IUnitOfWork _uow;

    public GetBusinessCategoriesQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<BusinessCategoryDto>> Handle(
        GetBusinessCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _uow.Repository<BusinessCategory>().Query()
            .OrderBy(x => x.Name)
            .Select(x => new BusinessCategoryDto(x.Id, x.Name, x.Slug, x.ParentId))
            .ToListAsync(cancellationToken);
    }
}
