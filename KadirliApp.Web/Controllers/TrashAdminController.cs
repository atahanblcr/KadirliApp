using System;
using System.Threading.Tasks;
using KadirliApp.Application.Features.Trash;
using KadirliApp.Application.Features.Trash.Commands;
using KadirliApp.Application.Features.Trash.Dtos;
using KadirliApp.Application.Features.Trash.Queries;
using KadirliApp.Web.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Web.Controllers;

/// <summary>
/// Faz 11.17 — **çöp kutusu.** Soft delete her modülde vardı, panelde karşılığı yoktu:
/// yanlışlıkla silinen bir duyuru/ilan ancak <c>psql</c> ile geri geliyordu.
///
/// ⚠️ <c>StaffAdmin</c>/<c>AuditLogsAdmin</c> ile aynı karar: **matrisin dışında**,
/// yalnız admin. Geri getirme, moderatörün silme kararını tersine çevirmektir; silme
/// yetkisiyle aynı güven değildir.
///
/// ⚠️ <c>GuideItem</c> burada yok — <c>ISoftDeletable</c> değil, silmesi fiziksel.
/// Bu bir eksik değil, bilinçli fark (bkz. <see cref="TrashModules"/>).
/// </summary>
[Authorize(Roles = "admin,super_admin")]
public class TrashAdminController : Controller
{
    private const int PageSize = 25;

    private readonly ISender _sender;

    public TrashAdminController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? module, [FromQuery] int page = 1)
    {
        var result = await _sender.Send(new GetTrashQuery(new QueryTrashDto
        {
            Module = module,
            Page = page,
            Limit = PageSize
        }));

        ViewBag.Module = module;
        ViewBag.Modules = TrashModules.Keys;
        return View(result);
    }

    [HttpPost]
    public async Task<IActionResult> Restore(string module, Guid id, string? returnModule)
    {
        try
        {
            var restored = await _sender.Send(new RestoreRecordCommand(module, id));
            TempData[restored ? "Success" : "Error"] = restored
                // Yayına almadığını açıkça söyle: kayıt silinmeden önceki durumuna döner.
                ? $"Kayıt geri getirildi. Silinmeden önceki durumuyla ({PanelDisplay.ModuleLabel(module)}) listeye döndü."
                : "Kayıt bulunamadı — zaten geri getirilmiş olabilir.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { module = returnModule });
    }
}
