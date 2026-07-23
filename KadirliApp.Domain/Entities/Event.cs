using KadirliApp.Domain.Common;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Domain.Entities;

public class Event : BaseEntity, ISoftDeletable
{
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public Guid CategoryId { get; set; }
    public DateTime EventDate { get; set; }
    public TimeSpan EventTime { get; set; }
    public int? DurationMinutes { get; set; }
    public string? VenueName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Organizer { get; set; }
    public decimal? TicketPrice { get; set; }
    public bool IsFree { get; set; }
    public int? AgeRestriction { get; set; }
    public int? Capacity { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? TicketUrl { get; set; }
    public Guid? CoverImageId { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
    public bool IsLocal { get; set; }
    public string Status { get; set; } = "pending";
    public Guid CreatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    public EventCategory Category { get; set; } = default!;
    public File? CoverImage { get; set; }
    public ICollection<EventImage> Images { get; set; } = new List<EventImage>();
}
