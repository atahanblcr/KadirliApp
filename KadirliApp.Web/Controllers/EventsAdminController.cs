using System;
using KadirliApp.Web.Authorization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Events.Commands;
using KadirliApp.Application.Features.Events.Dtos;
using KadirliApp.Application.Features.Events.Queries;
using KadirliApp.Domain.Entities;
using KadirliApp.Web.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Web.Controllers;

[Authorize(Roles = "admin,super_admin,moderator")]
[PanelPermission("events")]
public class EventsAdminController : Controller
{
    private readonly ISender _sender;
    private readonly IUnitOfWork _uow;

    public EventsAdminController(ISender sender, IUnitOfWork uow)
    {
        _sender = sender;
        _uow = uow;
    }

    private async Task LoadCategoriesAsync()
    {
        ViewBag.Categories = await _uow.Repository<EventCategory>().Query().OrderBy(x => x.Name).ToListAsync();
    }

    private Guid GetAdminId()
    {
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);
        return adminId;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] QueryEventDto query)
    {
        var result = await _sender.Send(new GetEventsQuery(query ?? new QueryEventDto()));
        return View(result);
    }

    /// <summary>Aylık takvim görünümü; bir güne tıklayınca o güne etkinlik eklenir.</summary>
    [HttpGet]
    public async Task<IActionResult> Calendar(int? year, int? month)
    {
        var today = DateTime.Today;
        var y = year ?? today.Year;
        var m = month ?? today.Month;
        if (m < 1) { m = 12; y--; }
        if (m > 12) { m = 1; y++; }

        var events = await _sender.Send(new GetEventCalendarQuery(y, m));

        ViewBag.Year = y;
        ViewBag.Month = m;
        return View(events);
    }

    [HttpGet]
    public async Task<IActionResult> Create(DateTime? date)
    {
        await LoadCategoriesAsync();
        return View(new CreateEventCommand { EventDate = date?.Date ?? DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateEventCommand command, IFormFile? CoverImage)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(command);
        }

        command.CoverImageId = await UploadHelper.UploadAsync(_sender, CoverImage, "event", GetAdminId());
        command.CreatedBy = GetAdminId();
        command.AutoApprove = true;

        await _sender.Send(command);
        TempData["Success"] = "Etkinlik başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var ev = await _sender.Send(new GetEventByIdQuery(id));
        if (ev == null)
        {
            TempData["Error"] = "Etkinlik bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var command = new UpdateEventCommand
        {
            Id = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            CategoryId = ev.CategoryId,
            EventDate = ev.EventDate,
            EventTime = ev.EventTime,
            VenueName = ev.VenueName,
            Address = ev.Address,
            Latitude = ev.Latitude,
            Longitude = ev.Longitude,
            Organizer = ev.Organizer,
            TicketPrice = ev.TicketPrice,
            IsFree = ev.IsFree,
            IsLocal = ev.IsLocal,
            CoverImageId = ev.CoverImageId,
            Status = ev.Status
        };

        ViewBag.CoverImageUrl = ev.CoverImageUrl;
        await LoadCategoriesAsync();
        return View(command);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateEventCommand command, IFormFile? CoverImage)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(command);
        }

        var newImageId = await UploadHelper.UploadAsync(_sender, CoverImage, "event", GetAdminId());
        if (newImageId.HasValue) command.CoverImageId = newImageId;

        var success = await _sender.Send(command);
        if (success)
        {
            TempData["Success"] = "Etkinlik başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = "Etkinlik güncellenirken bir hata oluştu.";
        await LoadCategoriesAsync();
        return View(command);
    }

    [HttpPost]
    public async Task<IActionResult> Approve(Guid id)
    {
        var success = await _sender.Send(new ApproveEventCommand(id, GetAdminId()));
        if (success)
            TempData["Success"] = "Etkinlik başarıyla onaylandı.";
        else
            TempData["Error"] = "Etkinlik bulunamadı veya onaylanamadı.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Reject(Guid id)
    {
        var success = await _sender.Send(new RejectEventCommand(id, GetAdminId()));
        if (success)
            TempData["Success"] = "Etkinlik reddedildi.";
        else
            TempData["Error"] = "Etkinlik bulunamadı veya reddedilemedi.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _sender.Send(new DeleteEventCommand(id));
        if (success)
            TempData["Success"] = "Etkinlik başarıyla silindi.";
        else
            TempData["Error"] = "Etkinlik bulunamadı veya silinemedi.";

        return RedirectToAction(nameof(Index));
    }

    // Faz 11.18 — toplu işlem. ⚠️ Ad "…Selected" ile bitmeli, "Bulk…" ile başlamamalı:
    // izin eylemi aksiyon adının önekinden türetilir (görünmez sözleşme #19).
    [HttpPost]
    public async Task<IActionResult> ApproveSelected(Guid[] ids, [FromQuery] string? returnUrl)
    {
        var adminId = GetAdminId();
        var outcome = await Common.PanelBulk.RunAsync(ids, id => _sender.Send(new ApproveEventCommand(id, adminId)));
        outcome.Report(TempData, "etkinlik", "onaylandı");
        return BackToList(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> RejectSelected(Guid[] ids, [FromQuery] string? returnUrl)
    {
        var adminId = GetAdminId();
        var outcome = await Common.PanelBulk.RunAsync(ids, id => _sender.Send(new RejectEventCommand(id, adminId)));
        outcome.Report(TempData, "etkinlik", "reddedildi");
        return BackToList(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSelected(Guid[] ids, [FromQuery] string? returnUrl)
    {
        var outcome = await Common.PanelBulk.RunAsync(ids, id => _sender.Send(new DeleteEventCommand(id)));
        outcome.Report(TempData, "etkinlik", "silindi");
        return BackToList(returnUrl);
    }

    /// <summary>Toplu işlemden sonra filtrelenmiş listeye geri döner (süzgeç kaybolmasın).</summary>
    private IActionResult BackToList(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction(nameof(Index));
}
