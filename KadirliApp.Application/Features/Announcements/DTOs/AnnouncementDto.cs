using System;
using System.Collections.Generic;

namespace KadirliApp.Application.Features.Announcements.DTOs;

public class AnnouncementDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public Guid TypeId { get; set; }
    public string? TypeName { get; set; }
    public int Priority { get; set; }
    public string Status { get; set; } = default!;
    public string? TargetType { get; set; }
    public List<Guid>? TargetNeighborhoodIds { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? VisibleUntil { get; set; }
    public bool SendPushNotification { get; set; }
    public string? Source { get; set; }
    public string? SourceUrl { get; set; }
    public bool HasLink { get; set; }
    public string? ExternalLink { get; set; }

    // Mobil istemcinin görsel ve konum butonu için kullanacağı alanlar
    public Guid? ImageFileId { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? LocationName { get; set; }
    public bool HasLocation => Latitude.HasValue && Longitude.HasValue;

    public DateTime CreatedAt { get; set; }
}
