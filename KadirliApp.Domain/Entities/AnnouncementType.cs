using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class AnnouncementType : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int DisplayOrder { get; set; }
}
