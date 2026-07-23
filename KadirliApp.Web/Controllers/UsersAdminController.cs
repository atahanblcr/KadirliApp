using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using System.Threading.Tasks;
using System.Linq;

using MediatR;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Features.Users.Commands.CreateUser;
using KadirliApp.Application.Features.Users.Commands.SetUserBan;
using KadirliApp.Application.Features.Users.Commands.UpdateUser;
using KadirliApp.Application.Features.Users.DTOs;
using System.Security.Claims;

namespace KadirliApp.Web.Controllers;

[Authorize(Roles = "admin,super_admin")]
public class UsersAdminController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly ISender _sender;

    public UsersAdminController(IUnitOfWork uow, ISender sender)
    {
        _uow = uow;
        _sender = sender;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _uow.Repository<User>()
            .Query()
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
            
        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Neighborhoods = await _uow.Repository<Neighborhood>().Query().OrderBy(n => n.Name).ToListAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Neighborhoods = await _uow.Repository<Neighborhood>().Query().OrderBy(n => n.Name).ToListAsync();
            return View(dto);
        }

        var result = await _sender.Send(new CreateUserCommand { Dto = dto });
        if (result.Success)
        {
            TempData["Success"] = "Kullanıcı başarıyla oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = result.Error?.Message ?? "Bir hata oluştu.";
        ViewBag.Neighborhoods = await _uow.Repository<Neighborhood>().Query().OrderBy(n => n.Name).ToListAsync();
        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _uow.Repository<User>().GetByIdAsync(id);
        if (user == null)
        {
            TempData["Error"] = "Kullanıcı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var dto = new UpdateUserDto
        {
            Phone = user.Phone,
            Email = user.Email,
            Username = user.Username,
            Role = (int)user.Role,
            Age = user.Age,
            PrimaryNeighborhoodId = user.PrimaryNeighborhoodId,
            LocationType = user.LocationType,
            IsActive = user.IsActive
        };

        ViewBag.Id = id;
        ViewBag.Neighborhoods = await _uow.Repository<Neighborhood>().Query().OrderBy(n => n.Name).ToListAsync();
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Id = id;
            ViewBag.Neighborhoods = await _uow.Repository<Neighborhood>().Query().OrderBy(n => n.Name).ToListAsync();
            return View(dto);
        }

        var result = await _sender.Send(new UpdateUserCommand { Id = id, Dto = dto });
        if (result.Success)
        {
            TempData["Success"] = "Kullanıcı başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = result.Error?.Message ?? "Bir hata oluştu.";
        ViewBag.Id = id;
        ViewBag.Neighborhoods = await _uow.Repository<Neighborhood>().Query().OrderBy(n => n.Name).ToListAsync();
        return View(dto);
    }

    // Faz 10.9(h): inline IsBanned yazımı SetUserBanCommand'e taşındı — BanReason/BannedAt/BannedBy artık dolu (Faz 9.4 kuralı)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ban(System.Guid id, string? reason)
    {
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);
        try
        {
            await _sender.Send(new SetUserBanCommand(id, true, adminId, string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()));
            TempData["Success"] = "Kullanıcı başarıyla banlandı.";
        }
        catch (NotFoundException)
        {
            TempData["Error"] = "Kullanıcı bulunamadı.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unban(System.Guid id)
    {
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);
        try
        {
            await _sender.Send(new SetUserBanCommand(id, false, adminId));
            TempData["Success"] = "Kullanıcının banı başarıyla kaldırıldı.";
        }
        catch (NotFoundException)
        {
            TempData["Error"] = "Kullanıcı bulunamadı.";
        }
        return RedirectToAction(nameof(Index));
    }
}
