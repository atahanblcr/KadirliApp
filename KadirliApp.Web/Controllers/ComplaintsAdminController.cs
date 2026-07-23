using System;
using System.Security.Claims;
using System.Threading.Tasks;
using KadirliApp.Application.Features.Complaints.Commands;
using KadirliApp.Application.Features.Complaints.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Web.Controllers;

[Authorize(Roles = "admin,super_admin")]
public class ComplaintsAdminController : Controller
{
    private readonly ISender _sender;

    public ComplaintsAdminController(ISender sender)
    {
        _sender = sender;
    }

    private Guid GetAdminId()
    {
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);
        return adminId;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? status, [FromQuery] int page = 1)
    {
        var result = await _sender.Send(new GetComplaintsQuery(status, page, 20));
        ViewBag.CurrentStatus = status;
        return View(result);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(Guid id, string status, string? adminNotes, string? returnStatus)
    {
        if (status is not ("in_progress" or "resolved" or "rejected"))
        {
            TempData["Error"] = "Geçersiz durum.";
            return RedirectToAction(nameof(Index), new { status = returnStatus });
        }

        var success = await _sender.Send(new ResolveComplaintCommand(id, GetAdminId(), status, adminNotes));

        if (success)
        {
            TempData["Success"] = status switch
            {
                "in_progress" => "Şikayet işleme alındı.",
                "resolved" => "Şikayet çözüldü olarak işaretlendi.",
                _ => "Şikayet reddedildi."
            };
        }
        else
        {
            TempData["Error"] = "Şikayet bulunamadı.";
        }

        return RedirectToAction(nameof(Index), new { status = returnStatus });
    }
}
