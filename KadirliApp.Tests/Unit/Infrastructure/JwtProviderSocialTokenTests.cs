using FluentAssertions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Enums;
using KadirliApp.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace KadirliApp.Tests.Unit.Infrastructure;

/// <summary>
/// Faz 12.7 — <b>§7 madde 70'in kilidi.</b> Sosyal kayıt taşıyıcısı ile telefonlu kayıt
/// taşıyıcısı <b>birbirinin yerine geçemez</b>.
/// </summary>
/// <remarks>
/// 🔴 <b>Neden bu ayrım hayati:</b> sosyal jeton telefon taşımıyor. İki jeton türü
/// karışabilseydi (yani sosyal jeton <c>ValidateTempToken</c>'dan geçebilseydi) sosyal giriş
/// <b>OTP'yi atlar</b> hâle gelirdi: Google hesabı olan herkes telefonunu hiç doğrulatmadan
/// ilan verebilen bir hesap açardı ve moderasyonun dayandığı <i>"her hesabın doğrulanmış bir
/// telefonu vardır"</i> varsayımı sessizce çökerdi. Aynı ayrım 10.2'de refresh ↔ registration
/// arasında kurulmuştu; bu onun üçüncü ayağı.
/// </remarks>
public class JwtProviderSocialTokenTests
{
    private static JwtProvider Build() => new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:AccessSecret"] = "test_access_secret_which_is_at_least_32_characters_long",
            ["Jwt:RefreshSecret"] = "test_refresh_secret_which_is_at_least_32_characters_long",
            ["Jwt:Issuer"] = "KadirliApp",
            ["Jwt:Audience"] = "KadirliAppClients",
            ["Jwt:TempTokenMinutes"] = "30"
        }).Build());

    private static SocialIdentityPayload Payload(
        string provider = SocialProviders.Google,
        string sub = "google-sub-1",
        string? email = "vatandas@ornek.com",
        bool emailVerified = true,
        string? name = "Ayşe Yılmaz")
        => new(provider, sub, email, emailVerified, name);

    [Fact]
    public void SocialTempToken_RoundTripsEveryField()
    {
        var jwt = Build();
        var restored = jwt.ValidateSocialTempToken(jwt.GenerateSocialTempToken(Payload()));

        restored.Should().BeEquivalentTo(Payload());
    }

    /// <summary>Boş alanlar claim olarak yazılmaz; geri okunduğunda <c>null</c> kalır.</summary>
    [Fact]
    public void SocialTempToken_KeepsMissingFieldsMissing()
    {
        var jwt = Build();
        var payload = Payload(email: null, name: null, emailVerified: false);

        var restored = jwt.ValidateSocialTempToken(jwt.GenerateSocialTempToken(payload));

        restored.Should().NotBeNull();
        restored!.Email.Should().BeNull();
        restored.DisplayName.Should().BeNull();
        restored.EmailVerified.Should().BeFalse();
    }

    /// <summary>
    /// 🔴 <b>§7 madde 70.</b> Sosyal jeton, telefonlu kayıt jetonunun yerine geçemez —
    /// geçebilseydi kayıt OTP'siz tamamlanırdı.
    /// </summary>
    [Fact]
    public void SocialTempToken_CannotBeUsedAsThePhoneRegistrationToken()
    {
        var jwt = Build();
        var socialToken = jwt.GenerateSocialTempToken(Payload());

        jwt.ValidateTempToken(socialToken).Should().BeNull(
            "sosyal jeton telefon TAŞIMAZ; kayıt jetonu olarak kabul edilseydi " +
            "sosyal giriş OTP'yi atlar ve telefonsuz hesap doğardı");
    }

    /// <summary>Ters yön de kapalı: telefonlu kayıt jetonu sosyal kimlik taşıyor gibi okunamaz.</summary>
    [Fact]
    public void PhoneRegistrationToken_CannotBeUsedAsTheSocialToken()
    {
        var jwt = Build();
        var phoneToken = jwt.GenerateTempToken("+905001112233");

        jwt.ValidateSocialTempToken(phoneToken).Should().BeNull();
    }

    /// <summary>Refresh token da üçüncü bir tür — kayıt akışının hiçbir yerinde geçemez.</summary>
    [Fact]
    public void RefreshToken_IsNotASocialToken()
    {
        var jwt = Build();
        var tokens = jwt.GenerateTokens(Guid.NewGuid(), "user", "+905001112233");

        jwt.ValidateSocialTempToken(tokens.RefreshToken).Should().BeNull();
        jwt.ValidateSocialTempToken(tokens.AccessToken).Should().BeNull();
    }

    [Fact]
    public void GarbageToken_IsRejected()
    {
        Build().ValidateSocialTempToken("bu.bir.jeton.degil").Should().BeNull();
    }
}
