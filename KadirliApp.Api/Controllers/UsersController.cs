using System;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KadirliApp.Application.Features.Ads.Queries;
using KadirliApp.Application.Features.Users.Queries.GetMyProfile;
using KadirliApp.Application.Features.Users.Commands.DeleteMyAccount;
using KadirliApp.Application.Features.Users.Commands.LinkSocialIdentity;
using KadirliApp.Application.Features.Users.Commands.UnlinkSocialIdentity;
using KadirliApp.Application.Features.Users.Commands.UpdateMyProfile;
using KadirliApp.Application.Features.Users.Commands.UpdateNotificationPreferences;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KadirliApp.Api.Controllers;

// Faz 10.3: masterclass kontratındaki me-scoped uçlar. 10.1'deki geçici {id}/profile uçları
// KALDIRILDI (id her zaman claim'den gelir — IDOR yüzeyi tamamen kapandı; admin karşılığı v1/admin/users'ta).
[Authorize]
public class UsersController : ApiControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var response = await Sender.Send(new GetMyProfileQuery(RequiredUserId));
        return Success(response);
    }

    [HttpPatch("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMyProfileCommand command)
    {
        command.UserId = RequiredUserId;
        var response = await Sender.Send(command);
        return Success(response);
    }

    [HttpPatch("me/notifications")]
    public async Task<IActionResult> UpdateMyNotifications([FromBody] UpdateNotificationPreferencesCommand command)
    {
        command.UserId = RequiredUserId;
        var response = await Sender.Send(command);
        return Success(response);
    }

    /// <summary>Faz 10.6: kullanıcının kendi ilanları — pending/rejected dahil tüm statüler, ?status= ile filtrelenebilir.</summary>
    [HttpGet("me/ads")]
    public async Task<IActionResult> GetMyAds([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var response = await Sender.Send(new GetMyAdsQuery(RequiredUserId, status, page, limit));
        return Success(response);
    }

    /// <summary>Faz 10.6: kullanıcının favori ilanları (favoriye eklenme sırasına göre).</summary>
    [HttpGet("me/favorites")]
    public async Task<IActionResult> GetMyFavorites([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var response = await Sender.Send(new GetMyFavoritesQuery(RequiredUserId, page, limit));
        return Success(response);
    }

    /// <summary>
    /// Faz 10.8: hesap silme (store zorunluluğu). Soft delete + anonimleştirme — telefon yeniden
    /// kayda açılır, ilanlar yayından düşer, refresh token iptal edilir. Body opsiyonel: {"refreshToken":"..."}.
    /// </summary>
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DeleteMyAccountDto? dto)
    {
        var response = await Sender.Send(new DeleteMyAccountCommand(RequiredUserId, dto?.RefreshToken));
        return Success(response);
    }

    public record DeleteMyAccountDto(string? RefreshToken);

    /// <summary>
    /// Faz 12.7 — hesaba sosyal hesap bağlar. <b>Bağlamanın tek meşru yolu budur</b>:
    /// kullanıcı burada hem KadirliApp hesabına (JWT) hem sosyal hesaba (imzalı jeton)
    /// erişimini kanıtlar. E-posta eşleşmesiyle otomatik bağlama <b>yoktur</b> (§7 madde 69).
    /// </summary>
    [HttpPost("me/identities")]
    public async Task<IActionResult> LinkIdentity([FromBody] LinkIdentityDto dto)
    {
        var response = await Sender.Send(
            new LinkSocialIdentityCommand(RequiredUserId, dto.Provider, dto.IdToken));
        return Success(response);
    }

    public record LinkIdentityDto(string Provider, string IdToken);

    /// <summary>
    /// Faz 12.7 — sosyal hesap bağlantısını çözer. <b>Son bağlantı da çözülebilir</b>:
    /// telefon çıpa olduğu için kullanıcı hesabından kilitlenmez.
    /// </summary>
    [HttpDelete("me/identities/{provider}")]
    public async Task<IActionResult> UnlinkIdentity(string provider)
    {
        var removed = await Sender.Send(new UnlinkSocialIdentityCommand(RequiredUserId, provider));
        return Success(new { Removed = removed });
    }

    private Guid RequiredUserId =>
        CurrentUserId ?? throw new UnauthorizedException("Token'da user_id claim'i yok.");
}
