using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class IntracityRoute : BaseEntity
{
    public string RouteNumber { get; set; } = default!;
    public string RouteName { get; set; } = default!;
    public TimeSpan? FirstDeparture { get; set; }
    public TimeSpan? LastDeparture { get; set; }
    public int? FrequencyMinutes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<IntracityStop> Stops { get; set; } = new List<IntracityStop>();
}
