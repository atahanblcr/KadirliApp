using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class AdFavorite : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid AdId { get; set; }

    public Ad Ad { get; set; } = default!;
}
