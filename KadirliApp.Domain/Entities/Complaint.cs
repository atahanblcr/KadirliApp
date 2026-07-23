using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class Complaint : BaseEntity
{
    public Guid? UserId { get; set; }
    public string? Type { get; set; }
    public string? RelatedModule { get; set; }
    public Guid? RelatedId { get; set; }
    public string Subject { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string Status { get; set; } = "pending";
    public string? AdminNotes { get; set; }
    public Guid? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public User? User { get; set; }
    public User? Resolver { get; set; }
}
