using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class CampaignCodeView : BaseEntity
{
    public Guid CampaignId { get; set; }
    public Guid UserId { get; set; }
    public DateTime ViewedAt { get; set; }

    public Campaign Campaign { get; set; } = default!;
    public User User { get; set; } = default!;
}
