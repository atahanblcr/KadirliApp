using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class EventCategory : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
