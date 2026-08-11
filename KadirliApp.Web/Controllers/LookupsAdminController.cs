using System;
using KadirliApp.Web.Authorization;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Features.Lookups;
using KadirliApp.Web.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Web.Controllers;

/// <summary>
/// Faz 10.9(d-panel): lookup tablolarının (mahalle/mezarlık/cami/etkinlik+mekan kategorisi) panel UI'ı —
/// Application/API katmanı 18 Tem'de yan ürün olarak yazılmıştı, panelden yönetim burada açılıyor.
/// KARAR: silme yok (FK'lı sözlük verisi — mahalle IsActive ile pasifleşir); tek Index sayfası,
/// 5 akordiyon bölüm. Tüm mutasyonlar Application command'leri üzerinden (Faz 9.4 kuralı) ve
/// `lookups` cache grubunu invalidate eder → mobil lookup uçları taze döner.
/// </summary>
[Authorize(Roles = "admin,super_admin,moderator")]
[PanelPermission("lookups")]
public class LookupsAdminController : Controller
{
    private readonly ISender _sender;

    public LookupsAdminController(ISender sender) => _sender = sender;

    /// <param name="open">POST sonrası hangi akordiyon bölümünün açık kalacağı (neighborhoods/cemeteries/mosques/event-categories/place-categories).</param>
    [HttpGet]
    public async Task<IActionResult> Index(string? open)
    {
        var model = new LookupsIndexViewModel
        {
            Neighborhoods = await _sender.Send(new GetNeighborhoodsAdminQuery()),
            Districts = await _sender.Send(new GetDistrictsAdminQuery()),
            DeparturePoints = await _sender.Send(new GetDeparturePointsAdminQuery()),
            Cemeteries = await _sender.Send(new GetCemeteriesQuery()),
            Mosques = await _sender.Send(new GetMosquesQuery()),
            EventCategories = await _sender.Send(new GetEventCategoriesQuery()),
            PlaceCategories = await _sender.Send(new GetPlaceCategoriesAdminQuery()),
            NewsCategories = await _sender.Send(new Application.Features.News.Queries.GetNewsCategoriesAdminQuery()),
            OpenSection = open
        };
        return View(model);
    }

    // ---- Haber kategorileri (Faz 12.13) ----

