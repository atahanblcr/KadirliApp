using System;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Taxis.Commands.CallTaxiDriver;
using KadirliApp.Application.Features.Taxis.Dtos;
using KadirliApp.Application.Features.Taxis.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers;

[Route("v1/taxis")]
public class TaxisController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public TaxisController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("drivers")]
    public async Task<ActionResult<PagedResult<TaxiDriverResponseDto>>> GetDrivers([FromQuery] QueryTaxiDriverDto dto)
    {
        // Faz 10.7: public uç yalnız doğrulanmış + aktif sürücüleri döner; ?isVerified=/?isActive= etkisiz.
        var query = new GetTaxiDriversQuery(dto, onlyPublic: true);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("drivers/{id}")]
    public async Task<ActionResult<TaxiDriverResponseDto>> GetDriverById(Guid id)
    {
        var query = new GetTaxiDriverByIdQuery(id, onlyPublic: true);
        var result = await _mediator.Send(query);
        if (result == null) return NotFound();
        return Ok(result);
    }

    // Faz 10.1: POST drivers kaldırıldı — admin karşılığı v1/admin/taxis'te (AdminPanel korumalı).

    /// <summary>Faz 10.12: çağrı kaydı (taxi_calls) + sürücü total_calls sayacı; yanıtta aranacak telefon.</summary>
    [HttpPost("drivers/{id:guid}/call")]
    [Authorize]
    public async Task<ActionResult<TaxiCallResultDto>> Call(Guid id)
        => Ok(await _mediator.Send(new CallTaxiDriverCommand(id, CurrentUserId!.Value)));
}
