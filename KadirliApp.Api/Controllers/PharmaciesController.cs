using System;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Pharmacies.Dtos;
using KadirliApp.Application.Features.Pharmacies.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers;

// Faz 10.1: Yazma uçları kaldırıldı — admin karşılıkları v1/admin/pharmacies'te (AdminPanel korumalı).
[Route("v1/pharmacies")]
public class PharmaciesController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public PharmaciesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PharmacyResponseDto>>> FindAll([FromQuery] QueryPharmacyDto dto)
    {
        // Faz 10.7: public uç yalnız aktif eczaneleri döner; ?isActive= parametresi etkisiz.
        var query = new GetPharmaciesQuery(dto, OnlyActive: true);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>Faz 10.4: verilen günün (varsayılan: Türkiye saatiyle bugün) nöbetçi eczaneleri.</summary>
    [HttpGet("on-duty")]
    public async Task<IActionResult> GetOnDuty([FromQuery] DateOnly? date)
        => Ok(await _mediator.Send(new GetOnDutyPharmaciesQuery(date)));

    /// <summary>Faz 10.4: aylık nöbet takvimi.</summary>
    [HttpGet("schedule")]
    public async Task<IActionResult> GetSchedule([FromQuery] int year, [FromQuery] int month)
        => Ok(await _mediator.Send(new GetPharmacyScheduleQuery(year, month)));

    [HttpGet("{id}")]
    public async Task<ActionResult<PharmacyResponseDto>> FindById(Guid id)
    {
        var result = await _mediator.Send(new GetPharmacyByIdQuery(id, OnlyActive: true));
        if (result == null) return NotFound();
        return Ok(result);
    }
}
