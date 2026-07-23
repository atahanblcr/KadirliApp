using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.EntityFrameworkCore;
using KadirliApp.Application.Features.Guide.Queries;
using KadirliApp.Application.Features.Guide.Dtos;
using KadirliApp.Application.Features.Guide.Commands;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace KadirliApp.Web.Controllers;

/// <summary>
/// Şehir Rehberi: hastane, belediye, kaymakamlık, itfaiye gibi önemli yerlerin
/// telefon/adres bilgilerini tutan kent rehberi. Ana ekran REHBER KAYITLARINI yönetir;
/// kategoriler (Sağlık, Resmi Kurumlar vb.) ayrı "Kategoriler" ekranından yönetilir.
/// </summary>
[Authorize(Roles = "admin,super_admin")]
public class GuideAdminController : Controller
{
    private readonly ISender _sender;
    private readonly IUnitOfWork _unitOfWork;

    public GuideAdminController(ISender sender, IUnitOfWork unitOfWork)
    {
        _sender = sender;
        _unitOfWork = unitOfWork;
    }

    private async Task LoadCategoriesAsync()
    {
        ViewBag.Categories = await _unitOfWork.Repository<GuideCategory>().Query()
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .ToListAsync();
    }

    // ============ REHBER KAYITLARI ============

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] QueryGuideItemDto query)
    {
        query ??= new QueryGuideItemDto();
        var result = await _sender.Send(new GetGuideItemsQuery(query));

        ViewBag.Search = query.Search;
        ViewBag.SelectedCategoryId = query.CategoryId;
        await LoadCategoriesAsync();
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCategoriesAsync();
        return View(new CreateGuideItemCommand());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateGuideItemCommand command)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(command);
        }

        await _sender.Send(command);
        TempData["Success"] = "Rehber kaydı başarıyla eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var item = await _sender.Send(new GetGuideItemByIdQuery(id));
        if (item == null)
        {
            TempData["Error"] = "Rehber kaydı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var command = new UpdateGuideItemCommand
        {
            Id = item.Id,
            CategoryId = item.CategoryId,
            Name = item.Name,
            Phone = item.Phone,
            Address = item.Address,
            Email = item.Email,
            WebsiteUrl = item.WebsiteUrl,
            WorkingHours = item.WorkingHours,
            Latitude = item.Latitude,
            Longitude = item.Longitude,
            Description = item.Description,
            IsActive = item.IsActive
        };

        await LoadCategoriesAsync();
        return View(command);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateGuideItemCommand command)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(command);
        }

        var success = await _sender.Send(command);
        if (success)
        {
            TempData["Success"] = "Rehber kaydı başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = "Rehber kaydı güncellenirken bir hata oluştu.";
        await LoadCategoriesAsync();
        return View(command);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _sender.Send(new DeleteGuideItemCommand(id));
        if (success)
            TempData["Success"] = "Rehber kaydı silindi.";
        else
            TempData["Error"] = "Rehber kaydı bulunamadı veya silinemedi.";

        return RedirectToAction(nameof(Index));
    }

    // ============ KATEGORİLER ============

    [HttpGet]
    public async Task<IActionResult> Categories([FromQuery] string? search, [FromQuery] int page = 1)
    {
        var queryDto = new QueryGuideCategoryDto { Search = search, Page = page, Limit = 50 };
        var result = await _sender.Send(new GetGuideCategoriesQuery(queryDto));

        // Kategori başına kayıt sayısı — hangi kategorinin dolu olduğu görülsün
        var counts = await _unitOfWork.Repository<GuideItem>().Query()
            .GroupBy(x => x.CategoryId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
        ViewBag.ItemCounts = counts;

        ViewBag.Search = search;
        return View(result);
    }

    [HttpGet]
    public IActionResult CategoryCreate()
    {
        return View(new CreateGuideCategoryCommand());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoryCreate(CreateGuideCategoryCommand command)
    {
        if (!ModelState.IsValid) return View(command);

        if (string.IsNullOrWhiteSpace(command.Slug))
            command.Slug = Slugify(command.Name);

        var result = await _sender.Send(command);
        if (result != Guid.Empty)
        {
            TempData["Success"] = "Rehber kategorisi başarıyla eklendi.";
            return RedirectToAction(nameof(Categories));
        }
        TempData["Error"] = "Bir hata oluştu.";
        return View(command);
    }

    [HttpGet]
    public async Task<IActionResult> CategoryEdit(Guid id)
    {
        var category = await _sender.Send(new GetGuideCategoryByIdQuery(id));
        if (category == null) return NotFound();

        var command = new UpdateGuideCategoryCommand
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            ParentId = category.ParentId,
            Icon = category.Icon,
            Color = category.Color,
            DisplayOrder = category.DisplayOrder
        };
        return View(command);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoryEdit(UpdateGuideCategoryCommand command)
    {
        if (!ModelState.IsValid) return View(command);

        if (string.IsNullOrWhiteSpace(command.Slug))
            command.Slug = Slugify(command.Name);

        var result = await _sender.Send(command);
        if (result)
        {
            TempData["Success"] = "Rehber kategorisi başarıyla güncellendi.";
            return RedirectToAction(nameof(Categories));
        }
        TempData["Error"] = "Bir hata oluştu.";
        return View(command);
    }

    // Faz 9.4: inline silme yerine Application command'i — Admin API ile aynı kurallar
    // (item VEYA alt kategori varsa Conflict) ve cache invalidation (ICacheInvalidator) kapsanır.
    [HttpPost]
    public async Task<IActionResult> CategoryDelete(Guid id)
    {
        try
        {
            var deleted = await _sender.Send(new DeleteGuideCategoryCommand(id));
            if (deleted)
                TempData["Success"] = "Rehber kategorisi başarıyla silindi.";
            else
                TempData["Error"] = "Kategori bulunamadı.";
        }
        catch (KadirliApp.Application.Common.Exceptions.ConflictException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Categories));
    }

    private static string Slugify(string value)
    {
        var map = new (char From, char To)[] { ('ç', 'c'), ('ğ', 'g'), ('ı', 'i'), ('ö', 'o'), ('ş', 's'), ('ü', 'u') };
        var lower = (value ?? string.Empty).ToLowerInvariant();
        var sb = new System.Text.StringBuilder(lower.Length);
        foreach (var ch in lower)
        {
            var c = ch;
            foreach (var (from, to) in map)
                if (c == from) { c = to; break; }

            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c == ' ' || c == '-' || c == '_') sb.Append('-');
        }
        return sb.ToString().Trim('-');
    }
}
