using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Pharmacies.Commands;

public class DeletePharmacyCommandHandler : IRequestHandler<DeletePharmacyCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeletePharmacyCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(DeletePharmacyCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Pharmacy>();
        var pharmacy = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (pharmacy == null) return false;

        repo.Remove(pharmacy);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
