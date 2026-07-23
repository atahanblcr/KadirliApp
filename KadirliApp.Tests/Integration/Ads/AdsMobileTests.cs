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
/// Faz 10.5 doğrulaması: ilan kategori ağacı + kategori özellikleri lookup'ları,
/// kullanıcı ilan verme (pending + propertyValues), detay ucu (sahiplik/404 + view_count) ve admin onay akışı.
/// </summary>
public class AdsMobileTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdsMobileTests(CustomWebApplicationFactory factory)
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

    /// <summary>Kök kategorilerden slug'ı verilen kategorinin JSON elemanını döner.</summary>
    private async Task<(Guid Id, int SubCategoryCount)> FindRootCategoryAsync(string slug)
    {
        var resp = await _client.GetAsync("/v1/ads/categories");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        foreach (var item in doc.RootElement.GetProperty("data").EnumerateArray())
        {
            if (item.GetProperty("slug").GetString() == slug)
                return (item.GetProperty("id").GetGuid(), item.GetProperty("subCategoryCount").GetInt32());
        }
        throw new Xunit.Sdk.XunitException($"Kök kategori bulunamadı: {slug}");
    }

    [Fact]
    public async Task Categories_ReturnSeededHierarchy_Anonymously()
    {
        // Kök kategoriler: seed 8 ana kategori, hepsi parentId=null
        var resp = await _client.GetAsync("/v1/ads/categories");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()))
        {
            var data = doc.RootElement.GetProperty("data");
            data.GetArrayLength().Should().BeGreaterThanOrEqualTo(8);
            foreach (var item in data.EnumerateArray())
                item.GetProperty("parentId").ValueKind.Should().Be(JsonValueKind.Null, "kök listede yalnız üst kategoriler olmalı");
        }

        // Araçlar'ın alt kategorileri (seed: Otomobil, Motosiklet, Ticari Araç)
        var (araclarId, subCount) = await FindRootCategoryAsync("araclar");
        subCount.Should().Be(3);
        var children = await _client.GetAsync($"/v1/ads/categories?parentId={araclarId}");
        children.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await children.Content.ReadAsStringAsync()))
        {
            var data = doc.RootElement.GetProperty("data");
            data.GetArrayLength().Should().Be(3);
            data.EnumerateArray().Select(x => x.GetProperty("slug").GetString()).Should().Contain("otomobil");
            foreach (var item in data.EnumerateArray())
                item.GetProperty("parentId").GetGuid().Should().Be(araclarId);
        }
    }

    [Fact]
    public async Task CategoryProperties_ReturnSeededDefinitions_And_404ForUnknownCategory()
    {
        var (araclarId, _) = await FindRootCategoryAsync("araclar");
        Guid otomobilId;
        using (var doc = JsonDocument.Parse(await (await _client.GetAsync($"/v1/ads/categories?parentId={araclarId}")).Content.ReadAsStringAsync()))
            otomobilId = doc.RootElement.GetProperty("data").EnumerateArray()
                .First(x => x.GetProperty("slug").GetString() == "otomobil").GetProperty("id").GetGuid();

        var resp = await _client.GetAsync($"/v1/ads/categories/{otomobilId}/properties");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()))
        {
            var props = doc.RootElement.GetProperty("data").EnumerateArray().ToList();
            props.Should().HaveCountGreaterThanOrEqualTo(3, "Otomobil için seed property'ler dolu olmalı");
            var yakit = props.First(p => p.GetProperty("propertyName").GetString() == "Yakıt Tipi");
            yakit.GetProperty("propertyType").GetString().Should().Be("Select");
            yakit.GetProperty("isRequired").GetBoolean().Should().BeTrue();
            yakit.GetProperty("options").EnumerateArray()
                .Select(o => o.GetProperty("optionValue").GetString()).Should().Contain("Dizel");
        }

        (await _client.GetAsync($"/v1/ads/categories/{Guid.NewGuid()}/properties"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UserAdFlow_CreatePending_OwnerDetail_AdminApprove_PublicVisibility_ViewCount()
    {
        // Otomobil kategorisi + property tanımları
        var (araclarId, _) = await FindRootCategoryAsync("araclar");
        Guid otomobilId;
        using (var doc = JsonDocument.Parse(await (await _client.GetAsync($"/v1/ads/categories?parentId={araclarId}")).Content.ReadAsStringAsync()))
            otomobilId = doc.RootElement.GetProperty("data").EnumerateArray()
                .First(x => x.GetProperty("slug").GetString() == "otomobil").GetProperty("id").GetGuid();
        var propsByName = new Dictionary<string, Guid>();
        using (var doc = JsonDocument.Parse(await (await _client.GetAsync($"/v1/ads/categories/{otomobilId}/properties")).Content.ReadAsStringAsync()))
            foreach (var p in doc.RootElement.GetProperty("data").EnumerateArray())
                propsByName[p.GetProperty("propertyName").GetString()!] = p.GetProperty("id").GetGuid();

        // Token'sız ilan verme → 401
        (await _client.PostAsJsonAsync("/v1/ads", new { categoryId = otomobilId, title = "Test", description = "x", contactPhone = "+905331112233" }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var userToken = await GetUserTokenAsync("+905055550001", "claudetest_ads1");
        var propertyValues = new Dictionary<string, string>
        {
            [propsByName["Yakıt Tipi"].ToString()] = "Dizel",
            [propsByName["Vites"].ToString()] = "Manuel",
            [propsByName["Model Yılı"].ToString()] = "2018"
        };
        var create = await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/ads", userToken, new
        {
            categoryId = otomobilId,
            title = "CLAUDE-TEST Satılık Dizel Otomobil",
            description = "Temiz kullanılmış test aracı.",
            price = 450000,
            contactPhone = "+905331112233",
            propertyValues
        }));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid adId;
        using (var doc = JsonDocument.Parse(await create.Content.ReadAsStringAsync()))
            adId = doc.RootElement.GetProperty("data").GetGuid();

        // DB: pending + sahibi doğru + 3 property değeri yazılmış
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ad = await db.Ads.Include(a => a.PropertyValues).FirstAsync(a => a.Id == adId);
            ad.Status.Should().Be("pending");
            ad.PropertyValues.Should().HaveCount(3);
        }

        // Pending ilan public listede YOK, anonim detayda 404, sahibi detayı görür
        using (var doc = JsonDocument.Parse(await (await _client.GetAsync("/v1/ads?search=CLAUDE-TEST")).Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("data").GetProperty("items").GetArrayLength().Should().Be(0, "pending ilan public listeye sızmamalı");
        (await _client.GetAsync($"/v1/ads/{adId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var ownerDetail = await _client.SendAsync(Authorized(HttpMethod.Get, $"/v1/ads/{adId}", userToken));
        ownerDetail.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await ownerDetail.Content.ReadAsStringAsync()))
        {
            var data = doc.RootElement.GetProperty("data");
            data.GetProperty("status").GetString().Should().Be("pending");
            data.GetProperty("categoryName").GetString().Should().Be("Otomobil");
            data.GetProperty("properties").EnumerateArray()
                .Select(p => p.GetProperty("value").GetString()).Should().Contain("Dizel");
        }

        // Admin onaylar → public listede ve anonim detayda görünür
        var adminToken = await GetAdminTokenAsync();
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/admin/ads/{adId}/approve", adminToken)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await (await _client.GetAsync("/v1/ads?search=CLAUDE-TEST")).Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("data").GetProperty("items").GetArrayLength().Should().Be(1);

        var firstView = await _client.GetAsync($"/v1/ads/{adId}");
        firstView.StatusCode.Should().Be(HttpStatusCode.OK);
        int firstCount;
        using (var doc = JsonDocument.Parse(await firstView.Content.ReadAsStringAsync()))
            firstCount = doc.RootElement.GetProperty("data").GetProperty("viewCount").GetInt32();
        using (var doc = JsonDocument.Parse(await (await _client.GetAsync($"/v1/ads/{adId}")).Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("data").GetProperty("viewCount").GetInt32()
                .Should().Be(firstCount + 1, "her başarılı detay çağrısı view_count'u artırmalı");
    }

    [Fact]
    public async Task UserAdCreate_ValidationRules_Return400()
    {
        var (araclarId, _) = await FindRootCategoryAsync("araclar");
        Guid otomobilId;
        using (var doc = JsonDocument.Parse(await (await _client.GetAsync($"/v1/ads/categories?parentId={araclarId}")).Content.ReadAsStringAsync()))
            otomobilId = doc.RootElement.GetProperty("data").EnumerateArray()
                .First(x => x.GetProperty("slug").GetString() == "otomobil").GetProperty("id").GetGuid();
        var propsByName = new Dictionary<string, Guid>();
        using (var doc = JsonDocument.Parse(await (await _client.GetAsync($"/v1/ads/categories/{otomobilId}/properties")).Content.ReadAsStringAsync()))
            foreach (var p in doc.RootElement.GetProperty("data").EnumerateArray())
                propsByName[p.GetProperty("propertyName").GetString()!] = p.GetProperty("id").GetGuid();

        var token = await GetUserTokenAsync("+905055550002", "claudetest_ads2");
        var validProps = new Dictionary<string, string>
        {
            [propsByName["Yakıt Tipi"].ToString()] = "Dizel",
            [propsByName["Vites"].ToString()] = "Manuel",
            [propsByName["Model Yılı"].ToString()] = "2020"
        };

        async Task AssertValidationError(object body, string because)
        {
            var resp = await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/ads", token, body));
            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest, because);
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        }

        // Zorunlu property'ler eksik
        await AssertValidationError(new
        {
            categoryId = otomobilId,
            title = "CLAUDE-TEST Eksik Property",
            description = "x",
            contactPhone = "+905331112233"
        }, "zorunlu kategori özellikleri gönderilmedi");

        // Select için tanımsız seçenek
        var badSelect = new Dictionary<string, string>(validProps) { [propsByName["Yakıt Tipi"].ToString()] = "Nükleer" };
        await AssertValidationError(new
        {
            categoryId = otomobilId,
            title = "CLAUDE-TEST Geçersiz Seçenek",
            description = "x",
            contactPhone = "+905331112233",
            propertyValues = badSelect
        }, "select değeri tanımlı seçeneklerden olmalı");

        // Başkasına/hiç kimseye ait olmayan görsel dosyası
        await AssertValidationError(new
        {
            categoryId = otomobilId,
            title = "CLAUDE-TEST Sahte Görsel",
            description = "x",
            contactPhone = "+905331112233",
            imageFileIds = new[] { Guid.NewGuid() },
            propertyValues = validProps
        }, "kullanıcı yalnız kendi yüklediği dosyaları bağlayabilmeli");

        // Çok kısa başlık
        await AssertValidationError(new
        {
            categoryId = otomobilId,
            title = "ab",
            description = "x",
            contactPhone = "+905331112233",
            propertyValues = validProps
        }, "başlık en az 3 karakter olmalı");
    }
}
