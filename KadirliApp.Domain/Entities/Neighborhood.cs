using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class Neighborhood : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Type { get; set; }
    public int? Population { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<UserNeighborhood> UserNeighborhoods { get; set; } = new List<UserNeighborhood>();
}
