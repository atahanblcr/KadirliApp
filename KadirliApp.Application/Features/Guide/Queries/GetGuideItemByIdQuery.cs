using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Guide.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Guide.Queries;

public record GetGuideItemByIdQuery(Guid Id) : IRequest<GuideItemResponseDto?>;

public class GetGuideItemByIdQueryHandler : IRequestHandler<GetGuideItemByIdQuery, GuideItemResponseDto?>
{
    private readonly IUnitOfWork _uow;

    public GetGuideItemByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<GuideItemResponseDto?> Handle(GetGuideItemByIdQuery request, CancellationToken cancellationToken)
    {
        return await _uow.Repository<GuideItem>().Query()
            .Where(x => x.Id == request.Id)
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
            .FirstOrDefaultAsync(cancellationToken);
    }
}
