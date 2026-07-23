using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class Permission : BaseEntity
{
    public string Module { get; set; } = default!;
    public string Action { get; set; } = default!;
}
