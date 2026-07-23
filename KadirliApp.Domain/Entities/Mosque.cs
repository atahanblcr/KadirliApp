using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class Mosque : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
