using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class Place : BaseEntity
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? EntranceFee { get; set; }
    public bool IsFree { get; set; }
    public string? OpeningHours { get; set; }
    public string? Amenities { get; set; }
    public string? BestSeason { get; set; }
    public string? HowToGetThere { get; set; }
    public decimal? DistanceFromCenter { get; set; }
    public Guid? CoverImageId { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }

    public PlaceCategory Category { get; set; } = default!;
    public File? CoverImage { get; set; }
    public User? Creator { get; set; }
    public ICollection<PlaceImage> Images { get; set; } = new List<PlaceImage>();
}
