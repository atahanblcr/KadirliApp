using KadirliApp.Api.Authorization;
using KadirliApp.Application.Features.Transport.Commands;
using KadirliApp.Application.Features.Transport.Dtos;
using KadirliApp.Application.Features.Transport.Queries;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers.Admin;

[Route("v1/admin/transport")]
public class TransportAdminController : AdminApiControllerBase
{
    // --- Şehirlerarası hatlar ---

    [HttpGet("intercity")]
    [RequirePermission("transport", "read")]
    public async Task<IActionResult> GetIntercityRoutes([FromQuery] QueryTransportDto dto)
    {
        return Success(await Sender.Send(new GetIntercityRoutesQuery(dto)));
    }

    [HttpPost("intercity")]
    [RequirePermission("transport", "create")]
    public async Task<IActionResult> CreateIntercityRoute([FromBody] CreateIntercityRouteCommand command)
    {
        return Success(await Sender.Send(command));
    }

    // --- Şehir içi hatlar ---

    [HttpGet("intracity")]
    [RequirePermission("transport", "read")]
    public async Task<IActionResult> GetIntracityRoutes([FromQuery] QueryTransportDto dto)
    {
        return Success(await Sender.Send(new GetIntracityRoutesQuery(dto)));
    }

    [HttpPost("intracity")]
    [RequirePermission("transport", "create")]
    public async Task<IActionResult> CreateIntracityRoute([FromBody] CreateIntracityRouteCommand command)
    {
        return Success(await Sender.Send(command));
    }

    [HttpPut("intracity/{id}")]
    [RequirePermission("transport", "update")]
    public async Task<IActionResult> UpdateIntracityRoute(Guid id, [FromBody] UpdateIntracityRouteCommand command)
    {
        command.Id = id;
        return Success(await Sender.Send(command));
    }

    // --- Faz 10.8: kalkış saatleri + duraklar (panel formu 10.9'da) ---

    [HttpPut("intercity/{id}")]
    [RequirePermission("transport", "update")]
    public async Task<IActionResult> UpdateIntercityRoute(Guid id, [FromBody] UpdateIntercityRouteCommand command)
    {
        command.Id = id;
        return Success(await Sender.Send(command));
    }

    /// <summary>
    /// Şehirlerarası hatta kalkış saati ekler.
    /// Body: {"departureTime":"HH:mm","operatingDays":127}. <c>operatingDays</c> verilmezse
    /// "her gün" (12.5 öncesindeki örtük varsayım) — eski istemciler kırılmaz.
    /// </summary>
    [HttpPost("intercity/{routeId}/schedules")]
    [RequirePermission("transport", "create")]
    public async Task<IActionResult> CreateIntercitySchedule(Guid routeId, [FromBody] CreateScheduleDto dto)
        => Success(await Sender.Send(new CreateIntercityScheduleCommand(
            routeId, dto.DepartureTime, dto.OperatingDays ?? Domain.Enums.OperatingDays.Daily)));

    /// <summary>Faz 12.5 — seferin saatini/günlerini/yayın durumunu düzenler.</summary>
    [HttpPut("intercity/schedules/{id}")]
    [RequirePermission("transport", "update")]
    public async Task<IActionResult> UpdateIntercitySchedule(Guid id, [FromBody] UpdateScheduleDto dto)
        => Success(await Sender.Send(new UpdateIntercityScheduleCommand(
            id, dto.DepartureTime, dto.OperatingDays, dto.IsActive)));

    [HttpDelete("intercity/schedules/{id}")]
    [RequirePermission("transport", "delete")]
    public async Task<IActionResult> DeleteIntercitySchedule(Guid id)
        => Success(await Sender.Send(new DeleteIntercityScheduleCommand(id)));

    /// <summary>Şehir içi hatta durak ekler. Body: {"stopName":"...","stopOrder":1,"timeFromStart":5}.</summary>
    [HttpPost("intracity/{routeId}/stops")]
    [RequirePermission("transport", "create")]
    public async Task<IActionResult> CreateIntracityStop(Guid routeId, [FromBody] CreateStopDto dto)
        => Success(await Sender.Send(new CreateIntracityStopCommand(routeId, dto.StopName, dto.StopOrder, dto.TimeFromStart)));

    [HttpDelete("intracity/stops/{id}")]
    [RequirePermission("transport", "delete")]
    public async Task<IActionResult> DeleteIntracityStop(Guid id)
        => Success(await Sender.Send(new DeleteIntracityStopCommand(id)));

    public record CreateScheduleDto(string DepartureTime, int? OperatingDays = null);
    public record UpdateScheduleDto(string DepartureTime, int OperatingDays, bool IsActive = true);
    public record CreateStopDto(string StopName, int StopOrder, int? TimeFromStart);
}
