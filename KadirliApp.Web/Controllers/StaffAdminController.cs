using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Features.Staff.Commands;
using KadirliApp.Application.Features.Staff.DTOs;
using KadirliApp.Application.Features.Staff.Queries;
using KadirliApp.Web.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Web.Controllers;

/// <summary>
/// Faz 10.9(e): personel (moderator/admin) yönetimi paneli — Application/API katmanı Faz 8'den
/// hazırdı, panel UI'ı burada açılıyor. İzin matrisi yalnız moderatörler için anlamlıdır
/// (admin/super_admin RequirePermission'dan otomatik geçer); super_admin satırı düzenlenemez/silinemez
/// (command'ler zaten reddeder, UI de göstermez). Tüm mutasyonlar Application command'leri üzerinden
/// (Faz 9.4 kuralı) ve AuditBehavior ile iz bırakır.
/// </summary>
[Authorize(Roles = "admin,super_admin")]
public class StaffAdminController : Controller
{
    /// <summary>
    /// İzin matrisindeki modüller — Admin API'deki [RequirePermission] modül adlarıyla birebir.
    ///
    /// ⚠️ 11.15c: bu liste daha önce elle yazılmış İKİNCİ bir kopyaydı (anahtar + Türkçe etiket
    /// <see cref="PanelMenu.Items"/>'ta zaten vardı). İki liste ayrıştığında hata sessizdir:
    /// matriste görünen modül menüde görünmez ya da tersi. Artık tek kaynaktan türüyor.
    /// </summary>
    public static readonly IReadOnlyList<(string Key, string Label)> Modules =
        PanelMenu.Items
            .Where(i => i.RequiresPermission)
            .Select(i => (Key: i.Module!, i.Label))
            .ToList();

    private readonly ISender _sender;

    public StaffAdminController(ISender sender) => _sender = sender;

    private Guid GetAdminId()
    {
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);
        return adminId;
    }

    /// <summary>Matristen gelen satırlardan yalnız en az bir yetkisi işaretli olanlar kaydedilir.</summary>
    private static List<StaffPermissionDto> CleanPermissions(List<StaffPermissionDto>? permissions)
        => (permissions ?? new List<StaffPermissionDto>())
            .Where(p => !string.IsNullOrWhiteSpace(p.Module) &&
                        (p.CanRead || p.CanCreate || p.CanUpdate || p.CanDelete || p.CanApprove))
            .ToList();

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? search, [FromQuery] int page = 1)
    {
        var result = await _sender.Send(new GetStaffQuery(search, page, 20));
        ViewBag.Search = search;
        return View(result);
    }

    [HttpGet]
    public IActionResult Create() => View((CreateStaffCommand?)null);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string phone, string? username, string? email, string password,
        string role, bool isActive, List<StaffPermissionDto>? permissions)
    {
        var command = new CreateStaffCommand
        {
            Phone = phone,
            Username = string.IsNullOrWhiteSpace(username) ? null : username.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            Password = password,
            Role = role,
            IsActive = isActive,
            Permissions = CleanPermissions(permissions)
        };
        try
        {
            await _sender.Send(command);
            TempData["Success"] = "Personel başarıyla oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }
        catch (AppException ex)
        {
            // 10.9 denetimi: girilen değerler (şifre HARİÇ) formda korunur — önceden hata sonrası form sıfırlanıyordu
            TempData["Error"] = ex.Message;
            command.Password = string.Empty;
            return View(command);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        try
        {
            var staff = await _sender.Send(new GetStaffByIdQuery(id));
            return View(staff);
        }
        catch (NotFoundException)
        {
            TempData["Error"] = "Personel bulunamadı.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, string? username, string? email, string role, bool isActive)
    {
        try
        {
            var success = await _sender.Send(new UpdateStaffCommand
            {
                Id = id,
                Username = string.IsNullOrWhiteSpace(username) ? null : username.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                Role = role,
                IsActive = isActive
            });
            TempData[success ? "Success" : "Error"] = success ? "Personel bilgileri güncellendi." : "Personel bulunamadı.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Edit), new { id });
    }

    /// <summary>İzin matrisi kaydı — mevcut satırlar komple değiştirilir (SetStaffPermissionsCommand semantiği).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Permissions(Guid id, List<StaffPermissionDto>? permissions)
    {
        try
        {
            var success = await _sender.Send(new SetStaffPermissionsCommand
            {
                Id = id,
                Permissions = CleanPermissions(permissions)
            });
            TempData[success ? "Success" : "Error"] = success ? "İzinler güncellendi." : "Personel bulunamadı.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(Guid id, string newPassword)
    {
        try
        {
            var success = await _sender.Send(new ResetStaffPasswordCommand { Id = id, NewPassword = newPassword });
            TempData[success ? "Success" : "Error"] = success ? "Şifre sıfırlandı." : "Personel bulunamadı.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var success = await _sender.Send(new DeleteStaffCommand(id, GetAdminId()));
            TempData[success ? "Success" : "Error"] = success ? "Personel silindi." : "Personel bulunamadı.";
        }
        catch (AppException ex) // kendi hesabı / super_admin engeli
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
