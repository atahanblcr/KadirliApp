using System;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Campaigns.Commands.ViewCampaignCode;
using KadirliApp.Application.Features.Campaigns.Dtos;
using KadirliApp.Application.Features.Campaigns.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers;

[Route("v1/campaigns")]
public class CampaignsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public CampaignsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CampaignResponseDto>>> GetCampaigns([FromQuery] QueryCampaignDto dto)
    {
        // Public uç nokta yalnızca onaylı ve tarih aralığı geçerli kampanyaları döndürür.
        dto.Status = "approved";
        dto.OnlyActive = true;
        var result = await _mediator.Send(new GetCampaignsQuery(dto));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CampaignResponseDto>> GetCampaignById(Guid id)
    {
        // Faz 10.7: approved + tarih aralığı geçerli olmayan kampanya id bilinse bile dönmez.
        var result = await _mediator.Send(new GetCampaignByIdQuery(id, OnlyPublished: true));
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Faz 10.12: indirim kodunu döner + campaign_code_views izi (kullanıcı başına tek kayıt; tekrar istekte aynı kayıt döner).</summary>
    [HttpPost("{id:guid}/view-code")]
    [Authorize]
    public async Task<ActionResult<CampaignCodeDto>> ViewCode(Guid id)
        => Ok(await _mediator.Send(new ViewCampaignCodeCommand(id, CurrentUserId!.Value)));
}
