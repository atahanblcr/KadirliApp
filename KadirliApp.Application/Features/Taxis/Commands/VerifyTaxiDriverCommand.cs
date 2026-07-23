using KadirliApp.Application.Common.Auditing;
using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Taxis.Commands;

public record VerifyTaxiDriverCommand(Guid Id, Guid AdminId) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "taxis";
    public string AuditAction => "verify";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "TaxiDriver";
}

public class VerifyTaxiDriverCommandHandler : IRequestHandler<VerifyTaxiDriverCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public VerifyTaxiDriverCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(VerifyTaxiDriverCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<TaxiDriver>();
        var driver = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (driver == null) return false;

        driver.IsVerified = true;
        driver.VerifiedBy = request.AdminId;
        driver.VerifiedAt = DateTime.UtcNow;

        repo.Update(driver);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
