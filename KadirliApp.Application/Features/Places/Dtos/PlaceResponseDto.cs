using System;

namespace KadirliApp.Application.Features.Places.Dtos;

public class PlaceResponseDto
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
    public string? Amenities { get; set; }
    public Guid? CoverImageId { get; set; }
    public string? CoverImageUrl { get; set; }
    public bool IsActive { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
