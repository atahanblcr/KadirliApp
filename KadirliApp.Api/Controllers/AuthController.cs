using System.Security.Claims;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Features.Auth.Commands.Login;
using KadirliApp.Application.Features.Auth.Commands.Logout;
using KadirliApp.Application.Features.Auth.Commands.RefreshToken;
using KadirliApp.Application.Features.Auth.Commands.Register;
using KadirliApp.Application.Features.Auth.Commands.SocialLogin;
using KadirliApp.Application.Features.Auth.Commands.VerifyOtp;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KadirliApp.Api.Controllers;

[ApiController]
[Route("v1/auth")]
[EnableRateLimiting("auth")] // Faz 9.2: IP başına sıkı limit (Brute-Force koruması)
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);

        // Faz 9.2: OTP artık yanıtta dönmez, SMS ile gönderilir. Tek istisna Otp:DevMode=true
        // (sağlayıcısız geliştirme/test) — o zaman DevOtp dolu gelir ve yanıta eklenir.
        return Ok(result.DevOtp is null
            ? new { Message = "OTP gönderildi", ExpiresIn = result.ExpiresInSeconds, RetryAfter = result.RetryAfterSeconds }
            : (object)new { Message = "OTP gönderildi", ExpiresIn = result.ExpiresInSeconds, RetryAfter = result.RetryAfterSeconds, Otp = result.DevOtp });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpCommand command)
    {
        var result = await _mediator.Send(command);

        // Faz 10.2 (masterclass 12.3): kayıtlı kullanıcı → access+refresh çifti;
        // yeni kullanıcı → tempToken (kayıt POST /v1/auth/register ile tamamlanır).
        return Ok(result.IsNewUser
            ? new { IsNewUser = true, TempToken = result.TempToken }
            : (object)new
            {
                IsNewUser = false,
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                ExpiresIn = result.ExpiresIn
            });
    }

    /// <summary>
    /// Faz 12.7 — sosyal giriş (Google / Apple). Sonucun şekli <c>verify-otp</c> ile
    /// <b>birebir aynı</b>: ya oturum, ya kayıt taşıyıcısı.
    /// </summary>
    /// <remarks>
    /// 🔴 <c>IsNewUser=true</c> dalı <b>telefon taşımaz</b>: istemci normal OTP akışını
    /// tamamlar ve <c>register</c>'a <b>iki jetonu birden</b> verir. Sosyal giriş OTP'yi
    /// atlamaz (§7 madde 70).
    /// </remarks>
    [HttpPost("social")]
    public async Task<IActionResult> Social([FromBody] SocialLoginCommand command)
    {
        var result = await _mediator.Send(command);

        return Ok(result.IsNewUser
            ? new { IsNewUser = true, SocialToken = result.SocialToken, Prefill = result.Prefill }
            : (object)new
            {
                IsNewUser = false,
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                ExpiresIn = result.ExpiresIn
            });
    }

    /// <summary>
    /// Kayıt tamamlama. Faz 12.16'dan beri gövdede <c>consents</c> taşıyabilir.
    /// </summary>
    /// <remarks>
    /// 🔴 Rıza kaydının bağlamı (<b>IP · tarayıcı</b>) <b>SUNUCUDA</b> doldurulur ve gövdeden
    /// geleni <b>ezer</b> (§7 madde 33'ün <c>ErrorLog.Source</c> kararının aynısı): istemci
    /// bunları söyleyebilseydi, "kim nereden onayladı" sütunu kanıt değil <b>istemcinin
    /// iddiası</b> olurdu — üstelik rıza defteri tam da o soruyu cevaplamak için var.
    /// </remarks>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var enriched = command with
        {
            IpAddress = HttpContext.Connection.RemoteIpAddress,
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        return Ok(await _mediator.Send(enriched));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command)
    {
        return Ok(await _mediator.Send(command));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue("user_id"), out var userId))
            throw new UnauthorizedException();

        await _mediator.Send(new LogoutCommand(userId, request.RefreshToken));
        return Ok(new { Message = "Çıkış yapıldı" });
    }
}

/// <summary>Logout gövdesi: refresh token verilirse iptal listesine yazılır (best-effort).</summary>
public sealed record LogoutRequest(string? RefreshToken);
