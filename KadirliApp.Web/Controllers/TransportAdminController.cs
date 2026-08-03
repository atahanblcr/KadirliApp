using Microsoft.AspNetCore.Authorization;
using KadirliApp.Web.Authorization;
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

/// <summary>
/// Ulaşım paneli. **Faz 11.17'ye kadar yalnız şehir içi hatları yönetiyordu** — şehirlerarası
/// hatlar, kalkış saatleri ve duraklar için komutlar 10.8'den beri <c>Application</c>'da hazırdı
/// ama onları çağıran bir istemci yoktu (mobil "Şehirlerarası" sekmesi seed verisiyle yaşıyordu,
/// ilk saat değişikliğinde <c>psql</c> gerekiyordu). 11.15c denetiminin bulduğu
/// **tek gerçek işlevsel boşluk** buydu.
///
/// ⚠️ Aksiyon adları izin eylemini belirler (görünmez sözleşme #19,
/// <c>PanelPermissionFilter.ActionFor</c>): <c>IntercityCreate</c>→create, <c>AddSchedule</c>→create,
/// <c>DeleteStop</c>→delete… Yeniden adlandırırken izin sessizce kayar.
/// </summary>
[Authorize(Roles = "admin,super_admin,moderator")]
[PanelPermission("transport")]
public class TransportAdminController : Controller
{
    private readonly ISender _sender;
    private readonly IUnitOfWork _unitOfWork;

    public TransportAdminController(ISender sender, IUnitOfWork unitOfWork)
    {
        _sender = sender;
        _unitOfWork = unitOfWork;
    }

    // ---------------------------------------------------------------- şehir içi hatlar

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
            TempData["Success"] = "Ulaşım güzergahı başarıyla silindi.";
        }
        else
        {
            TempData["Error"] = "Güzergah bulunamadı.";
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

    // ------------------------------------------------------- şehir içi duraklar (Faz 11.17)

    /// <summary>Hat durakları — mobildeki "durak zaman çizelgesi"nin kaynağı.</summary>
    [HttpGet]
    public async Task<IActionResult> Stops(Guid id)
    {
        var route = await _sender.Send(new GetIntracityRouteByIdQuery(id));
        if (route == null)
        {
            TempData["Error"] = "Güzergah bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        return View(route);
    }

    [HttpPost]
    public async Task<IActionResult> AddStop(Guid routeId, string stopName, int stopOrder, int? timeFromStart)
    {
        try
        {
            await _sender.Send(new CreateIntracityStopCommand(routeId, stopName, stopOrder, timeFromStart));
            TempData["Success"] = $"\"{stopName}\" durağı eklendi.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Stops), new { id = routeId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteStop(Guid id, Guid routeId)
    {
        try
        {
            await _sender.Send(new DeleteIntracityStopCommand(id));
            TempData["Success"] = "Durak silindi.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Stops), new { id = routeId });
    }

    // ------------------------------------------------------ şehirlerarası hatlar (Faz 11.17)

    [HttpGet]
    public async Task<IActionResult> Intercity([FromQuery] string? search, [FromQuery] int page = 1)
    {
        var queryDto = new QueryTransportDto { SearchTerm = search, Page = page, Limit = 20 };
        var result = await _sender.Send(new GetIntercityRoutesQuery(queryDto));

        ViewBag.Search = search;
        return View(result);
    }

    [HttpGet]
    public IActionResult IntercityCreate() => View(new CreateIntercityRouteCommand());

    [HttpPost]
    public async Task<IActionResult> IntercityCreate(CreateIntercityRouteCommand command)
    {
        if (!ModelState.IsValid) return View(command);

        try
        {
            // Saatsiz hat mobilde "sefer yok" demektir → kullanıcıyı doğrudan saat ekranına al.
            var id = await _sender.Send(command);
            TempData["Success"] = $"\"{command.Destination}\" hattı eklendi. Şimdi kalkış saatlerini girin.";
            return RedirectToAction(nameof(IntercityEdit), new { id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Hat eklenirken bir hata oluştu: {ex.Message}";
            return View(command);
        }
    }

    /// <summary>Hat bilgileri + kalkış saatleri tek ekranda (saatler hattın asıl içeriği).</summary>
    [HttpGet]
    public async Task<IActionResult> IntercityEdit(Guid id)
    {
        var route = await _sender.Send(new GetIntercityRouteByIdQuery(id));
        if (route == null)
        {
            TempData["Error"] = "Hat bulunamadı.";
            return RedirectToAction(nameof(Intercity));
        }

        ViewBag.Schedules = route.Schedules;
        return View(new UpdateIntercityRouteCommand
        {
            Id = route.Id,
            Destination = route.Destination,
            Price = route.Price,
            DurationMinutes = route.DurationMinutes,
            Company = route.Company,
            IsActive = route.IsActive
        });
    }

    [HttpPost]
    public async Task<IActionResult> IntercityEdit(UpdateIntercityRouteCommand command)
    {
        if (!ModelState.IsValid)
        {
            // Doğrulama hatasında da saat listesi çizilmeli — yoksa ekran yarım görünür.
            ViewBag.Schedules = (await _sender.Send(new GetIntercityRouteByIdQuery(command.Id)))?.Schedules
                                ?? new System.Collections.Generic.List<IntercityRouteResponseDto.ScheduleDto>();
            return View(command);
        }

        try
        {
            var result = await _sender.Send(command);
            if (result)
            {
                TempData["Success"] = "Hat bilgileri güncellendi.";
                return RedirectToAction(nameof(Intercity));
            }

            TempData["Error"] = "Hat bulunamadı.";
            return RedirectToAction(nameof(Intercity));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Hat güncellenirken bir hata oluştu: {ex.Message}";
            return RedirectToAction(nameof(IntercityEdit), new { id = command.Id });
        }
    }

    [HttpPost]
    public async Task<IActionResult> IntercityDelete(Guid id)
    {
        var result = await _sender.Send(new DeleteIntercityRouteCommand(id));
        TempData[result ? "Success" : "Error"] = result
            ? "Şehirlerarası hat ve kalkış saatleri silindi."
            : "Hat bulunamadı.";

        return RedirectToAction(nameof(Intercity));
    }

    [HttpPost]
    public async Task<IActionResult> AddSchedule(Guid routeId, string departureTime)
    {
        try
        {
            await _sender.Send(new CreateIntercityScheduleCommand(routeId, departureTime));
            TempData["Success"] = $"{departureTime} kalkışı eklendi.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(IntercityEdit), new { id = routeId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSchedule(Guid id, Guid routeId)
    {
        try
        {
            await _sender.Send(new DeleteIntercityScheduleCommand(id));
            TempData["Success"] = "Kalkış saati silindi.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(IntercityEdit), new { id = routeId });
    }
}
