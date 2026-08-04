using System.Security.Claims;
using KadirliApp.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using KadirliApp.Application.Features.Announcements.Queries.GetAnnouncements;
using KadirliApp.Application.Features.Announcements.Queries.GetAnnouncementById;
using KadirliApp.Application.Features.Announcements.DTOs;

using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Application.Features.Announcements.Commands.CreateAnnouncement;
using KadirliApp.Application.Features.Announcements.Commands.UpdateAnnouncement;
using KadirliApp.Application.Features.Announcements.Commands.CreateAnnouncementType;
using KadirliApp.Web.Common;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Web.Controllers;

[Authorize(Roles = "admin,super_admin,moderator")]
[PanelPermission("announcements")]
public class AnnouncementsAdminController : Controller
{
    private readonly ISender _sender;
    private readonly IUnitOfWork _uow;

    public AnnouncementsAdminController(ISender sender, IUnitOfWork uow)
    {
        _sender = sender;
        _uow = uow;
    }

    private async Task LoadLookupsAsync()
    {
        ViewBag.Types = await _uow.Repository<AnnouncementType>().Query()
            .OrderBy(x => x.DisplayOrder).ToListAsync();
        ViewBag.Neighborhoods = await _uow.Repository<Neighborhood>().Query()
            .Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();
    }

    private Guid GetAdminId()
    {
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);
        return adminId;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] string? sort = null)
    {
        // Faz 10.8'de query PagedResult'a geçmişti ama panel sayfalama UI'ı olmadığı için
        // tek sayfada 200 kayıt çekiliyordu; UI geldi, sayfalı okumaya dönüldü.
        // Faz 11.18: sütun sıralaması (bu aksiyon query nesnesini elle kurduğu için
        // parametre açıkça geçirilmeli — diğer listelerde [FromQuery] DTO kendiliğinden bağlıyor).
        var result = await _sender.Send(new GetAnnouncementsQuery { Page = page, Limit = 20, Sort = sort });
        // Faz 10.10-A: görüntülenme/tıklama/tekil-erişim panel-only ayrı query'den (public DTO'ya sızdırılmaz)
        ViewBag.Stats = await _sender.Send(new KadirliApp.Application.Features.Announcements.Queries.GetAnnouncementAdminStats.GetAnnouncementAdminStatsQuery());
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAnnouncementDto dto, IFormFile? Image)
    {
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View(dto);
        }

        dto.ImageFileId = await UploadHelper.UploadAsync(_sender, Image, "announcement", GetAdminId());

        var result = await _sender.Send(new CreateAnnouncementCommand { Dto = dto, CreatedBy = GetAdminId() });
        if (result.Success)
        {
            TempData["Success"] = dto.ScheduledFor.HasValue && dto.ScheduledFor.Value > DateTime.Now
                ? $"Duyuru oluşturuldu, {dto.ScheduledFor.Value:dd.MM.yyyy HH:mm} tarihinde otomatik yayınlanacak."
                : "Duyuru oluşturuldu ve hemen yayınlandı.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = result.Error?.Message ?? "Bir hata oluştu.";
        await LoadLookupsAsync();
        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await _sender.Send(new GetAnnouncementByIdQuery { Id = id });
        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = "Duyuru bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var a = result.Data;
        var dto = new UpdateAnnouncementDto
        {
            Title = a.Title,
            Body = a.Body,
            TypeId = a.TypeId,
            Priority = a.Priority,
            TargetType = a.TargetType,
            TargetNeighborhoodIds = a.TargetNeighborhoodIds,
            ScheduledFor = a.ScheduledFor,
            SendPushNotification = a.SendPushNotification,
            Source = a.Source,
            SourceUrl = a.SourceUrl,
            VisibleUntil = a.VisibleUntil,
            HasLink = a.HasLink,
            ExternalLink = a.ExternalLink,
            ImageFileId = a.ImageFileId,
            Latitude = a.Latitude,
            Longitude = a.Longitude,
            LocationName = a.LocationName
        };

        ViewBag.Id = id;
        ViewBag.Status = a.Status;
        ViewBag.ImageUrl = a.ImageUrl;
        await LoadLookupsAsync();
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UpdateAnnouncementDto dto, IFormFile? Image)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Id = id;
            await LoadLookupsAsync();
            return View(dto);
        }

        var newImageId = await UploadHelper.UploadAsync(_sender, Image, "announcement", GetAdminId());
        if (newImageId.HasValue) dto.ImageFileId = newImageId;

        var result = await _sender.Send(new UpdateAnnouncementCommand { Id = id, Dto = dto });
        if (result.Success)
        {
            TempData["Success"] = "Duyuru başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = result.Error?.Message ?? "Bir hata oluştu.";
        ViewBag.Id = id;
        await LoadLookupsAsync();
        return View(dto);
    }

    /// <summary>Create/Edit formundaki "Yeni Tür" penceresinden AJAX ile çağrılır.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateType(string name, string? icon, string? color)
    {
        var result = await _sender.Send(new CreateAnnouncementTypeCommand { Name = name, Icon = icon, Color = color });
        if (!result.Success)
            return BadRequest(new { message = result.Error?.Message ?? "Tür eklenemedi." });

        return Json(new { id = result.Data, name });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(System.Guid id)
    {
        var result = await _sender.Send(new KadirliApp.Application.Features.Announcements.Commands.DeleteAnnouncement.DeleteAnnouncementCommand { Id = id });

        if (!result.Success)
        {
            TempData["Error"] = result.Error?.Message ?? "Duyuru bulunamadı veya silinemedi.";
        }
        else
        {
            TempData["Success"] = "Duyuru başarıyla silindi.";
        }

        return RedirectToAction(nameof(Index));
    }

    // Faz 11.18 — toplu silme. ⚠️ Ad "…Selected" ile bitmeli (görünmez sözleşme #19).
    // 🔑 Tek-kayıt komutu çağrılıyor: `DeleteAnnouncementCommand` duyuruya bağlı
    // bildirimleri de fiziksel siliyor (görünmez sözleşme #24). Toplu bir SQL silme
    // yazılsaydı mobilde **ölü bildirimler** kalırdı — 11.15c'de tam olarak bu yaşandı.
    [HttpPost]
    public async Task<IActionResult> DeleteSelected(System.Guid[] ids, [FromQuery] string? returnUrl)
    {
        var outcome = await Common.PanelBulk.RunAsync(ids, async id =>
        {
            var result = await _sender.Send(
                new KadirliApp.Application.Features.Announcements.Commands.DeleteAnnouncement.DeleteAnnouncementCommand { Id = id });
            return result.Success;
        });

        outcome.Report(TempData, "duyuru", "silindi");
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction(nameof(Index));
    }
}
