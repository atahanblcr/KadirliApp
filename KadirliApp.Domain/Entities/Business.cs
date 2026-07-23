using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class Business : BaseEntity
{
    public Guid? UserId { get; set; }
    public string BusinessName { get; set; } = default!;
    public Guid CategoryId { get; set; }
    public string? TaxNumber { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? InstagramHandle { get; set; }
    public Guid? LogoFileId { get; set; }
    public bool IsVerified { get; set; }
    public Guid? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public BusinessCategory Category { get; set; } = default!;
    public File? LogoFile { get; set; }
    public User? User { get; set; }
    public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
}
