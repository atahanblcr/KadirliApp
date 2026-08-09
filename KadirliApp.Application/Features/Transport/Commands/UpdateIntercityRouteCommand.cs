using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using MediatR;

namespace KadirliApp.Application.Features.Transport.Commands;

/// <summary>
/// Faz 11.17: şehirlerarası hat düzenleme. 10.8'de yalnız Create yazılmıştı — hat bir kez
/// oluşturulduktan sonra fiyatı/firması panelden değiştirilemiyordu (yalnız psql ile).
/// Faz 12.5: araç tipi + kalkış noktası eklendi, komut denetim izine düşer oldu.
/// </summary>
public class UpdateIntercityRouteCommand : IRequest<bool>, IAuditableCommand
{
    public Guid Id { get; set; }
    public string Destination { get; set; } = default!;
    public decimal? Price { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Company { get; set; }
    public bool IsActive { get; set; }

    /// <summary>"bus" | "minibus". Tanınmayan değer <b>varsayılana</b> (otobüs) düşer.</summary>
    public string? VehicleType { get; set; }

    /// <summary>Kalkış noktası — boş bırakılabilir (bkz. <c>TransportDeparturePointResolver</c>).</summary>
    public Guid? DeparturePointId { get; set; }

    public string AuditModule => "transport";
    public string AuditAction => "update";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "IntercityRoute";
    public object? AuditDetails => new { destination = Destination, vehicleType = VehicleType, isActive = IsActive };
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

        // 🐛 Kaydın ŞU ANKİ kalkış noktası da veriliyor: verilmezse, noktası sonradan
        // pasifleştirilen bir hat hiç düzenlenemez hâle gelir (12.4'ün etkinlik ilçesinde
        // canlıda görülen hatasının aynısı).
        var point = await TransportDeparturePointResolver.ResolveAsync(
            _uow, request.DeparturePointId, cancellationToken, currentDeparturePointId: route.DeparturePointId);
        if (!point.IsValid)
            throw new AppException(TransportDeparturePointResolver.NotFoundMessage, "VALIDATION_ERROR");

        route.Destination = request.Destination;
        route.Price = request.Price;
        route.DurationMinutes = request.DurationMinutes;
        route.Company = request.Company;
        route.VehicleType = TransportVehicleTypes.Normalize(request.VehicleType);
        route.DeparturePointId = point.Id;
        route.IsActive = request.IsActive;

        repo.Update(route);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
