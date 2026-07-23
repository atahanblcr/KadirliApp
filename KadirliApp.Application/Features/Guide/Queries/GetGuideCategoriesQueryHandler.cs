using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Guide.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Guide.Queries;

public class GetGuideCategoriesQueryHandler : IRequestHandler<GetGuideCategoriesQuery, PagedResult<GuideCategoryResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetGuideCategoriesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<GuideCategoryResponseDto>> Handle(GetGuideCategoriesQuery request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var query = _uow.Repository<GuideCategory>().Query();

        if (dto.ParentId.HasValue)
            query = query.Where(x => x.ParentId == dto.ParentId.Value);

        if (!string.IsNullOrWhiteSpace(dto.Search))
            query = query.Where(x => x.Name.ToLower().Contains(dto.Search.ToLower()));

        var totalCount = await query.CountAsync(cancellationToken);

        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit, Pagination.AdminMaxLimit);

        var items = await query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new GuideCategoryResponseDto
            {
                Id = x.Id,
                Name = x.Name,
                Slug = x.Slug,
                ParentId = x.ParentId,
                Icon = x.Icon,
                Color = x.Color,
                DisplayOrder = x.DisplayOrder,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<GuideCategoryResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
