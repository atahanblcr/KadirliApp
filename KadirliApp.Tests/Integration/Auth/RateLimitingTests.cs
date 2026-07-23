using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace KadirliApp.Tests.Integration.Auth;

/// <summary>
/// Faz 9.2 senaryoları: production modunda (Otp:DevMode=false) OTP yanıtta dönmez
/// ve auth uçları IP başına rate limit'e takılınca 429 + RATE_LIMITED zarfı döner.
/// </summary>
public class ProductionModeFactory : CustomWebApplicationFactory
{
    protected override IDictionary<string, string?> ExtraConfiguration => new Dictionary<string, string?>
    {
        { "Otp:DevMode", "false" },           // SMS yolu (Dev sağlayıcı log'a yazar), OTP yanıtta yok
        { "RateLimiting:Auth:PermitLimit", "3" },
        { "RateLimiting:Auth:WindowSeconds", "60" },
        // Faz 10.7: public-write policy testi için düşük limit
        { "RateLimiting:PublicWrite:PermitLimit", "3" },
        { "RateLimiting:PublicWrite:WindowSeconds", "60" }
    };
}

public class RateLimitingTests : IClassFixture<ProductionModeFactory>
{
    private readonly HttpClient _client;

    public RateLimitingTests(ProductionModeFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Login_InProductionMode_ShouldNotReturnOtp_AndExcessRequests_ShouldReturn429()
    {
        // 1. İlk istek: 200 ama data içinde "otp" alanı YOK (DevMode=false → kod SMS servisine gider)
        var first = await _client.PostAsJsonAsync("/v1/auth/login", new { phone = "+905009998877" });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstContent = await first.Content.ReadAsStringAsync();
        using (var doc = JsonDocument.Parse(firstContent))
        {
            doc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
            var data = doc.RootElement.GetProperty("data");
            data.TryGetProperty("otp", out _).Should().BeFalse("Otp:DevMode=false iken OTP yanıtta dönmemeli");
            data.GetProperty("expiresIn").GetInt32().Should().BeGreaterThan(0);
        }

        // 2. Limit (3/dk) doldurulur, 4. istek 429 + RATE_LIMITED zarfı dönmeli
        await _client.PostAsJsonAsync("/v1/auth/login", new { phone = "+905009998877" });
        await _client.PostAsJsonAsync("/v1/auth/login", new { phone = "+905009998877" });
        var rejected = await _client.PostAsJsonAsync("/v1/auth/login", new { phone = "+905009998877" });

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var rejectedContent = await rejected.Content.ReadAsStringAsync();
        using (var doc = JsonDocument.Parse(rejectedContent))
        {
            doc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
            doc.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("RATE_LIMITED");
        }
    }

    [Fact]
    public async Task PublicWriteEndpoint_ExcessRequests_ShouldReturn429()
    {
        // Faz 10.7: anonim POST /v1/complaints "public-write" policy'sine tabi (3/dk testte) —
        // limit dolunca 429 + RATE_LIMITED zarfı; pending kuyruğu sınırsız doldurulamaz.
        var body = new { subject = "Rate limit testi", message = "x" };

        for (var i = 0; i < 3; i++)
        {
            var ok = await _client.PostAsJsonAsync("/v1/complaints", body);
            ok.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"ilk 3 istek limit içinde (istek {i + 1})");
        }

        var rejected = await _client.PostAsJsonAsync("/v1/complaints", body);
        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        using var doc = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("RATE_LIMITED");
    }
}
