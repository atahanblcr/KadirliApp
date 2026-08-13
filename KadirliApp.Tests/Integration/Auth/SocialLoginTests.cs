using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Enums;
using KadirliApp.Infrastructure.Persistence;
using KadirliApp.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Auth;

/// <summary>
/// Faz 12.7 — sosyal girişin uçtan uca doğrulaması (gerçek Postgres + <b>gerçek</b> jeton
/// doğrulayıcı, yalnız anahtar sunucusu sahte).
/// </summary>
/// <remarks>
/// Kilitlediği görünmez sözleşmeler: <b>68</b> (<c>aud</c>), <b>69</b> (e-posta eşleşmesiyle
/// otomatik bağlama yasak), <b>70</b> (sosyal giriş OTP'yi atlamaz).
/// </remarks>
public class SocialLoginTests : IClassFixture<SocialLoginWebApplicationFactory>
{
    private readonly SocialLoginWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SocialLoginTests(SocialLoginWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    // ─────────────────────────── yardımcılar ───────────────────────────

    /// <summary>Her test kendi kimliğini üretir — paylaşılan veritabanında çakışmasınlar.</summary>
    private static string NewSub() => $"google-sub-{Guid.NewGuid():N}";

    /// <summary>Her test kendi telefonunu üretir (telefon unique).</summary>
    private static string NewPhone() => $"+9055{Random.Shared.Next(10_000_000, 99_999_999)}";

    private async Task<Guid> NeighborhoodIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Neighborhoods.Where(n => n.IsActive).Select(n => n.Id).FirstAsync();
    }

