using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Guide.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Guide.Queries;

public record GetGuideItemsQuery(QueryGuideItemDto Dto)
    : IRequest<PagedResult<GuideItemResponseDto>>, ICacheableQuery
{
    public string CacheKey => $"guide:items:p{Dto.Page}:l{Dto.Limit}:s{Dto.Search}:c{Dto.CategoryId}:a{Dto.IsActive}";
    public string CacheGroup => CacheGroups.Guide;
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);
}

public class GetGuideItemsQueryHandler : IRequestHandler<GetGuideItemsQuery, PagedResult<GuideItemResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetGuideItemsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<GuideItemResponseDto>> Handle(GetGuideItemsQuery request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var query = _uow.Repository<GuideItem>().Query();

        if (dto.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == dto.CategoryId.Value);

        if (dto.IsActive.HasValue)
            query = query.Where(x => x.IsActive == dto.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(dto.Search))
        {
            var s = dto.Search.ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(s)
                || (x.Phone != null && x.Phone.Contains(s))
                || (x.Address != null && x.Address.ToLower().Contains(s)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit, Pagination.AdminMaxLimit);

        var items = await query
            .OrderBy(x => x.Category.DisplayOrder)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new GuideItemResponseDto
            {
                Id = x.Id,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                CategoryIcon = x.Category.Icon,
                CategoryColor = x.Category.Color,
                Name = x.Name,
                Phone = x.Phone,
                Address = x.Address,
                Email = x.Email,
                WebsiteUrl = x.WebsiteUrl,
                WorkingHours = x.WorkingHours,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                Description = x.Description,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<GuideItemResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
