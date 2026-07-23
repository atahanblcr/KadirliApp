using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class AdCategory : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public Guid? ParentId { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public AdCategory? Parent { get; set; }
    public ICollection<AdCategory> SubCategories { get; set; } = new List<AdCategory>();
    public ICollection<CategoryProperty> Properties { get; set; } = new List<CategoryProperty>();
}
