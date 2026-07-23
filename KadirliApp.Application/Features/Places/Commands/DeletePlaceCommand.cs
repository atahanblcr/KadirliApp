using KadirliApp.Application.Common.Auditing;
using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Places.Commands;

public record DeletePlaceCommand(Guid Id) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "places";
    public string AuditAction => "delete";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "Place";
}

public class DeletePlaceCommandHandler : IRequestHandler<DeletePlaceCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeletePlaceCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(DeletePlaceCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Place>();
        var place = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (place == null) return false;

        repo.Remove(place);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
