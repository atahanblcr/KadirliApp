using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class PharmacySchedule : BaseEntity
{
    public Guid PharmacyId { get; set; }
    public DateTime DutyDate { get; set; }
    public TimeSpan StartTime { get; set; } = new TimeSpan(19, 0, 0);
    public TimeSpan EndTime { get; set; } = new TimeSpan(9, 0, 0);
    public string? Source { get; set; }

    public Pharmacy Pharmacy { get; set; } = default!;
}
