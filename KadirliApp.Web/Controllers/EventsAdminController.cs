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
        // Faz 12.4: ilçe listesi form ve süzgeç için aynı sorgudan gelir (il başlığına göre sıralı).
        ViewBag.Districts = await _sender.Send(new Application.Features.Lookups.GetDistrictsAdminQuery());
    }

    /// <summary>Faz 12.4 — yeni etkinlikte önceden seçili gelen ilçe: Kadirli.</summary>
    private static Guid? HomeDistrictId(IReadOnlyList<Application.Features.Lookups.DistrictAdminDto> districts)
        => districts.FirstOrDefault(d => d.IsHome)?.Id;

    private Guid GetAdminId()
    {
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);
        return adminId;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] QueryEventDto query)
    {
        query ??= new QueryEventDto();
        var result = await _sender.Send(new GetEventsQuery(query));

        // Faz 12.4 — ilçe süzgeci. Şeritteki değerler ekrana ham basılmaz
        // (EventLocationScopes.Label → Türkçe), Değişmez Kural #6.
        ViewBag.Districts = await _sender.Send(new Application.Features.Lookups.GetDistrictsAdminQuery());
        ViewBag.DistrictId = query.DistrictId;
        ViewBag.LocationScope = Application.Features.Events.EventLocationScopes.Parse(query.LocationScope, query.OnlyLocal);

        return View(result);
    }

    /// <summary>Faz 11.16b — filtrelenmiş listeyi CSV olarak indirir (bkz. AdsAdmin.ExportCsv).</summary>
    [HttpGet]
    public async Task<IActionResult> ExportCsv([FromQuery] QueryEventDto query)
    {
        query ??= new QueryEventDto();

        var (rows, total) = await Common.PanelCsv.CollectAsync<EventResponseDto>((page, size) =>
        {
            query.Page = page;
            query.Limit = size;
            return _sender.Send(new GetEventsQuery(query));
        });

        if (Common.PanelCsv.RejectIfTooLarge(total) is { } tooLarge)
        {
            TempData["Error"] = tooLarge;
            return RedirectToAction(nameof(Index));
        }

        return Common.PanelCsv.File(rows, EventCsvColumns, "etkinlikler");
    }

    private static readonly IReadOnlyList<Common.PanelCsv.Column<EventResponseDto>> EventCsvColumns =
    [
        new("Başlık", x => x.Title),
        new("Kategori", x => x.CategoryName),
        new("Durum", x => Common.PanelDisplay.Status(x.Status).Label),
        new("Tarih", x => x.EventDate.ToString("dd.MM.yyyy")),
        new("Saat", x => x.EventTime.ToString(@"hh\:mm")),
        new("Yer", x => x.VenueName),
        // Faz 12.4: konum. Etiket sunucudan geldiği gibi yazılır — CSV'de ayrı bir
        // biçimlendirme yazılsaydı dosya ile ekran farklı konum gösterirdi.
        new("Konum", x => x.LocationLabel),
        new("Düzenleyen", x => x.Organizer),
        new("Bilet", x => x.IsFree ? "Ücretsiz" : Common.PanelCsv.Number(x.TicketPrice)),
        new("Oluşturulma", x => Common.PanelCsv.Date(x.CreatedAt)),
    ];

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
        return View(new CreateEventCommand
        {
            EventDate = date?.Date ?? DateTime.Today,
            // Etkinliklerin ezici çoğunluğu Kadirli'de; ilçe zorunlu olduğu için
            // varsayılan seçili gelmezse her kayıtta bir tıklama daha gerekirdi.
            DistrictId = HomeDistrictId((IReadOnlyList<Application.Features.Lookups.DistrictAdminDto>)ViewBag.Districts)
        });
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

        try
        {
            await _sender.Send(command);
        }
        catch (Application.Common.Exceptions.AppException ex)
        {
            // Faz 12.4: ilçe doğrulaması komutta — mesajı yutup boş bir "hata oluştu"
            // göstermek, yöneticiye neyi düzelteceğini söylemezdi.
            TempData["Error"] = ex.Message;
            await LoadCategoriesAsync();
            return View(command);
        }

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
            DistrictId = ev.DistrictId,
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

        bool success;
        try
        {
            success = await _sender.Send(command);
        }
        catch (Application.Common.Exceptions.AppException ex)
        {
            TempData["Error"] = ex.Message;
            await LoadCategoriesAsync();
            return View(command);
        }

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
