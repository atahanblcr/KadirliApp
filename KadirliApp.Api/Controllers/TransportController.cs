using System;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Transport.Dtos;
using KadirliApp.Application.Features.Transport.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers;

// Faz 10.1: POST intercity/intracity kaldırıldı — admin karşılıkları v1/admin/transport'ta (AdminPanel korumalı).
[Route("v1/transport")]
public class TransportController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public TransportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("intercity-routes")]
    public async Task<ActionResult<PagedResult<IntercityRouteResponseDto>>> GetIntercityRoutes([FromQuery] QueryTransportDto dto)
    {
        // Faz 10.7: public uç yalnız aktif hatları döner.
        var query = new GetIntercityRoutesQuery(dto, onlyActive: true);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("intracity-routes")]
    public async Task<ActionResult<PagedResult<IntracityRouteResponseDto>>> GetIntracityRoutes([FromQuery] QueryTransportDto dto)
    {
        var query = new GetIntracityRoutesQuery(dto, onlyActive: true);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
