using Microsoft.AspNetCore.Authorization;
using KadirliApp.Web.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using System.Threading.Tasks;
using System.Linq;

using MediatR;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Features.Users.Commands.CreateUser;
using KadirliApp.Application.Features.Users.Commands.SetUserBan;
using KadirliApp.Application.Features.Users.Commands.RemoveUserIdentity;
using KadirliApp.Application.Features.Users.Commands.UpdateUser;
using KadirliApp.Application.Features.Users.DTOs;
using KadirliApp.Application.Features.Users.Queries.GetUsers;
using KadirliApp.Application.Common.Security;
using KadirliApp.Application.Features.LoginAttempts.Dtos;
using KadirliApp.Application.Features.LoginAttempts.Queries;
using System.Security.Claims;

namespace KadirliApp.Web.Controllers;

[Authorize(Roles = "admin,super_admin,moderator")]
[PanelPermission("users")]
public class UsersAdminController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly ISender _sender;

    public UsersAdminController(IUnitOfWork uow, ISender sender)
    {
        _uow = uow;
        _sender = sender;
    }

    public async Task<IActionResult> Index([FromQuery] string? search, [FromQuery] int page = 1)
    {
        // Faz 9.4 kuralı: panel inline sorgu kurmaz. Eskiden burada filtresiz `ToListAsync()`
        // vardı — kullanıcı tablosu en hızlı büyüyen tablo olduğundan tüm satırlar belleğe
        // çekiliyordu ve panelde sayfalama da yoktu. GetUsersQuery arama + sayfalamayı zaten
        // sağlıyordu (Admin API kullanıyordu), panel ondan sapmıştı.
        var result = await _sender.Send(new GetUsersQuery(search, null, page, 20));
        ViewBag.Search = search;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Neighborhoods = await _uow.Repository<Neighborhood>().Query().OrderBy(n => n.Name).ToListAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Neighborhoods = await _uow.Repository<Neighborhood>().Query().OrderBy(n => n.Name).ToListAsync();
            return View(dto);
        }

        var result = await _sender.Send(new CreateUserCommand { Dto = dto });
        if (result.Success)
        {
            TempData["Success"] = "Kullanıcı başarıyla oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = result.Error?.Message ?? "Bir hata oluştu.";
        ViewBag.Neighborhoods = await _uow.Repository<Neighborhood>().Query().OrderBy(n => n.Name).ToListAsync();
        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _uow.Repository<User>().GetByIdAsync(id);
        if (user == null)
        {
            TempData["Error"] = "Kullanıcı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var dto = new UpdateUserDto
        {
            Phone = user.Phone,
            Email = user.Email,
            Username = user.Username,
            Role = (int)user.Role,
            Age = user.Age,
            PrimaryNeighborhoodId = user.PrimaryNeighborhoodId,
            LocationType = user.LocationType,
            IsActive = user.IsActive
        };

        ViewBag.Id = id;
        ViewBag.Neighborhoods = await _uow.Repository<Neighborhood>().Query().OrderBy(n => n.Name).ToListAsync();
        ViewBag.LoginAttempts = await RecentLoginAttemptsAsync(id, user.Phone);
        ViewBag.Identities = await LinkedIdentitiesAsync(id);
        return View(dto);
    }

    /// <summary>
    /// Faz 12.7 — hesaba bağlı sosyal hesaplar.
    /// </summary>
    /// <remarks>
    /// 🔑 <b>Giriş denemeleri kutusunun aksine bu kutu moderatöre de AÇIK</b> ve fark
    /// bilinçli: burada gösterilen şey bir güvenlik kaydı değil, <b>hesabın kendi alanı</b>
    /// (kullanıcı zaten kendi profilinde görüyor). Kişisel veri sızıntısı riski de yok —
    /// <c>provider_user_id</c> ekrana <b>hiç çıkmıyor</b>.
    /// </remarks>
    private async Task<IReadOnlyList<UserIdentity>> LinkedIdentitiesAsync(Guid userId)
        => await _uow.Repository<UserIdentity>().Query()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Provider)
            .ToListAsync();

    /// <summary>
    /// Faz 12.2 — hesabın son giriş denemeleri (yalnız admin'e).
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Rol kapısı burada, görünümde değil.</b> Bu ekran moderatöre AÇIK
    /// (<c>[PanelPermission("users")]</c>); giriş denemesi ekranı ise bilinçli olarak
    /// yalnız-admin. Veriyi koşulsuz çekseydik, kapalı bir ekranın içeriğini açık bir
    /// ekrandan sızdırmış olurduk — "ekran kapalı ama verisi başka yerde görünüyor" tam
    /// olarak sessiz yetki sızıntısıdır. Yalnız görünümde gizlemek de yetmez: veri yine
    /// sorgulanır ve bir sonraki düzenlemede ekrana düşer.
    ///
    /// 🔑 <c>UserId</c> <b>ve</b> maskeli telefon birlikte sorgulanır: hatalı OTP
    /// satırlarında <c>UserId</c> boştur (o dalda kullanıcı tablosuna dokunulmuyor) ve
    /// yalnız kimlikle bulunabilirler. Tek alanla süzülseydi kutu, bu hesaba yapılan
    /// başarısız OTP denemelerini <b>hiç göstermezdi</b>.
    /// </remarks>
    private async Task<IReadOnlyList<LoginAttemptResponseDto>> RecentLoginAttemptsAsync(Guid userId, string? phone)
    {
        if (!User.IsInRole("admin") && !User.IsInRole("super_admin"))
            return Array.Empty<LoginAttemptResponseDto>();

        var result = await _sender.Send(new GetLoginAttemptsQuery(new QueryLoginAttemptDto
        {
            UserId = userId,
            MaskedIdentifier = string.IsNullOrWhiteSpace(phone) ? null : LoginIdentifierMasker.MaskIdentifier(phone),
            Page = 1,
            Limit = 10
        }));

        return result.Items.ToList();
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Id = id;
            ViewBag.Neighborhoods = await _uow.Repository<Neighborhood>().Query().OrderBy(n => n.Name).ToListAsync();
            return View(dto);
        }

        var result = await _sender.Send(new UpdateUserCommand { Id = id, Dto = dto });
        if (result.Success)
        {
            TempData["Success"] = "Kullanıcı başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = result.Error?.Message ?? "Bir hata oluştu.";
        ViewBag.Id = id;
        ViewBag.Neighborhoods = await _uow.Repository<Neighborhood>().Query().OrderBy(n => n.Name).ToListAsync();
        return View(dto);
    }

    /// <summary>
    /// Faz 12.7 — yönetici bir kullanıcının sosyal hesap bağlantısını kaldırır.
    /// </summary>
    /// <remarks>
    /// 🔑 <b>Aksiyon adı bir karardır (§7 madde 19).</b> <c>Remove…</c> öneki
    /// <c>PanelPermissionFilter.ActionFor</c>'da <c>delete</c> iznine düşüyor ve bu bilinçli:
    /// bir giriş yöntemini kaldırmak <b>güvenlik etkili</b> bir işlem, "profil düzenleme"
    /// (<c>update</c>) değil. Ad <c>Unlink…</c> olsaydı hiçbir önekle eşleşmez, POST olduğu
    /// için sessizce <c>update</c>'e düşerdi — madde 19'un tam olarak altı kez tekrarlamış
    /// tuzağı (ve <c>Un…</c> biçimi listedeki en sinsi hâli).
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveIdentity(Guid id, string provider)
    {
        try
        {
            var removed = await _sender.Send(new RemoveUserIdentityCommand(id, provider));
            TempData[removed ? "Success" : "Error"] = removed
                ? "Sosyal hesap bağlantısı kaldırıldı."
                : "Bu sağlayıcıya ait bir bağlantı bulunamadı.";
        }
        catch (NotFoundException)
        {
            TempData["Error"] = "Kullanıcı bulunamadı.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    // Faz 10.9(h): inline IsBanned yazımı SetUserBanCommand'e taşındı — BanReason/BannedAt/BannedBy artık dolu (Faz 9.4 kuralı)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ban(System.Guid id, string? reason)
    {
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);
        try
        {
            await _sender.Send(new SetUserBanCommand(id, true, adminId, string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()));
            TempData["Success"] = "Kullanıcı başarıyla banlandı.";
        }
        catch (NotFoundException)
        {
            TempData["Error"] = "Kullanıcı bulunamadı.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unban(System.Guid id)
    {
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);
        try
        {
            await _sender.Send(new SetUserBanCommand(id, false, adminId));
            TempData["Success"] = "Kullanıcının banı başarıyla kaldırıldı.";
        }
        catch (NotFoundException)
        {
            TempData["Error"] = "Kullanıcı bulunamadı.";
        }
        return RedirectToAction(nameof(Index));
    }
}
