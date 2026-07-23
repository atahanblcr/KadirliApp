using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Events.Commands;

public record DeleteEventCommand(Guid Id) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "events";
    public string AuditAction => "delete";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "Event";
}

public class DeleteEventCommandHandler : IRequestHandler<DeleteEventCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteEventCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Event>();
        var ev = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (ev == null) return false;

        repo.SoftRemove(ev);
        repo.Update(ev);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
