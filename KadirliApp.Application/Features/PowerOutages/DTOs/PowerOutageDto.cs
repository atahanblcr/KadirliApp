using System;

namespace KadirliApp.Application.Features.PowerOutages.DTOs;

public class PowerOutageDto
{
    public Guid Id { get; set; }
    public string? Neighborhood { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Reason { get; set; }
}
