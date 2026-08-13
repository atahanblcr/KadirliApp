using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Enums;
using KadirliApp.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
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

    /// <summary>
    /// 🐛 <b>ASIL KİLİT BURASI — ve bu test bir BOZMA TURUNDAN doğdu.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Yukarıdaki test (<see cref="SocialTempToken_CannotBeUsedAsThePhoneRegistrationToken"/>)
    /// doğru davranışı ölçüyor ama <b>yanlış sebepten</b> geçiyordu: <c>token_type</c> kontrolü
    /// <c>ValidateTempToken</c>'dan tamamen silindiğinde bile <b>yeşil kaldı</b>. Sebebi şu —
    /// bugünkü sosyal jetonun <c>phone</c> claim'i <b>zaten yok</b>, yani metot tür kontrolü
    /// olmasa da <c>null</c> döndürüyor. Kısacası: sözleşme <i>"türler ayrıdır"</i> diyordu,
    /// test ise yalnızca <i>"sosyal jetonda telefon yok"</i>u ölçüyordu.
    /// </para>
    /// <para>
    /// 🔴 <b>Neden bu önemli:</b> bugün iki bağımsız sebep bizi koruyor, ama biri
    /// <b>tesadüfi</b>. Yarın biri <c>GenerateSocialTempToken</c>'a bir <c>phone</c> claim'i
    /// eklerse (ör. *"sağlayıcıdan gelen telefonu ön dolduralım"*) ya da iki üreticiyi ortak
    /// bir yardımcıya çekerse, ayakta kalan <b>tek</b> koruma <c>token_type</c> olur — ve
    /// onu silen değişikliği hiçbir test yakalamazdı. Sonuç §7 madde 70'in tam olarak
    /// engellediği şey olurdu: <b>OTP'siz kayıt</b>.
    /// </para>
    /// <para>
    /// 🔑 Bu yüzden burada jeton <b>elle</b> üretiliyor: sosyal türde <b>ama telefon taşıyan</b>
    /// bir jeton, yani "tesadüfi koruma"nın devre dışı kaldığı hâl. İddia artık doğrudan
    /// <c>token_type</c> ayrımını tutuyor — bozma turunda kırmızıya döndüğü <b>ölçüldü</b>.
    /// 📌 Bu, projenin beş fazda beş kez patlayan *"iddiası zayıf test"* sınıfının
    /// yeni bir tekrarı ve bu sefer **bozma turu tarafından yakalandı**.
    /// </para>
    /// </remarks>
    [Fact]
    public void ASocialTypedToken_IsRejectedAsAPhoneToken_EvenWhenItCarriesAPhoneClaim()
    {
        var forged = MintToken(
            tokenType: "social_registration",
            extra: new Claim("phone", "+905001112233"));

        Build().ValidateTempToken(forged).Should().BeNull(
            "ayrımı yapan şey telefonun YOKLUĞU değil, token_type kontrolüdür; " +
            "silinirse sosyal giriş OTP'yi atlar (§7 madde 70)");
    }

    /// <summary>
    /// Aynı kilidin ters yönü: doğru türü taşıyan jeton <b>kabul ediliyor</b>. İki yön
    /// birlikte, reddin sebebinin gerçekten <c>token_type</c> olduğunu kanıtlar — tek yönlü
    /// iddia, "hiçbir jetonu kabul etme" gerçeklemesinde de yeşil kalırdı.
    /// </summary>
    [Fact]
    public void APhoneTypedToken_WithTheSameShape_IsAccepted()
    {
        var valid = MintToken(
            tokenType: "registration",
            extra: new Claim("phone", "+905001112233"));

        Build().ValidateTempToken(valid).Should().Be("+905001112233");
    }

    /// <summary>
    /// Testin kendi jetonunu üretmesi <b>şart</b>: üretici bugün sosyal jetona telefon
    /// koymuyor, yani "tesadüfi koruma"yı devre dışı bırakan jetonu <c>JwtProvider</c>'ın
    /// genel arayüzünden elde etmek mümkün değil.
    /// </summary>
    private static string MintToken(string tokenType, Claim extra)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("test_refresh_secret_which_is_at_least_32_characters_long"));

        var token = new JwtSecurityToken(
            issuer: "KadirliApp",
            audience: "KadirliAppClients",
            claims: new[] { new Claim("token_type", tokenType), extra },
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
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
