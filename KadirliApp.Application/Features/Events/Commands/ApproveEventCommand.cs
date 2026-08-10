using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Events.Commands;

public record ApproveEventCommand(Guid Id, Guid AdminId) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "events";
    public string AuditAction => "approve";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "Event";
}

public class ApproveEventCommandHandler : IRequestHandler<ApproveEventCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public ApproveEventCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(ApproveEventCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Event>();
        var ev = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (ev == null) return false;

        // Faz 12.10: kuralın tek sahibi EventModeration.
        EventModeration.Approve(ev);

        repo.Update(ev);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
