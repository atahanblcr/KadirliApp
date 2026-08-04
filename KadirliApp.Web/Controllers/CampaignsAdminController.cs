using System;
using KadirliApp.Web.Authorization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Campaigns.Commands;
using KadirliApp.Application.Features.Campaigns.Dtos;
using KadirliApp.Application.Features.Campaigns.Queries;
using KadirliApp.Domain.Entities;
using KadirliApp.Web.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Web.Controllers;

[Authorize(Roles = "admin,super_admin,moderator")]
[PanelPermission("campaigns")]
public class CampaignsAdminController : Controller
{
    private readonly ISender _sender;
    private readonly IUnitOfWork _uow;

    public CampaignsAdminController(ISender sender, IUnitOfWork uow)
    {
        _sender = sender;
        _uow = uow;
    }

    // Faz 10.9(b): inline sorgu yerine Application query'si (Faz 9.4 kuralı); DTO da .Id/.BusinessName taşıdığından view değişmedi
    private async Task LoadBusinessesAsync()
    {
        var result = await _sender.Send(new Application.Features.Businesses.Queries.GetBusinessesQuery(
            new Application.Features.Businesses.Dtos.QueryBusinessDto(null, null, null, 1, 200)));
        ViewBag.Businesses = result.Items.ToList();
    }

    private Guid GetAdminId()
    {
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);
        return adminId;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] QueryCampaignDto query)
    {
        var result = await _sender.Send(new GetCampaignsQuery(query ?? new QueryCampaignDto()));
        return View(result);
    }

    /// <summary>Faz 11.16b — filtrelenmiş listeyi CSV olarak indirir (bkz. AdsAdmin.ExportCsv).</summary>
    [HttpGet]
    public async Task<IActionResult> ExportCsv([FromQuery] QueryCampaignDto query)
    {
        query ??= new QueryCampaignDto();

        var (rows, total) = await Common.PanelCsv.CollectAsync<CampaignResponseDto>((page, size) =>
        {
            query.Page = page;
            query.Limit = size;
            return _sender.Send(new GetCampaignsQuery(query));
        });

        if (Common.PanelCsv.RejectIfTooLarge(total) is { } tooLarge)
        {
            TempData["Error"] = tooLarge;
            return RedirectToAction(nameof(Index));
        }

        return Common.PanelCsv.File(rows, CampaignCsvColumns, "kampanyalar");
    }

    private static readonly IReadOnlyList<Common.PanelCsv.Column<CampaignResponseDto>> CampaignCsvColumns =
    [
        new("Başlık", x => x.Title),
        new("İşletme", x => x.BusinessName),
        new("Durum", x => Common.PanelDisplay.Status(x.Status).Label),
        new("İndirim (%)", x => Common.PanelCsv.Number(x.DiscountPercentage)),
        new("Kod", x => x.DiscountCode),
        new("Başlangıç", x => x.StartDate.ToString("dd.MM.yyyy")),
        new("Bitiş", x => x.EndDate.ToString("dd.MM.yyyy")),
        new("Kod görüntülenme", x => x.CodeViewCount.ToString()),
        new("Oluşturulma", x => Common.PanelCsv.Date(x.CreatedAt)),
    ];

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadBusinessesAsync();
        return View(new CreateCampaignCommand
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(1)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCampaignCommand command, IFormFile? CoverImage)
    {
        if (!ModelState.IsValid)
        {
            await LoadBusinessesAsync();
            return View(command);
        }

        if (command.EndDate < command.StartDate)
        {
            ModelState.AddModelError(nameof(command.EndDate), "Bitiş tarihi başlangıçtan önce olamaz.");
            await LoadBusinessesAsync();
            return View(command);
        }

        command.CoverImageId = await UploadHelper.UploadAsync(_sender, CoverImage, "campaign", GetAdminId());
        command.AutoApprove = true;
        command.ApprovedBy = GetAdminId();

        await _sender.Send(command);
        TempData["Success"] = "Kampanya başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var campaign = await _sender.Send(new GetCampaignByIdQuery(id));
        if (campaign == null)
        {
            TempData["Error"] = "Kampanya bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var command = new UpdateCampaignCommand
        {
            Id = campaign.Id,
            BusinessId = campaign.BusinessId,
            Title = campaign.Title,
            Description = campaign.Description,
            DiscountPercentage = campaign.DiscountPercentage,
            DiscountCode = campaign.DiscountCode,
            Terms = campaign.Terms,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            CoverImageId = campaign.CoverImageId,
            Status = campaign.Status
        };

        ViewBag.CoverImageUrl = campaign.CoverImageUrl;
        await LoadBusinessesAsync();
        return View(command);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateCampaignCommand command, IFormFile? CoverImage)
    {
        if (!ModelState.IsValid)
        {
            await LoadBusinessesAsync();
            return View(command);
        }

        var newImageId = await UploadHelper.UploadAsync(_sender, CoverImage, "campaign", GetAdminId());
        if (newImageId.HasValue) command.CoverImageId = newImageId;

        var success = await _sender.Send(command);
        if (success)
        {
            TempData["Success"] = "Kampanya başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = "Kampanya güncellenirken bir hata oluştu.";
        await LoadBusinessesAsync();
        return View(command);
    }

    [HttpPost]
    public async Task<IActionResult> Approve(Guid id)
    {
        var success = await _sender.Send(new ApproveCampaignCommand(id, GetAdminId()));
        if (success)
            TempData["Success"] = "Kampanya başarıyla onaylandı.";
        else
            TempData["Error"] = "Kampanya bulunamadı veya onaylanamadı.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Reject(Guid id)
    {
        var success = await _sender.Send(new RejectCampaignCommand(id, GetAdminId()));
        if (success)
            TempData["Success"] = "Kampanya reddedildi.";
        else
            TempData["Error"] = "Kampanya bulunamadı veya reddedilemedi.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _sender.Send(new DeleteCampaignCommand(id));
        if (success)
            TempData["Success"] = "Kampanya başarıyla silindi.";
        else
            TempData["Error"] = "Kampanya bulunamadı veya silinemedi.";

        return RedirectToAction(nameof(Index));
    }

    // Faz 11.18 — toplu işlem. ⚠️ Ad "…Selected" ile bitmeli (görünmez sözleşme #19).
    [HttpPost]
    public async Task<IActionResult> ApproveSelected(Guid[] ids, [FromQuery] string? returnUrl)
    {
        var adminId = GetAdminId();
        var outcome = await Common.PanelBulk.RunAsync(ids, id => _sender.Send(new ApproveCampaignCommand(id, adminId)));
        outcome.Report(TempData, "kampanya", "onaylandı");
        return BackToList(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> RejectSelected(Guid[] ids, [FromQuery] string? returnUrl)
    {
        var adminId = GetAdminId();
        var outcome = await Common.PanelBulk.RunAsync(ids, id => _sender.Send(new RejectCampaignCommand(id, adminId)));
        outcome.Report(TempData, "kampanya", "reddedildi");
        return BackToList(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSelected(Guid[] ids, [FromQuery] string? returnUrl)
    {
        var outcome = await Common.PanelBulk.RunAsync(ids, id => _sender.Send(new DeleteCampaignCommand(id)));
        outcome.Report(TempData, "kampanya", "silindi");
        return BackToList(returnUrl);
    }

    /// <summary>Toplu işlemden sonra filtrelenmiş listeye geri döner (süzgeç kaybolmasın).</summary>
    private IActionResult BackToList(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction(nameof(Index));
}
