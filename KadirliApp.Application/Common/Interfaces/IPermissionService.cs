using System;
using System.Threading;
using System.Threading.Tasks;

namespace KadirliApp.Application.Common.Interfaces;

/// <summary>
/// NestJS PermissionGuard'ın veri tarafı: moderator'ün admin_permissions tablosundaki
/// modül bazlı can_* bayraklarını okur (masterclass 12.5). Rol bypass'ı (super_admin/admin)
/// burada değil, Api katmanındaki PermissionHandler'dadır.
/// </summary>
public interface IPermissionService
{
    /// <param name="action">"read" | "create" | "update" | "delete" | "approve"</param>
    Task<bool> HasAsync(Guid userId, string module, string action, CancellationToken ct = default);
}
