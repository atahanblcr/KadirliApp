using System;

namespace KadirliApp.Application.Features.PowerOutages.DTOs;

public class CreatePowerOutageDto
{
    public string? Neighborhood { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Reason { get; set; }
}
