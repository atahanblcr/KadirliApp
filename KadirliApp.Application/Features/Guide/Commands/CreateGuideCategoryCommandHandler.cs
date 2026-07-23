using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Guide.Commands;

public class CreateGuideCategoryCommandHandler : IRequestHandler<CreateGuideCategoryCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateGuideCategoryCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateGuideCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new GuideCategory
        {
            Name = request.Name,
            Slug = request.Slug,
            ParentId = request.ParentId,
            Icon = request.Icon,
            Color = request.Color,
            DisplayOrder = request.DisplayOrder
        };

        await _uow.Repository<GuideCategory>().AddAsync(category, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
