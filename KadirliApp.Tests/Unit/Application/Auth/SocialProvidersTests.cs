using FluentAssertions;
using KadirliApp.Domain.Enums;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Auth;

/// <summary>
/// Faz 12.7 — sağlayıcı adının normalleştirilmesi.
/// </summary>
/// <remarks>
/// 🔴 <b>Bu testin tek amacı bir DAVRANIŞ FARKINI kilitlemektir.</b> Projedeki kardeş sınıf
/// <see cref="TransportVehicleTypes"/> bilinmeyen değeri <b>varsayılana düşürür</b>
/// (§7 madde 47 — bir yazım hatası listeyi boşaltmasın). Burada kural <b>tam tersi</b>:
/// bilinmeyen sağlayıcı <c>null</c> olur. Sebep, orada bedelin bir görüntüleme tercihi,
/// burada ise bir <b>güven kararı</b> olması: varsayılana düşülseydi
/// <c>?provider=gogle</c> yazan (ya da yarın eklenecek üçüncü bir sağlayıcıyı deneyen)
/// bir istemcinin jetonu <b>Google'ınmış gibi</b> doğrulanmaya çalışılırdı.
///
/// ⚠️ Biri "tutarlılık" gerekçesiyle bu sınıfı kardeşine benzetirse test kırmızıya döner —
/// kilidin var olma sebebi budur.
/// </remarks>
public class SocialProvidersTests
{
    [Theory]
    [InlineData("google", "google")]
    [InlineData("GOOGLE", "google")]
    [InlineData("  Google  ", "google")]
    [InlineData("apple", "apple")]
    [InlineData("Apple", "apple")]
    public void KnownProviders_AreCanonicalised(string raw, string expected)
        => SocialProviders.Normalize(raw).Should().Be(expected);

    [Theory]
    [InlineData("gogle")]
    [InlineData("facebook")]
    [InlineData("google ile giriş")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UnknownProvider_IsNull_NotADefault(string? raw)
        => SocialProviders.Normalize(raw).Should().BeNull(
            "kimlik doğrulamada 'şüphede kalınca varsayılana düş' kuralı GEÇERSİZDİR: " +
            "varsayılan burada bir güven kararıdır, bir görüntüleme tercihi değil");

    [Fact]
    public void All_ContainsExactlyTheProvidersWeVerify()
        => SocialProviders.All.Should().BeEquivalentTo(new[] { "google", "apple" });

    /// <summary>
    /// Değerler DTO'ya ve <c>user_identities.provider</c> kolonuna çıkıyor — yani
    /// <b>kontrattır</b>. Yeniden adlandırılırsa mağazadaki eski sürümlerin gönderdiği
    /// <c>"google"</c> tanınmaz ve sosyal giriş sessizce "geçersiz sağlayıcı"ya döner.
    /// </summary>
    [Fact]
    public void ProviderValues_AreLowercaseAndStable()
    {
        SocialProviders.Google.Should().Be("google");
        SocialProviders.Apple.Should().Be("apple");
    }

    [Fact]
    public void IsKnown_MirrorsNormalize()
    {
        SocialProviders.IsKnown("Google").Should().BeTrue();
        SocialProviders.IsKnown("twitter").Should().BeFalse();
    }
}
