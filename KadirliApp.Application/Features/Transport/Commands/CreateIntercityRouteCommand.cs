using System;
using MediatR;

namespace KadirliApp.Application.Features.Transport.Commands;

public class CreateIntercityRouteCommand : IRequest<Guid>
{
    public string Destination { get; set; } = default!;
    public decimal? Price { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Company { get; set; }
}
