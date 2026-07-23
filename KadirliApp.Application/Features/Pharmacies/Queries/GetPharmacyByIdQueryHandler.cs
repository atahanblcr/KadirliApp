using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Pharmacies.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Pharmacies.Queries;

public class GetPharmacyByIdQueryHandler : IRequestHandler<GetPharmacyByIdQuery, PharmacyResponseDto?>
{
    private readonly IUnitOfWork _uow;

    public GetPharmacyByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PharmacyResponseDto?> Handle(GetPharmacyByIdQuery request, CancellationToken cancellationToken)
    {
        var pharmacy = await _uow.Repository<Pharmacy>().GetByIdAsync(request.Id, cancellationToken);
        if (pharmacy == null) return null;

        // Faz 10.7 düzeltmesi: public uçta pasif eczane dönmez.
        if (request.OnlyActive && !pharmacy.IsActive)
            return null;

        return new PharmacyResponseDto(
            pharmacy.Id,
            pharmacy.Name,
            pharmacy.Address,
            pharmacy.Phone,
            pharmacy.Latitude,
            pharmacy.Longitude,
            pharmacy.WorkingHours,
            pharmacy.PharmacistName,
            pharmacy.IsActive
        );
    }
}
