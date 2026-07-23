using KadirliApp.Domain.Common;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Domain.Entities;

public class DeathNotice : BaseEntity, ISoftDeletable
{
    public string DeceasedName { get; set; } = default!;
    public Guid? PhotoFileId { get; set; }
    public DateTime FuneralDate { get; set; }
    public TimeSpan FuneralTime { get; set; }
    public Guid? CemeteryId { get; set; }
    public Guid? MosqueId { get; set; }
    public Guid? NeighborhoodId { get; set; }
    public string? CondolenceAddress { get; set; }
    public decimal? CondolenceLatitude { get; set; }
    public decimal? CondolenceLongitude { get; set; }
    public Guid AddedBy { get; set; }
    public string Status { get; set; } = "pending";
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectedReason { get; set; }
    public DateTime? AutoArchiveAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Cemetery? Cemetery { get; set; }
    public Mosque? Mosque { get; set; }
}
