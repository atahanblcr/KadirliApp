using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Pharmacies.Commands;

public class UpdatePharmacyCommandHandler : IRequestHandler<UpdatePharmacyCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UpdatePharmacyCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(UpdatePharmacyCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Pharmacy>();
        var pharmacy = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (pharmacy == null) return false;

        var dto = request.Dto;
        pharmacy.Name = dto.Name;
        pharmacy.Address = dto.Address;
        pharmacy.Phone = dto.Phone;
        pharmacy.Latitude = dto.Latitude;
        pharmacy.Longitude = dto.Longitude;
        pharmacy.WorkingHours = dto.WorkingHours;
        pharmacy.PharmacistName = dto.PharmacistName;
        pharmacy.IsActive = dto.IsActive;

        repo.Update(pharmacy);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
