using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace KadirliApp.Api.Controllers.Admin;

/// <summary>
/// Tüm v1/admin/* controller'larının tabanı. AdminPanel policy'si:
/// admin, super_admin ve moderator rollerini kabul eder (Program.cs).
/// </summary>
[Authorize(Policy = "AdminPanel")]
public abstract class AdminApiControllerBase : ApiControllerBase
{
    /// <summary>JWT'deki user_id claim'i (kontrat gereği snake_case).</summary>
    protected Guid CurrentAdminId =>
        Guid.TryParse(User.FindFirstValue("user_id"), out var id) ? id : Guid.Empty;
}
