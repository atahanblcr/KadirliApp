using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class TaxiCall : BaseEntity
{
    public Guid PassengerId { get; set; }
    public Guid DriverId { get; set; }
    public DateTime CalledAt { get; set; }

    public User Passenger { get; set; } = default!;
    public TaxiDriver Driver { get; set; } = default!;
}
