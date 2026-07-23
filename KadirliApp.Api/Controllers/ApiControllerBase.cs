using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace KadirliApp.Api.Controllers;

[ApiController]
[Route("v1/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _sender;

    protected ISender Sender => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>JWT'deki user_id claim'i (kontrat gereği snake_case); anonim istekte null.</summary>
    protected Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirstValue("user_id"), out var id) ? id : null;

    protected IActionResult Success(object? data = null)
    {
        return Ok(data);
    }
}
