using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using KadirliApp.Web.Models;

namespace KadirliApp.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    /// <summary>
    /// Faz 11.15c: gövdesiz hata sayfalarının markalı karşılığı.
    ///
    /// Önceki hâlde <c>UseStatusCodePages*</c> hiç yoktu: panelde var olmayan bir adres
    /// (<c>/BuBirSayfaDegil</c>) <b>404 + 0 bayt</b>, yani bembeyaz bir sayfa döndürüyordu.
    /// Yönetici ne olduğunu anlamıyor ve panele dönecek bir bağlantı da bulamıyordu.
    ///
    /// <c>UseExceptionHandler("/Home/Error")</c> yalnız 500'ü ve yalnız Development DIŞINDA
    /// karşılıyor — bu aksiyon durum kodlarını (404/403/…) her ortamda karşılar.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusCode(int code)
    {
        Response.StatusCode = code; // özgün kod korunur (arama motoru/izleme 200 görmemeli)
        return View("StatusCode", new StatusCodeViewModel(code, OriginalPath()));
    }

    private string? OriginalPath() =>
        HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodeReExecuteFeature>()?.OriginalPath;
}
