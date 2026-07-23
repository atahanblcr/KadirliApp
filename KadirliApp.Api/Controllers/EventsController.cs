using System;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Events.Dtos;
using KadirliApp.Application.Features.Events.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers;

[Route("v1/events")]
public class EventsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public EventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<EventResponseDto>>> GetEvents([FromQuery] QueryEventDto dto)
    {
        // Public uç nokta yalnızca onaylı etkinlikleri döndürür.
        dto.Status = "approved";
        var result = await _mediator.Send(new GetEventsQuery(dto));
        return Ok(result);
    }

    /// <summary>Mobil takvim: verilen ayda hangi günlerde etkinlik olduğunu döndürür (yalnızca onaylılar).</summary>
    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendar([FromQuery] int year, [FromQuery] int month)
    {
        if (year < 2000 || year > 2100 || month < 1 || month > 12)
            return BadRequest("Geçersiz yıl/ay.");

        var items = await _mediator.Send(new GetEventCalendarQuery(year, month, OnlyApproved: true));
        return Ok(items);
    }

    // Faz 10.4: mobil etkinlik filtresi için kategori lookup'ı (masterclass §13.2: GET categories)
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
        => Ok(await _mediator.Send(new Application.Features.Lookups.GetEventCategoriesQuery()));

    [HttpGet("{id}")]
    public async Task<ActionResult<EventResponseDto>> GetEventById(Guid id)
    {
        // Faz 10.7: approved olmayan etkinlik id bilinse bile dönmez.
        var result = await _mediator.Send(new GetEventByIdQuery(id, OnlyPublished: true));
        if (result == null) return NotFound();
        return Ok(result);
    }

    // Faz 10.1: POST kaldırıldı (10.1 karar matrisi: etkinlik oluşturma admin-işi; v1/admin/events'te).
    // Mobilde "kullanıcı etkinlik önerir" akışı istenirse [Authorize] + CreatedBy claim'den + AutoApprove=false
    // ile ayrı bir maddede geri eklenmeli.
}
