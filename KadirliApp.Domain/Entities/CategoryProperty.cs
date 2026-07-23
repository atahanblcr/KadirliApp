using KadirliApp.Domain.Common;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Domain.Entities;

public class CategoryProperty : BaseEntity
{
    public Guid CategoryId { get; set; }
    public string PropertyName { get; set; } = default!;
    public PropertyType PropertyType { get; set; } = PropertyType.Text;
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public int DisplayOrder { get; set; }

    public AdCategory Category { get; set; } = default!;
    public ICollection<PropertyOption> Options { get; set; } = new List<PropertyOption>();
}
