using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid UserId { get; set; }
    public string Action { get; set; } = default!;
    public string Module { get; set; } = default!;
    public Guid? AffectedId { get; set; }
    public string? AffectedType { get; set; }
    public string? Details { get; set; }
    public System.Net.IPAddress? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
