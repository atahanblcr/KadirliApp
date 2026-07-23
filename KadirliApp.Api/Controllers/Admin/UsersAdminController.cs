using KadirliApp.Api.Authorization;
using KadirliApp.Application.Features.Users.Commands.CreateUser;
using KadirliApp.Application.Features.Users.Commands.SetUserBan;
using KadirliApp.Application.Features.Users.Commands.UpdateUser;
using KadirliApp.Application.Features.Users.DTOs;
using KadirliApp.Application.Features.Users.Queries.GetUsers;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers.Admin;

[Route("v1/admin/users")]
public class UsersAdminController : AdminApiControllerBase
{
    [HttpGet]
    [RequirePermission("users", "read")]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] bool? isBanned,
        [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        return Success(await Sender.Send(new GetUsersQuery(search, isBanned, page, limit)));
    }

    [HttpPost]
    [RequirePermission("users", "create")]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        return Success(await Sender.Send(new CreateUserCommand { Dto = dto }));
    }

    [HttpPut("{id}")]
    [RequirePermission("users", "update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
    {
        return Success(await Sender.Send(new UpdateUserCommand { Id = id, Dto = dto }));
    }

    [HttpPost("{id}/ban")]
    [RequirePermission("users", "approve")]
    public async Task<IActionResult> Ban(Guid id, [FromBody] BanBody? body = null)
    {
        return Success(await Sender.Send(new SetUserBanCommand(id, true, CurrentAdminId, body?.Reason)));
    }

    [HttpPost("{id}/unban")]
    [RequirePermission("users", "approve")]
    public async Task<IActionResult> Unban(Guid id)
    {
        return Success(await Sender.Send(new SetUserBanCommand(id, false, CurrentAdminId)));
    }

    public record BanBody(string? Reason);
}
