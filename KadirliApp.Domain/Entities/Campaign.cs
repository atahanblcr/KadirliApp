using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class Campaign : BaseEntity, ISoftDeletable
{
    public Guid BusinessId { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal? DiscountPercentage { get; set; }
    public string? DiscountCode { get; set; }
    public string? Terms { get; set; }
    public decimal? MinimumAmount { get; set; }
    public int? StockLimit { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? CoverImageId { get; set; }
    public int CodeViewCount { get; set; }
    public string Status { get; set; } = "pending";
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectedReason { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Business Business { get; set; } = default!;
    public File? CoverImage { get; set; }
    public ICollection<CampaignImage> Images { get; set; } = new List<CampaignImage>();
    public ICollection<CampaignCodeView> CodeViews { get; set; } = new List<CampaignCodeView>();
}
