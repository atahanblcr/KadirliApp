using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class PropertyOption : BaseEntity
{
    public Guid PropertyId { get; set; }
    public string OptionValue { get; set; } = default!;
    public int DisplayOrder { get; set; }

    public CategoryProperty Property { get; set; } = default!;
}
