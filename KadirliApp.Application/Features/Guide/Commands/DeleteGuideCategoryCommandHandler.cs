using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Guide.Commands;

public class DeleteGuideCategoryCommandHandler : IRequestHandler<DeleteGuideCategoryCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteGuideCategoryCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteGuideCategoryCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<GuideCategory>();
        var category = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (category == null) return false;

        var hasItems = await _uow.Repository<GuideItem>().Query()
            .AnyAsync(x => x.CategoryId == request.Id, cancellationToken);
        if (hasItems)
            throw new ConflictException("Bu kategoride rehber kayıtları var. Önce kayıtları silin veya başka kategoriye taşıyın.");

        var hasSubCategories = await repo.Query()
            .AnyAsync(x => x.ParentId == request.Id, cancellationToken);
        if (hasSubCategories)
            throw new ConflictException("Bu kategorinin alt kategorileri var. Önce alt kategorileri silin veya taşıyın.");

        repo.Remove(category);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
