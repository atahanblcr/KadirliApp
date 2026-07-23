using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class IntercityRoute : BaseEntity
{
    public string Destination { get; set; } = default!;
    public decimal? Price { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Company { get; set; }
    public bool IsActive { get; set; } = true;
    
    public ICollection<IntercitySchedule> Schedules { get; set; } = new List<IntercitySchedule>();
}
