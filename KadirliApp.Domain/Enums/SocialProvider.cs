namespace KadirliApp.Domain.Enums;

/// <summary>
/// Faz 12.7 — sosyal giriş sağlayıcısının adı. Değer veritabanında ve DTO'da
/// <b>metin</b> olarak durur (<c>"google"</c> / <c>"apple"</c>).
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b><see cref="Normalize"/> bilinmeyen değeri VARSAYILANA DÜŞÜRMEZ — <c>null</c> döner.</b>
/// Bu, kardeş sınıf <see cref="TransportVehicleTypes"/>'ın davranışının <b>tam tersidir</b> ve
/// bilinçlidir. Orada bedel bir kaydın yanlış süzülmesi; burada bedel şu olurdu:
/// <c>?provider=gogle</c> yazan (ya da yarın eklenecek üçüncü bir sağlayıcıyı deneyen) bir
/// istemcinin jetonu <b>Google'ınmış gibi</b> doğrulanmaya çalışılırdı. Kimlik doğrulamada
/// "şüphede kalınca varsayılana düş" kuralı geçerli değildir: burada varsayılan bir
/// <b>güven kararıdır</b>, bir görüntüleme tercihi değil.
/// </para>
/// <para>
/// ⚠️ Bu değerler DTO'ya ve <c>user_identities.provider</c> kolonuna çıkıyor, yani
/// <b>kontrattır</b>: yeniden adlandırılırsa mağazadaki eski sürümlerin gönderdiği
/// <c>"google"</c> tanınmaz hâle gelir ve sosyal giriş <b>sessizce</b> "geçersiz sağlayıcı"
/// hatasına döner.
/// </para>
/// </remarks>
public static class SocialProviders
{
    public const string Google = "google";
    public const string Apple = "apple";

    public static IReadOnlyList<string> All { get; } = new[] { Google, Apple };

    /// <summary>
    /// Ham metni kanonik hâle getirir; <b>tanınmayan/boş değer için <c>null</c></b>
    /// ("böyle bir sağlayıcı yok" — varsayılana düşülmez, bkz. sınıf açıklaması).
    /// </summary>
    public static string? Normalize(string? raw)
    {
        var value = raw?.Trim().ToLowerInvariant();
        return value is Google or Apple ? value : null;
    }

    /// <summary>Tanınan bir sağlayıcı mı? (<see cref="Normalize"/>'ın evet/hayır hâli.)</summary>
    public static bool IsKnown(string? raw) => Normalize(raw) is not null;
}
