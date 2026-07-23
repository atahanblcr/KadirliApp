namespace KadirliApp.Domain.Entities;

public class AnnouncementView
{
    public Guid AnnouncementId { get; set; }
    public Guid UserId { get; set; }

    public Announcement Announcement { get; set; } = default!;
    public User User { get; set; } = default!;
}
