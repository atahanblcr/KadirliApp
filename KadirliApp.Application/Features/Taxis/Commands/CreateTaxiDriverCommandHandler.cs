using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Taxis.Commands;

public class CreateTaxiDriverCommandHandler : IRequestHandler<CreateTaxiDriverCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateTaxiDriverCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateTaxiDriverCommand request, CancellationToken cancellationToken)
    {
        var driver = new TaxiDriver
        {
            UserId = request.UserId,
            Name = request.Name,
            Phone = request.Phone,
            Plaka = request.Plaka,
            VehicleInfo = request.VehicleInfo,
            IsVerified = false,
            IsActive = true
        };

        await _uow.Repository<TaxiDriver>().AddAsync(driver, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return driver.Id;
    }
}
