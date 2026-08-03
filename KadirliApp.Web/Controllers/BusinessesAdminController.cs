using System;
using KadirliApp.Web.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Features.Businesses.Commands;
using KadirliApp.Application.Features.Businesses.Dtos;
using KadirliApp.Application.Features.Businesses.Queries;
using KadirliApp.Web.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Web.Controllers;

/// <summary>Faz 10.9(b): işletme yönetimi paneli — kampanya formu artık gerçek işletmelerle çalışabilir.</summary>
[Authorize(Roles = "admin,super_admin,moderator")]
[PanelPermission("businesses")]
public class BusinessesAdminController : Controller
{
    private readonly ISender _sender;

    public BusinessesAdminController(ISender sender)
    {
        _sender = sender;
    }

    private Guid GetAdminId()
    {
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);
        return adminId;
    }

    private async Task LoadCategoriesAsync()
    {
        ViewBag.Categories = await _sender.Send(new GetBusinessCategoriesQuery());
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? search, [FromQuery] Guid? categoryId, [FromQuery] int page = 1)
    {
        var result = await _sender.Send(new GetBusinessesQuery(new QueryBusinessDto(search, categoryId, null, page, 20)));
        await LoadCategoriesAsync();
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCategoriesAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBusinessDto dto, IFormFile? Logo)
    {
        try
        {
            var logoId = await UploadHelper.UploadAsync(_sender, Logo, "business", GetAdminId());
            var id = await _sender.Send(new CreateBusinessCommand(dto with { LogoFileId = logoId }));
            TempData["Success"] = "İşletme başarıyla eklendi.";
            return RedirectToAction(nameof(Index));
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
            await LoadCategoriesAsync();
            return View(dto);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var business = await _sender.Send(new GetBusinessByIdQuery(id));
        if (business == null)
        {
            TempData["Error"] = "İşletme bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var dto = new UpdateBusinessDto(
            business.BusinessName, business.CategoryId, business.TaxNumber,
            business.Address, business.Phone, business.Email,
            business.WebsiteUrl, business.InstagramHandle, business.LogoFileId);

        ViewBag.Id = id;
        ViewBag.LogoUrl = business.LogoUrl;
        ViewBag.IsVerified = business.IsVerified;
        await LoadCategoriesAsync();
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UpdateBusinessDto dto, IFormFile? Logo)
    {
        try
        {
            var logoId = await UploadHelper.UploadAsync(_sender, Logo, "business", GetAdminId());
            if (logoId.HasValue) dto = dto with { LogoFileId = logoId };

            var success = await _sender.Send(new UpdateBusinessCommand(id, dto));
            if (success)
            {
                TempData["Success"] = "İşletme başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "İşletme bulunamadı.";
            return RedirectToAction(nameof(Index));
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
            ViewBag.Id = id;
            await LoadCategoriesAsync();
            return View(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(Guid id)
    {
        var success = await _sender.Send(new SetBusinessVerificationCommand(id, true, GetAdminId()));
        TempData[success ? "Success" : "Error"] = success ? "İşletme doğrulandı." : "İşletme bulunamadı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unverify(Guid id)
    {
        var success = await _sender.Send(new SetBusinessVerificationCommand(id, false, GetAdminId()));
        TempData[success ? "Success" : "Error"] = success ? "Doğrulama kaldırıldı." : "İşletme bulunamadı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var success = await _sender.Send(new DeleteBusinessCommand(id));
            TempData[success ? "Success" : "Error"] = success ? "İşletme silindi." : "İşletme bulunamadı.";
        }
        catch (ConflictException ex) // kampanyası olan işletme silinemez
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Formdan hızlı kategori ekleme (AnnouncementsAdmin "yeni tür" modal deseni). returnUrl: Create/Edit'ten çağrılabilir.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(string name, string? returnUrl)
    {
        try
        {
            await _sender.Send(new CreateBusinessCategoryCommand(name));
            TempData["Success"] = $"\"{name?.Trim()}\" kategorisi eklendi.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action(nameof(Create))!);
    }
}
