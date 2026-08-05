using FluentAssertions;
using KadirliApp.Application.Common.Observability;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Observability;

/// <summary>
/// Faz 12.1 — hata kaydına PII sızmasını önleyen maskeleme.
///
/// Bu bir kozmetik değil güvenlik sınırı: hata kayıtları panelden görülüyor, **CSV olarak
/// dışa aktarılıyor** ve 90 gün saklanıyor. Bir kez yazıldıktan sonra "sonra temizleriz"
/// diye bir şey yok (<c>CODE_REVIEW_CHECKLIST</c> §7).
/// </summary>
public class SensitiveDataMaskerTests
{
    [Fact]
    public void Phone_InQueryString_IsMasked()
    {
        // Gerçek senaryo: OTP akışında telefon sorgu dizesine düşebiliyor.
        SensitiveDataMasker.MaskPath("/v1/auth/login?phone=+905001112233")
            .Should().Be("/v1/auth/login?phone=***");
    }

    [Theory]
    [InlineData("otp")]
    [InlineData("token")]
    [InlineData("password")]
    [InlineData("email")]
    [InlineData("secret")]
    public void SensitiveKeys_AreMasked(string key)
    {
        SensitiveDataMasker.MaskPath($"/v1/x?{key}=gizli")
            .Should().Be($"/v1/x?{key}=***");
    }

    [Fact]
    public void SensitiveKey_IsMatchedByContent_NotExactName()
    {
        // `userPhone`, `access_token` gibi türevler de yakalanmalı — hassas veri
        // anahtarın tam adına değil içeriğine bağlıdır.
        SensitiveDataMasker.MaskPath("/v1/x?userPhone=+905001112233&access_token=abc")
            .Should().Be("/v1/x?userPhone=***&access_token=***");
    }

    [Fact]
    public void NonSensitiveParameters_AreKept()
    {
        // Tanılama değeri olan parametreler korunmalı — hepsini maskelemek
        // "hata hangi filtrede oluştu?" sorusunu cevapsız bırakırdı.
        SensitiveDataMasker.MaskPath("/v1/ads?page=2&status=pending")
            .Should().Be("/v1/ads?page=2&status=pending");
    }

    [Fact]
    public void MixedParameters_OnlySensitiveOnesAreMasked()
    {
        SensitiveDataMasker.MaskPath("/v1/ads?page=2&phone=+905001112233&sort=title_asc")
            .Should().Be("/v1/ads?page=2&phone=***&sort=title_asc");
    }

    [Fact]
    public void PathWithoutQuery_IsUntouched()
    {
        SensitiveDataMasker.MaskPath("/v1/ads/3f2a9c14").Should().Be("/v1/ads/3f2a9c14");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NullOrEmpty_DoesNotThrow(string? value)
    {
        var act = () => SensitiveDataMasker.MaskPath(value);
        act.Should().NotThrow();
    }
}
