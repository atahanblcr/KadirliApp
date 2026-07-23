using System;
using MediatR;

namespace KadirliApp.Application.Features.Taxis.Commands;

public class CreateTaxiDriverCommand : IRequest<Guid>
{
    public Guid? UserId { get; set; }
    public string Name { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string? Plaka { get; set; }
    public string? VehicleInfo { get; set; }
}
