using FluentAssertions;
using KadirliApp.Domain.Enums;
using KadirliApp.Infrastructure.Identity.Social;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace KadirliApp.Tests.Unit.Infrastructure;

/// <summary>
/// Faz 12.7 — <b>§7 madde 68'in kilidi.</b> Sosyal giriş jetonunun doğrulanması:
/// imza · <c>iss</c> · <b><c>aud</c></b> · süre · algoritma · <c>sub</c>.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Bu dosyanın var olma sebebi <c>aud</c> testidir.</b> Sosyal girişin bir numaralı
/// gerçek zafiyeti odur: doğrulanmazsa <i>başka bir uygulamanın</i> Google jetonu bizde de
/// geçerli olur — imza doğru, issuer doğru, süre doğru ve <b>hesap ele geçirilmiş</b> olur.
/// Hiçbir hata görünmez, log temiz kalır.
/// </para>
/// <para>
/// 🔑 Container gerektirmez ve <b>ağa çıkmaz</b>: gerçek RSA anahtarıyla imzalanmış gerçek
/// biçimli jetonlar, sahte bir anahtar sunucusu üzerinden doğrulanır
/// (<see cref="SocialTokenTestKit"/>).
/// </para>
/// </remarks>
public class SocialTokenVerifierTests
{
    private static JwksSocialTokenVerifier Build(
        IJsonWebKeySetProvider? keys = null,
        params SocialProviderSettings[] providers)
    {
        var configured = providers.Length > 0
            ? providers
            : new[] { SocialTokenTestKit.GoogleSettings(), SocialTokenTestKit.AppleSettings() };

        return new JwksSocialTokenVerifier(
            configured,
            keys ?? new FakeJsonWebKeySetProvider(),
            NullLogger<JwksSocialTokenVerifier>.Instance);
    }

    // ───────────────────────── mutlu yol ─────────────────────────

    [Fact]
    public async Task ValidGoogleToken_YieldsTheIdentity()
    {
        var payload = await Build().VerifyAsync(
            SocialProviders.Google, SocialTokenTestKit.MintToken(), CancellationToken.None);

        payload.Should().NotBeNull();
        payload!.Provider.Should().Be(SocialProviders.Google);
        payload.ProviderUserId.Should().Be("google-sub-1");
        payload.Email.Should().Be("vatandas@ornek.com");
        payload.EmailVerified.Should().BeTrue();
        payload.DisplayName.Should().Be("Ayşe Yılmaz");
    }

    [Fact]
    public async Task ValidAppleToken_YieldsTheIdentity()
    {
        var token = SocialTokenTestKit.MintToken(
            issuer: SocialTokenTestKit.AppleIssuer,
            audience: SocialTokenTestKit.OurAppleBundleId,
            subject: "apple-sub-1",
            // Apple adı id_token'da GÖNDERMEZ — ön doldurmanın boş kalması normaldir.
            name: null);

        var payload = await Build().VerifyAsync(SocialProviders.Apple, token, CancellationToken.None);

        payload.Should().NotBeNull();
        payload!.Provider.Should().Be(SocialProviders.Apple);
        payload.ProviderUserId.Should().Be("apple-sub-1");
        payload.DisplayName.Should().BeNull();
    }

    // ───────────────── 🔴 aud — bu fazın bir numaralı kuralı ─────────────────

    /// <summary>
    /// 🔴 <b>§7 madde 68.</b> Başka bir uygulamanın client id'sine kesilmiş jeton
    /// <b>reddedilmeli</b>. Jeton her açıdan geçerli: Google imzalamış, issuer doğru,
    /// süresi dolmamış. Tek sorun <b>bizim için kesilmemiş</b> olması.
    /// </summary>
    [Fact]
    public async Task TokenIssuedForAnotherApp_IsRejected_EvenThoughItsSignatureIsValid()
    {
        var token = SocialTokenTestKit.MintToken(audience: SocialTokenTestKit.SomeoneElsesClientId);

        var payload = await Build().VerifyAsync(SocialProviders.Google, token, CancellationToken.None);

        payload.Should().BeNull(
            "aud doğrulanmazsa saldırgan KENDİ uygulamasına giren kurbanın jetonuyla " +
            "bizim hesabına girer — imza, issuer ve süre doğru olduğu için hiçbir yerde " +
            "hata görünmez (§7 madde 68)");
    }

