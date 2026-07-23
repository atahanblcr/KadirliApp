using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Taxis.Commands;

public class UpdateTaxiDriverCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string? Plaka { get; set; }
    public string? VehicleInfo { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateTaxiDriverCommandHandler : IRequestHandler<UpdateTaxiDriverCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UpdateTaxiDriverCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateTaxiDriverCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<TaxiDriver>();
        var driver = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (driver == null) return false;

        driver.Name = request.Name;
        driver.Phone = request.Phone;
        driver.Plaka = request.Plaka;
        driver.VehicleInfo = request.VehicleInfo;
        driver.IsActive = request.IsActive;

        repo.Update(driver);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
