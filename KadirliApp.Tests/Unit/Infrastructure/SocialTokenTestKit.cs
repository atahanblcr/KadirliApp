using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using KadirliApp.Infrastructure.Identity.Social;
using Microsoft.IdentityModel.Tokens;

namespace KadirliApp.Tests.Unit.Infrastructure;

/// <summary>
/// Faz 12.7 — sosyal giriş testlerinin ortak zemini: <b>gerçek RSA anahtarıyla imzalanmış,
/// gerçek Google/Apple biçiminde</b> jetonlar üretir ve bunları sahte bir JWKS üzerinden
/// doğrulatır.
/// </summary>
/// <remarks>
/// 🔑 <b>Neden sahte bir doğrulayıcı DEĞİL de sahte bir ANAHTAR SUNUCUSU:</b> sahte bir
/// <c>ISocialTokenVerifier</c> yazsaydık testler yalnız *bizim* akışımızı denerdi ve bu
/// fazın en kritik kuralı — <c>aud</c> doğrulaması (§7 madde 68) — <b>hiçbir testle
/// kilitlenmemiş</b> olurdu. Anahtar sunucusunu sahteleyerek doğrulamanın kendisi
/// (imza · <c>iss</c> · <c>aud</c> · süre · algoritma) ağa çıkmadan uçtan uca koşuyor.
/// </remarks>
internal static class SocialTokenTestKit
{
    public const string GoogleIssuer = "https://accounts.google.com";
    public const string AppleIssuer = "https://appleid.apple.com";

    public const string OurGoogleClientId = "111-kadirli.apps.googleusercontent.com";
    public const string OurAppleBundleId = "app.kadirli";

    /// <summary>Testlerin "başka bir uygulamanın jetonu" senaryosunda kullandığı client id.</summary>
    public const string SomeoneElsesClientId = "999-baska-uygulama.apps.googleusercontent.com";

    /// <summary>Süreç boyunca sabit imza anahtarı — her testte yeni anahtar üretmek yavaş.</summary>
    public static readonly RsaSecurityKey SigningKey =
        new(RSA.Create(2048)) { KeyId = "kadirli-test-key" };

    /// <summary>İmzayı bozmak için kullanılan, JWKS'te <b>olmayan</b> ikinci anahtar.</summary>
    public static readonly RsaSecurityKey ForeignKey =
        new(RSA.Create(2048)) { KeyId = "yabanci-anahtar" };

    public static SocialProviderSettings GoogleSettings(params string[] audiences)
        => SocialProviderSettings.ForGoogle(audiences.Length > 0 ? audiences : new[] { OurGoogleClientId });

    public static SocialProviderSettings AppleSettings(params string[] audiences)
        => SocialProviderSettings.ForApple(audiences.Length > 0 ? audiences : new[] { OurAppleBundleId });

    /// <summary>
    /// Sağlayıcının üreteceği türden bir <c>id_token</c> üretir. Her parametre bilerek
    /// ezilebilir — testlerin tek tek bozacağı şeyler bunlar.
    /// </summary>
    public static string MintToken(
        string issuer = GoogleIssuer,
        string audience = OurGoogleClientId,
        string? subject = "google-sub-1",
        string? email = "vatandas@ornek.com",
        bool emailVerified = true,
        string? name = "Ayşe Yılmaz",
        TimeSpan? lifetime = null,
        SecurityKey? key = null,
        string algorithm = SecurityAlgorithms.RsaSha256)
    {
        var claims = new List<Claim>
        {
            new("email_verified", emailVerified ? "true" : "false")
        };

        if (subject is not null) claims.Add(new Claim(JwtRegisteredClaimNames.Sub, subject));
        if (email is not null) claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));
        if (name is not null) claims.Add(new Claim("name", name));

        var now = DateTime.UtcNow;
        var life = lifetime ?? TimeSpan.FromMinutes(30);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            // ⚠️ notBefore geçmişte: ClockSkew testlerinin süreyi ölçtüğünden emin olalım.
            notBefore: now.Add(-life).AddMinutes(-1),
            expires: now.Add(life),
            signingCredentials: new SigningCredentials(key ?? SigningKey, algorithm));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Süresi <b>dolmuş</b> ama imzası geçerli jeton.</summary>
    public static string MintExpiredToken(string audience = OurGoogleClientId)
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: GoogleIssuer,
            audience: audience,
            claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, "google-sub-1") },
            notBefore: now.AddHours(-2),
            // ClockSkew 2 dk; 1 saat önce dolmuş bir jeton payı fazlasıyla aşar.
            expires: now.AddHours(-1),
            signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.RsaSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>
/// Sahte anahtar sunucusu. <see cref="Keys"/> boş bırakılırsa "JWKS'e ulaşılamadı"
/// senaryosunu (fail-closed) canlandırır.
/// </summary>
internal sealed class FakeJsonWebKeySetProvider : IJsonWebKeySetProvider
{
    public List<SecurityKey> Keys { get; init; } = new() { SocialTokenTestKit.SigningKey };

    /// <summary>Kaç kez çağrıldı — "tanınmayan kid tek seferlik yenileme tetikler" iddiası için.</summary>
    public int CallCount { get; private set; }

    public Task<IReadOnlyList<SecurityKey>> GetKeysAsync(
        SocialProviderSettings settings, bool forceRefresh, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult<IReadOnlyList<SecurityKey>>(Keys);
    }
}
