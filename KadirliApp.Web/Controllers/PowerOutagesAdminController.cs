using Microsoft.AspNetCore.Authorization;
using KadirliApp.Web.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using KadirliApp.Application.Features.PowerOutages.Queries.GetPowerOutages;
using KadirliApp.Application.Features.PowerOutages.Commands.DeletePowerOutage;
using KadirliApp.Application.Features.PowerOutages.DTOs;
using KadirliApp.Application.Common.Models;
using KadirliApp.Web.Common;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace KadirliApp.Web.Controllers;

[Authorize(Roles = "admin,super_admin,moderator")]
[PanelPermission("power-outages")]
public class PowerOutagesAdminController : Controller
{
    private readonly ISender _sender;

    public PowerOutagesAdminController(ISender sender)
    {
        _sender = sender;
    }

    private const int PageSize = 20;

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? neighborhood,
        [FromQuery] string? phase,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1)
    {
        var result = await _sender.Send(new GetPowerOutagesQuery());
        var outages = result.Success ? result.Data ?? new List<PowerOutageDto>() : new List<PowerOutageDto>();

        // ⚠️ Sayfalama BİLİNÇLİ olarak bellekte yapılıyor: GetPowerOutagesQuery tarih filtresi
        // olmadan TÜM kayıtları döner ve mobil (Faz 11.4) süren/planlı ayrımını istemcide bu tam
        // listeye bakarak yapıyor. Sorguyu PagedResult'a çevirmek public kontratı kırardı.
        // Panelin tek sorunu 1000 satırı ekrana basmaktı — çözülen o.
        //
        // Faz 11.17: süzgeç de aynı sebeple bellekte. Uca filtre parametresi eklemek
        // görünmez sözleşme #1'i (sayfalamayan düz dizi) tartışmaya açardı.
        var now = DateTime.UtcNow;
        var wantedPhase = PowerOutagePhaseRules.Parse(phase);

        var filtered = outages.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(neighborhood))
            filtered = filtered.Where(o => o.Neighborhood != null &&
                o.Neighborhood.Contains(neighborhood.Trim(), StringComparison.OrdinalIgnoreCase));

        if (wantedPhase is { } p)
            filtered = filtered.Where(o => PowerOutagePhaseRules.Phase(o.StartTime, o.EndTime, now) == p);

        // Tarih aralığı **kesişim** üzerinden: "1–3 Ağustos" seçen yönetici, 31 Temmuz'da
        // başlayıp 2 Ağustos'ta biten kesintiyi de görmek ister. Yalnız StartTime'a bakmak
        // uzun kesintileri sessizce eler.
        if (from is { } f)
        {
            var start = DateTime.SpecifyKind(f.Date, DateTimeKind.Utc);
            filtered = filtered.Where(o => o.EndTime >= start);
        }

        if (to is { } t)
        {
            var end = DateTime.SpecifyKind(t.Date.AddDays(1), DateTimeKind.Utc);
            filtered = filtered.Where(o => o.StartTime < end);
        }

        ViewBag.Neighborhood = neighborhood;
        ViewBag.Phase = phase;
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.Now = now;
        ViewBag.TotalBeforeFilter = outages.Count;

        return View(Paginate(filtered.ToList(), page));
    }

    private static PagedResult<PowerOutageDto> Paginate(List<PowerOutageDto> source, int page)
    {
        var (currentPage, pageSize) = Pagination.Clamp(page, PageSize, Pagination.AdminMaxLimit);

        return new PagedResult<PowerOutageDto>
        {
            Items = source.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList(),
            TotalCount = source.Count,
            CurrentPage = currentPage,
            PageSize = pageSize
        };
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePowerOutageDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        
        var result = await _sender.Send(new KadirliApp.Application.Features.PowerOutages.Commands.CreatePowerOutage.CreatePowerOutageCommand { Dto = dto });
        if (result.Success)
        {
            TempData["Success"] = "Elektrik kesintisi başarıyla eklendi.";
            return RedirectToAction(nameof(Index));
        }
        
        TempData["Error"] = result.Error?.Message ?? "Kesinti eklenirken bir hata oluştu.";
        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await _sender.Send(new KadirliApp.Application.Features.PowerOutages.Queries.GetPowerOutageById.GetPowerOutageByIdQuery { Id = id });
        if (!result.Success || result.Data == null) return NotFound();

        var dto = new UpdatePowerOutageDto
        {
            Neighborhood = result.Data.Neighborhood,
            StartTime = result.Data.StartTime,
            EndTime = result.Data.EndTime,
            Reason = result.Data.Reason
        };
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, UpdatePowerOutageDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        
        var result = await _sender.Send(new KadirliApp.Application.Features.PowerOutages.Commands.UpdatePowerOutage.UpdatePowerOutageCommand { Id = id, Dto = dto });
        if (result.Success)
        {
            TempData["Success"] = "Elektrik kesintisi başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }
        
        TempData["Error"] = result.Error?.Message ?? "Kesinti güncellenirken bir hata oluştu.";
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _sender.Send(new DeletePowerOutageCommand { Id = id });
        if (result.Success)
        {
            TempData["Success"] = "Kesinti bilgisi başarıyla silindi.";
        }
        else
        {
            TempData["Error"] = result.Error?.Message ?? "Silinirken bir hata oluştu.";
        }
        return RedirectToAction(nameof(Index));
    }
}
