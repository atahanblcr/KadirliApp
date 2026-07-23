using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class PlaceImage : BaseEntity
{
    public Guid PlaceId { get; set; }
    public Guid FileId { get; set; }
    public int DisplayOrder { get; set; }

    public Place Place { get; set; } = default!;
    public File File { get; set; } = default!;
}
