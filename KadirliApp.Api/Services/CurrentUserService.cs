using System.Security.Claims;
using KadirliApp.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace KadirliApp.Api.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userIdStr = _httpContextAccessor.HttpContext?.User?.FindFirstValue("user_id");
            if (Guid.TryParse(userIdStr, out var userId))
            {
                return userId;
            }
            return null;
        }
    }

    public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
}