    /// <summary>
    /// 🔑 <b>Yukarıdaki iddianın İKİNCİ YÖNÜ — ve bu test olmadan birincisi zayıftır.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tek başına *"yanlış <c>aud</c>'lu jeton reddedilir"* iddiası, <b>hiçbir jetonu kabul
    /// etmeyen</b> bir gerçeklemede de yeşil kalır — yani reddin sebebinin gerçekten
    /// <c>aud</c> kontrolü olduğunu kanıtlamaz. Burada <b>birebir aynı jeton</b>, yalnızca
    /// yapılandırılmış <c>aud</c> listesine eklendiğinde <b>kabul ediliyor</b>: iki test
    /// birlikte kontrolün canlı olduğunu ve <b>tam olarak o kontrolün</b> reddettiğini
    /// gösteriyor.
    /// </para>
    /// <para>
    /// 📌 Bu, §7 madde 50'nin dersinin uygulanışı (*"çizilmeyen kadar çizilen de
    /// denetlenmeli"*) ve 12.14'ün *"taşma testi yazmak yetmez"* bulgusunun aynı ailesi.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task TheSameToken_IsAccepted_OnceItsAudienceIsOneOfOurs()
    {
        var token = SocialTokenTestKit.MintToken(audience: SocialTokenTestKit.SomeoneElsesClientId);

        var strict = Build(null, SocialTokenTestKit.GoogleSettings());
        var permissive = Build(null, SocialTokenTestKit.GoogleSettings(
            SocialTokenTestKit.OurGoogleClientId, SocialTokenTestKit.SomeoneElsesClientId));

        (await strict.VerifyAsync(SocialProviders.Google, token, CancellationToken.None))
            .Should().BeNull();

        (await permissive.VerifyAsync(SocialProviders.Google, token, CancellationToken.None))
            .Should().NotBeNull(
                "aynı jeton yalnız aud listesi değiştiği için kabul edildi — yani reddin " +
                "sebebi GERÇEKTEN aud kontrolü, tesadüfi bir başka kapı değil");
    }

    /// <summary>Birden çok client id (Android + iOS + Web) yapılandırılabilir; hepsi geçerli.</summary>
    [Fact]
    public async Task AnyConfiguredAudience_IsAccepted()
    {
        const string second = "222-ios.apps.googleusercontent.com";
        var verifier = Build(null, SocialTokenTestKit.GoogleSettings(
            SocialTokenTestKit.OurGoogleClientId, second));

        var payload = await verifier.VerifyAsync(
            SocialProviders.Google, SocialTokenTestKit.MintToken(audience: second), CancellationToken.None);

        payload.Should().NotBeNull();
    }

    // ───────────────────────── diğer kapılar ─────────────────────────

    [Fact]
    public async Task TokenFromAnotherIssuer_IsRejected()
    {
        var token = SocialTokenTestKit.MintToken(issuer: "https://kotu-adam.example");

        var payload = await Build().VerifyAsync(SocialProviders.Google, token, CancellationToken.None);

        payload.Should().BeNull();
    }

    /// <summary>
    /// 🔴 Apple jetonu Google olarak sunulamaz. Sağlayıcılar ayrı issuer/aud kümeleri
    /// taşıdığı için bu kendiliğinden düşüyor — ama <b>kendiliğinden</b> olan şeyler
    /// yarın bir "ortak ayar" refaktörüyle sessizce kaybolur.
    /// </summary>
    [Fact]
    public async Task AppleTokenPresentedAsGoogle_IsRejected()
    {
        var appleToken = SocialTokenTestKit.MintToken(
            issuer: SocialTokenTestKit.AppleIssuer, audience: SocialTokenTestKit.OurAppleBundleId);

        var payload = await Build().VerifyAsync(SocialProviders.Google, appleToken, CancellationToken.None);

        payload.Should().BeNull();
    }

    [Fact]
    public async Task ExpiredToken_IsRejected()
    {
        var payload = await Build().VerifyAsync(
            SocialProviders.Google, SocialTokenTestKit.MintExpiredToken(), CancellationToken.None);

        payload.Should().BeNull();
    }

    /// <summary>JWKS'te olmayan bir anahtarla imzalanmış jeton — yani sahte jeton.</summary>
    [Fact]
    public async Task TokenSignedWithAnUnknownKey_IsRejected()
    {
        var token = SocialTokenTestKit.MintToken(key: SocialTokenTestKit.ForeignKey);

        var payload = await Build().VerifyAsync(SocialProviders.Google, token, CancellationToken.None);

        payload.Should().BeNull();
    }

    /// <summary>
    /// 🔴 <b>Algoritma sabitlenmiş.</b> Sabitlenmezse jetonun kendi <c>alg</c> başlığı
    /// belirleyici olur — JWT'nin klasik HS256/RS256 karıştırma zafiyeti.
    /// </summary>
    [Fact]
    public async Task TokenSignedWithASymmetricAlgorithm_IsRejected()
    {
        var symmetric = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes("bu-anahtar-en-az-otuz-iki-karakter-uzunlugunda"));

        var token = SocialTokenTestKit.MintToken(
            key: symmetric, algorithm: SecurityAlgorithms.HmacSha256);

