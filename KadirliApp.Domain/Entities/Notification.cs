using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public string? Type { get; set; }
    public Guid? RelatedId { get; set; }
    public string? RelatedType { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool FcmSent { get; set; }
    public DateTime? FcmSentAt { get; set; }
    public string? FcmError { get; set; }

    public User User { get; set; } = default!;
}
