using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Businesses;

/// <summary>
/// Faz 10.9(b) doğrulaması: işletme yönetimi admin API'si — CRUD + doğrulama rozeti +
/// "kampanyası olan işletme silinemez" (409) kuralı + yetkisiz erişim.
/// </summary>
public class BusinessesAdminTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BusinessesAdminTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private async Task<string> GetAdminTokenAsync()
    {
        const string phone = "+905000000001";
        (await _client.PostAsJsonAsync("/v1/auth/login", new { phone })).StatusCode.Should().Be(HttpStatusCode.OK);
        var verify = await _client.PostAsJsonAsync("/v1/auth/verify-otp", new { phone, otp = "123456" });
        verify.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await verify.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string url, string token, object? body = null)
    {
        var req = new HttpRequestMessage(method, url)
        {
            Content = body is null ? null : JsonContent.Create(body)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    [Fact]
    public async Task BusinessesAdmin_WithoutToken_Returns401()
    {
        (await _client.GetAsync("/v1/admin/businesses")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BusinessCrud_FullFlow_Works()
    {
        var token = await GetAdminTokenAsync();

        // Kategori ekle (panel modal ucu) — mükerrer ad 409
        var catName = $"Test Kategori {Guid.NewGuid():N}"[..30];
        var catResp = await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/admin/businesses/categories", token, new { name = catName }));
        catResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var catDoc = JsonDocument.Parse(await catResp.Content.ReadAsStringAsync());
        var categoryId = catDoc.RootElement.GetProperty("data").GetGuid();

        (await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/admin/businesses/categories", token, new { name = catName })))
            .StatusCode.Should().Be(HttpStatusCode.Conflict, "aynı adla ikinci kategori 409 olmalı");

        // İşletme oluştur — geçersiz kategori 400
        (await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/admin/businesses", token,
                new { businessName = "X", categoryId = Guid.NewGuid() })))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest, "olmayan kategoriyle işletme açılamamalı");

        var createResp = await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/admin/businesses", token,
            new { businessName = "Faz109 Test İşletmesi", categoryId, instagramHandle = "@faz109" }));
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var createDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var businessId = createDoc.RootElement.GetProperty("data").GetGuid();

        // Listede görünür + @ soyulmuş
        var listResp = await _client.SendAsync(Authorized(HttpMethod.Get, "/v1/admin/businesses?search=Faz109", token));
        using var listDoc = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());
        var items = listDoc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray().ToList();
        items.Should().ContainSingle(x => x.GetProperty("id").GetGuid() == businessId);
        items[0].GetProperty("instagramHandle").GetString().Should().Be("faz109");
        items[0].GetProperty("categoryName").GetString().Should().Be(catName);

        // Doğrula → isVerified true + verifiedAt dolu
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/admin/businesses/{businessId}/verify", token)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var byIdResp = await _client.SendAsync(Authorized(HttpMethod.Get, $"/v1/admin/businesses/{businessId}", token));
        using var byIdDoc = JsonDocument.Parse(await byIdResp.Content.ReadAsStringAsync());
        byIdDoc.RootElement.GetProperty("data").GetProperty("isVerified").GetBoolean().Should().BeTrue();
        byIdDoc.RootElement.GetProperty("data").GetProperty("verifiedAt").ValueKind.Should().NotBe(JsonValueKind.Null);

        // Güncelle
        (await _client.SendAsync(Authorized(HttpMethod.Put, $"/v1/admin/businesses/{businessId}", token,
                new { businessName = "Faz109 Güncel", categoryId, phone = "0328 000 00 00" })))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Kampanya bağla → silme 409; kampanyayı kaldırınca silme 200
        Guid campaignId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var campaign = new Campaign
            {
                BusinessId = businessId,
                Title = "Faz109 Kampanya",
                Description = "test",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = "approved"
            };
            db.Campaigns.Add(campaign);
            await db.SaveChangesAsync();
            campaignId = campaign.Id;
        }

        (await _client.SendAsync(Authorized(HttpMethod.Delete, $"/v1/admin/businesses/{businessId}", token)))
            .StatusCode.Should().Be(HttpStatusCode.Conflict, "kampanyası olan işletme silinememeli (DB cascade geçmişi yok eder)");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Campaigns.IgnoreQueryFilters().Where(c => c.Id == campaignId).ExecuteDeleteAsync();
        }

        (await _client.SendAsync(Authorized(HttpMethod.Delete, $"/v1/admin/businesses/{businessId}", token)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Businesses.AnyAsync(b => b.Id == businessId)).Should().BeFalse("hard delete beklenir");
            // test kategorisini de temizle
            await db.BusinessCategories.Where(c => c.Id == categoryId).ExecuteDeleteAsync();
        }
    }
}