        var payload = await Build().VerifyAsync(SocialProviders.Google, token, CancellationToken.None);

        payload.Should().BeNull();
    }

    /// <summary>
    /// 🔴 <c>sub</c> olmadan kimlik yoktur. Boş geçilseydi <c>sub</c>'ı olmayan iki farklı
    /// kişi <b>aynı</b> kimliğe eşlenir ve benzersiz indeks yüzünden ikincisi
    /// <b>birincinin hesabına</b> girerdi.
    /// </summary>
    [Fact]
    public async Task TokenWithoutSubject_IsRejected()
    {
        var payload = await Build().VerifyAsync(
            SocialProviders.Google, SocialTokenTestKit.MintToken(subject: null), CancellationToken.None);

        payload.Should().BeNull();
    }

    /// <summary>
    /// 🔴 <b>Fail-closed.</b> Anahtar sunucusuna ulaşılamadığında doğrulama <i>yapılamaz</i> —
    /// ve yapılamayan doğrulama "geçti" sayılamaz. (Redis'teki fail-<i>open</i> kararının,
    /// §7 madde 36, bilinçli tersi.)
    /// </summary>
    [Fact]
    public async Task WhenTheKeyServerIsUnreachable_TokensAreRejected_NotAccepted()
    {
        var verifier = Build(new FakeJsonWebKeySetProvider { Keys = new List<SecurityKey>() });

        var payload = await verifier.VerifyAsync(
            SocialProviders.Google, SocialTokenTestKit.MintToken(), CancellationToken.None);

        payload.Should().BeNull();
    }

    /// <summary>
    /// Anahtar döndürmesi: ilk deneme tutmazsa <b>bir kez</b> zorla tazelenir. Bu olmasaydı
    /// Google anahtarını her değiştirdiğinde önbellek TTL'i kadar süre boyunca
    /// <b>hiç kimse</b> giriş yapamaz ve logda yalnız "geçersiz jeton" görünürdü.
    /// </summary>
    [Fact]
    public async Task WhenTheFirstAttemptFails_TheKeysAreRefreshedExactlyOnce()
    {
        var keys = new FakeJsonWebKeySetProvider();
        var verifier = Build(keys);

        await verifier.VerifyAsync(
            SocialProviders.Google,
            SocialTokenTestKit.MintToken(key: SocialTokenTestKit.ForeignKey),
            CancellationToken.None);

        keys.CallCount.Should().Be(2, "bir kez normal, bir kez zorla tazeleme");
    }

    // ───────────────────────── sağlayıcı kapısı ─────────────────────────

    /// <summary>
    /// 🔴 <b>Client id'si olmayan sağlayıcı KAPALIDIR.</b> "Boş liste = herkesi kabul et"
    /// yorumu, <c>aud</c> deliğinin en geniş hâli olurdu.
    /// </summary>
    [Fact]
    public async Task ProviderWithoutClientIds_IsDisabled_AndRejectsEverything()
    {
        var verifier = Build(null, SocialTokenTestKit.AppleSettings());

        verifier.IsEnabled(SocialProviders.Google).Should().BeFalse();
        verifier.IsEnabled(SocialProviders.Apple).Should().BeTrue();
        verifier.EnabledProviders.Should().ContainSingle().Which.Should().Be(SocialProviders.Apple);

        var payload = await verifier.VerifyAsync(
            SocialProviders.Google, SocialTokenTestKit.MintToken(), CancellationToken.None);

        payload.Should().BeNull("kapalı sağlayıcı doğrulama YAPAMAZ, yani jetonu kabul edemez");
    }

    [Theory]
    [InlineData("gogle")]
    [InlineData("facebook")]
    [InlineData("")]
    public async Task UnknownProvider_IsRejected(string provider)
    {
        var payload = await Build().VerifyAsync(
            provider, SocialTokenTestKit.MintToken(), CancellationToken.None);

        payload.Should().BeNull();
    }

    [Fact]
    public async Task EmptyToken_IsRejected()
    {
        var payload = await Build().VerifyAsync(SocialProviders.Google, "  ", CancellationToken.None);

        payload.Should().BeNull();
    }

    /// <summary>
    /// <c>email_verified</c> <b>yoksa</b> "doğrulanmamış" sayılır — şüphede kalınca DAR taraf.
    /// (Bu alan hiçbir eşleştirmede kullanılmıyor, ama varsayımı değil ölçümü saklıyoruz.)
    /// </summary>
    [Fact]
    public async Task UnverifiedEmail_IsCarriedAsUnverified()
    {
        var payload = await Build().VerifyAsync(
            SocialProviders.Google,
            SocialTokenTestKit.MintToken(emailVerified: false),
            CancellationToken.None);

        payload.Should().NotBeNull();
        payload!.EmailVerified.Should().BeFalse();
    }
}
