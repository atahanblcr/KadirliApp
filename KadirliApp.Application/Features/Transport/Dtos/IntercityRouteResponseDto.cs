using System;
using System.Collections.Generic;

namespace KadirliApp.Application.Features.Transport.Dtos;

public class IntercityRouteResponseDto
{
    public Guid Id { get; set; }
    public string Destination { get; set; } = default!;
    public decimal? Price { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Company { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Faz 10.8: kalkış saatleri — "HH:mm", sıralı; yalnız aktif seferler (mobil "Adana otobüsü kaçta?").</summary>
    public List<ScheduleDto> Schedules { get; set; } = new();

    public record ScheduleDto(Guid Id, string DepartureTime);
}
