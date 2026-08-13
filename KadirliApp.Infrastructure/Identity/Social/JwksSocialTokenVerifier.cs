using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace KadirliApp.Infrastructure.Identity.Social;

/// <summary>
/// Faz 12.7 — Google ve Apple <c>id_token</c>'larının <b>tek</b> doğrulayıcısı.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b>Neden iki sağlayıcı için tek gerçekleme:</b> ikisi de OpenID Connect ve ikisi de
/// RS256 imzalı JWT üretiyor; fark yalnız <c>iss</c>, <c>aud</c> ve JWKS adresi — yani
/// <b>veri</b>, kod değil. İki ayrı sınıf yazmak, aynı güvenlik kuralının iki sahibi olması
/// demekti (bu projenin en sık tekrarlayan hasar sınıfı, §7 madde 23/38/55): biri sıkılaşır,
/// diğeri unutulur ve <b>hiçbir yerde hata görünmez</b>.
/// </para>
/// <para>
/// 📌 <b>Plandan bilinçli sapma:</b> 12.7 planı Google için
/// <c>GoogleJsonWebSignature.ValidateAsync</c> diyordu. O metot <b>statiktir ve gerçek
/// Google anahtarlarına bağlıdır</b> — yani <c>aud</c> kuralını (bu fazın "bir numaralı
/// gerçek zafiyet" dediği şeyi) <b>hiçbir testle kilitleyemezdik</b>. Kural burada kendi
/// kodumuzda olduğu için sahte bir anahtar kümesiyle uçtan uca denenebiliyor ve bozma turunda
/// kırmızıya dönüyor. Planın <i>niyeti</i> (jeton sunucuda doğrulanır, <c>aud</c> bizim client
/// id'lerimizden biri olmalı) birebir korundu; değişen yalnız gerçekleme.
/// </para>
/// </remarks>
public sealed class JwksSocialTokenVerifier : ISocialTokenVerifier
{
    /// <summary>
    /// 🔴 Algoritma <b>sabitlenir</b>. Sabitlenmezse jetonun kendi <c>alg</c> başlığı
    /// belirleyici olur — JWT'nin klasik zafiyeti (<c>alg: none</c> / HS256 karıştırması).
    /// </summary>
    private static readonly string[] AllowedAlgorithms = [SecurityAlgorithms.RsaSha256];

    private readonly IReadOnlyDictionary<string, SocialProviderSettings> _providers;
    private readonly IJsonWebKeySetProvider _keys;
    private readonly ILogger<JwksSocialTokenVerifier> _logger;

    public JwksSocialTokenVerifier(
        IEnumerable<SocialProviderSettings> providers,
        IJsonWebKeySetProvider keys,
        ILogger<JwksSocialTokenVerifier> logger)
    {
        // ⚠️ Client id'si olmayan sağlayıcı listeye HİÇ girmez: "açık ama doğrulayamayan"
        // bir sağlayıcı, kapalı olandan tehlikelidir.
        _providers = providers
            .Where(p => p.Audiences.Any(a => !string.IsNullOrWhiteSpace(a)))
            .ToDictionary(p => p.Provider, StringComparer.Ordinal);

        _keys = keys;
        _logger = logger;
    }

    public bool IsEnabled(string provider)
        => SocialProviders.Normalize(provider) is { } p && _providers.ContainsKey(p);

    public IReadOnlyList<string> EnabledProviders
        => _providers.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();

    public async Task<SocialIdentityPayload?> VerifyAsync(
        string provider, string idToken, CancellationToken cancellationToken)
    {
        if (SocialProviders.Normalize(provider) is not { } canonical ||
            !_providers.TryGetValue(canonical, out var settings) ||
            string.IsNullOrWhiteSpace(idToken))
        {
            return null;
        }

        var principal = await ValidateAsync(idToken, settings, forceKeyRefresh: false, cancellationToken);

        // Anahtar döndürmüş olabilir: bir kez zorla tazeleyip tekrar dene (kısmalı).
        principal ??= await ValidateAsync(idToken, settings, forceKeyRefresh: true, cancellationToken);

        if (principal is null) return null;

        // 🔴 `sub` OLMADAN kimlik YOKTUR. Boşsa jeton biçimsel olarak geçerli olsa bile
        // reddedilir — aksi hâlde `sub`'ı boş iki farklı kişi AYNI kimliğe eşlenirdi
        // (benzersiz indeks yüzünden ikincisi ilkinin hesabına girerdi).
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrWhiteSpace(sub)) return null;

        var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email);

        // Google `email_verified`'ı bool, Apple string ("true") gönderir — ikisi de burada
        // aynı yere düşer. Yokluğu "doğrulanmamış" sayılır (şüphede kalınca DAR taraf).
        var emailVerified = string.Equals(
            principal.FindFirstValue("email_verified"), "true", StringComparison.OrdinalIgnoreCase);

        // Apple adı yalnız İLK girişte ve id_token'da DEĞİL, ayrı bir gövdede yollar —
        // yani burada çoğu zaman null olur ve bu normaldir (ön doldurma boş kalır).
        var name = principal.FindFirstValue("name")
                   ?? principal.FindFirstValue(JwtRegisteredClaimNames.GivenName);

        return new SocialIdentityPayload(canonical, sub, email, emailVerified, name);
    }

    private async Task<ClaimsPrincipal?> ValidateAsync(
        string idToken, SocialProviderSettings settings, bool forceKeyRefresh, CancellationToken ct)
    {
        var signingKeys = await _keys.GetKeysAsync(settings, forceKeyRefresh, ct);
        if (signingKeys.Count == 0)
        {
            // Anahtar yoksa doğrulama YAPILAMAZ → jeton reddedilir (fail-closed).
            _logger.LogWarning(
                "{Provider} için imza anahtarı yok — sosyal giriş jetonu reddedildi.", settings.Provider);
            return null;
        }

        var handler = new JwtSecurityTokenHandler();
        handler.InboundClaimTypeMap.Clear(); // claim adları geldiği gibi kalsın (sub, email, name)

        try
        {
            return handler.ValidateToken(idToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = settings.Issuers,

                // 🔴 SOSYAL GİRİŞİN BİR NUMARALI GÜVENLİK KURALI (§7 madde 68).
                // Kapatılırsa BAŞKA BİR UYGULAMANIN Google jetonu bizde de geçerli olur:
                // imza doğru, issuer doğru, süre doğru — ve hesap ele geçirilmiş olur.
                ValidateAudience = true,
                ValidAudiences = settings.Audiences,

                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = signingKeys,
                ValidAlgorithms = AllowedAlgorithms,

                // Sağlayıcı ile aramızdaki saat farkı için küçük bir pay; sıfır olsaydı
                // saniyelik sapmalar rastgele "geçersiz jeton" üretirdi.
                ClockSkew = TimeSpan.FromMinutes(2)
            }, out _);
        }
        catch (Exception)
        {
            // İmza/issuer/audience/süre/algoritma — hepsi tek bir "geçersiz jeton".
            // Sebebi istemciye söylemek saldırgana ücretsiz hata ayıklama kanalı olurdu.
            return null;
        }
    }
}
