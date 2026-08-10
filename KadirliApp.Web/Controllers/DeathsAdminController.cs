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

    /// <summary>Faz 11.16b — filtrelenmiş listeyi CSV olarak indirir (bkz. AdsAdmin.ExportCsv).</summary>
    [HttpGet]
    public async Task<IActionResult> ExportCsv([FromQuery] QueryDeathNoticeDto dto)
    {
        var query = dto ?? new QueryDeathNoticeDto(null, null, null, 1, 20);

        var (rows, total) = await Common.PanelCsv.CollectAsync<KadirliApp.Application.Features.Deaths.Dtos.DeathNoticeResponseDto>(
            (page, size) => _sender.Send(new GetDeathNoticesQuery(query with { Page = page, Limit = size })));

        if (Common.PanelCsv.RejectIfTooLarge(total) is { } tooLarge)
        {
            TempData["Error"] = tooLarge;
            return RedirectToAction(nameof(Index));
        }

        return Common.PanelCsv.File(rows, DeathCsvColumns, "vefat-ilanlari");
    }

    private static readonly IReadOnlyList<Common.PanelCsv.Column<KadirliApp.Application.Features.Deaths.Dtos.DeathNoticeResponseDto>> DeathCsvColumns =
    [
        new("Vefat eden", x => x.DeceasedName),
        new("Durum", x => Common.PanelDisplay.Status(x.Status).Label),
        new("Cenaze tarihi", x => x.FuneralDate.ToString("dd.MM.yyyy")),
        new("Cenaze saati", x => x.FuneralTime.ToString(@"hh\:mm")),
        new("Cami", x => x.MosqueName),
        new("Mezarlık", x => x.CemeteryName),
        new("Taziye adresi", x => x.CondolenceAddress),
        new("Oluşturulma", x => Common.PanelCsv.Date(x.CreatedAt)),
    ];
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
            return await RedisplayEditAsync(id, dto);

        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);
        var photoId = await UploadHelper.UploadAsync(_sender, Photo, "death_notice", adminId);
        if (photoId.HasValue) dto = dto with { PhotoFileId = photoId };

        bool result;
        try
        {
            result = await _sender.Send(new KadirliApp.Application.Features.Deaths.Commands.UpdateDeathNoticeCommand(id, dto));
        }
        catch (Application.Common.Exceptions.AppException ex) // 12.10: durum bu yoldan değiştirilemez
        {
            TempData["Error"] = ex.Message;
            return await RedisplayEditAsync(id, dto);
        }

        if (result)
        {
            TempData["Success"] = "Vefat ilanı başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Error"] = "Bir hata oluştu.";
        return await RedisplayEditAsync(id, dto);
    }

    /// <summary>
    /// Düzenle formunu hatadan sonra yeniden çizer; durumu <b>veritabanından tazeler</b>
    /// (12.10 — form artık <c>Status</c> göndermiyor, bkz. <c>AdsAdminController</c>).
    /// </summary>
    private async Task<IActionResult> RedisplayEditAsync(
        Guid id, KadirliApp.Application.Features.Deaths.Dtos.UpdateDeathNoticeDto dto)
    {
        var current = await _sender.Send(new GetDeathNoticeByIdQuery(id));
        dto = dto with { Status = current?.Status };

        ViewBag.Id = id;
        ViewBag.PhotoUrl = current?.PhotoUrl;
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

    // Faz 12.10 — 12.10 öncesinde vefatta reddetmenin TEK yolu Düzenle formundaki durum
    // menüsüydü ve o yol ne izi ne gerekçeyi tutuyordu. Menü kaldırıldı, karşılığı burada.
    // Sebep alanı ilanlardaki "JS'siz details popover" desenini izliyor.
    [HttpPost]
    public async Task<IActionResult> Reject(System.Guid id, string? reason)
    {
        var adminIdStr = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (!System.Guid.TryParse(adminIdStr, out var adminId))
        {
            return Unauthorized();
        }

        var success = await _sender.Send(
            new KadirliApp.Application.Features.Deaths.Commands.RejectDeathNoticeCommand(id, adminId, reason));

        TempData[success ? "Success" : "Error"] = success
            ? "İlan reddedildi."
            : "İlan bulunamadı veya reddedilemedi.";

        return RedirectToAction(nameof(Index));
    }

    // Faz 12.10 — elle arşivleme (ArchiveDeathsJob'ın yaptığı geçişin insan eliyle yapılan hâli).
    // ⚠️ Aksiyon adı "Archive": izin eylemi önekten türer (#19) ve 12.10'da "Archive" moderasyon
    // listesine eklendi — yoksa POST olduğu için sessizce "update"e düşerdi.
    [HttpPost]
    public async Task<IActionResult> Archive(System.Guid id)
    {
        var adminIdStr = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (!System.Guid.TryParse(adminIdStr, out var adminId))
        {
            return Unauthorized();
        }

        var success = await _sender.Send(
            new KadirliApp.Application.Features.Deaths.Commands.ArchiveDeathNoticeCommand(id, adminId));

        TempData[success ? "Success" : "Error"] = success
            ? "İlan arşivlendi."
            : "İlan bulunamadı veya arşivlenemedi.";

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

    // Faz 12.10: toplu red artık mümkün (komut doğdu). Diğer üç moderasyonlu modülde
    // zaten vardı; vefatta yalnız reddetme komutu olmadığı için eksikti.
    [HttpPost]
    public async Task<IActionResult> RejectSelected(System.Guid[] ids, string? reason, [FromQuery] string? returnUrl)
    {
        var adminId = CurrentAdminId();
        var outcome = await Common.PanelBulk.RunAsync(ids, id => _sender.Send(
            new KadirliApp.Application.Features.Deaths.Commands.RejectDeathNoticeCommand(id, adminId, reason)));
        outcome.Report(TempData, "vefat kaydı", "reddedildi");
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
