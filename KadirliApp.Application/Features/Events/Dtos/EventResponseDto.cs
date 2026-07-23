using System;

namespace KadirliApp.Application.Features.Events.Dtos;

public class EventResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public DateTime EventDate { get; set; }
    public TimeSpan EventTime { get; set; }
    public string? VenueName { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool HasLocation => Latitude.HasValue && Longitude.HasValue;
    public string? Organizer { get; set; }
    public decimal? TicketPrice { get; set; }
    public bool IsFree { get; set; }
    public bool IsLocal { get; set; }
    public Guid? CoverImageId { get; set; }
    public string? CoverImageUrl { get; set; }
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
