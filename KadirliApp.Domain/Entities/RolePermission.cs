using KadirliApp.Domain.Common;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Domain.Entities;

public class RolePermission : BaseEntity
{
    public UserRole Role { get; set; }
    public Guid PermissionId { get; set; }

    public Permission Permission { get; set; } = default!;
}
