using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class GuideItem : BaseEntity
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? WorkingHours { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public Guid? LogoFileId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public GuideCategory Category { get; set; } = default!;
    public File? LogoFile { get; set; }
}
