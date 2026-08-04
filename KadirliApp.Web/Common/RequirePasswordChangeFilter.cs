using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KadirliApp.Web.Common;

/// <summary>
/// Faz 11.18 — parolası **başkası tarafından** belirlenmiş kullanıcıyı, kendi parolasını
/// seçene kadar panelin geri kalanından uzak tutar (11.15c C grubu: "ilk girişte parola
/// değişimini ZORLA").
///
/// 🔑 **Neden bir uyarı şeridi değil de kapı?** Uyarı, yöneticinin görmezden gelebileceği
/// bir şeydir; varsayılan parolayla çalışan panel de tam olarak böyle doğar. Kapı, işi
/// yapılmadan hiçbir ekranın açılmaması demektir — <c>DbSeeder</c>'ın yazdığı
/// <c>admin / Admin123!</c> artık yalnızca bu ekranı açan bir anahtar.
///
/// ⚠️ <c>AccountController</c> tümüyle muaf (parola değiştirme, çıkış, giriş, "yetkiniz yok")
/// ve <c>HomeController</c> de öyle (hata/durum sayfaları) — aksi hâlde yönlendirme
/// kendi hedefine de uygulanır ve tarayıcı **sonsuz döngüye** girer.
///
/// Bayrak DB'den <see cref="PanelPrincipalValidator"/> tarafından tazelenip
/// <c>HttpContext.Items</c>'a konur; bu filtre ek sorgu atmaz.
///
/// 🔑 <b>Neden yetkilendirme filtresi (ve neden <c>Order = int.MinValue</c>)?</b>
/// İlk hâli bir <c>IActionFilter</c>'dı ve testte yakalandı: aksiyon filtreleri
/// <see cref="Authorization.PanelPermissionFilter"/>'dan <b>sonra</b> koştuğu için,
/// izni olmayan bir moderatör parola ekranına değil <c>/account/denied</c>'e düşüyordu —
/// yani kullanıcıya "yetkiniz yok" deniyor, oysa asıl sorun parola borcuydu ve
/// gidebileceği tek yer gösterilmiyordu. Borç, yetkiden **önce** sorulmalı: borcu olan
/// kullanıcının hiçbir yetkisi zaten değerlendirilmemeli.
/// </summary>
public sealed class RequirePasswordChangeFilter : IAuthorizationFilter, IOrderedFilter
{
    private static readonly string[] ExemptControllers = { "Account", "Home" };

    /// <summary>Panelin izin filtresinden de önce koşsun.</summary>
    public int Order => int.MinValue;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            return;

        if (context.HttpContext.Items[PanelPrincipalValidator.MustChangePasswordItemKey] is not true)
            return;

        var controller = context.RouteData.Values["controller"]?.ToString();
        if (controller is not null &&
            ExemptControllers.Contains(controller, StringComparer.OrdinalIgnoreCase))
            return;

        context.Result = new RedirectToActionResult("ChangePassword", "Account", null);
    }
}
