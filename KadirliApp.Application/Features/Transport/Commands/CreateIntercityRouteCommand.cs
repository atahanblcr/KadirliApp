using System;
using KadirliApp.Application.Common.Auditing;
using MediatR;

namespace KadirliApp.Application.Features.Transport.Commands;

/// <summary>
/// Faz 12.5: <c>VehicleType</c> + <c>DeparturePointId</c> eklendi ve komut
/// <see cref="IAuditableCommand"/> oldu (11.17'de yazılırken atlanmıştı — hattı kimin
/// eklediği denetim izinde görünmüyordu).
/// </summary>
public class CreateIntercityRouteCommand : IRequest<Guid>, IAuditableCommand
{
    public string Destination { get; set; } = default!;
    public decimal? Price { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Company { get; set; }

    /// <summary>"bus" | "minibus". Tanınmayan değer <b>varsayılana</b> (otobüs) düşer.</summary>
    public string? VehicleType { get; set; }

    /// <summary>Kalkış noktası — zorunlu değil (bkz. <c>TransportDeparturePointResolver</c>).</summary>
    public Guid? DeparturePointId { get; set; }

    public string AuditModule => "transport";
    public string AuditAction => "create";
    public string? AuditAffectedType => "IntercityRoute";
    public object? AuditDetails => new { destination = Destination, vehicleType = VehicleType };
}
