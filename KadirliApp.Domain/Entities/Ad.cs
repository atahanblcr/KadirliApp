using KadirliApp.Domain.Common;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Domain.Entities;

public class Ad : BaseEntity, ISoftDeletable
{
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal? Price { get; set; }
    public Guid UserId { get; set; }
    public string? SellerName { get; set; }
    public string ContactPhone { get; set; } = default!;
    public string Status { get; set; } = "pending";
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectedReason { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int ExtensionCount { get; set; }
    public int MaxExtensions { get; set; } = 3;
    public int ViewCount { get; set; }
    public int PhoneClickCount { get; set; }
    public int WhatsappClickCount { get; set; }
    public DateTime? DeletedAt { get; set; }

    public AdCategory Category { get; set; } = default!;
    public ICollection<AdImage> Images { get; set; } = new List<AdImage>();
    public ICollection<AdPropertyValue> PropertyValues { get; set; } = new List<AdPropertyValue>();
    public ICollection<AdFavorite> Favorites { get; set; } = new List<AdFavorite>();
    public ICollection<AdExtension> Extensions { get; set; } = new List<AdExtension>();
}
