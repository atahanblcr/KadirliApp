using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Pharmacies;

/// <summary>
/// Faz 10.4 doğrulaması: nöbetçi eczane uçları + nöbet CRUD yetkisi/invalidation'ı + public lookup uçları.
/// </summary>
public class PharmacyScheduleAndLookupTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PharmacyScheduleAndLookupTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    /// <summary>Seed super_admin (+905000000001) OTP akışıyla giriş yapar — kayıtlı olduğundan direkt access token döner.</summary>
    private async Task<string> GetAdminTokenAsync()
    {
        const string phone = "+905000000001";
        (await _client.PostAsJsonAsync("/v1/auth/login", new { phone })).StatusCode.Should().Be(HttpStatusCode.OK);
        var verify = await _client.PostAsJsonAsync("/v1/auth/verify-otp", new { phone, otp = "123456" });
        verify.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await verify.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("isNewUser").GetBoolean().Should().BeFalse("seed admin kayıtlı olmalı");
        return data.GetProperty("accessToken").GetString()!;
    }

    private async Task<string> GetUserTokenAsync(string phone, string username)
    {
        (await _client.PostAsJsonAsync("/v1/auth/login", new { phone })).StatusCode.Should().Be(HttpStatusCode.OK);
        var verify = await _client.PostAsJsonAsync("/v1/auth/verify-otp", new { phone, otp = "123456" });
        using var doc = JsonDocument.Parse(await verify.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");
        if (!data.GetProperty("isNewUser").GetBoolean())
            return data.GetProperty("accessToken").GetString()!;

        Guid neighborhoodId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            neighborhoodId = await db.Neighborhoods.Where(n => n.IsActive).Select(n => n.Id).FirstAsync();
        }
        var register = await _client.PostAsJsonAsync("/v1/auth/register",
            new { tempToken = data.GetProperty("tempToken").GetString(), username, primaryNeighborhoodId = neighborhoodId });
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        using var regDoc = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        return regDoc.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private HttpRequestMessage Authorized(HttpMethod method, string url, string token, object? body = null)
    {
        var req = new HttpRequestMessage(method, url)
        {
            Content = body is null ? null : JsonContent.Create(body)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    [Theory]
    [InlineData("/v1/neighborhoods")]
    [InlineData("/v1/deaths/cemeteries")]
    [InlineData("/v1/deaths/mosques")]
    [InlineData("/v1/events/categories")]
    public async Task LookupEndpoints_ReturnSeededData_Anonymously(string url)
    {
        var response = await _client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("data").GetArrayLength().Should().BeGreaterThan(0, $"{url} seed verisi dönmeli");
    }

    [Fact]
    public async Task ScheduleCrud_Authorization_OnDuty_And_CacheInvalidation()
    {
        var adminToken = await GetAdminTokenAsync();
        var userToken = await GetUserTokenAsync("+905044440001", "claudetest_ecz");

        // Eczane oluştur (test DB'de MockDataSeeder koşmaz — eczane tablosu boş başlar)
        var pharmacyResp = await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/admin/pharmacies", adminToken,
            new { name = "Test Nöbet Eczanesi", address = "Test Cad. 1", phone = "+903280000000", isActive = true }));
        pharmacyResp.StatusCode.Should().Be(HttpStatusCode.OK);
        Guid pharmacyId;
        using (var doc = JsonDocument.Parse(await pharmacyResp.Content.ReadAsStringAsync()))
            pharmacyId = doc.RootElement.GetProperty("data").GetGuid();

        // Yetki: anonim → 401, user token → 403
        var anon = await _client.PostAsJsonAsync("/v1/admin/pharmacies/schedule",
            new { pharmacyId, dutyDate = "2026-08-05" });
        anon.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var forbidden = await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/admin/pharmacies/schedule", userToken,
            new { pharmacyId, dutyDate = "2026-08-05" }));
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Cache'i doldur: nöbet YOKKEN on-duty boş döner (invalidation'ı kanıtlamak için bilinçli olarak önce çağrılıyor)
        var emptyBefore = await _client.GetAsync("/v1/pharmacies/on-duty?date=2026-08-05");
        emptyBefore.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await emptyBefore.Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("data").GetArrayLength().Should().Be(0);

        // Admin nöbet oluşturur → cache invalidation
        var create = await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/admin/pharmacies/schedule", adminToken,
            new { pharmacyId, dutyDate = "2026-08-05", source = "claudetest" }));
        create.StatusCode.Should().Be(HttpStatusCode.OK);
        Guid scheduleId;
        using (var doc = JsonDocument.Parse(await create.Content.ReadAsStringAsync()))
            scheduleId = doc.RootElement.GetProperty("data").GetGuid();

        // Aynı eczane + aynı gün → 409
        var duplicate = await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/admin/pharmacies/schedule", adminToken,
            new { pharmacyId, dutyDate = "2026-08-05" }));
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // on-duty artık nöbetçiyi döner (cache invalidate edilmiş olmalı — 15 dk TTL beklenmeden taze veri)
        var onDuty = await _client.GetAsync("/v1/pharmacies/on-duty?date=2026-08-05");
        onDuty.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await onDuty.Content.ReadAsStringAsync()))
        {
            var items = doc.RootElement.GetProperty("data");
            items.GetArrayLength().Should().Be(1);
            items[0].GetProperty("name").GetString().Should().Be("Test Nöbet Eczanesi");
            items[0].GetProperty("startTime").GetString().Should().Be("19:00");
            items[0].GetProperty("endTime").GetString().Should().Be("09:00");
        }

        // Aylık takvim
        var schedule = await _client.GetAsync("/v1/pharmacies/schedule?year=2026&month=8");
        schedule.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await schedule.Content.ReadAsStringAsync()))
        {
            var items = doc.RootElement.GetProperty("data");
            items.GetArrayLength().Should().Be(1);
            items[0].GetProperty("pharmacyName").GetString().Should().Be("Test Nöbet Eczanesi");
            items[0].GetProperty("source").GetString().Should().Be("claudetest");
        }

        // Geçersiz ay → 400
        var badMonth = await _client.GetAsync("/v1/pharmacies/schedule?year=2026&month=13");
        badMonth.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Sil → on-duty tekrar boş (ikinci invalidation kanıtı)
        var delete = await _client.SendAsync(Authorized(HttpMethod.Delete, $"/v1/admin/pharmacies/schedule/{scheduleId}", adminToken));
        delete.StatusCode.Should().Be(HttpStatusCode.OK);
        var emptyAfter = await _client.GetAsync("/v1/pharmacies/on-duty?date=2026-08-05");
        using (var doc = JsonDocument.Parse(await emptyAfter.Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("data").GetArrayLength().Should().Be(0);
    }
}
