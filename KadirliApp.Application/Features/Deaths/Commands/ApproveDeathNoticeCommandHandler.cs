using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Deaths.Commands;

public class ApproveDeathNoticeCommandHandler : IRequestHandler<ApproveDeathNoticeCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public ApproveDeathNoticeCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(ApproveDeathNoticeCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<DeathNotice>();
        var notice = await repo.GetByIdAsync(request.Id, cancellationToken);

        if (notice == null) return false;

        // Faz 12.10: kuralın tek sahibi varlığın kendisi (Faz 12.11).
        notice.Approve(request.AdminId, DateTime.UtcNow);

        repo.Update(notice);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
