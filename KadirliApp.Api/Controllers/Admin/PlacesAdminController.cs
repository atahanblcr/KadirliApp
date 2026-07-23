using KadirliApp.Api.Authorization;
using KadirliApp.Application.Features.Places.Commands;
using KadirliApp.Application.Features.Places.Dtos;
using KadirliApp.Application.Features.Places.Queries;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers.Admin;

[Route("v1/admin/places")]
public class PlacesAdminController : AdminApiControllerBase
{
    [HttpGet]
    [RequirePermission("places", "read")]
    public async Task<IActionResult> GetAll([FromQuery] QueryPlaceDto dto)
    {
        return Success(await Sender.Send(new GetPlacesQuery(dto)));
    }

    [HttpGet("{id}")]
    [RequirePermission("places", "read")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Success(await Sender.Send(new GetPlaceByIdQuery(id)));
    }

    [HttpPost]
    [RequirePermission("places", "create")]
    public async Task<IActionResult> Create([FromBody] CreatePlaceCommand command)
    {
        return Success(await Sender.Send(command));
    }

    [HttpPut("{id}")]
    [RequirePermission("places", "update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlaceCommand command)
    {
        command.Id = id;
        return Success(await Sender.Send(command));
    }

    [HttpDelete("{id}")]
    [RequirePermission("places", "delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return Success(await Sender.Send(new DeletePlaceCommand(id)));
    }
}
