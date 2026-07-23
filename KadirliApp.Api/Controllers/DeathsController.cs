using System;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Deaths.Commands;
using KadirliApp.Application.Features.Deaths.Dtos;
using KadirliApp.Application.Features.Deaths.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KadirliApp.Api.Controllers;

// Faz 10.1: PUT/DELETE kaldırıldı — düzenleme/silme/onay admin-işi (v1/admin/deaths, AdminPanel korumalı).
// POST kullanıcı-işi olarak kaldı: vatandaş vefat ilanı gönderir, "pending" durumuna düşer, admin onaylar.
[Route("v1/deaths")]
public class DeathsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public DeathsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<DeathNoticeResponseDto>>> FindAll([FromQuery] QueryDeathNoticeDto dto)
    {
        // Faz 10.7: public liste yalnız approved döner; istemcinin ?status= parametresi etkisizdir.
        var query = new GetDeathNoticesQuery(dto, OnlyPublished: true);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    // Faz 10.4: mobil vefat formu/detayı için lookup uçları (masterclass §13.2: GET cemeteries / GET mosques)
    [HttpGet("cemeteries")]
    public async Task<IActionResult> GetCemeteries()
        => Ok(await _mediator.Send(new Application.Features.Lookups.GetCemeteriesQuery()));

    [HttpGet("mosques")]
    public async Task<IActionResult> GetMosques()
        => Ok(await _mediator.Send(new Application.Features.Lookups.GetMosquesQuery()));

    [HttpGet("{id}")]
    public async Task<ActionResult<DeathNoticeResponseDto>> FindById(Guid id)
    {
        // Faz 10.7: approved olmayan ilanı yalnız ekleyen görür (JWT varsa), diğerlerine 404.
        var result = await _mediator.Send(new GetDeathNoticeByIdQuery(id, OnlyPublished: true, RequesterId: CurrentUserId));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    [EnableRateLimiting("public-write")] // Faz 10.7: pending kuyruğunu doldurma koruması
    public async Task<ActionResult<Guid>> Create([FromBody] CreateDeathNoticeDto dto)
    {
        var result = await _mediator.Send(new CreateDeathNoticeCommand(dto, AddedBy: CurrentUserId, AutoApprove: false));
        return CreatedAtAction(nameof(FindById), new { id = result }, result);
    }
}
