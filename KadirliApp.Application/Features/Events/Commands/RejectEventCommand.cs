using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Events.Commands;

public record RejectEventCommand(Guid Id, Guid AdminId) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "events";
    public string AuditAction => "reject";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "Event";
}

public class RejectEventCommandHandler : IRequestHandler<RejectEventCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public RejectEventCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(RejectEventCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Event>();
        var ev = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (ev == null) return false;

        // Faz 12.10: kuralın tek sahibi varlığın kendisi (Faz 12.11).
        ev.Reject();

        repo.Update(ev);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
