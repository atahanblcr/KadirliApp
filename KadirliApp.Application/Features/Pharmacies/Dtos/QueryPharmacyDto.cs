using System;

namespace KadirliApp.Application.Features.Pharmacies.Dtos;

public record QueryPharmacyDto(
    string? Search,
    bool? IsActive,
    int Page = 1,
    int Limit = 20
);
