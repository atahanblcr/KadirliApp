using MediatR;

namespace KadirliApp.Application.Features.Auth.Commands.SocialLogin;

/// <summary>
/// Faz 12.7 — <c>POST /v1/auth/social</c>. İstemci sağlayıcıdan aldığı <c>id_token</c>'ı
/// olduğu gibi yollar; <b>doğrulama tümüyle sunucuda</b>.
/// </summary>
public sealed record SocialLoginCommand(string Provider, string IdToken) : IRequest<SocialLoginResult>;

/// <summary>
/// İki dallı sonuç — <c>verify-otp</c>'nin (10.2) birebir aynı şekli.
/// </summary>
/// <remarks>
/// 🔑 Şeklin aynı olması bilinçli: mobil taraf (12.8) sosyal giriş için <b>yeni bir akış
/// öğrenmiyor</b>, var olan "ya oturum ya kayıt" dallanmasını yeniden kullanıyor. Fark tek
/// bir alanda: <see cref="Prefill"/>.
/// </remarks>
public sealed record SocialLoginResult
{
    public bool IsNewUser { get; private init; }

    // Mevcut kullanıcı dalı
    public string? AccessToken { get; private init; }
    public string? RefreshToken { get; private init; }
    public int? ExpiresIn { get; private init; }

    // Yeni kullanıcı dalı
    /// <summary>
    /// Sosyal kayıt taşıyıcısı. <b>Telefon TAŞIMAZ</b> — kayıt yine OTP'den geçer (§7 madde 70).
    /// </summary>
    public string? SocialToken { get; private init; }

    /// <summary>Kayıt formunu ön doldurmak için sağlayıcının verdiği (doğrulanmış) alanlar.</summary>
    public SocialPrefill? Prefill { get; private init; }

    public static SocialLoginResult ExistingUser(Common.Interfaces.AuthTokens tokens) => new()
    {
        IsNewUser = false,
        AccessToken = tokens.AccessToken,
        RefreshToken = tokens.RefreshToken,
        ExpiresIn = tokens.ExpiresIn
    };

    public static SocialLoginResult NewUser(string socialToken, SocialPrefill prefill) => new()
    {
        IsNewUser = true,
        SocialToken = socialToken,
        Prefill = prefill
    };
}

/// <summary>Kayıt ekranının ön doldurabileceği alanlar. Hiçbiri güvenilir kimlik değildir.</summary>
public sealed record SocialPrefill(string Provider, string? Email, string? DisplayName);
