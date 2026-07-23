using System;

namespace KadirliApp.Application.Features.Taxis.Dtos;

public class TaxiDriverResponseDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Name { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string? Plaka { get; set; }
    public string? VehicleInfo { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; }
}
