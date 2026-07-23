using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Campaigns.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Campaigns.Queries;

/// <summary>OnlyPublished=true (public uç): yalnız approved VE tarih aralığı geçerli kampanya döner (liste OnlyActive kuralıyla tutarlı), diğerine null → 404.</summary>
public record GetCampaignByIdQuery(Guid Id, bool OnlyPublished = false) : IRequest<CampaignResponseDto?>;

public class GetCampaignByIdQueryHandler : IRequestHandler<GetCampaignByIdQuery, CampaignResponseDto?>
{
    private readonly IUnitOfWork _uow;

    public GetCampaignByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<CampaignResponseDto?> Handle(GetCampaignByIdQuery request, CancellationToken cancellationToken)
    {
        var query = _uow.Repository<Campaign>().Query()
            .Where(x => x.Id == request.Id);

        // Faz 10.7 düzeltmesi: id bilinirse pending/rejected veya süresi geçmiş kampanya dönüyordu.
        if (request.OnlyPublished)
        {
            var now = DateTime.UtcNow;
            query = query.Where(x => x.Status == "approved" && x.StartDate <= now && x.EndDate >= now);
        }

        return await query
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
            .FirstOrDefaultAsync(cancellationToken);
    }
}
