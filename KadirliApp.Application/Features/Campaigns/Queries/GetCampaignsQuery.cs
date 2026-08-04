using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Campaigns.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Campaigns.Queries;

public record GetCampaignsQuery(QueryCampaignDto Dto) : IRequest<PagedResult<CampaignResponseDto>>;

public class GetCampaignsQueryHandler : IRequestHandler<GetCampaignsQuery, PagedResult<CampaignResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetCampaignsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<CampaignResponseDto>> Handle(GetCampaignsQuery request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var query = _uow.Repository<Campaign>().Query();

        if (dto.BusinessId.HasValue)
            query = query.Where(x => x.BusinessId == dto.BusinessId.Value);

        if (dto.CategoryId.HasValue)
            query = query.Where(x => x.Business.CategoryId == dto.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(dto.Status))
            query = query.Where(x => x.Status == dto.Status);

        if (dto.OnlyActive)
        {
            var now = DateTime.UtcNow;
            query = query.Where(x => x.StartDate <= now && x.EndDate >= now);
        }

        if (!string.IsNullOrWhiteSpace(dto.Search))
        {
            var search = dto.Search.ToLower();
            query = query.Where(x =>
                x.Title.ToLower().Contains(search) ||
                x.Business.BusinessName.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit, Pagination.AdminMaxLimit);

        // Faz 11.18: panel sütun sıralaması. Boş Sort → "created_desc" = eski davranışın aynısı.
        var items = await Common.Sorting.PanelSorts.Campaigns.Apply(query, dto.Sort)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new CampaignResponseDto
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                BusinessName = x.Business.BusinessName,
                Title = x.Title,
                Description = x.Description,
                DiscountPercentage = x.DiscountPercentage,
                DiscountCode = x.DiscountCode,
                Terms = x.Terms,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                CodeViewCount = x.CodeViewCount,
                CoverImageId = x.CoverImageId,
                CoverImageUrl = x.CoverImage != null ? x.CoverImage.CdnUrl : null,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<CampaignResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
