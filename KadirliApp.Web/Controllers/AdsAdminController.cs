using System;
using KadirliApp.Web.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KadirliApp.Application.Features.Ads.Commands.ApproveAd;
using KadirliApp.Application.Features.Ads.Dtos;
using KadirliApp.Application.Features.Ads.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Web.Controllers;

[Authorize(Roles = "admin,super_admin,moderator")]
[PanelPermission("ads")]
public class AdsAdminController : Controller
{
    private readonly ISender _sender;

    public AdsAdminController(ISender sender)
    {
        _sender = sender;
    }

    // Faz 10.9(c): inline _uow sorgusundan GetAdCategoriesAdminQuery'ye geçirildi (Faz 9.4 kuralı).
    private async Task LoadCategoriesAsync()
    {
        var categories = await _sender.Send(new GetAdCategoriesAdminQuery());
        ViewBag.Categories = categories.Where(x => x.IsActive).ToList();
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] QueryAdDto query)
    {
        var result = await _sender.Send(new GetAdsQuery(query));
        // Filtre çubuğunun kategori seçicisi için (sorgu zaten CategoryId/fiyat/arama/sort destekliyordu,
        // panelde bunları girecek alan yoktu).
        await LoadCategoriesAsync();
        ViewBag.Query = query;
        return View(result);
    }
    /// <summary>
    /// Faz 11.16b — filtrelenmiş listeyi CSV olarak indirir.
    /// </summary>
    /// <remarks>
    /// ⚠️ Aksiyon adı bilerek <c>ExportCsv</c>: izin eylemi aksiyon adının önekinden
    /// türetilir (görünmez sözleşme #19) ve bu ad hiçbir yazma önekiyle eşleşmediği için
    /// GET olarak <b>"read"</b>e düşer — doğrusu da bu, dışa aktarma toplu bir okumadır.
    /// 🔑 Dışa aktarma <b>Index ile AYNI sorguyu</b> gönderir: ekranda görünen filtre neyse
    /// dosyada o vardır. Ayrı bir sorgu yazılsaydı "ekranda 12 satır görüyorum ama dosyada
    /// 400 var" sınıfı bir ayrışma doğardı.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> ExportCsv([FromQuery] QueryAdDto query)
    {
        var (rows, total) = await Common.PanelCsv.CollectAsync<AdResponseDto>(
            (page, size) => _sender.Send(new GetAdsQuery(query with { Page = page, Limit = size })));

        if (Common.PanelCsv.RejectIfTooLarge(total) is { } tooLarge)
        {
            TempData["Error"] = tooLarge;
            return RedirectToAction(nameof(Index), query);
        }

        return Common.PanelCsv.File(rows, AdCsvColumns, "ilanlar");
    }

    /// <summary>
    /// Dışa aktarılan sütunlar. ⚠️ Durum <c>PanelDisplay.Status()</c>'ten geçiyor —
    /// ham <c>expired</c>/<c>pending</c> basmak değişmez kural 6'nın ihlali olurdu
    /// (arayüz Türkçe), ve CSV de kullanıcıya giden bir çıktıdır.
    /// </summary>
    private static readonly IReadOnlyList<Common.PanelCsv.Column<AdResponseDto>> AdCsvColumns =
    [
        new("Başlık", x => x.Title),
        new("Durum", x => Common.PanelDisplay.Status(x.Status).Label),
        new("Fiyat", x => Common.PanelCsv.Number(x.Price)),
        new("İletişim", x => x.ContactPhone),
        new("Görüntülenme", x => x.ViewCount.ToString()),
        new("Oluşturulma", x => Common.PanelCsv.Date(x.CreatedAt)),
    ];

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCategoriesAsync();
        return View(new KadirliApp.Application.Features.Ads.Commands.CreateAd.CreateAdCommand());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KadirliApp.Application.Features.Ads.Commands.CreateAd.CreateAdCommand command, List<IFormFile> Images)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(command);
        }

        var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(adminIdStr, out var adminId))
        {
            command.UserId = adminId;
        }

        foreach (var image in Images)
        {
            var fileId = await Common.UploadHelper.UploadAsync(_sender, image, "ad", command.UserId);
            if (fileId.HasValue) command.ImageFileIds.Add(fileId.Value);
        }

        var result = await _sender.Send(command);
        TempData["Success"] = "İlan başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var command = await _sender.Send(new KadirliApp.Application.Features.Ads.Queries.GetAdByIdForEditQuery(id));
        if (command == null)
        {
            TempData["Error"] = "İlan bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Images = await _sender.Send(new KadirliApp.Application.Features.Ads.Queries.GetAdImagesQuery(id));
        // Faz 10.9(g): kategoriye özel alan değerleri Edit'te salt-okunur gösterilir
        ViewBag.PropertyValues = await _sender.Send(new KadirliApp.Application.Features.Ads.Queries.GetAdPropertyValuesQuery(id));
        // Faz 10.10-A: mobil etkileşim sayaçları salt-okunur kartta (10.9g kart deseni)
        ViewBag.Engagement = await _sender.Send(new KadirliApp.Application.Features.Ads.Queries.GetAdAdminStatsQuery(id));
        await LoadCategoriesAsync();
        return View(command);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(KadirliApp.Application.Features.Ads.Commands.UpdateAd.UpdateAdCommand command, List<IFormFile> Images)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Images = await _sender.Send(new KadirliApp.Application.Features.Ads.Queries.GetAdImagesQuery(command.Id));
            ViewBag.PropertyValues = await _sender.Send(new KadirliApp.Application.Features.Ads.Queries.GetAdPropertyValuesQuery(command.Id));
            ViewBag.Engagement = await _sender.Send(new KadirliApp.Application.Features.Ads.Queries.GetAdAdminStatsQuery(command.Id));
            await LoadCategoriesAsync();
            return View(command);
        }

        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);
        foreach (var image in Images)
        {
            var fileId = await Common.UploadHelper.UploadAsync(_sender, image, "ad", adminId);
            if (fileId.HasValue) command.NewImageFileIds.Add(fileId.Value);
        }

        var success = await _sender.Send(command);
        if (success)
        {
            TempData["Success"] = "İlan başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = "İlan güncellenirken bir hata oluştu.";
        ViewBag.Images = await _sender.Send(new KadirliApp.Application.Features.Ads.Queries.GetAdImagesQuery(command.Id));
        await LoadCategoriesAsync();
        return View(command);
    }

    [HttpPost]
    public async Task<IActionResult> Approve(Guid id)
    {
        var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(adminIdStr, out var adminId))
        {
            return Unauthorized();
        }

        var success = await _sender.Send(new ApproveAdCommand(id, adminId));

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
    public async Task<IActionResult> Reject(Guid id, string? reason)
    {
        var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(adminIdStr, out var adminId))
        {
            return Unauthorized();
        }

        var success = await _sender.Send(new KadirliApp.Application.Features.Ads.Commands.RejectAd.RejectAdCommand(id, adminId, reason));

        if (!success)
        {
            TempData["Error"] = "İlan bulunamadı veya reddedilemedi.";
        }
        else
        {
            TempData["Success"] = "İlan başarıyla reddedildi.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _sender.Send(new KadirliApp.Application.Features.Ads.Commands.DeleteAd.DeleteAdCommand(id));

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

    // ————————————————————————————————————————————————————————————————
    // Faz 11.18 — toplu işlem. Onay kuyruğundaki 40 ilanı tek tek onaylamanın sonu.
    //
    // ⚠️ Aksiyon adları BİLEREK "…Selected" ile bitiyor, "Bulk…" ile BAŞLAMIYOR:
    // panelin izin eylemi aksiyon adının ÖNEKİNDEN türetilir (görünmez sözleşme #19,
    // `PanelPermissionFilter.ActionFor`). "BulkApprove" hiçbir moderasyon önekiyle
    // eşleşmez ve sessizce "update" iznine düşerdi — yani yalnız düzenleme yetkisi olan
    // bir moderatör toplu ONAY yapabilir hâle gelirdi. `PanelBulkActionTests` bunu kilitliyor.
    // ————————————————————————————————————————————————————————————————

    [HttpPost]
    public async Task<IActionResult> ApproveSelected(Guid[] ids, [FromQuery] string? returnUrl)
    {
        var adminId = CurrentAdminId();
        var outcome = await Common.PanelBulk.RunAsync(ids, id => _sender.Send(new ApproveAdCommand(id, adminId)));
        outcome.Report(TempData, "ilan", "onaylandı");
        return RedirectBack(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> RejectSelected(Guid[] ids, string? reason, [FromQuery] string? returnUrl)
    {
        var adminId = CurrentAdminId();
        var outcome = await Common.PanelBulk.RunAsync(ids, id => _sender.Send(
            new KadirliApp.Application.Features.Ads.Commands.RejectAd.RejectAdCommand(id, adminId, reason)));
        outcome.Report(TempData, "ilan", "reddedildi");
        return RedirectBack(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSelected(Guid[] ids, [FromQuery] string? returnUrl)
    {
        var outcome = await Common.PanelBulk.RunAsync(ids, id => _sender.Send(
            new KadirliApp.Application.Features.Ads.Commands.DeleteAd.DeleteAdCommand(id)));
        outcome.Report(TempData, "ilan", "silindi");
        return RedirectBack(returnUrl);
    }

    private Guid CurrentAdminId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    /// <summary>
    /// Toplu işlemden sonra yöneticiyi **filtrelenmiş listeye geri** götürür.
    /// Düz <c>Index</c>'e dönseydi "bekleyenler" süzgeci kaybolur ve yönetici her
    /// partiden sonra filtreyi yeniden kurmak zorunda kalırdı.
    /// </summary>
    private IActionResult RedirectBack(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction(nameof(Index));
}
