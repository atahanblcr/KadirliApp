using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class AdImage : BaseEntity
{
    public Guid AdId { get; set; }
    public Guid FileId { get; set; }
    public bool IsCover { get; set; }
    public int DisplayOrder { get; set; }

    public Ad Ad { get; set; } = default!;
}
