using KadirliApp.Api.Authorization;
using KadirliApp.Application.Features.Pharmacies.Commands;
using KadirliApp.Application.Features.Pharmacies.Dtos;
using KadirliApp.Application.Features.Pharmacies.Queries;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers.Admin;

[Route("v1/admin/pharmacies")]
public class PharmaciesAdminController : AdminApiControllerBase
{
    [HttpGet]
    [RequirePermission("pharmacies", "read")]
    public async Task<IActionResult> GetAll([FromQuery] QueryPharmacyDto dto)
    {
        return Success(await Sender.Send(new GetPharmaciesQuery(dto)));
    }

    [HttpGet("{id}")]
    [RequirePermission("pharmacies", "read")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Success(await Sender.Send(new GetPharmacyByIdQuery(id)));
    }

    [HttpPost]
    [RequirePermission("pharmacies", "create")]
    public async Task<IActionResult> Create([FromBody] CreatePharmacyDto dto)
    {
        return Success(await Sender.Send(new CreatePharmacyCommand(dto)));
    }

    [HttpPut("{id}")]
    [RequirePermission("pharmacies", "update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePharmacyDto dto)
    {
        return Success(await Sender.Send(new UpdatePharmacyCommand(id, dto)));
    }

    [HttpDelete("{id}")]
    [RequirePermission("pharmacies", "delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return Success(await Sender.Send(new DeletePharmacyCommand(id)));
    }

    // Faz 10.4: nöbet takvimi yönetimi (PharmacySchedule CRUD'u ilk kez açıldı; public karşılığı v1/pharmacies/on-duty|schedule)

    [HttpGet("schedule")]
    [RequirePermission("pharmacies", "read")]
    public async Task<IActionResult> GetSchedule([FromQuery] int year, [FromQuery] int month)
    {
        return Success(await Sender.Send(new GetPharmacyScheduleQuery(year, month)));
    }

    [HttpPost("schedule")]
    [RequirePermission("pharmacies", "create")]
    public async Task<IActionResult> CreateSchedule([FromBody] CreatePharmacyScheduleDto dto)
    {
        return Success(await Sender.Send(new CreatePharmacyScheduleCommand(dto)));
    }

    [HttpDelete("schedule/{id}")]
    [RequirePermission("pharmacies", "delete")]
    public async Task<IActionResult> DeleteSchedule(Guid id)
    {
        return Success(await Sender.Send(new DeletePharmacyScheduleCommand(id)));
    }
}
