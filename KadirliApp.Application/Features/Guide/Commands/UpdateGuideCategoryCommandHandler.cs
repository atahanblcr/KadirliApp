using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Guide.Commands;

public class UpdateGuideCategoryCommandHandler : IRequestHandler<UpdateGuideCategoryCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UpdateGuideCategoryCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateGuideCategoryCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<GuideCategory>();
        var category = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (category == null) return false;

        category.Name = request.Name;
        category.Slug = request.Slug;
        category.ParentId = request.ParentId;
        category.Icon = request.Icon;
        category.Color = request.Color;
        category.DisplayOrder = request.DisplayOrder;

        repo.Update(category);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
