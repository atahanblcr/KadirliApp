using System;
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

[Authorize(Roles = "admin,super_admin")]
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
        return View(result);
    }
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
    public async Task<IActionResult> Reject(Guid id)
    {
        var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(adminIdStr, out var adminId))
        {
            return Unauthorized();
        }

        var success = await _sender.Send(new KadirliApp.Application.Features.Ads.Commands.RejectAd.RejectAdCommand(id, adminId));

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
}
