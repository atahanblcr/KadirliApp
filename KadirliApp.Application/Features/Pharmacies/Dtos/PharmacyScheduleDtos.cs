using System;

namespace KadirliApp.Application.Features.Pharmacies.Dtos;

/// <summary>Faz 10.4: nöbetçi eczane yanıtı — schedule + eczane bilgisi tek satırda (mobil tek istekle gösterir).</summary>
public record OnDutyPharmacyDto(
    Guid ScheduleId,
    DateTime DutyDate,
    string StartTime,
    string EndTime,
    Guid PharmacyId,
    string Name,
    string? Address,
    string? Phone,
    decimal? Latitude,
    decimal? Longitude,
    string? PharmacistName,
    string? WorkingHours
);

/// <summary>Aylık nöbet takvimi satırı.</summary>
public record PharmacyScheduleEntryDto(
    Guid Id,
    DateTime DutyDate,
    string StartTime,
    string EndTime,
    Guid PharmacyId,
    string PharmacyName,
    string? Source
);

public record CreatePharmacyScheduleDto(
    Guid PharmacyId,
    DateTime DutyDate,
    TimeSpan? StartTime,
    TimeSpan? EndTime,
    string? Source
);
