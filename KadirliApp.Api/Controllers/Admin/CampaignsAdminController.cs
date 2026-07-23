using KadirliApp.Api.Authorization;
using KadirliApp.Application.Features.Campaigns.Commands;
using KadirliApp.Application.Features.Campaigns.Dtos;
using KadirliApp.Application.Features.Campaigns.Queries;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers.Admin;

[Route("v1/admin/campaigns")]
public class CampaignsAdminController : AdminApiControllerBase
{
    [HttpGet]
    [RequirePermission("campaigns", "read")]
    public async Task<IActionResult> GetAll([FromQuery] QueryCampaignDto dto)
    {
        return Success(await Sender.Send(new GetCampaignsQuery(dto)));
    }

    [HttpGet("{id}")]
    [RequirePermission("campaigns", "read")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Success(await Sender.Send(new GetCampaignByIdQuery(id)));
    }

    [HttpPost]
    [RequirePermission("campaigns", "create")]
    public async Task<IActionResult> Create([FromBody] CreateCampaignCommand command)
    {
        command.AutoApprove = true;
        command.ApprovedBy = CurrentAdminId;
        return Success(await Sender.Send(command));
    }

    [HttpPut("{id}")]
    [RequirePermission("campaigns", "update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCampaignCommand command)
    {
        command.Id = id;
        return Success(await Sender.Send(command));
    }

    [HttpPost("{id}/approve")]
    [RequirePermission("campaigns", "approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        return Success(await Sender.Send(new ApproveCampaignCommand(id, CurrentAdminId)));
    }

    [HttpPost("{id}/reject")]
    [RequirePermission("campaigns", "approve")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectCampaignBody? body = null)
    {
        return Success(await Sender.Send(new RejectCampaignCommand(id, CurrentAdminId, body?.Reason)));
    }

    [HttpDelete("{id}")]
    [RequirePermission("campaigns", "delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return Success(await Sender.Send(new DeleteCampaignCommand(id)));
    }

    public record RejectCampaignBody(string? Reason);
}
