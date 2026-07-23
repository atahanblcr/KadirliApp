using Microsoft.AspNetCore.Authorization;

namespace KadirliApp.Api.Authorization;

/// <summary>
/// NestJS @Permission(module, action) karşılığı (masterclass 12.5).
/// super_admin/admin her zaman geçer; moderator admin_permissions tablosundan kontrol edilir.
/// </summary>
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "perm:";

    public RequirePermissionAttribute(string module, string action)
        => Policy = $"{PolicyPrefix}{module}:{action}";
}
