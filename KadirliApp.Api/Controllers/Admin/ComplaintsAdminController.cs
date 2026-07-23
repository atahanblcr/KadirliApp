using KadirliApp.Api.Authorization;
using KadirliApp.Application.Features.Complaints.Commands;
using KadirliApp.Application.Features.Complaints.Queries;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers.Admin;

[Route("v1/admin/complaints")]
public class ComplaintsAdminController : AdminApiControllerBase
{
    [HttpGet]
    [RequirePermission("complaints", "read")]
    public async Task<IActionResult> GetAll([FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        return Success(await Sender.Send(new GetComplaintsQuery(status, page, limit)));
    }

    /// <summary>Şikayeti çözümler/reddeder: status = in_progress | resolved | rejected.</summary>
    [HttpPost("{id}/status")]
    [RequirePermission("complaints", "approve")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateComplaintStatusBody body)
    {
        return Success(await Sender.Send(new ResolveComplaintCommand(id, CurrentAdminId, body.Status, body.AdminNotes)));
    }

    public record UpdateComplaintStatusBody(string Status, string? AdminNotes);
}
