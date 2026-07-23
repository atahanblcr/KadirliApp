using System;

namespace KadirliApp.Application.Features.Complaints.Dtos;

public class ComplaintResponseDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Type { get; set; }
    public string? RelatedModule { get; set; }
    public Guid? RelatedId { get; set; }
    public string Subject { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? AdminNotes { get; set; }
    public Guid? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
