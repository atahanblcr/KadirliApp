using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KadirliApp.Web.Models;

namespace KadirliApp.Web.Controllers;

/// <summary>
/// Faz 12.20a — panelin <b>hata sayfaları</b>. Başka hiçbir şey.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Bu sınıf 16 Ağustos 2026 denetiminin B1 bulgusuydu.</b> <c>dotnet new mvc</c>
/// iskelesinden gelen <c>Index</c> ve <c>Privacy</c> aksiyonları burada duruyordu:
/// kimlik doğrulaması istemiyorlardı (panelde <c>[Authorize]</c> taşımayan **tek**
/// controller buydu), İngilizce metin basıyorlardı (Değişmez Kural #6'yı ihlal eden tek
/// yer) ve <c>/Home/Privacy</c> — yani <b>tahmin edilebilir bir gizlilik metni adresi</b> —
/// *"Use this page to detail your site's privacy policy"* yazıyordu. Proje az önce
/// 12.16–12.17'de bütün bir KVKK bloğunu kapatmıştı; o yer tutucu, bloğun tam olarak
/// savaştığı şeydi. İkisi de silindi (hiçbir yerden referans almıyorlardı — ölçüldü).
/// </para>
/// <para>
/// ⚠️ <b>Kalan iki aksiyon <c>[AllowAnonymous]</c> olmak ZORUNDA.</b>
/// <c>UseExceptionHandler("/Home/Error")</c> ve
/// <c>UseStatusCodePagesWithReExecute("/Home/StatusCode")</c> boru hattını **yeniden
/// çalıştırır**: kapı kapalı olsaydı 500 alan yönetici hata sayfası yerine giriş
/// ekranına atılırdı ve gerçek hata hiçbir yerde görünmezdi. Aynı sebeple oturumsuz
/// bir ziyaretçinin gördüğü 404 de markalı kalmalı.
/// </para>
/// <para>
/// 🔑 Muafiyet artık <b>aksiyon</b> granülaritesinde:
/// <c>PanelAuthenticationTests.AnonymousActions</c>. Bu sınıfa yarın eklenecek üçüncü bir
/// aksiyon <c>[AllowAnonymous]</c> taşırsa test <b>kırmızıya döner</b> — 12.20a'ya kadar
/// muafiyet controller adı üzerindendi ve o aksiyon sessizce anonim doğardı.
/// </para>
/// <para>
/// 🐛 <b><c>[Authorize]</c> bilinçli olarak ROL LİSTESİZ</b> ve bunu bir test söyletti.
/// İlk yazımda panelin alışılmış deseni (<c>Roles = "admin,super_admin,moderator"</c>)
/// kopyalanmıştı; <c>PanelModeratorPermissionTests</c> anında kırmızıya döndü ve <b>haklıydı</b>:
/// o rol listesi <i>"bu bir modül ekranıdır ve moderatöre açıktır"</i> demektir, o zaman da
/// <c>[PanelPermission]</c> + menü satırı + izin matrisinde bir anahtar gerekir. Burası bir
/// modül değil, panelin <b>hata yüzeyi</b>. Rolsüz <c>[Authorize]</c> yalnız
/// <i>"geçerli bir panel oturumu"</i> der — panele zaten yalnız o üç rol girebildiği için
/// (<c>AccountController</c> girişte diğerlerini reddeder) kapsam aynıdır, ama <b>iddia
/// dürüsttür</b>: izin matrisinde karşılığı olmayan bir yetki belirmez (11.15b'nin en büyük
/// bulgusu tam olarak buydu).
/// </para>
/// </remarks>
[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [AllowAnonymous]
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
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusCode(int code)
    {
        Response.StatusCode = code; // özgün kod korunur (arama motoru/izleme 200 görmemeli)
        return View("StatusCode", new StatusCodeViewModel(code, OriginalPath()));
    }

    private string? OriginalPath() =>
        HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodeReExecuteFeature>()?.OriginalPath;
}
