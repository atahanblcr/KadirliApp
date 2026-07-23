using System;
using System.Net;
using System.Security.Claims;
using KadirliApp.Application.Common.Auditing;
using Microsoft.AspNetCore.Http;

namespace KadirliApp.Web.Common;

/// <summary>Faz 10.9(i): AuditBehavior'a aktör bilgisi — panel cookie'sindeki NameIdentifier claim'i + istek IP/UA.</summary>
public sealed class HttpAuditContext : IAuditContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpAuditContext(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid? UserId =>
        Guid.TryParse(_accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public IPAddress? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress;

    public string? UserAgent => _accessor.HttpContext?.Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null;
}
