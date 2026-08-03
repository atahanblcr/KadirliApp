using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Transport.Commands;

/// <summary>
/// Faz 11.17: şehirlerarası hat düzenleme. 10.8'de yalnız Create yazılmıştı — hat bir kez
/// oluşturulduktan sonra fiyatı/firması panelden değiştirilemiyordu (yalnız psql ile).
/// </summary>
public class UpdateIntercityRouteCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string Destination { get; set; } = default!;
    public decimal? Price { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Company { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateIntercityRouteCommandHandler : IRequestHandler<UpdateIntercityRouteCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UpdateIntercityRouteCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(UpdateIntercityRouteCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<IntercityRoute>();
        var route = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (route == null) return false;

        route.Destination = request.Destination;
        route.Price = request.Price;
        route.DurationMinutes = request.DurationMinutes;
        route.Company = request.Company;
        route.IsActive = request.IsActive;

        repo.Update(route);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
