using KadirliApp.Api.Authorization;
using KadirliApp.Application.Features.PowerOutages.Commands.CreatePowerOutage;
using KadirliApp.Application.Features.PowerOutages.Commands.DeletePowerOutage;
using KadirliApp.Application.Features.PowerOutages.Commands.UpdatePowerOutage;
using KadirliApp.Application.Features.PowerOutages.DTOs;
using KadirliApp.Application.Features.PowerOutages.Queries.GetPowerOutageById;
using KadirliApp.Application.Features.PowerOutages.Queries.GetPowerOutages;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers.Admin;

[Route("v1/admin/power-outages")]
public class PowerOutagesAdminController : AdminApiControllerBase
{
    [HttpGet]
    [RequirePermission("power-outages", "read")]
    public async Task<IActionResult> GetAll()
    {
        return Success(await Sender.Send(new GetPowerOutagesQuery()));
    }

    [HttpGet("{id}")]
    [RequirePermission("power-outages", "read")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Success(await Sender.Send(new GetPowerOutageByIdQuery { Id = id }));
    }

    [HttpPost]
    [RequirePermission("power-outages", "create")]
    public async Task<IActionResult> Create([FromBody] CreatePowerOutageDto dto)
    {
        return Success(await Sender.Send(new CreatePowerOutageCommand { Dto = dto }));
    }

    [HttpPut("{id}")]
    [RequirePermission("power-outages", "update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePowerOutageDto dto)
    {
        return Success(await Sender.Send(new UpdatePowerOutageCommand { Id = id, Dto = dto }));
    }

    [HttpDelete("{id}")]
    [RequirePermission("power-outages", "delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return Success(await Sender.Send(new DeletePowerOutageCommand { Id = id }));
    }
}
