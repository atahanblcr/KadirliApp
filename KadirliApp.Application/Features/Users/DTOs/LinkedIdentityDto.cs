using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Users.DTOs;

/// <summary>
/// Faz 12.7 — bir hesabın bağlı sosyal hesabı (mobil "Bağlı hesaplar" ekranı, 12.8).
/// </summary>
/// <remarks>
/// ⚠️ <c>ProviderUserId</c> (<c>sub</c>) <b>DTO'ya çıkmaz.</b> İstemcinin ona ihtiyacı yok
/// ve dışarı verilen her kimlik değeri, ileride birinin onunla eşleştirme yapmaya
/// kalkışabileceği bir yüzeydir — eşleştirmenin tek yeri sunucudur.
/// </remarks>
public sealed class LinkedIdentityDto
{
    public string Provider { get; set; } = default!;
    public string? Email { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime LinkedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }

    public static LinkedIdentityDto FromEntity(UserIdentity identity) => new()
    {
        Provider = identity.Provider,
        Email = identity.Email,
        EmailVerified = identity.EmailVerified,
        LinkedAt = identity.LinkedAt,
        LastUsedAt = identity.LastUsedAt
    };
}
