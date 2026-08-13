namespace KadirliApp.Application.Common.Interfaces;

/// <summary>Access + refresh çifti. ExpiresIn: access token'ın saniye cinsinden ömrü.</summary>
public sealed record AuthTokens(string AccessToken, string RefreshToken, int ExpiresIn);

/// <summary>Doğrulanmış refresh token içeriği — jti + kalan ömür, rotasyon/iptal listesinde kullanılır.</summary>
public sealed record RefreshTokenPayload(Guid UserId, string Jti, DateTime ExpiresAtUtc);

public interface IJwtProvider
{
    /// <summary>
    /// user_id/role/phone claim'li access token (AccessSecret) + jti claim'li refresh token
    /// (RefreshSecret) çifti üretir.
    /// </summary>
    AuthTokens GenerateTokens(Guid userId, string role, string phone);

    /// <summary>
    /// Kayıt akışı (masterclass 12.3): verify-otp'de kullanıcı yoksa dönen kısa ömürlü
    /// registration token'ı (varsayılan 30 dk, Jwt:TempTokenMinutes).
    /// </summary>
    string GenerateTempToken(string phone);

    /// <summary>Temp token geçerliyse telefon numarasını, geçersiz/süresi dolmuşsa null döner.</summary>
    string? ValidateTempToken(string tempToken);

    /// <summary>Refresh token'ı RefreshSecret ile doğrular; geçersiz/süresi dolmuşsa null döner.</summary>
    RefreshTokenPayload? ValidateRefreshToken(string refreshToken);

    /// <summary>
    /// Faz 12.7 — sosyal giriş yapan ama <b>henüz hesabı olmayan</b> kullanıcı için kısa ömürlü
    /// taşıyıcı. Doğrulanmış sosyal kimliği taşır, <b>telefon TAŞIMAZ</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>Telefon taşımaması bu jetonun en önemli özelliğidir.</b> Taşısaydı sosyal giriş
    /// OTP'yi <b>atlar</b> hâle gelirdi (§7 madde 70): Google hesabı olan herkes, telefonunu
    /// hiç doğrulatmadan ilan verebilen/taksi çağırabilen bir hesap açardı ve moderasyonun
    /// dayandığı "her hesabın doğrulanmış bir telefonu vardır" varsayımı <b>sessizce</b>
    /// çökerdi. Telefon <b>yalnız</b> OTP'den gelen kayıt jetonunda bulunur; kayıt ikisini
    /// birden ister.
    /// </para>
    /// <para>
    /// ⚠️ <c>token_type</c> ayrı (<c>social_registration</c>): bu jeton telefonlu kayıt
    /// jetonunun yerine geçemez, o da bunun yerine geçemez — 10.2'deki refresh ↔ registration
    /// ayrımının aynısı.
    /// </para>
    /// </remarks>
    string GenerateSocialTempToken(SocialIdentityPayload identity);

    /// <summary>Sosyal kayıt jetonu geçerliyse taşıdığı kimliği, değilse <c>null</c> döner.</summary>
    SocialIdentityPayload? ValidateSocialTempToken(string socialToken);
}
