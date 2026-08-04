using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Security;
using KadirliApp.Application.Features.Users.Commands.ChangeMyPassword;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KadirliApp.Web.Controllers;

public class AccountController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISender _sender;

    public AccountController(IUnitOfWork uow, IPasswordHasher passwordHasher, ISender sender)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
        _sender = sender;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // Eğer kullanıcı zaten giriş yapmışsa direkt yönlendir.
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return Redirect(returnUrl ?? "/Dashboard/Index");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [EnableRateLimiting("panel-login")] // Faz 9.2: IP başına Brute-Force koruması
    public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Kullanıcı adı ve şifre zorunludur.";
            return View();
        }

        // Faz 11.18: kilit sayacını yazabilmek için izlenen (tracking) varlık gerekiyor.
        var user = await _uow.Repository<User>().Query(tracking: true)
            .FirstOrDefaultAsync(u =>
                (u.Username == username || u.Phone == username) && u.Password != null);

        if (user == null)
        {
            // ⚠️ Var olmayan kullanıcı ile hatalı parola AYNI mesajı alır — aksi hâlde
            // giriş ekranı bir "kullanıcı adı var mı?" sorgulama aracına dönüşür.
            ViewBag.Error = "Kullanıcı adı veya şifre hatalı!";
            return View();
        }

        var now = DateTime.UtcNow;

        // 🔴 Faz 11.18: hesap kilidi parola denetiminden ÖNCE gelir — kilitliyken
        // doğru parola da kabul edilmez. Sonra gelseydi kilit yalnızca yanlış tahminleri
        // yavaşlatır, doğru tahmini hiç engellemezdi.
        if (PanelLockoutPolicy.IsLockedOut(user, now))
        {
            ViewBag.Error = $"Hesabınız çok fazla hatalı denemeden dolayı geçici olarak kilitlendi. " +
                            $"Lütfen {PanelLockoutPolicy.RemainingMinutes(user, now)} dakika sonra tekrar deneyin.";
            return View();
        }

        if (!_passwordHasher.VerifyPassword(password, user.Password!))
        {
            PanelLockoutPolicy.RegisterFailure(user, now);
            await _uow.SaveChangesAsync();

            ViewBag.Error = PanelLockoutPolicy.IsLockedOut(user, now)
                ? $"Kullanıcı adı veya şifre hatalı! Çok fazla hatalı deneme yaptığınız için hesabınız " +
                  $"{PanelLockoutPolicy.RemainingMinutes(user, now)} dakika kilitlendi."
                : "Kullanıcı adı veya şifre hatalı!";
            return View();
        }

        if (user.IsBanned || !user.IsActive)
        {
            ViewBag.Error = "Hesabınız pasif veya engellenmiş durumda.";
            return View();
        }

        if (user.Role is not (UserRole.Admin or UserRole.SuperAdmin or UserRole.Moderator))
        {
            ViewBag.Error = "Bu panele erişim yetkiniz bulunmuyor.";
            return View();
        }

        // Doğru parola → sayaç ve kilit temizlenir (kısmi denemeler birikip sonradan patlamasın).
        PanelLockoutPolicy.RegisterSuccess(user);
        await _uow.SaveChangesAsync();

        await SignInPanelUserAsync(user);

        // Faz 11.18: parolası yönetici tarafından belirlenmişse doğrudan parola ekranına.
        // (Filtre zaten her sayfada aynı yönlendirmeyi yapar; buradaki, kullanıcıyı
        // "neden buradayım?" sorusuyla baş başa bırakmamak için doğrudan ve açık yol.)
        if (user.MustChangePassword)
            return RedirectToAction(nameof(ChangePassword));

        return Redirect(returnUrl ?? "/Dashboard/Index");
    }

    /// <summary>
    /// Panel çerezini kurar. Faz 11.18'de ayrı bir metoda alındı: parola değişiminden
    /// sonra çerezin **yeniden** düzenlenmesi gerekiyor (yoksa <c>OnValidatePrincipal</c>
    /// kullanıcının kendi oturumunu, kendi yaptığı parola değişimi yüzünden düşürür).
    /// </summary>
    private async Task SignInPanelUserAsync(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username ?? user.Phone),
            new Claim(ClaimTypes.Role, user.Role.ToRoleString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true, // Beni hatırla
            IssuedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);
    }

    // Faz 10.9(f): admin kendi şifresini panelden değiştirebilir (öncesinde yalnız seed şifresiyle yaşıyordu)
    [HttpGet]
    [Authorize(Roles = "admin,super_admin,moderator")]
    public IActionResult ChangePassword()
    {
        // Faz 11.18: ekran iki bağlamda açılır — kullanıcı kendi isteğiyle geldiğinde ve
        // filtre onu buraya ZORLADIĞINDA. İkincisinde görünüm bunu açıkça söyler, yoksa
        // kullanıcı menüsüz bir sayfada ne olduğunu anlamadan kalır.
        ViewBag.Forced = HttpContext.Items[Common.PanelPrincipalValidator.MustChangePasswordItemKey] is true;
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "admin,super_admin,moderator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string newPasswordConfirm)
    {
        var forced = HttpContext.Items[Common.PanelPrincipalValidator.MustChangePasswordItemKey] is true;
        ViewBag.Forced = forced;

        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            ViewBag.Error = "Tüm alanlar zorunludur.";
            return View();
        }

        if (newPassword != newPasswordConfirm)
        {
            ViewBag.Error = "Yeni şifreler birbiriyle uyuşmuyor.";
            return View();
        }

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return RedirectToAction(nameof(Login));
        }

        try
        {
            await _sender.Send(new ChangeMyPasswordCommand(userId, currentPassword, newPassword));
        }
        catch (AppException ex) // hatalı mevcut şifre / parola politikası
        {
            ViewBag.Error = ex.Message;
            return View();
        }

        // 🔑 Faz 11.18: parola değişimi TÜM açık oturumları düşürür (PasswordChangedAt >
        // çerezin düzenlenme anı). Kullanıcının KENDİ oturumu da bu kurala takılırdı;
        // çerez burada yeniden düzenlenerek o istisna açıkça veriliyor — parolasını
        // değiştiren kişinin dışarı atılması için hiçbir sebep yok, başkalarınınki için var.
        var refreshed = await _uow.Repository<User>().Query()
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (refreshed is not null)
            await SignInPanelUserAsync(refreshed);

        TempData["Success"] = "Şifreniz başarıyla değiştirildi.";
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    /// <summary>
    /// 10.9 denetimi: cookie config'indeki AccessDeniedPath ("/account/denied") bu action olmadan
    /// 404'e düşüyordu — panele girip yetkisi olmayan sayfayı açan (örn. moderatör) boş sayfa görüyordu.
    /// </summary>
    [HttpGet]
    [Authorize]
    public IActionResult Denied() => View();
}
