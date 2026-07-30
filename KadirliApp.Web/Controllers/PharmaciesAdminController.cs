using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Features.Pharmacies.Queries;
using KadirliApp.Application.Features.Pharmacies.Commands;
using KadirliApp.Application.Features.Pharmacies.Dtos;
using System.Threading.Tasks;
using System;

namespace KadirliApp.Web.Controllers;

[Authorize(Roles = "admin,super_admin")]
public class PharmaciesAdminController : Controller
{
    private readonly ISender _sender;

    public PharmaciesAdminController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? search, [FromQuery] int page = 1)
    {
        var queryDto = new QueryPharmacyDto(search, null, page, 20);
        var result = await _sender.Send(new GetPharmaciesQuery(queryDto));
        
        ViewBag.Search = search;
        return View(result);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePharmacyDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        
        var result = await _sender.Send(new CreatePharmacyCommand(dto));
        if (result != Guid.Empty)
        {
            TempData["Success"] = "Eczane başarıyla eklendi.";
            return RedirectToAction(nameof(Index));
        }
        
        TempData["Error"] = "Eczane eklenirken bir hata oluştu.";
        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await _sender.Send(new GetPharmacyByIdQuery(id));
        if (result == null) return NotFound();

        var dto = new UpdatePharmacyDto(
            result.Name, result.Address, result.Phone,
            result.Latitude, result.Longitude,
            result.WorkingHours, result.PharmacistName,
            result.IsActive
        );
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, UpdatePharmacyDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        
        var result = await _sender.Send(new UpdatePharmacyCommand(id, dto));
        if (result)
        {
            TempData["Success"] = "Eczane başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }
        
        TempData["Error"] = "Eczane güncellenirken bir hata oluştu.";
        return View(dto);
    }

    /// <summary>Faz 10.9(a): aylık nöbet takvimi — mobilin 1 no'lu verisi (on-duty) artık panelden yönetilir.</summary>
    [HttpGet]
    public async Task<IActionResult> Schedule(int? year, int? month, DateTime? date)
    {
        var today = DateTime.Today;
        var y = year ?? today.Year;
        var m = month ?? today.Month;
        if (m < 1) { m = 12; y--; }
        if (m > 12) { m = 1; y++; }

        var entries = await _sender.Send(new GetPharmacyScheduleQuery(y, m));
        // Atama dropdown'u yalnız aktif eczaneler; nöbet zaten aktif eczane işi
        var pharmacies = await _sender.Send(new GetPharmaciesQuery(new QueryPharmacyDto(null, true, 1, 200)));
        var onDutyToday = await _sender.Send(new GetOnDutyPharmaciesQuery(null));

        ViewBag.Year = y;
        ViewBag.Month = m;
        ViewBag.Pharmacies = pharmacies.Items;
        ViewBag.OnDutyToday = onDutyToday;
        ViewBag.SelectedDate = date; // takvimden "+" ile gelinen gün — form ön-dolu açılır
        return View(entries);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ScheduleCreate(Guid pharmacyId, DateTime dutyDate, TimeSpan? startTime, TimeSpan? endTime)
    {
        if (pharmacyId == Guid.Empty || dutyDate == default)
        {
            TempData["Error"] = "Eczane ve nöbet günü seçilmelidir.";
            return RedirectToAction(nameof(Schedule));
        }

        try
        {
            await _sender.Send(new CreatePharmacyScheduleCommand(
                new CreatePharmacyScheduleDto(pharmacyId, dutyDate, startTime, endTime, Source: "panel")));
            TempData["Success"] = $"{dutyDate:dd.MM.yyyy} günü için nöbet kaydı eklendi.";
        }
        catch (AppException ex) // ConflictException (aynı eczane+gün) dahil
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Schedule), new { year = dutyDate.Year, month = dutyDate.Month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ScheduleDelete(Guid id, int? year, int? month)
    {
        var result = await _sender.Send(new DeletePharmacyScheduleCommand(id));
        if (result)
            TempData["Success"] = "Nöbet kaydı silindi.";
        else
            TempData["Error"] = "Nöbet kaydı bulunamadı.";
        return RedirectToAction(nameof(Schedule), new { year, month });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _sender.Send(new DeletePharmacyCommand(id));
        if (result)
        {
            TempData["Success"] = "Eczane başarıyla silindi.";
        }
        else
        {
            TempData["Error"] = "Eczane silinirken bir hata oluştu.";
        }
        return RedirectToAction(nameof(Index));
    }
}
