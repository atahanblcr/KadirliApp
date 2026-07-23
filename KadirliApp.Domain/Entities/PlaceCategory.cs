using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class PlaceCategory : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
}
