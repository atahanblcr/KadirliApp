using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class IntracityStop : BaseEntity
{
    public Guid RouteId { get; set; }
    public string StopName { get; set; } = default!;
    public int StopOrder { get; set; }
    public int? TimeFromStart { get; set; }

    public IntracityRoute Route { get; set; } = default!;
}
