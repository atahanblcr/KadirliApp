using Microsoft.AspNetCore.Authorization;
using KadirliApp.Web.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Deaths.Queries;
using KadirliApp.Application.Features.Deaths.Dtos;
using KadirliApp.Domain.Entities;
using KadirliApp.Web.Common;
using System.Security.Claims;

namespace KadirliApp.Web.Controllers;

[Authorize(Roles = "admin,super_admin,moderator")]
[PanelPermission("deaths")]
public class DeathsAdminController : Controller
{
    private readonly ISender _sender;
    private readonly IUnitOfWork _uow;

    public DeathsAdminController(ISender sender, IUnitOfWork uow)
    {
        _sender = sender;
        _uow = uow;
    }

    private async Task LoadLookupsAsync()
    {
        ViewBag.Cemeteries = await _uow.Repository<Cemetery>().Query().OrderBy(x => x.Name).ToListAsync();
        ViewBag.Mosques = await _uow.Repository<Mosque>().Query().OrderBy(x => x.Name).ToListAsync();
        ViewBag.Neighborhoods = await _uow.Repository<Neighborhood>().Query().OrderBy(x => x.Name).ToListAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] QueryDeathNoticeDto dto)
    {
        var result = await _sender.Send(new GetDeathNoticesQuery(dto ?? new QueryDeathNoticeDto(null, null, null, 1, 20)));
        return View(result);
    }
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();
        return View(new KadirliApp.Application.Features.Deaths.Dtos.CreateDeathNoticeDto(string.Empty, null, DateTime.Today, TimeSpan.Zero, null, null, null, null, null, null));
    }

    [HttpPost]
    public async Task<IActionResult> Create(KadirliApp.Application.Features.Deaths.Dtos.CreateDeathNoticeDto dto, IFormFile? Photo)
    {
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View(dto);
        }

        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);

        var photoId = await UploadHelper.UploadAsync(_sender, Photo, "death_notice", adminId);
        if (photoId.HasValue) dto = dto with { PhotoFileId = photoId };

        var result = await _sender.Send(new KadirliApp.Application.Features.Deaths.Commands.CreateDeathNoticeCommand(dto, adminId, AutoApprove: true));
        if (result != Guid.Empty)
        {
            TempData["Success"] = "Vefat ilanı başarıyla eklendi.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Error"] = "Bir hata oluştu.";
        await LoadLookupsAsync();
        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var deathNotice = await _sender.Send(new GetDeathNoticeByIdQuery(id));
        if (deathNotice == null) return NotFound();

        var dto = new KadirliApp.Application.Features.Deaths.Dtos.UpdateDeathNoticeDto(
            deathNotice.DeceasedName,
            deathNotice.PhotoFileId,
            deathNotice.FuneralDate,
            deathNotice.FuneralTime,
            deathNotice.CemeteryId,
            deathNotice.MosqueId,
            deathNotice.NeighborhoodId,
            deathNotice.CondolenceAddress,
            deathNotice.CondolenceLatitude,
            deathNotice.CondolenceLongitude,
            deathNotice.Status
        );
        ViewBag.Id = id;
        ViewBag.PhotoUrl = deathNotice.PhotoUrl;
        await LoadLookupsAsync();
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, KadirliApp.Application.Features.Deaths.Dtos.UpdateDeathNoticeDto dto, IFormFile? Photo)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Id = id;
            await LoadLookupsAsync();
            return View(dto);
        }

        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);
        var photoId = await UploadHelper.UploadAsync(_sender, Photo, "death_notice", adminId);
        if (photoId.HasValue) dto = dto with { PhotoFileId = photoId };

        var result = await _sender.Send(new KadirliApp.Application.Features.Deaths.Commands.UpdateDeathNoticeCommand(id, dto));
        if (result)
        {
            TempData["Success"] = "Vefat ilanı başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Error"] = "Bir hata oluştu.";
        ViewBag.Id = id;
        await LoadLookupsAsync();
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Approve(System.Guid id)
    {
        var adminIdStr = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (!System.Guid.TryParse(adminIdStr, out var adminId))
        {
            return Unauthorized();
        }

        var success = await _sender.Send(new KadirliApp.Application.Features.Deaths.Commands.ApproveDeathNoticeCommand(id, adminId));

        if (!success)
        {
            TempData["Error"] = "İlan bulunamadı veya onaylanamadı.";
        }
        else
        {
            TempData["Success"] = "İlan başarıyla onaylandı.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(System.Guid id)
    {
        var success = await _sender.Send(new KadirliApp.Application.Features.Deaths.Commands.DeleteDeathNoticeCommand(id));

        if (!success)
        {
            TempData["Error"] = "İlan bulunamadı veya silinemedi.";
        }
        else
        {
            TempData["Success"] = "İlan başarıyla silindi.";
        }

        return RedirectToAction(nameof(Index));
    }

    // Faz 11.18 — toplu işlem. ⚠️ Ad "…Selected" ile bitmeli (görünmez sözleşme #19).
    [HttpPost]
    public async Task<IActionResult> ApproveSelected(System.Guid[] ids, [FromQuery] string? returnUrl)
    {
        var adminId = CurrentAdminId();
        var outcome = await Common.PanelBulk.RunAsync(ids, id => _sender.Send(
            new KadirliApp.Application.Features.Deaths.Commands.ApproveDeathNoticeCommand(id, adminId)));
        outcome.Report(TempData, "vefat kaydı", "onaylandı");
        return BackToList(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSelected(System.Guid[] ids, [FromQuery] string? returnUrl)
    {
        var outcome = await Common.PanelBulk.RunAsync(ids, id => _sender.Send(
            new KadirliApp.Application.Features.Deaths.Commands.DeleteDeathNoticeCommand(id)));
        outcome.Report(TempData, "vefat kaydı", "silindi");
        return BackToList(returnUrl);
    }

    private System.Guid CurrentAdminId() =>
        System.Guid.TryParse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier), out var id)
            ? id : System.Guid.Empty;

    /// <summary>Toplu işlemden sonra filtrelenmiş listeye geri döner (süzgeç kaybolmasın).</summary>
    private IActionResult BackToList(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction(nameof(Index));
}
