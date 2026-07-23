using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class PowerOutage : BaseEntity
{
    public Guid? AnnouncementId { get; set; }
    public string? Neighborhood { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Reason { get; set; }
    public string? Source { get; set; }

    public Announcement? Announcement { get; set; }
}
