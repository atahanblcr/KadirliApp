using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class CampaignImage : BaseEntity
{
    public Guid CampaignId { get; set; }
    public Guid FileId { get; set; }
    public int DisplayOrder { get; set; }

    public Campaign Campaign { get; set; } = default!;
    public File File { get; set; } = default!;
}
