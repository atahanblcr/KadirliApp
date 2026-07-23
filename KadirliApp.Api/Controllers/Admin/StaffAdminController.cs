using KadirliApp.Api.Authorization;
using KadirliApp.Application.Features.Staff.Commands;
using KadirliApp.Application.Features.Staff.Queries;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers.Admin;

/// <summary>
/// Masterclass: /admin/staff (CRUD + permissions + reset-password).
/// admin/super_admin bypass; moderator ancak "staff" modül izniyle erişebilir.
/// </summary>
[Route("v1/admin/staff")]
public class StaffAdminController : AdminApiControllerBase
{
    [HttpGet]
    [RequirePermission("staff", "read")]
    public async Task<IActionResult> GetList([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        return Success(await Sender.Send(new GetStaffQuery(search, page, limit)));
    }

    [HttpGet("{id}")]
    [RequirePermission("staff", "read")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Success(await Sender.Send(new GetStaffByIdQuery(id)));
    }

    [HttpPost]
    [RequirePermission("staff", "create")]
    public async Task<IActionResult> Create([FromBody] CreateStaffCommand command)
    {
        return Success(await Sender.Send(command));
    }

    [HttpPut("{id}")]
    [RequirePermission("staff", "update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStaffCommand command)
    {
        command.Id = id;
        return Success(await Sender.Send(command));
    }

    [HttpDelete("{id}")]
    [RequirePermission("staff", "delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return Success(await Sender.Send(new DeleteStaffCommand(id, CurrentAdminId)));
    }

    [HttpPut("{id}/permissions")]
    [RequirePermission("staff", "update")]
    public async Task<IActionResult> SetPermissions(Guid id, [FromBody] SetStaffPermissionsCommand command)
    {
        command.Id = id;
        return Success(await Sender.Send(command));
    }

    [HttpPost("{id}/reset-password")]
    [RequirePermission("staff", "update")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetStaffPasswordCommand command)
    {
        command.Id = id;
        return Success(await Sender.Send(command));
    }
}
