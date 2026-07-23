using System;

namespace KadirliApp.Application.Features.Pharmacies.Dtos;

public record PharmacyResponseDto(
    Guid Id,
    string Name,
    string? Address,
    string? Phone,
    decimal? Latitude,
    decimal? Longitude,
    string? WorkingHours,
    string? PharmacistName,
    bool IsActive
);