    /// <summary>
    /// Kategorinin <b>görünürlüğünü</b> yazar (dışlama · şeritte göster · sıra).
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Görünürlük semantiği DIŞLAMA'dır</b>, "en az bir görünür kategorisi olsun"
    /// değil — ve bu tercihi ölçüm zorladı: bir haber çoklu kategoride
    /// (<c>[49,51,52]</c>). OR semantiğinde "E-Gazete"yi kapatmak <b>işe yaramazdı</b>:
    /// o haberler "Haberler"e de ait olduğu için görünmeye devam eder, yönetici anahtarı
    /// çevirir ve <b>hiçbir şey olmaz</b> — panelin en sinsi yalan biçimi (§7 madde 37).
    ///
    /// 📌 Dışlama <b>geriye dönük ve anında</b> etkilidir: süzgeç kayıtlara yazılan bir
    /// bayrakta değil sorguda yaşıyor (<c>NewsVisibility</c>), yani 366 eski haber de aynı
    /// anda düşer. Yönetici kayıt kayıt gizlemek zorunda kalmaz.
    ///
    /// ⚠️ Aksiyon adı <c>…Update</c> → izin eylemi <c>update</c> (§7 madde 19), modül
    /// <c>lookups</c>. Denetim izine ise <c>news</c> modülüyle düşer: değişikliğin etkisi
    /// haberlerde görünüyor, sözlükte değil.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewsCategoryUpdate(
        Guid id, bool isExcluded, bool showInFilterStrip, int displayOrder)
    {
        var result = await _sender.Send(new Application.Features.News.Commands.UpdateNewsCategoryVisibilityCommand
        {
            Id = id,
            IsExcluded = isExcluded,
            ShowInFilterStrip = showInFilterStrip,
            DisplayOrder = displayOrder
        });

        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? (isExcluded
                ? "Kategori dışlandı — bu kategorideki haberler uygulamada görünmeyecek."
                : "Kategori güncellendi.")
            : result.Error?.Message ?? "Kategori güncellenemedi.";

        return RedirectToAction(nameof(Index), new { open = "news-categories" });
    }

    // ---- Mahalleler ----

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NeighborhoodCreate(string name, string? type, int displayOrder,
        decimal? latitude, decimal? longitude)
    {
        try
        {
            await _sender.Send(new CreateNeighborhoodCommand(name, type, displayOrder, latitude, longitude));
            TempData["Success"] = $"\"{name?.Trim()}\" mahallesi eklendi.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { open = "neighborhoods" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NeighborhoodUpdate(Guid id, string name, string? type, int displayOrder,
        bool isActive, decimal? latitude, decimal? longitude)
    {
        try
        {
            var success = await _sender.Send(new UpdateNeighborhoodCommand(id, name, type, displayOrder, isActive, latitude, longitude));
            TempData[success ? "Success" : "Error"] = success ? "Mahalle güncellendi." : "Mahalle bulunamadı.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { open = "neighborhoods" });
    }

    // ---- İl / ilçe (Faz 12.4) ----

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DistrictCreate(string provinceName, string name, bool isCenter, int displayOrder)
    {
        try
        {
            await _sender.Send(new CreateDistrictCommand(provinceName, name, isCenter, displayOrder));
            TempData["Success"] = $"\"{provinceName?.Trim()} / {name?.Trim()}\" ilçesi eklendi.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { open = "districts" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DistrictUpdate(Guid id, string provinceName, string name, bool isCenter, int displayOrder, bool isActive)
    {
        try
        {
            var success = await _sender.Send(new UpdateDistrictCommand(id, provinceName, name, isCenter, displayOrder, isActive));
            TempData[success ? "Success" : "Error"] = success ? "İlçe güncellendi." : "İlçe bulunamadı.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { open = "districts" });
    }

    // ---- Kalkış noktaları (Faz 12.5) ----

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeparturePointCreate(string name, string? address, decimal? latitude, decimal? longitude, int displayOrder)
    {
        try
        {
            await _sender.Send(new CreateDeparturePointCommand(name, address, latitude, longitude, displayOrder));
            TempData["Success"] = $"\"{name?.Trim()}\" kalkış noktası eklendi.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { open = "departure-points" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeparturePointUpdate(Guid id, string name, string? address, decimal? latitude, decimal? longitude, int displayOrder, bool isActive)
    {
        try
        {
            var success = await _sender.Send(new UpdateDeparturePointCommand(id, name, address, latitude, longitude, displayOrder, isActive));
            TempData[success ? "Success" : "Error"] = success ? "Kalkış noktası güncellendi." : "Kalkış noktası bulunamadı.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { open = "departure-points" });
    }

    // ---- Mezarlıklar ----

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CemeteryCreate(string name, string? address, decimal? latitude, decimal? longitude)
    {
        try
        {
            await _sender.Send(new CreateCemeteryCommand(name, address, latitude, longitude));
            TempData["Success"] = $"\"{name?.Trim()}\" mezarlığı eklendi.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { open = "cemeteries" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CemeteryUpdate(Guid id, string name, string? address, decimal? latitude, decimal? longitude)
    {
        try
        {
            var success = await _sender.Send(new UpdateCemeteryCommand(id, name, address, latitude, longitude));
            TempData[success ? "Success" : "Error"] = success ? "Mezarlık güncellendi." : "Mezarlık bulunamadı.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { open = "cemeteries" });
    }

    // ---- Camiler ----

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MosqueCreate(string name, string? address, decimal? latitude, decimal? longitude)
    {
        try
        {
            await _sender.Send(new CreateMosqueCommand(name, address, latitude, longitude));
            TempData["Success"] = $"\"{name?.Trim()}\" camisi eklendi.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { open = "mosques" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MosqueUpdate(Guid id, string name, string? address, decimal? latitude, decimal? longitude)
    {
        try
        {
            var success = await _sender.Send(new UpdateMosqueCommand(id, name, address, latitude, longitude));
            TempData[success ? "Success" : "Error"] = success ? "Cami güncellendi." : "Cami bulunamadı.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { open = "mosques" });
    }

    // ---- Etkinlik kategorileri ----

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EventCategoryCreate(string name)
    {
        try
        {
            await _sender.Send(new CreateEventCategoryCommand(name));
            TempData["Success"] = $"\"{name?.Trim()}\" etkinlik kategorisi eklendi.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { open = "event-categories" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EventCategoryUpdate(Guid id, string name)
    {
        try
        {
            var success = await _sender.Send(new UpdateEventCategoryCommand(id, name));
            TempData[success ? "Success" : "Error"] = success ? "Etkinlik kategorisi güncellendi." : "Etkinlik kategorisi bulunamadı.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { open = "event-categories" });
    }

    // ---- Mekan kategorileri ----

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceCategoryCreate(string name, string? icon, int displayOrder)
    {
        try
        {
            await _sender.Send(new CreatePlaceCategoryCommand(name, icon, displayOrder));
            TempData["Success"] = $"\"{name?.Trim()}\" mekan kategorisi eklendi.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { open = "place-categories" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceCategoryUpdate(Guid id, string name, string? icon, int displayOrder)
    {
        try
        {
            var success = await _sender.Send(new UpdatePlaceCategoryCommand(id, name, icon, displayOrder));
            TempData[success ? "Success" : "Error"] = success ? "Mekan kategorisi güncellendi." : "Mekan kategorisi bulunamadı.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { open = "place-categories" });
    }
}
