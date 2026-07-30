using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
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

        var user = await _uow.Repository<User>().Query()
            .FirstOrDefaultAsync(u =>
                (u.Username == username || u.Phone == username) && u.Password != null);

        if (user == null || !_passwordHasher.VerifyPassword(password, user.Password!))
        {
            ViewBag.Error = "Kullanıcı adı veya şifre hatalı!";
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
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return Redirect(returnUrl ?? "/Dashboard/Index");
    }

    // Faz 10.9(f): admin kendi şifresini panelden değiştirebilir (öncesinde yalnız seed şifresiyle yaşıyordu)
    [HttpGet]
    [Authorize(Roles = "admin,super_admin,moderator")]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "admin,super_admin,moderator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string newPasswordConfirm)
    {
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
        catch (AppException ex) // hatalı mevcut şifre / validasyon
        {
            ViewBag.Error = ex.Message;
            return View();
        }

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
