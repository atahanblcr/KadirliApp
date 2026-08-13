using KadirliApp.Domain.Enums;

namespace KadirliApp.Infrastructure.Identity.Social;

/// <summary>
/// Faz 12.7 — bir sosyal sağlayıcının doğrulama ayarları.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b>Yalnız <see cref="Audiences"/> yapılandırmadan gelir.</b> <see cref="Issuers"/> ve
/// <see cref="JwksUri"/> protokol sabitleridir (Google/Apple bunları yıllardır değiştirmiyor)
/// ve <b>bilinçli olarak koda gömülü</b>: ayar hâline getirilselerdi yanlış bir <c>appsettings</c>
/// satırı doğrulamayı <i>saldırganın seçtiği</i> bir anahtar sunucusuna yönlendirebilirdi —
/// yani ayar sayısını azaltmak burada bir güvenlik kararıdır. Testlerin sahte bir JWKS
/// kullanabilmesi için yine de <c>init</c> ile ezilebilirler.
/// </para>
/// <para>
/// ⚠️ <see cref="Audiences"/> <b>boşsa sağlayıcı KAPALIDIR</b>. "Boş liste = herkesi kabul et"
/// yorumu, sosyal girişin bir numaralı zafiyetinin (yanlış <c>aud</c>) en geniş hâli olurdu.
/// </para>
/// </remarks>
public sealed record SocialProviderSettings
{
    public required string Provider { get; init; }

    /// <summary>Kabul edilen <c>aud</c> değerleri — bizim OAuth client id'lerimiz.</summary>
    public required IReadOnlyList<string> Audiences { get; init; }

    /// <summary>Kabul edilen <c>iss</c> değerleri.</summary>
    public required IReadOnlyList<string> Issuers { get; init; }

    /// <summary>Sağlayıcının açık anahtar (JWKS) adresi.</summary>
    public required string JwksUri { get; init; }

    /// <summary>
    /// Google: iki issuer da <b>geçerlidir</b> ve ikisi de canlıda görülüyor
    /// (<c>accounts.google.com</c> eski istemcilerden, <c>https://</c>'li olan yenilerden gelir).
    /// Yalnız birini kabul etmek, kullanıcıların bir kısmını <b>sebebi görünmeden</b> dışarıda
    /// bırakırdı.
    /// </summary>
    public static SocialProviderSettings ForGoogle(IReadOnlyList<string> audiences) => new()
    {
        Provider = SocialProviders.Google,
        Audiences = audiences,
        Issuers = new[] { "https://accounts.google.com", "accounts.google.com" },
        JwksUri = "https://www.googleapis.com/oauth2/v3/certs"
    };

    /// <summary>
    /// Apple'da <c>aud</c> <b>bundle id</b>'dir (Google'daki gibi bir OAuth client id değil) —
    /// iki sağlayıcının ayarı aynı isimde ama <b>aynı şey değil</b>; <c>secrets/README.md</c>
    /// bunu ayrıca yazar.
    /// </summary>
    public static SocialProviderSettings ForApple(IReadOnlyList<string> audiences) => new()
    {
        Provider = SocialProviders.Apple,
        Audiences = audiences,
        Issuers = new[] { "https://appleid.apple.com" },
        JwksUri = "https://appleid.apple.com/auth/keys"
    };
}
