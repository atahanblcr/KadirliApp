namespace KadirliApp.Application.Common.Interfaces;

/// <summary>
/// Refresh token iptal listesi (jti bazlı, Redis). Rotasyonda eski token ve logout'ta gönderilen
/// token kalan ömrü kadar listeye yazılır — TTL dolunca anahtar kendiliğinden düşer, liste şişmez.
/// </summary>
public interface ITokenBlacklistService
{
    Task RevokeAsync(string jti, TimeSpan ttl);
    Task<bool> IsRevokedAsync(string jti);
}
