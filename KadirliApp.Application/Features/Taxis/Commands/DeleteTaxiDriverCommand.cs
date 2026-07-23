using KadirliApp.Application.Common.Auditing;
using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Taxis.Commands;

public record DeleteTaxiDriverCommand(Guid Id) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "taxis";
    public string AuditAction => "delete";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "TaxiDriver";
}

public class DeleteTaxiDriverCommandHandler : IRequestHandler<DeleteTaxiDriverCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteTaxiDriverCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteTaxiDriverCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<TaxiDriver>();
        var driver = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (driver == null) return false;

        repo.SoftRemove(driver);
        repo.Update(driver);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
