using System;
using MediatR;

namespace KadirliApp.Application.Features.Transport.Commands;

public class UpdateIntracityRouteCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string RouteNumber { get; set; } = default!;
    public string RouteName { get; set; } = default!;
    public TimeSpan? FirstDeparture { get; set; }
    public TimeSpan? LastDeparture { get; set; }
    public int? FrequencyMinutes { get; set; }
    public bool IsActive { get; set; }
}
