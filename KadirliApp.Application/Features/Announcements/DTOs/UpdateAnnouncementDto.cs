using System;
using System.Collections.Generic;

namespace KadirliApp.Application.Features.Announcements.DTOs;

public class UpdateAnnouncementDto
{
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public Guid TypeId { get; set; }

    public int Priority { get; set; } = 0;
    public string? TargetType { get; set; }
    public List<Guid>? TargetNeighborhoodIds { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public bool IsRecurring { get; set; } = false;
    public string? RecurrencePattern { get; set; }
    public bool SendPushNotification { get; set; } = true;
    public string? Source { get; set; }
    public string? SourceUrl { get; set; }
    public DateTime? VisibleUntil { get; set; }
    public bool HasPdf { get; set; } = false;
    public bool HasLink { get; set; } = false;
    public string? ExternalLink { get; set; }

    // Opsiyonel görsel ve konum
    public Guid? ImageFileId { get; set; }
    public bool RemoveImage { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? LocationName { get; set; }
}
