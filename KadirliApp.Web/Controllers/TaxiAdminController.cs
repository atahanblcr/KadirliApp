using System;
using System.Security.Claims;
using System.Threading.Tasks;
using KadirliApp.Application.Features.Taxis.Commands;
using KadirliApp.Application.Features.Taxis.Dtos;
using KadirliApp.Application.Features.Taxis.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Web.Controllers;

[Authorize(Roles = "admin,super_admin")]
public class TaxiAdminController : Controller
{
    private readonly ISender _sender;

    public TaxiAdminController(ISender sender)
    {
        _sender = sender;
    }

    private Guid GetAdminId()
    {
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);
        return adminId;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] QueryTaxiDriverDto query)
    {
        query ??= new QueryTaxiDriverDto();
        if (query.Limit == 10) query.Limit = 20;

        var result = await _sender.Send(new GetTaxiDriversQuery(query));
        // Faz 10.10-A: çağrı istatistikleri panel-only ayrı query'den (public DTO'ya sayaç sızdırılmaz)
        ViewBag.CallStats = await _sender.Send(new GetTaxiAdminStatsQuery());
        return View(result);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateTaxiDriverCommand());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTaxiDriverCommand command)
    {
        if (!ModelState.IsValid)
        {
            return View(command);
        }

        await _sender.Send(command);
        TempData["Success"] = "Taksici başarıyla eklendi. Doğrulama için listeden 'Onayla' butonunu kullanın.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var driver = await _sender.Send(new GetTaxiDriverByIdQuery(id));
        if (driver == null)
        {
            TempData["Error"] = "Taksici bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var command = new UpdateTaxiDriverCommand
        {
            Id = driver.Id,
            Name = driver.Name,
            Phone = driver.Phone,
            Plaka = driver.Plaka,
            VehicleInfo = driver.VehicleInfo,
            IsActive = driver.IsActive
        };

        return View(command);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateTaxiDriverCommand command)
    {
        if (!ModelState.IsValid)
        {
            return View(command);
        }

        var success = await _sender.Send(command);
        if (success)
        {
            TempData["Success"] = "Taksici bilgileri güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = "Taksici güncellenirken bir hata oluştu.";
        return View(command);
    }

    [HttpPost]
    public async Task<IActionResult> Verify(Guid id)
    {
        var success = await _sender.Send(new VerifyTaxiDriverCommand(id, GetAdminId()));
        if (success)
            TempData["Success"] = "Taksici başarıyla doğrulandı.";
        else
            TempData["Error"] = "Taksici bulunamadı veya doğrulanamadı.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _sender.Send(new DeleteTaxiDriverCommand(id));
        if (success)
            TempData["Success"] = "Taksici başarıyla silindi.";
        else
            TempData["Error"] = "Taksici bulunamadı veya silinemedi.";

        return RedirectToAction(nameof(Index));
    }
}
