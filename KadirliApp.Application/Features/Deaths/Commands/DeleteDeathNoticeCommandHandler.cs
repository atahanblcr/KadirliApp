using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Deaths.Commands;

public class DeleteDeathNoticeCommandHandler : IRequestHandler<DeleteDeathNoticeCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteDeathNoticeCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteDeathNoticeCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<DeathNotice>();
        var notice = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (notice == null) return false;

        repo.SoftRemove(notice);
        repo.Update(notice);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
