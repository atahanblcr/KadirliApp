using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Guide.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Guide.Queries;

public class GetGuideCategoryByIdQueryHandler : IRequestHandler<GetGuideCategoryByIdQuery, GuideCategoryResponseDto?>
{
    private readonly IUnitOfWork _uow;

    public GetGuideCategoryByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<GuideCategoryResponseDto?> Handle(GetGuideCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _uow.Repository<GuideCategory>().GetByIdAsync(request.Id, cancellationToken);
        if (category == null) return null;

        return new GuideCategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            ParentId = category.ParentId,
            Icon = category.Icon,
            Color = category.Color,
            DisplayOrder = category.DisplayOrder,
            CreatedAt = category.CreatedAt
        };
    }
}
