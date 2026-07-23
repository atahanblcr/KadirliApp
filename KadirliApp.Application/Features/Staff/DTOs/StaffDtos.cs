using System;
using System.Collections.Generic;

namespace KadirliApp.Application.Features.Staff.DTOs;

/// <summary>Panel personeli (moderator/admin) — masterclass /admin/staff kontratı.</summary>
public class StaffDto
{
    public Guid Id { get; set; }
    public string Phone { get; set; } = default!;
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string Role { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<StaffPermissionDto> Permissions { get; set; } = new();
}

/// <summary>admin_permissions satırının karşılığı (modül bazlı can_* bayrakları).</summary>
public class StaffPermissionDto
{
    public string Module { get; set; } = default!;
    public bool CanRead { get; set; }
    public bool CanCreate { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanApprove { get; set; }
}
