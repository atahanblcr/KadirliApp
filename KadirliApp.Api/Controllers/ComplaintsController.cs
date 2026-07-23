using System;
using System.Security.Claims;
using System.Threading.Tasks;
using KadirliApp.Application.Features.Complaints.Commands;
using KadirliApp.Application.Features.Complaints.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KadirliApp.Api.Controllers;

[Route("v1/complaints")]
public class ComplaintsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ComplaintsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Şikayet/öneri gönder. Girişli kullanıcıda user_id claim'i otomatik bağlanır; anonim gönderime de izin verilir.</summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("public-write")] // Faz 10.7: anonim uç — spam/kuyruk doldurma koruması
    public async Task<ActionResult<Guid>> CreateComplaint([FromBody] CreateComplaintCommand command)
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            command.UserId = userId;
        }

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Faz 10.8: kullanıcının kendi şikayetleri (mobil "şikayetlerim" ekranı; anonim gönderimler listelenemez).</summary>
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyComplaints([FromQuery] int page = 1, [FromQuery] int limit = 20)
        => Ok(await _mediator.Send(new GetMyComplaintsQuery(CurrentUserId!.Value, page, limit)));
}
