using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class AdPropertyValue : BaseEntity
{
    public Guid AdId { get; set; }
    public Guid PropertyId { get; set; }
    public string Value { get; set; } = default!;

    public Ad Ad { get; set; } = default!;
    public CategoryProperty Property { get; set; } = default!;
}
