using KadirliApp.Api.Authorization;
using KadirliApp.Application.Features.Events.Commands;
using KadirliApp.Application.Features.Events.Dtos;
using KadirliApp.Application.Features.Events.Queries;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers.Admin;

[Route("v1/admin/events")]
public class EventsAdminController : AdminApiControllerBase
{
    [HttpGet]
    [RequirePermission("events", "read")]
    public async Task<IActionResult> GetAll([FromQuery] QueryEventDto dto)
    {
        return Success(await Sender.Send(new GetEventsQuery(dto)));
    }

    [HttpGet("calendar")]
    [RequirePermission("events", "read")]
    public async Task<IActionResult> GetCalendar([FromQuery] int year, [FromQuery] int month)
    {
        return Success(await Sender.Send(new GetEventCalendarQuery(year, month)));
    }

    [HttpGet("{id}")]
    [RequirePermission("events", "read")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Success(await Sender.Send(new GetEventByIdQuery(id)));
    }

    [HttpPost]
    [RequirePermission("events", "create")]
    public async Task<IActionResult> Create([FromBody] CreateEventCommand command)
    {
        command.CreatedBy = CurrentAdminId;
        command.AutoApprove = true;
        return Success(await Sender.Send(command));
    }

    [HttpPut("{id}")]
    [RequirePermission("events", "update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventCommand command)
    {
        command.Id = id;
        return Success(await Sender.Send(command));
    }

    [HttpPost("{id}/approve")]
    [RequirePermission("events", "approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        return Success(await Sender.Send(new ApproveEventCommand(id, CurrentAdminId)));
    }

    [HttpPost("{id}/reject")]
    [RequirePermission("events", "approve")]
    public async Task<IActionResult> Reject(Guid id)
    {
        return Success(await Sender.Send(new RejectEventCommand(id, CurrentAdminId)));
    }

    [HttpDelete("{id}")]
    [RequirePermission("events", "delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return Success(await Sender.Send(new DeleteEventCommand(id)));
    }
}
