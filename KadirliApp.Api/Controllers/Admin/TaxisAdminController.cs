using KadirliApp.Api.Authorization;
using KadirliApp.Application.Features.Taxis.Commands;
using KadirliApp.Application.Features.Taxis.Dtos;
using KadirliApp.Application.Features.Taxis.Queries;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers.Admin;

[Route("v1/admin/taxis")]
public class TaxisAdminController : AdminApiControllerBase
{
    [HttpGet]
    [RequirePermission("taxis", "read")]
    public async Task<IActionResult> GetAll([FromQuery] QueryTaxiDriverDto dto)
    {
        return Success(await Sender.Send(new GetTaxiDriversQuery(dto)));
    }

    [HttpGet("{id}")]
    [RequirePermission("taxis", "read")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Success(await Sender.Send(new GetTaxiDriverByIdQuery(id)));
    }

    [HttpPost]
    [RequirePermission("taxis", "create")]
    public async Task<IActionResult> Create([FromBody] CreateTaxiDriverCommand command)
    {
        return Success(await Sender.Send(command));
    }

    [HttpPut("{id}")]
    [RequirePermission("taxis", "update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaxiDriverCommand command)
    {
        command.Id = id;
        return Success(await Sender.Send(command));
    }

    [HttpPost("{id}/verify")]
    [RequirePermission("taxis", "approve")]
    public async Task<IActionResult> Verify(Guid id)
    {
        return Success(await Sender.Send(new VerifyTaxiDriverCommand(id, CurrentAdminId)));
    }

    [HttpDelete("{id}")]
    [RequirePermission("taxis", "delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return Success(await Sender.Send(new DeleteTaxiDriverCommand(id)));
    }
}
