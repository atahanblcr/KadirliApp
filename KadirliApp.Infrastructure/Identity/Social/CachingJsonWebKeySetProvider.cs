using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace KadirliApp.Infrastructure.Identity.Social;

/// <summary>
/// Faz 12.7 — JWKS'i HTTP'den çeken, süreli önbellekleyen gerçekleme.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Fail-closed.</b> Ağ hatası, 500 ya da bozuk JSON → <b>boş liste</b> → jeton reddedilir.
/// Bu, projedeki Redis kararının (§7 madde 36 — fail-<i>open</i>) bilinçli tersi: orada bedel
/// fazladan bir e-posta, burada bedel <b>doğrulanmamış bir jetonla hesaba girmek</b>.
/// </para>
/// <para>
/// ⚠️ <b>Anahtar döndürmesi (key rotation) kendiliğinden çalışmalı.</b> Google anahtarlarını
/// düzenli değiştiriyor; yalnız TTL'e güvenilseydi her döndürmede <b>TTL kadar süre boyunca
/// hiç kimse giriş yapamazdı</b> ve loglarda yalnız "geçersiz jeton" görünürdü. Bu yüzden
/// tanınmayan bir <c>kid</c> tek seferlik bir <b>zorla yenileme</b> tetikler.
/// </para>
/// <para>
/// ⚠️ Zorla yenileme <b>kısılır</b> (<see cref="RefreshThrottle"/>): kısılmasaydı geçersiz
/// <c>kid</c>'li jeton üreten bir saldırgan, her isteğinde Google'a bir istek attırarak
/// <b>bizi kendi kaynağımıza karşı bir amplifikatöre</b> çevirirdi (§7 madde 36'nın
/// "kısmasız uyarı kanalı" dersinin aynısı).
/// </para>
/// </remarks>
public sealed class CachingJsonWebKeySetProvider : IJsonWebKeySetProvider
{
    public const string HttpClientName = "social-jwks";

    /// <summary>Anahtarların normal tazelenme aralığı.</summary>
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(6);

    /// <summary>Tanınmayan <c>kid</c> yüzünden yapılan zorla yenilemenin en sık aralığı.</summary>
    private static readonly TimeSpan RefreshThrottle = TimeSpan.FromMinutes(5);

    private sealed record CacheEntry(IReadOnlyList<SecurityKey> Keys, DateTime FetchedAtUtc);

    private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new(StringComparer.Ordinal);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CachingJsonWebKeySetProvider> _logger;

    public CachingJsonWebKeySetProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<CachingJsonWebKeySetProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SecurityKey>> GetKeysAsync(
        SocialProviderSettings settings, bool forceRefresh, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        if (Cache.TryGetValue(settings.JwksUri, out var cached))
        {
            var age = now - cached.FetchedAtUtc;
            var stale = age >= CacheTtl;
            var throttled = age < RefreshThrottle;

            // Taze önbellek + zorlama yok → doğrudan dön.
            // Zorlama var ama kısma penceresindeyiz → yine de önbelleği dön (amplifikatör olmayalım).
            if (!stale && (!forceRefresh || throttled))
                return cached.Keys;
        }

        var fetched = await FetchAsync(settings, cancellationToken);
        if (fetched.Count > 0)
        {
            Cache[settings.JwksUri] = new CacheEntry(fetched, now);
            return fetched;
        }

        // 🔑 Çekemedik: elimizdeki BAYAT anahtarlar hiç anahtar olmamasından iyidir.
        // Sağlayıcının kısa bir kesintisi, bütün kullanıcıları giriş yapamaz hâle
        // getirmemeli — imza doğrulaması yine de yapılıyor, yalnız anahtar listesi eski.
        return cached?.Keys ?? Array.Empty<SecurityKey>();
    }

    private async Task<IReadOnlyList<SecurityKey>> FetchAsync(
        SocialProviderSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            var http = _httpClientFactory.CreateClient(HttpClientName);
            var json = await http.GetStringAsync(settings.JwksUri, cancellationToken);
            var keySet = new JsonWebKeySet(json);
            return keySet.GetSigningKeys().ToList();
        }
        catch (Exception ex)
        {
            // Fırlatmıyoruz: çağıran "doğrulayamadım" → 401 üretir. Burada patlamak
            // isteği zarfsız 500'e düşürürdü (§7 madde 10/31'in aynı sınıfı).
            _logger.LogError(ex,
                "Sosyal giriş anahtarları alınamadı ({Provider}, {JwksUri}). Jetonlar reddedilecek.",
                settings.Provider, settings.JwksUri);
            return Array.Empty<SecurityKey>();
        }
    }

    /// <summary>Testler için: süreç içi önbelleği boşaltır.</summary>
    internal static void ClearCache() => Cache.Clear();
}
