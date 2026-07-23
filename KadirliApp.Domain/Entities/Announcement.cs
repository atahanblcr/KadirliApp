using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class Announcement : BaseEntity, ISoftDeletable
{
    public Guid TypeId { get; set; }
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public int Priority { get; set; }
    public string? TargetType { get; set; }
    public string? TargetNeighborhoods { get; set; }
    public string? TargetUserIds { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public DateTime? SentAt { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
    public bool SendPushNotification { get; set; }
    public string? Source { get; set; }
    public string? SourceUrl { get; set; }
    public DateTime? VisibleUntil { get; set; }
    public bool HasPdf { get; set; }
    public Guid? PdfFileId { get; set; }
    public bool HasLink { get; set; }
    public string? ExternalLink { get; set; }
    public int ViewCount { get; set; }
    public int ClickCount { get; set; }
    public Guid? ImageFileId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? LocationName { get; set; }
    public string Status { get; set; } = "draft";
    public Guid? CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    public AnnouncementType Type { get; set; } = default!;
    public File? ImageFile { get; set; }
}
