using KadirliApp.Application.Common.Auditing;
using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Transport.Commands;

/// <summary>
/// Faz 11.17: şehirlerarası hat silme. <see cref="DeleteIntracityRouteCommand"/> ile aynı karar —
/// IntercityRoute soft-delete DEĞİL (lookup verisi), hard delete; kalkış saatleri FK cascade ile gider.
/// </summary>
public sealed record DeleteIntercityRouteCommand(Guid Id) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "transport";
    public string AuditAction => "delete";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "IntercityRoute";
}

public sealed class DeleteIntercityRouteCommandHandler : IRequestHandler<DeleteIntercityRouteCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteIntercityRouteCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(DeleteIntercityRouteCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<IntercityRoute>();
        var route = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (route == null) return false;

        repo.Remove(route);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
