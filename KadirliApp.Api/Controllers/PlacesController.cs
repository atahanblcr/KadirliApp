using System;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Places.Dtos;
using KadirliApp.Application.Features.Places.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers;

[Route("v1/places")]
public class PlacesController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public PlacesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PlaceResponseDto>>> GetPlaces([FromQuery] QueryPlaceDto dto)
    {
        // Faz 10.7: public uç yalnız aktif mekanları döner.
        var query = new GetPlacesQuery(dto, OnlyActive: true);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PlaceResponseDto>> GetPlaceById(Guid id)
    {
        var query = new GetPlaceByIdQuery(id, OnlyActive: true);
        var result = await _mediator.Send(query);
        if (result == null) return NotFound();
        return Ok(result);
    }

    // Faz 10.1: POST kaldırıldı — admin karşılığı v1/admin/places'te (AdminPanel korumalı).
}
