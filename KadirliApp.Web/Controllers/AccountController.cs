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
    private readonly ILoginAttemptRecorder _loginAttempts;

    public AccountController(
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        ISender sender,
        ILoginAttemptRecorder loginAttempts)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
        _sender = sender;
        _loginAttempts = loginAttempts;
    }

    /// <summary>
    /// Faz 12.2 — giriş denemesini kaydeder. ⚠️ Kaydedici asla fırlatmaz; giriş akışı
    /// kendi gözlemcisi yüzünden bozulamaz (bkz. <c>ILoginAttemptRecorder</c>).
    /// </summary>
    private Task RecordAttemptAsync(
        string identifier, Guid? userId, bool succeeded,
        string? failureReason = null, bool isPanelUser = false, DateTime? lockedOutUntil = null) =>
        _loginAttempts.RecordAsync(new LoginAttemptRecord(
            Channel: LoginChannels.Panel,
            RawIdentifier: identifier,
            UserId: userId,
            Succeeded: succeeded,
            FailureReason: failureReason,
            IsPanelUser: isPanelUser,
            LockedOutUntil: lockedOutUntil));

    // 🔑 Faz 12.20a: panelin varsayılanı artık "reddet" (Program.cs → FallbackPolicy).
    // Giriş akışının anonim olması akışın TANIMI gereği zorunlu — oturum açmak için oturum
    // istenemez — ve bu artık açıkça yazılıyor, bir varsayılanın yan etkisi değil.
    [AllowAnonymous]
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

    [AllowAnonymous]
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
            // 🔑 Ama KAYIT ikisini ayırır (unknown_user ≠ bad_password): "var olmayan
            // hesaba 200 deneme" ile "tek hesaba 200 deneme" çok farklı saldırılar ve
            // panelde aynı görünmemeliler.
            await RecordAttemptAsync(username, null, succeeded: false, LoginFailureReasons.UnknownUser);
            ViewBag.Error = "Kullanıcı adı veya şifre hatalı!";
            return View();
        }

        var now = DateTime.UtcNow;
        // 🔑 Kilit bitişi BAŞARILI girişten ÖNCE alınır: RegisterSuccess onu temizliyor ve
        // R4 ("kilit biter bitmez gelen başarılı giriş") tam o değere bakıyor. Sonra
        // okunsaydı R4 hiçbir zaman yanmazdı — sessizce hiç çalışmayan bir kural.
        var lockedOutUntilBefore = user.LockedOutUntil;

        // 🔴 Faz 11.18: hesap kilidi parola denetiminden ÖNCE gelir — kilitliyken
        // doğru parola da kabul edilmez. Sonra gelseydi kilit yalnızca yanlış tahminleri
        // yavaşlatır, doğru tahmini hiç engellemezdi.
        if (PanelLockoutPolicy.IsLockedOut(user, now))
        {
            await RecordAttemptAsync(username, user.Id, succeeded: false, LoginFailureReasons.LockedOut);
            ViewBag.Error = $"Hesabınız çok fazla hatalı denemeden dolayı geçici olarak kilitlendi. " +
                            $"Lütfen {PanelLockoutPolicy.RemainingMinutes(user, now)} dakika sonra tekrar deneyin.";
            return View();
        }

        if (!_passwordHasher.VerifyPassword(password, user.Password!))
        {
            PanelLockoutPolicy.RegisterFailure(user, now);
            await _uow.SaveChangesAsync();
            await RecordAttemptAsync(username, user.Id, succeeded: false, LoginFailureReasons.BadPassword);

            ViewBag.Error = PanelLockoutPolicy.IsLockedOut(user, now)
                ? $"Kullanıcı adı veya şifre hatalı! Çok fazla hatalı deneme yaptığınız için hesabınız " +
                  $"{PanelLockoutPolicy.RemainingMinutes(user, now)} dakika kilitlendi."
                : "Kullanıcı adı veya şifre hatalı!";
            return View();
        }

        if (user.IsBanned || !user.IsActive)
        {
            await RecordAttemptAsync(username, user.Id, succeeded: false,
                user.IsBanned ? LoginFailureReasons.Banned : LoginFailureReasons.Inactive);
            ViewBag.Error = "Hesabınız pasif veya engellenmiş durumda.";
            return View();
        }

        if (user.Role is not (UserRole.Admin or UserRole.SuperAdmin or UserRole.Moderator))
        {
            // ⚠️ Parola DOĞRUYDU ama rol yetmedi. Bu "başarılı giriş" değil: vatandaş
            // hesabıyla panele girmeye çalışmak başlı başına bir sinyal.
            await RecordAttemptAsync(username, user.Id, succeeded: false, LoginFailureReasons.RoleDenied);
            ViewBag.Error = "Bu panele erişim yetkiniz bulunmuyor.";
            return View();
        }

        // Doğru parola → sayaç ve kilit temizlenir (kısmi denemeler birikip sonradan patlamasın).
        PanelLockoutPolicy.RegisterSuccess(user);
        await _uow.SaveChangesAsync();

        await RecordAttemptAsync(username, user.Id, succeeded: true,
            isPanelUser: true, lockedOutUntil: lockedOutUntilBefore);

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

    // ⚠️ Çıkış da anonim: süresi dolmuş bir çerezle "Çıkış Yap"a basan yönetici, kapı
    // kapalı olsaydı giriş ekranına atılır ve çerez ASLA temizlenmezdi (SignOut hiç koşmaz).
    [AllowAnonymous]
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
