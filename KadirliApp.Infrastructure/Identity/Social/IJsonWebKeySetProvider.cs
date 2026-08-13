using Microsoft.IdentityModel.Tokens;

namespace KadirliApp.Infrastructure.Identity.Social;

/// <summary>
/// Faz 12.7 — sağlayıcının imza anahtarlarını (JWKS) getirir.
/// </summary>
/// <remarks>
/// 🔑 Ayrı bir arayüz olmasının tek sebebi <b>testlenebilirlik</b>: doğrulamanın kendisi
/// (imza · <c>iss</c> · <c>aud</c> · süre · algoritma) ağa çıkmadan, sahte bir anahtar
/// kümesiyle uçtan uca denenebiliyor. Bu, projenin *"bayrakla kapalı yol = hiç test edilmemiş
/// yol"* kuralının (10.11 — <c>FcmPushService</c>) doğrudan uygulanışı: sosyal giriş
/// yapılandırma gelene kadar kapalı duracak, yani ilk gerçek koşusu **canlıda** olacak.
/// </remarks>
public interface IJsonWebKeySetProvider
{
    /// <summary>
    /// Sağlayıcının anahtarlarını döner. Erişilemezse <b>boş liste</b> döner, fırlatmaz —
    /// çağıran bunu "doğrulayamadım" olarak ele alır ve jetonu <b>reddeder</b> (fail-closed).
    /// </summary>
    /// <param name="forceRefresh">
    /// Önbelleği atla. Sağlayıcı anahtarını döndürdüğünde (<c>kid</c> tanınmadığında) çağrılır.
    /// </param>
    Task<IReadOnlyList<SecurityKey>> GetKeysAsync(
        SocialProviderSettings settings, bool forceRefresh, CancellationToken cancellationToken);
}
