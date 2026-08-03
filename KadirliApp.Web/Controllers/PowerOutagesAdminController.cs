using Microsoft.AspNetCore.Authorization;
using KadirliApp.Web.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using KadirliApp.Application.Features.PowerOutages.Queries.GetPowerOutages;
using KadirliApp.Application.Features.PowerOutages.Commands.DeletePowerOutage;
using KadirliApp.Application.Features.PowerOutages.DTOs;
using KadirliApp.Application.Common.Models;
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
    public async Task<IActionResult> Index([FromQuery] int page = 1)
    {
        var result = await _sender.Send(new GetPowerOutagesQuery());
        var outages = result.Success ? result.Data ?? new List<PowerOutageDto>() : new List<PowerOutageDto>();

        // ⚠️ Sayfalama BİLİNÇLİ olarak bellekte yapılıyor: GetPowerOutagesQuery tarih filtresi
        // olmadan TÜM kayıtları döner ve mobil (Faz 11.4) süren/planlı ayrımını istemcide bu tam
        // listeye bakarak yapıyor. Sorguyu PagedResult'a çevirmek public kontratı kırardı.
        // Panelin tek sorunu 1000 satırı ekrana basmaktı — çözülen o.
        return View(Paginate(outages, page));
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
