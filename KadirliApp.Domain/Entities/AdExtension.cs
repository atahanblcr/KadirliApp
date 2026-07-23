using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class AdExtension : BaseEntity
{
    public Guid AdId { get; set; }
    public Guid UserId { get; set; }
    public int AdsWatched { get; set; }
    public int DaysExtended { get; set; }
    public DateTime ExtendedAt { get; set; }

    public Ad Ad { get; set; } = default!;
}
