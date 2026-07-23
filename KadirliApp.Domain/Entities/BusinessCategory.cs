using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class BusinessCategory : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public Guid? ParentId { get; set; }

    public BusinessCategory? Parent { get; set; }
    public ICollection<BusinessCategory> SubCategories { get; set; } = new List<BusinessCategory>();
}
