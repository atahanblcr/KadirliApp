using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class EventImage : BaseEntity
{
    public Guid EventId { get; set; }
    public Guid FileId { get; set; }
    public int DisplayOrder { get; set; }

    public Event Event { get; set; } = default!;
}
