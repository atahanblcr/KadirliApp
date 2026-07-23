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

namespace KadirliApp.Tests.Integration.Ads;

/// <summary>
/// Faz 10.9(c) doğrulaması: ilan kategori/özellik yönetimi admin API'si — CRUD + silme korumaları (409)
/// + ads-lookup CACHE INVALIDATION kanıtı (public uç, admin mutasyonundan sonra taze döner)
/// + Faz 10.9(i) audit izi (AuditBehavior audit_logs'a satır yazar).
/// </summary>
public class AdCategoryAdminTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdCategoryAdminTests(CustomWebApplicationFactory factory)
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
    public async Task AdCategoriesAdmin_WithoutToken_Returns401()
    {
        (await _client.GetAsync("/v1/admin/ads/categories")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdCategoryCrud_FullFlow_WithCacheInvalidationAndAudit_Works()
    {
        var token = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        // Public kategori ucunu ısıt (cache'e girsin) — invalidation kanıtının ön koşulu.
        (await _client.GetAsync("/v1/ads/categories")).StatusCode.Should().Be(HttpStatusCode.OK);

        // Kök kategori oluştur
        var rootName = $"Faz109c Kök {suffix}";
        var rootResp = await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/admin/ads/categories", token,
            new { name = rootName, displayOrder = 99, isActive = true }));
        rootResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var rootDoc = JsonDocument.Parse(await rootResp.Content.ReadAsStringAsync());
        var rootId = rootDoc.RootElement.GetProperty("data").GetGuid();

        // Aynı ad (slug çakışması) → 409
        (await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/admin/ads/categories", token,
                new { name = rootName, displayOrder = 1 })))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);

        // CACHE INVALIDATION KANITI: public uç, ısıtılmış cache'e rağmen yeni kategoriyi HEMEN döner.
        var publicResp = await _client.GetAsync("/v1/ads/categories");
        publicResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var publicDoc = JsonDocument.Parse(await publicResp.Content.ReadAsStringAsync());
        publicDoc.RootElement.GetProperty("data").EnumerateArray()
            .Should().Contain(x => x.GetProperty("id").GetGuid() == rootId,
                "admin create sonrası ads-lookup grubu invalidate edilmeli — TTL beklenmemeli");

        // Alt kategori → kök artık silinemez (409)
        var subResp = await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/admin/ads/categories", token,
            new { name = $"Faz109c Alt {suffix}", parentId = rootId, displayOrder = 1 }));
        subResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var subDoc = JsonDocument.Parse(await subResp.Content.ReadAsStringAsync());
        var subId = subDoc.RootElement.GetProperty("data").GetGuid();

        (await _client.SendAsync(Authorized(HttpMethod.Delete, $"/v1/admin/ads/categories/{rootId}", token)))
            .StatusCode.Should().Be(HttpStatusCode.Conflict, "alt kategorisi olan kategori silinememeli");

        // Özellik: Select seçeneksiz → 400; seçenekli → 200; aynı ad → 409
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/admin/ads/categories/{subId}/properties", token,
                new { propertyName = "Yakıt Tipi", propertyType = "Select", isRequired = true, displayOrder = 1 })))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest, "Select özellik seçeneksiz oluşturulamamalı");

        var propResp = await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/admin/ads/categories/{subId}/properties", token,
            new { propertyName = "Yakıt Tipi", propertyType = "Select", isRequired = true, displayOrder = 1, options = new[] { "Benzin", "Dizel" } }));
        propResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var propDoc = JsonDocument.Parse(await propResp.Content.ReadAsStringAsync());
        var propertyId = propDoc.RootElement.GetProperty("data").GetGuid();

        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/admin/ads/categories/{subId}/properties", token,
                new { propertyName = "yakıt tipi", propertyType = "Text", displayOrder = 2 })))
            .StatusCode.Should().Be(HttpStatusCode.Conflict, "aynı kategoride aynı adla ikinci özellik 409 olmalı");

        // Seçenek ekle → public properties ucunda 3 seçenek görünür (yine taze)
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/admin/ads/categories/properties/{propertyId}/options", token,
                new { optionValue = "LPG", displayOrder = 3 })))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var pubPropsResp = await _client.GetAsync($"/v1/ads/categories/{subId}/properties");
        pubPropsResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var pubPropsDoc = JsonDocument.Parse(await pubPropsResp.Content.ReadAsStringAsync());
        var pubProp = pubPropsDoc.RootElement.GetProperty("data").EnumerateArray().Single();
        pubProp.GetProperty("options").GetArrayLength().Should().Be(3);

        // AUDIT KANITI (10.9-i): create-category izi doğru aktör ve hedefle yazılmış olmalı.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var audit = await db.AuditLogs
                .Where(a => a.Module == "ads" && a.Action == "create-category" && a.AffectedId == rootId)
                .SingleOrDefaultAsync();
            audit.Should().NotBeNull("AuditBehavior create-category izini yazmalı");
            var adminId = await db.Users.Where(u => u.Phone == "+905000000001").Select(u => u.Id).SingleAsync();
            audit!.UserId.Should().Be(adminId);
            audit.AffectedType.Should().Be("AdCategory");
        }

        // Temizlik ve silme kuralları: özellik sil → alt sil → kök sil (artık engel yok)
        (await _client.SendAsync(Authorized(HttpMethod.Delete, $"/v1/admin/ads/categories/properties/{propertyId}", token)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.SendAsync(Authorized(HttpMethod.Delete, $"/v1/admin/ads/categories/{subId}", token)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.SendAsync(Authorized(HttpMethod.Delete, $"/v1/admin/ads/categories/{rootId}", token)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Public liste de temizlenmiş halde (invalidation delete'te de çalışır)
        var finalResp = await _client.GetAsync("/v1/ads/categories");
        using var finalDoc = JsonDocument.Parse(await finalResp.Content.ReadAsStringAsync());
        finalDoc.RootElement.GetProperty("data").EnumerateArray()
            .Should().NotContain(x => x.GetProperty("id").GetGuid() == rootId);
    }
}
