using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class Pharmacy : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? WorkingHours { get; set; }
    public string? PharmacistName { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<PharmacySchedule> Schedules { get; set; } = new List<PharmacySchedule>();
}
