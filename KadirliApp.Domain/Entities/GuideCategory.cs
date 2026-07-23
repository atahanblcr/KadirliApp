using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class GuideCategory : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public Guid? ParentId { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int DisplayOrder { get; set; }

    public GuideCategory? Parent { get; set; }
    public ICollection<GuideCategory> SubCategories { get; set; } = new List<GuideCategory>();
}
