using KadirliApp.Application.Common.Auditing;
using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Transport.Commands;

/// <summary>
/// Faz 10.9(h): Web panelindeki inline hard delete Application'a taşındı (Faz 9.4 kuralı).
/// IntracityRoute soft-delete DEĞİL (lookup verisi) — hard delete; duraklar FK cascade ile silinir.
/// </summary>
public sealed record DeleteIntracityRouteCommand(Guid Id) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "transport";
    public string AuditAction => "delete";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "IntracityRoute";
}

public sealed class DeleteIntracityRouteCommandHandler : IRequestHandler<DeleteIntracityRouteCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteIntracityRouteCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(DeleteIntracityRouteCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<IntracityRoute>();
        var route = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (route == null) return false;

        repo.Remove(route);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
