using System;
using MediatR;

namespace KadirliApp.Application.Features.Places.Commands;

public class UpdatePlaceCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? EntranceFee { get; set; }
    public bool IsFree { get; set; }
    public string? OpeningHours { get; set; }
    public string? BestSeason { get; set; }
    public string? HowToGetThere { get; set; }
    public decimal? DistanceFromCenter { get; set; }

    /// <summary>JSON sözlük: {"wc": true, "wifi": false, ...}</summary>
    public string? Amenities { get; set; }
    public Guid? CoverImageId { get; set; }
    public bool RemoveCoverImage { get; set; }
    public bool IsActive { get; set; } = true;
}
