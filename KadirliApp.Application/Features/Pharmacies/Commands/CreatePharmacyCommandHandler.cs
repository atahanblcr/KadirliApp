using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Pharmacies.Commands;

public class CreatePharmacyCommandHandler : IRequestHandler<CreatePharmacyCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreatePharmacyCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(CreatePharmacyCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var pharmacy = new Pharmacy
        {
            Name = dto.Name,
            Address = dto.Address,
            Phone = dto.Phone,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            WorkingHours = dto.WorkingHours,
            PharmacistName = dto.PharmacistName,
            IsActive = dto.IsActive
        };

        await _uow.Repository<Pharmacy>().AddAsync(pharmacy, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return pharmacy.Id;
    }
}
