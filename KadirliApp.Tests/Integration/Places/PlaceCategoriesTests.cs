using System.Net;
using System.Text.Json;
using FluentAssertions;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Places;

/// <summary>
/// Faz 11.11 (mobil): GET /v1/places/categories — mekan kategorisi lookup'ı.
/// `PlaceResponseDto` yalnız `CategoryId` taşıdığı için mobil ne kategori adını
/// yazabiliyor ne de filtre chip'i çizebiliyordu. Uç additive'dir (kimlik
/// doğrulama istemez, mevcut hiçbir yanıt değişmedi) ve `?categoryId=` filtresi
/// döndürdüğü kimliklerle çalışmalı.
/// </summary>
public class PlaceCategoriesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PlaceCategoriesTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Categories_Are_Public_And_Ordered_By_DisplayOrder()
    {
        List<PlaceCategory> expected;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            expected = await db.Set<PlaceCategory>()
                .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
                .ToListAsync();
        }

        var response = await _client.GetAsync("/v1/places/categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();

        var items = doc.RootElement.GetProperty("data").EnumerateArray().ToList();
        items.Should().HaveCount(expected.Count);
        items.Select(x => x.GetProperty("name").GetString())
            .Should().Equal(expected.Select(c => c.Name));
        // Slug istemcide ikon eşlemesi için kullanılıyor → boş gelmemeli.
        items.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.GetProperty("slug").GetString()));
    }

    [Fact]
    public async Task Returned_CategoryId_Filters_The_Places_List()
    {
        var categoriesResponse = await _client.GetAsync("/v1/places/categories");
        using var categoriesDoc = JsonDocument.Parse(await categoriesResponse.Content.ReadAsStringAsync());
        var categoryId = categoriesDoc.RootElement.GetProperty("data")[0].GetProperty("id").GetGuid();

        var response = await _client.GetAsync($"/v1/places?categoryId={categoryId}&limit=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray().ToList();
        items.Should().OnlyContain(x => x.GetProperty("categoryId").GetGuid() == categoryId);
    }

    /// <summary>
    /// Literal segment parametreli segmentten önce eşleşmeli — aksi hâlde
    /// "categories" bir Guid gibi bağlanmaya çalışılır ve uç 400 döner.
    /// </summary>
    [Fact]
    public async Task Categories_Route_Does_Not_Collide_With_Place_Detail()
    {
        var response = await _client.GetAsync("/v1/places/categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var unknown = await _client.GetAsync($"/v1/places/{Guid.NewGuid()}");
        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
