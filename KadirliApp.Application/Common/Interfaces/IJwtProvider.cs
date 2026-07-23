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
}
