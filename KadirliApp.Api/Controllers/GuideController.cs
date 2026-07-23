using System;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Guide.Dtos;
using KadirliApp.Application.Features.Guide.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers;

[Route("v1/guide")]
public class GuideController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public GuideController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Mobil: rehber kayıtları (telefon/adres defteri). Yalnızca aktif kayıtlar döner.</summary>
    [HttpGet("items")]
    public async Task<ActionResult<PagedResult<GuideItemResponseDto>>> GetItems([FromQuery] QueryGuideItemDto dto)
    {
        dto ??= new QueryGuideItemDto();
        dto.IsActive = true;
        var result = await _mediator.Send(new GetGuideItemsQuery(dto));
        return Ok(result);
    }

    [HttpGet("items/{id}")]
    public async Task<ActionResult<GuideItemResponseDto>> GetItemById(Guid id)
    {
        var result = await _mediator.Send(new GetGuideItemByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<PagedResult<GuideCategoryResponseDto>>> GetCategories([FromQuery] QueryGuideCategoryDto dto)
    {
        var query = new GetGuideCategoriesQuery(dto);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("categories/{id}")]
    public async Task<ActionResult<GuideCategoryResponseDto>> GetCategoryById(Guid id)
    {
        var query = new GetGuideCategoryByIdQuery(id);
        var result = await _mediator.Send(query);
        if (result == null) return NotFound();
        return Ok(result);
    }

    // Faz 10.1: POST categories kaldırıldı — admin karşılığı v1/admin/guide/categories'te (AdminPanel korumalı).
}
