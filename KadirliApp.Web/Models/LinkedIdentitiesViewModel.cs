using KadirliApp.Domain.Entities;

namespace KadirliApp.Web.Models;

/// <summary>
/// Faz 12.7 — <c>_LinkedIdentities</c> partial'ının modeli.
/// </summary>
/// <remarks>
/// 🔑 <c>UserId</c> ayrı bir alan olmak zorunda: kaldırma formu <b>kullanıcıyı</b> adresliyor
/// ve liste boşken de kutu çiziliyor — yani kimliği listenin ilk satırından türetmek
/// <b>tam da boş listede</b> çalışmazdı.
/// </remarks>
public sealed class LinkedIdentitiesViewModel
{
    public required Guid UserId { get; init; }

    public required IReadOnlyList<UserIdentity> Identities { get; init; }
}
