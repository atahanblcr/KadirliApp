using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class IntercitySchedule : BaseEntity
{
    public Guid RouteId { get; set; }
    public TimeSpan DepartureTime { get; set; }
    public bool IsActive { get; set; } = true;

    public IntercityRoute Route { get; set; } = default!;
}
