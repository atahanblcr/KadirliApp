using System;
using System.Linq;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Places.Commands;
using KadirliApp.Application.Features.Places.Dtos;
using KadirliApp.Application.Features.Places.Queries;
using KadirliApp.Domain.Entities;
using KadirliApp.Web.Common;
using MediatR;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Web.Controllers;

[Authorize(Roles = "admin,super_admin")]
public class PlacesAdminController : Controller
{
    private readonly ISender _sender;
    private readonly IUnitOfWork _uow;

    public PlacesAdminController(ISender sender, IUnitOfWork uow)
    {
        _sender = sender;
        _uow = uow;
    }

    private async Task LoadCategoriesAsync()
    {
        ViewBag.Categories = await _uow.Repository<PlaceCategory>().Query()
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] QueryPlaceDto query)
    {
        query ??= new QueryPlaceDto();
        if (query.Limit == 10) query.Limit = 20;

        var result = await _sender.Send(new GetPlacesQuery(query));
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCategoriesAsync();
        return View(new CreatePlaceCommand());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePlaceCommand command, IFormFile? CoverImage)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(command);
        }

        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);
        command.CoverImageId = await UploadHelper.UploadAsync(_sender, CoverImage, "place", adminId);

        await _sender.Send(command);
        TempData["Success"] = "Mekan başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var place = await _sender.Send(new GetPlaceByIdQuery(id));
        if (place == null)
        {
            TempData["Error"] = "Mekan bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var command = new UpdatePlaceCommand
        {
            Id = place.Id,
            CategoryId = place.CategoryId,
            Name = place.Name,
            Description = place.Description,
            Address = place.Address,
            Latitude = place.Latitude,
            Longitude = place.Longitude,
            EntranceFee = place.EntranceFee,
            IsFree = place.IsFree,
            OpeningHours = place.OpeningHours,
            BestSeason = place.BestSeason,
            HowToGetThere = place.HowToGetThere,
            DistanceFromCenter = place.DistanceFromCenter,
            Amenities = place.Amenities,
            CoverImageId = place.CoverImageId,
            IsActive = place.IsActive
        };

        ViewBag.CoverImageUrl = place.CoverImageUrl;
        await LoadCategoriesAsync();
        return View(command);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdatePlaceCommand command, IFormFile? CoverImage)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(command);
        }

        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);
        var newImageId = await UploadHelper.UploadAsync(_sender, CoverImage, "place", adminId);
        if (newImageId.HasValue) command.CoverImageId = newImageId;

        var success = await _sender.Send(command);
        if (success)
        {
            TempData["Success"] = "Mekan başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = "Mekan güncellenirken bir hata oluştu.";
        await LoadCategoriesAsync();
        return View(command);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _sender.Send(new DeletePlaceCommand(id));
        if (success)
            TempData["Success"] = "Mekan başarıyla silindi.";
        else
            TempData["Error"] = "Mekan bulunamadı veya silinemedi.";

        return RedirectToAction(nameof(Index));
    }
}
