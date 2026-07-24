using KadirliApp.Api.Authorization;
using KadirliApp.Application.Features.Ads.Commands.ApproveAd;
using KadirliApp.Application.Features.Ads.Commands.CreateAd;
using KadirliApp.Application.Features.Ads.Commands.DeleteAd;
using KadirliApp.Application.Features.Ads.Commands.RejectAd;
using KadirliApp.Application.Features.Ads.Commands.UpdateAd;
using KadirliApp.Application.Features.Ads.Dtos;
using KadirliApp.Application.Features.Ads.Queries;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers.Admin;

[Route("v1/admin/ads")]
public class AdsAdminController : AdminApiControllerBase
{
    [HttpGet]
    [RequirePermission("ads", "read")]
    public async Task<IActionResult> GetAll([FromQuery] QueryAdDto dto)
    {
        return Success(await Sender.Send(new GetAdsQuery(dto)));
    }

    [HttpGet("{id}")]
    [RequirePermission("ads", "read")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Success(await Sender.Send(new GetAdByIdForEditQuery(id)));
    }

    /// <summary>Faz 10.9(g): moderasyon için ilanın kategoriye özel alan değerleri (salt-okunur).</summary>
    [HttpGet("{id}/properties")]
    [RequirePermission("ads", "read")]
    public async Task<IActionResult> GetPropertyValues(Guid id)
    {
        return Success(await Sender.Send(new GetAdPropertyValuesQuery(id)));
    }

    [HttpPost]
    [RequirePermission("ads", "create")]
    public async Task<IActionResult> Create([FromBody] CreateAdCommand command)
    {
        command.UserId = CurrentAdminId;
        return Success(await Sender.Send(command));
    }

    [HttpPut("{id}")]
    [RequirePermission("ads", "update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAdCommand command)
    {
        command.Id = id;
        return Success(await Sender.Send(command));
    }

    [HttpPost("{id}/approve")]
    [RequirePermission("ads", "approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        return Success(await Sender.Send(new ApproveAdCommand(id, CurrentAdminId)));
    }

    [HttpPost("{id}/reject")]
    [RequirePermission("ads", "approve")]
    public async Task<IActionResult> Reject(Guid id, [FromQuery] string? reason = null)
    {
        return Success(await Sender.Send(new RejectAdCommand(id, CurrentAdminId, reason)));
    }

    [HttpDelete("{id}")]
    [RequirePermission("ads", "delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return Success(await Sender.Send(new DeleteAdCommand(id)));
    }
}
