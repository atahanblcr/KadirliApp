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

        notice.Status = "approved";
        notice.ApprovedBy = request.AdminId;
        notice.ApprovedAt = DateTime.UtcNow;
        repo.Update(notice);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