    private async Task<JsonElement> PostSocialAsync(string idToken, string provider = "google")
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/social", new { provider, idToken });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").Clone();
    }

    /// <summary>DevMode OTP ile telefon doğrular ve telefonlu kayıt jetonunu döner.</summary>
    private async Task<string> PhoneTempTokenAsync(string phone)
    {
        (await _client.PostAsJsonAsync("/v1/auth/login", new { phone }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var verify = await _client.PostAsJsonAsync("/v1/auth/verify-otp", new { phone, otp = "123456" });
        verify.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await verify.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").GetProperty("tempToken").GetString()!;
    }

    /// <summary>Sosyal giriş → OTP → kayıt zincirinin tamamını koşar; access token döner.</summary>
    private async Task<(string Access, Guid UserId, string Phone)> RegisterViaSocialAsync(string sub)
    {
        var social = await PostSocialAsync(SocialTokenTestKit.MintToken(subject: sub));
        social.GetProperty("isNewUser").GetBoolean().Should().BeTrue();
        var socialToken = social.GetProperty("socialToken").GetString()!;

        var phone = NewPhone();
        var register = await _client.PostAsJsonAsync("/v1/auth/register", new
        {
            tempToken = await PhoneTempTokenAsync(phone),
            username = $"kullanici{Guid.NewGuid():N}"[..20],
            primaryNeighborhoodId = await NeighborhoodIdAsync(),
            age = 30,
            socialToken
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        var access = doc.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
        return (access, DecodeUserId(access), phone);
    }

    private static Guid DecodeUserId(string jwt)
    {
        var payload = jwt.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
        return Guid.Parse(doc.RootElement.GetProperty("user_id").GetString()!);
    }

    private HttpRequestMessage Authorized(HttpMethod method, string url, string token, HttpContent? body = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = body };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    // ─────────────────────────── yeni kullanıcı ───────────────────────────

    /// <summary>
    /// 🔴 <b>§7 madde 70.</b> Hesabı olmayan bir sosyal kullanıcı <b>oturum ALMAZ</b> —
    /// yalnız kayıt taşıyıcısı alır ve o taşıyıcı <b>telefon içermez</b>.
    /// </summary>
    [Fact]
    public async Task NewSocialUser_GetsARegistrationCarrier_NotASession()
    {
        var data = await PostSocialAsync(SocialTokenTestKit.MintToken(subject: NewSub()));

        data.GetProperty("isNewUser").GetBoolean().Should().BeTrue();
        data.GetProperty("socialToken").GetString().Should().NotBeNullOrWhiteSpace();

        data.TryGetProperty("accessToken", out _).Should().BeFalse(
            "sosyal giriş OTP'yi ATLAMAZ — telefon doğrulanmadan oturum açılmaz");

        var prefill = data.GetProperty("prefill");
        prefill.GetProperty("email").GetString().Should().Be("vatandas@ornek.com");
        prefill.GetProperty("displayName").GetString().Should().Be("Ayşe Yılmaz");
    }

    /// <summary>
    /// 🔴 <b>§7 madde 70'in ikinci ayağı.</b> Sosyal jeton <b>tek başına</b> kayıt
    /// tamamlayamaz: telefonlu kayıt jetonunun yerine konursa reddedilir.
    /// </summary>
    [Fact]
    public async Task SocialToken_CannotStandInForThePhoneRegistrationToken()
    {
        var data = await PostSocialAsync(SocialTokenTestKit.MintToken(subject: NewSub()));
        var socialToken = data.GetProperty("socialToken").GetString()!;

        var register = await _client.PostAsJsonAsync("/v1/auth/register", new
        {
            tempToken = socialToken, // ⚠️ yanlış yere konuldu
            username = $"k{Guid.NewGuid():N}"[..20],
            primaryNeighborhoodId = await NeighborhoodIdAsync(),
            age = 30
        });

        register.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AfterRegistering_TheIdentityIsLinked_AndTheSecondLoginIsASingleTap()
    {
        var sub = NewSub();
        var (_, userId, _) = await RegisterViaSocialAsync(sub);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var link = await db.UserIdentities.SingleAsync(x => x.ProviderUserId == sub);
            link.UserId.Should().Be(userId);
            link.Provider.Should().Be(SocialProviders.Google);
            link.Email.Should().Be("vatandas@ornek.com");
        }

        // İkinci giriş: artık tek dokunuş — OTP yok, doğrudan oturum.
        var second = await PostSocialAsync(SocialTokenTestKit.MintToken(subject: sub));
        second.GetProperty("isNewUser").GetBoolean().Should().BeFalse();
        second.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        DecodeUserId(second.GetProperty("accessToken").GetString()!).Should().Be(userId);
    }

    // ─────────────────────── 🔴 aud — uçtan uca ───────────────────────

    /// <summary>
    /// 🔴 <b>§7 madde 68, uçtan uca.</b> Başka bir uygulamaya kesilmiş jeton <b>ucun
    /// kendisinde</b> reddedilmeli — birim testi doğrulayıcıyı kanıtlıyor, bu test
    /// doğrulayıcının <b>gerçekten devrede olduğunu</b> kanıtlıyor.
    /// </summary>
    [Fact]
    public async Task TokenIssuedForAnotherApp_IsRejectedByTheEndpoint()
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/social", new
        {
            provider = "google",
            idToken = SocialTokenTestKit.MintToken(audience: SocialTokenTestKit.SomeoneElsesClientId)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ForgedToken_IsRejectedByTheEndpoint()
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/social", new
        {
            provider = "google",
            idToken = SocialTokenTestKit.MintToken(key: SocialTokenTestKit.ForeignKey)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// 🔴 <b>Geçersiz jeton denemesi PANELE DÜŞER.</b> Yanlış <c>aud</c>'lu jetonların
    /// birikmesi, başka bir uygulamanın jetonuyla giriş girişiminin ta kendisidir —
    /// hiçbir yere yazılmasaydı bu saldırı <b>tamamen görünmez</b> olurdu.
    /// </summary>
    [Fact]
    public async Task ARejectedSocialAttempt_IsRecordedForThePanel()
    {
        await _client.PostAsJsonAsync("/v1/auth/social", new
        {
            provider = "google",
            idToken = SocialTokenTestKit.MintToken(audience: SocialTokenTestKit.SomeoneElsesClientId)
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var recorded = await db.LoginAttempts
            .Where(x => x.Channel == LoginChannels.Social && !x.Succeeded)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        recorded.Should().NotBeNull();
        recorded!.FailureReason.Should().Be(LoginFailureReasons.BadSocialToken);
    }

    // ─────────────────── 🔴 e-posta ile otomatik bağlama YOK ───────────────────

    /// <summary>
    /// 🔴 <b>§7 madde 69 — bu fazın en çekici ve en tehlikeli kısayolu.</b>
    /// Var olan bir hesabın e-postası ile <b>aynı</b> e-postayı taşıyan bir Google jetonu
    /// gelir; hesap <b>bulunmamalı</b>, kullanıcı yeni kayıt akışına düşmeli.
    /// <c>User.Email</c> panelden elle giriliyor ve <b>hiç doğrulanmıyor</b> — otomatik
    /// bağlansaydı saldırgan kurbanın e-postasıyla bir Google hesabı açıp doğrudan
    /// o hesaba girerdi.
    /// </summary>
    [Fact]
    public async Task AMatchingEmail_DoesNotLinkTheAccountAutomatically()
    {
        var (access, userId, _) = await RegisterViaSocialAsync(NewSub());

        const string knownEmail = "eslesen@ornek.com";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Users.Where(x => x.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Email, knownEmail));
        }

        // BAŞKA bir Google hesabı, ama AYNI e-posta.
        var data = await PostSocialAsync(
            SocialTokenTestKit.MintToken(subject: NewSub(), email: knownEmail));

        data.GetProperty("isNewUser").GetBoolean().Should().BeTrue(
            "doğrulanmamış bir e-posta eşleşmesi hesap bağlamaz — bu bir hesap ele " +
            "geçirme yoludur (§7 madde 69)");

        access.Should().NotBeNullOrWhiteSpace();
    }

    // ─────────────────────────── ban / pasiflik ───────────────────────────

    [Fact]
    public async Task ABannedUser_CannotGetInThroughSocialLogin()
    {
        var sub = NewSub();
        var (_, userId, _) = await RegisterViaSocialAsync(sub);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Users.Where(x => x.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsBanned, true));
        }

        var response = await _client.PostAsJsonAsync("/v1/auth/social", new
        {
            provider = "google",
            idToken = SocialTokenTestKit.MintToken(subject: sub)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "ayrı bir giriş kapısı moderasyon kararını sessizce delerdi");
    }

    // ─────────────────────────── sağlayıcı kapısı ───────────────────────────

    /// <summary>
    /// Apple bu ortamda <b>yapılandırılmamış</b> (12.8'e kadar canlıdaki durum bu olacak):
    /// uç "geçersiz jeton" demez, <b>anlamlı</b> bir hata döner.
    /// </summary>
    [Fact]
    public async Task ADisabledProvider_SaysSo_InsteadOfPretendingTheTokenIsBad()
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/social", new
        {
            provider = "apple",
            idToken = SocialTokenTestKit.MintToken(
                issuer: SocialTokenTestKit.AppleIssuer, audience: SocialTokenTestKit.OurAppleBundleId)
        });

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("SOCIAL_PROVIDER_DISABLED");
    }

    [Fact]
    public async Task AnUnknownProvider_IsRejected()
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/social", new
        {
            provider = "facebook",
            idToken = SocialTokenTestKit.MintToken()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─────────────────────────── bağla / çöz ───────────────────────────

    [Fact]
    public async Task AUserCanLinkAndUnlinkTheirOwnAccount_AndTheProfileShowsIt()
    {
        // Sosyal bağlantısı OLMAYAN bir hesap: sırf OTP ile kayıt.
        var phone = NewPhone();
        var register = await _client.PostAsJsonAsync("/v1/auth/register", new
        {
            tempToken = await PhoneTempTokenAsync(phone),
            username = $"k{Guid.NewGuid():N}"[..20],
            primaryNeighborhoodId = await NeighborhoodIdAsync(),
            age = 25
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        using var registerDoc = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        var access = registerDoc.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;

        // Profilde henüz bağlantı yok.
        (await ProfileIdentitiesAsync(access)).Should().BeEmpty();

        var sub = NewSub();
        var link = await _client.SendAsync(Authorized(
            HttpMethod.Post, "/v1/users/me/identities", access,
            JsonContent.Create(new { provider = "google", idToken = SocialTokenTestKit.MintToken(subject: sub) })));
        link.StatusCode.Should().Be(HttpStatusCode.OK);

        (await ProfileIdentitiesAsync(access)).Should().ContainSingle()
            .Which.GetProperty("provider").GetString().Should().Be("google");

        // 🔑 SON bağlantı da çözülebilir — telefon çıpa olduğu için kullanıcı kilitlenmez.
        var unlink = await _client.SendAsync(
            Authorized(HttpMethod.Delete, "/v1/users/me/identities/google", access));
        unlink.StatusCode.Should().Be(HttpStatusCode.OK);

        (await ProfileIdentitiesAsync(access)).Should().BeEmpty();

        // 🔑 Telefon + OTP hâlâ çalışıyor: kullanıcı hesabından KİLİTLENMEDİ.
        // (⚠️ OTP tek kullanımlık — yeni kod istemeden verify çağrılamaz.)
        (await _client.PostAsJsonAsync("/v1/auth/login", new { phone }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var stillWorks = await _client.PostAsJsonAsync("/v1/auth/verify-otp", new { phone, otp = "123456" });
        stillWorks.StatusCode.Should().Be(HttpStatusCode.OK);

        using var stillWorksDoc = JsonDocument.Parse(await stillWorks.Content.ReadAsStringAsync());
        stillWorksDoc.RootElement.GetProperty("data").GetProperty("isNewUser").GetBoolean()
            .Should().BeFalse("hesap duruyor — çözülen yalnız sosyal bağlantıydı");
    }

    /// <summary>
    /// 🔴 Bir sosyal hesap iki KadirliApp hesabına bağlanamaz — ve ikinci deneme
    /// <b>sessizce taşımaz</b>, çatışmayı söyler. Taşısaydı sosyal hesabına erişimi olan
    /// biri dilediği hesaba geçebilirdi.
    /// </summary>
    [Fact]
    public async Task AnIdentityAlreadyLinkedElsewhere_CannotBeStolen()
    {
        var sub = NewSub();
        await RegisterViaSocialAsync(sub); // 1. hesap bu kimliği aldı

        var (secondAccess, _, _) = await RegisterViaSocialAsync(NewSub()); // 2. hesap

        var link = await _client.SendAsync(Authorized(
            HttpMethod.Post, "/v1/users/me/identities", secondAccess,
            JsonContent.Create(new { provider = "google", idToken = SocialTokenTestKit.MintToken(subject: sub) })));

        link.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    /// <summary>Zaten bağlı olmayan bir sağlayıcıyı çözmek hata değildir (idempotent düğme).</summary>
    [Fact]
    public async Task UnlinkingSomethingThatIsNotLinked_IsNotAnError()
    {
        var (access, _, _) = await RegisterViaSocialAsync(NewSub());

        var response = await _client.SendAsync(
            Authorized(HttpMethod.Delete, "/v1/users/me/identities/apple", access));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("false");
    }

    // ─────────────────────────── hesap silme ───────────────────────────

    /// <summary>
    /// 🔴 Hesap silinince kimlik satırları da <b>fiziksel olarak</b> gider. Kalsalardı
    /// iki şey birden bozulurdu: kişisel veri (sağlayıcı kimliği + e-posta) anonimleştirme
    /// sözüne rağmen tabloda kalır, <b>ve</b> benzersiz indeks yüzünden o kişi aynı Google
    /// hesabıyla <b>bir daha asla</b> kayıt olamazdı.
    /// </summary>
    [Fact]
    public async Task DeletingTheAccount_AlsoRemovesTheSocialIdentities_SoTheyCanRegisterAgain()
    {
        var sub = NewSub();
        var (access, userId, _) = await RegisterViaSocialAsync(sub);

        var delete = await _client.SendAsync(Authorized(HttpMethod.Delete, "/v1/users/me", access));
        delete.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.UserIdentities.AnyAsync(x => x.UserId == userId)).Should().BeFalse();
        }

        // Aynı Google hesabı yeniden kayıt açabiliyor.
        var again = await PostSocialAsync(SocialTokenTestKit.MintToken(subject: sub));
        again.GetProperty("isNewUser").GetBoolean().Should().BeTrue();
    }

    private async Task<List<JsonElement>> ProfileIdentitiesAsync(string access)
    {
        var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/v1/users/me", access));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").GetProperty("linkedIdentities")
            .EnumerateArray().Select(x => x.Clone()).ToList();
    }
}
