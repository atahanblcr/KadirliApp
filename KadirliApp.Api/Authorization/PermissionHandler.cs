using System.Security.Claims;
using KadirliApp.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace KadirliApp.Api.Authorization;

/// <summary>NestJS PermissionGuard karşılığı (masterclass 12.5).</summary>
public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _perm;

    public PermissionHandler(IPermissionService perm)
    {
        _perm = perm;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx, PermissionRequirement req)
    {
        var role = ctx.User.FindFirstValue(ClaimTypes.Role);
        if (role is "super_admin" or "admin")
        {
            ctx.Succeed(req);
            return;
        }

        if (role == "moderator" &&
            Guid.TryParse(ctx.User.FindFirstValue("user_id"), out var userId) &&
            await _perm.HasAsync(userId, req.Module, req.Action))
        {
            ctx.Succeed(req);
        }
    }
}
