using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class AdminPermission : BaseEntity
{
    public Guid UserId { get; set; }
    public string Module { get; set; } = default!;
    public bool CanRead { get; set; }
    public bool CanCreate { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanApprove { get; set; }

    public User User { get; set; } = default!;
}
