using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Features.Ads.Commands;
using KadirliApp.Application.Features.Ads.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Web.Controllers;

/// <summary>
/// Faz 10.9(c): ilan kategori ağacı + kategori özellikleri (property/option) paneli —
/// öncesinde bu veriler YALNIZ DbSeeder'dan geliyordu. Tüm mutasyonlar Application
/// command'leri üzerinden (Faz 9.4 kuralı) ve ads-lookup cache'ini invalidate eder.
/// </summary>
[Authorize(Roles = "admin,super_admin")]
public class AdCategoriesAdminController : Controller
{
    private readonly ISender _sender;

    public AdCategoriesAdminController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categories = await _sender.Send(new GetAdCategoriesAdminQuery());
        return View(categories);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? parentId)
    {
        await LoadParentsAsync();
        ViewBag.ParentId = parentId;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, Guid? parentId, string? icon, int displayOrder, bool isActive)
    {
        try
        {
            await _sender.Send(new CreateAdCategoryCommand(name, parentId, icon, displayOrder, isActive));
            TempData["Success"] = $"\"{name?.Trim()}\" kategorisi eklendi.";
            return RedirectToAction(nameof(Index));
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
            await LoadParentsAsync();
            ViewBag.ParentId = parentId;
            return View();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var category = (await _sender.Send(new GetAdCategoriesAdminQuery())).FirstOrDefault(c => c.Id == id);
        if (category == null)
        {
            TempData["Error"] = "Kategori bulunamadı.";
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, string name, string? icon, int displayOrder, bool isActive)
    {
        try
        {
            var success = await _sender.Send(new UpdateAdCategoryCommand(id, name, icon, displayOrder, isActive));
            TempData[success ? "Success" : "Error"] = success ? "Kategori güncellendi." : "Kategori bulunamadı.";
            return RedirectToAction(nameof(Index));
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var success = await _sender.Send(new DeleteAdCategoryCommand(id));
            TempData[success ? "Success" : "Error"] = success ? "Kategori silindi." : "Kategori bulunamadı.";
        }
        catch (ConflictException ex) // ilanı/alt kategorisi/özelliği olan kategori silinemez
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    // ---- Özellikler (property) ----

    [HttpGet]
    public async Task<IActionResult> Properties(Guid id)
    {
        var category = (await _sender.Send(new GetAdCategoriesAdminQuery())).FirstOrDefault(c => c.Id == id);
        if (category == null)
        {
            TempData["Error"] = "Kategori bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Category = category;
        var properties = await _sender.Send(new GetCategoryPropertiesAdminQuery(id));
        return View(properties);
    }

    /// <param name="options">Select/MultiSelect için virgülle ayrılmış seçenek listesi.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PropertyCreate(Guid categoryId, string propertyName, string propertyType,
        bool isRequired, string? defaultValue, int displayOrder, string? options)
    {
        try
        {
            var optionList = (options ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            await _sender.Send(new CreateCategoryPropertyCommand(
                categoryId, propertyName, propertyType, isRequired, defaultValue, displayOrder, optionList));
            TempData["Success"] = $"\"{propertyName?.Trim()}\" özelliği eklendi.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Properties), new { id = categoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PropertyUpdate(Guid id, Guid categoryId, string propertyName,
        bool isRequired, string? defaultValue, int displayOrder)
    {
        try
        {
            var success = await _sender.Send(new UpdateCategoryPropertyCommand(id, propertyName, isRequired, defaultValue, displayOrder));
            TempData[success ? "Success" : "Error"] = success ? "Özellik güncellendi." : "Özellik bulunamadı.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Properties), new { id = categoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PropertyDelete(Guid id, Guid categoryId)
    {
        try
        {
            var success = await _sender.Send(new DeleteCategoryPropertyCommand(id));
            TempData[success ? "Success" : "Error"] = success ? "Özellik silindi." : "Özellik bulunamadı.";
        }
        catch (ConflictException ex) // ilan değeri olan özellik silinemez
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Properties), new { id = categoryId });
    }

    // ---- Seçenekler (option) ----

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OptionCreate(Guid propertyId, Guid categoryId, string optionValue, int displayOrder)
    {
        try
        {
            await _sender.Send(new CreatePropertyOptionCommand(propertyId, optionValue, displayOrder));
            TempData["Success"] = $"\"{optionValue?.Trim()}\" seçeneği eklendi.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Properties), new { id = categoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OptionDelete(Guid id, Guid categoryId)
    {
        var success = await _sender.Send(new DeletePropertyOptionCommand(id));
        TempData[success ? "Success" : "Error"] = success ? "Seçenek silindi." : "Seçenek bulunamadı.";
        return RedirectToAction(nameof(Properties), new { id = categoryId });
    }

    /// <summary>Yeni kategori formundaki üst kategori dropdown'u — tüm kategoriler (pasifler işaretli).</summary>
    private async Task LoadParentsAsync()
        => ViewBag.Parents = await _sender.Send(new GetAdCategoriesAdminQuery());
}
