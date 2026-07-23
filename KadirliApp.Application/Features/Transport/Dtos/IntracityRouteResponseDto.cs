using System;
using System.Collections.Generic;

namespace KadirliApp.Application.Features.Transport.Dtos;

public class IntracityRouteResponseDto
{
    public Guid Id { get; set; }
    public string RouteNumber { get; set; } = default!;
    public string RouteName { get; set; } = default!;
    public TimeSpan? FirstDeparture { get; set; }
    public TimeSpan? LastDeparture { get; set; }
    public int? FrequencyMinutes { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Faz 10.8: güzergâh durakları — StopOrder'a göre sıralı (mobil hat detayı).</summary>
    public List<StopDto> Stops { get; set; } = new();

    public record StopDto(Guid Id, string StopName, int StopOrder, int? TimeFromStart);
}
