using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using KadirliApp.Application.Features.Transport.Queries;
using KadirliApp.Application.Features.Transport.Dtos;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Application.Features.Transport.Commands;
using System.Threading.Tasks;
using System;

namespace KadirliApp.Web.Controllers;

[Authorize(Roles = "admin,super_admin")]
public class TransportAdminController : Controller
{
    private readonly ISender _sender;
    private readonly IUnitOfWork _unitOfWork;

    public TransportAdminController(ISender sender, IUnitOfWork unitOfWork)
    {
        _sender = sender;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? search, [FromQuery] int page = 1)
    {
        var queryDto = new QueryTransportDto { SearchTerm = search, Page = page, Limit = 20 };
        var result = await _sender.Send(new GetIntracityRoutesQuery(queryDto));
        
        ViewBag.Search = search;
        return View(result);
    }

    // Faz 10.9(h): inline Remove yerine Application command'i (Faz 9.4 kuralı)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _sender.Send(new DeleteIntracityRouteCommand(id));
        if (result)
        {
            TempData["SuccessMessage"] = "Ulaşım güzergahı başarıyla silindi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Güzergah bulunamadı.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateIntracityRouteCommand());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateIntracityRouteCommand command)
    {
        if (!ModelState.IsValid)
        {
            return View(command);
        }

        try
        {
            await _sender.Send(command);
            TempData["Success"] = "Yeni güzergah başarıyla eklendi.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Güzergah eklenirken bir hata oluştu: {ex.Message}";
            return View(command);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var route = await _unitOfWork.Repository<IntracityRoute>().GetByIdAsync(id);
        if (route == null)
        {
            TempData["Error"] = "Güzergah bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var command = new UpdateIntracityRouteCommand
        {
            Id = route.Id,
            RouteNumber = route.RouteNumber,
            RouteName = route.RouteName,
            FirstDeparture = route.FirstDeparture,
            LastDeparture = route.LastDeparture,
            FrequencyMinutes = route.FrequencyMinutes,
            IsActive = route.IsActive
        };

        return View(command);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UpdateIntracityRouteCommand command)
    {
        if (!ModelState.IsValid)
        {
            return View(command);
        }

        try
        {
            var result = await _sender.Send(command);
            if (result)
            {
                TempData["Success"] = "Güzergah başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            
            TempData["Error"] = "Güzergah güncellenemedi.";
            return View(command);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Güzergah güncellenirken bir hata oluştu: {ex.Message}";
            return View(command);
        }
    }
}
