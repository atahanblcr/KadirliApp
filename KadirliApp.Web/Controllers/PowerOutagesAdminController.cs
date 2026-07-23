using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using KadirliApp.Application.Features.PowerOutages.Queries.GetPowerOutages;
using KadirliApp.Application.Features.PowerOutages.Commands.DeletePowerOutage;
using KadirliApp.Application.Features.PowerOutages.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace KadirliApp.Web.Controllers;

[Authorize(Roles = "admin,super_admin")]
public class PowerOutagesAdminController : Controller
{
    private readonly ISender _sender;

    public PowerOutagesAdminController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _sender.Send(new GetPowerOutagesQuery());
        if (result.Success)
        {
            return View(result.Data);
        }
        
        return View(new List<PowerOutageDto>());
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
            TempData["SuccessMessage"] = "Kesinti bilgisi başarıyla silindi.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Error?.Message ?? "Silinirken bir hata oluştu.";
        }
        return RedirectToAction(nameof(Index));
    }
}
