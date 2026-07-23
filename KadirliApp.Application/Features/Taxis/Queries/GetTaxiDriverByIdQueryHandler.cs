using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Taxis.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Taxis.Queries;

public class GetTaxiDriverByIdQueryHandler : IRequestHandler<GetTaxiDriverByIdQuery, TaxiDriverResponseDto?>
{
    private readonly IUnitOfWork _uow;

    public GetTaxiDriverByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<TaxiDriverResponseDto?> Handle(GetTaxiDriverByIdQuery request, CancellationToken cancellationToken)
    {
        var driver = await _uow.Repository<TaxiDriver>().GetByIdAsync(request.Id, cancellationToken);
        if (driver == null) return null;

        // Faz 10.7 düzeltmesi: public uçta doğrulanmamış/pasif sürücü (telefonuyla) dönmez.
        if (request.OnlyPublic && (!driver.IsVerified || !driver.IsActive))
            return null;

        return new TaxiDriverResponseDto
        {
            Id = driver.Id,
            UserId = driver.UserId,
            Name = driver.Name,
            Phone = driver.Phone,
            Plaka = driver.Plaka,
            VehicleInfo = driver.VehicleInfo,
            IsVerified = driver.IsVerified,
            IsActive = driver.IsActive
        };
    }
}
