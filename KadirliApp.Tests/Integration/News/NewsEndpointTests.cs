using System.Net;
using System.Text.Json;
using FluentAssertions;
using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.News;

/// <summary>
/// Faz 12.12 — <c>/v1/news</c> uçlarının <b>kontratı ve görünürlüğü</b>.
///
/// 📌 Değişmez Kural #3'ün haber karşılığı: public uç yalnız <b>arşivlenmemiş + kaynağı
/// yayında + dışlanmış kategorisi olmayan</b> kaydı döndürür ve bu filtre <b>sorguda</b>
/// zorlanır — istemciden gelen hiçbir parametre onu gevşetemez.
/// </summary>
public class NewsEndpointTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string Marker = "CLAUDE-NEWS-12.12";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NewsEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public async Task InitializeAsync() => await SeedAsync();

    public async Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM news_article_categories WHERE news_article_id IN (SELECT id FROM news_articles WHERE source_title LIKE '%' || {0} || '%')",
            Marker);
        await db.NewsArticles.Where(x => x.SourceTitle.Contains(Marker)).ExecuteDeleteAsync();
        await db.NewsCategories.Where(x => x.Slug.StartsWith("clause-news")).ExecuteDeleteAsync();

        await InvalidateAsync(scope.ServiceProvider);
    }

    /// <summary>
    /// 🐛 <b>Bu satır bir test bulgusundan doğdu ve gerçek davranışı taklit ediyor.</b>
    /// Liste ucu 15 dk önbellekli; testler kayıtları <b>doğrudan veritabanına</b> yazdığı için
    /// önbellek temizlenmiyordu ve bir sonraki test, silinmiş kayıtların kimliklerini taşıyan
    /// bayat bir listeyi okuyup detayda <b>404</b> alıyordu. Üretimde bu yol yok: her yazan
    /// (senkron + panel komutları) grubu temizliyor (§7 madde 22). Test de aynısını yapmalı —
    /// önbelleği kapatmak yerine, çünkü kapatmak onu test kapsamı dışına çıkarırdı.
    /// </summary>
    private static async Task InvalidateAsync(IServiceProvider sp) =>
        await sp.GetRequiredService<ICacheService>().InvalidateGroupsAsync(new[] { CacheGroups.News });

    /// <summary>
    /// Kayıtlar <b>nesne başlatıcıyla</b> kuruluyor — <c>init</c> alanların bilinçli olarak
    /// izin verdiği tek yol. Yüklenmiş bir varlığa aynı alanları yazmak <c>CS8852</c>'dir.
    /// </summary>
    private async Task SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.NewsArticles.AnyAsync(x => x.SourceTitle.Contains(Marker)))
        {
            await InvalidateAsync(scope.ServiceProvider);
            return;
        }

        var normal = new NewsCategory { WpId = 900001, Name = "Test Gündem", Slug = "clause-news-gundem" };
        var excluded = new NewsCategory
        {
            WpId = 900002, Name = "Test E-Gazete", Slug = "clause-news-e-gazete", IsExcluded = true
        };
        db.NewsCategories.AddRange(normal, excluded);
        await db.SaveChangesAsync();

        db.NewsArticles.AddRange(
            Article(910001, $"{Marker} görünen haber", normal),
            Article(910002, $"{Marker} arşivlenmiş haber", normal, isArchived: true),
            Article(910003, $"{Marker} kaynakta yok", normal, state: NewsSourceStates.Gone),
            Article(910004, $"{Marker} dışlanmış kategoride", excluded));

        await db.SaveChangesAsync();
        await InvalidateAsync(scope.ServiceProvider);
    }

    private static NewsArticle Article(
        int wpId, string title, NewsCategory category,
        bool isArchived = false, string state = NewsSourceStates.Published)
    {
        var article = new NewsArticle
        {
            WpId = wpId,
            SourceTitle = title,
            SourceExcerpt = "Özet metni",
            SourceContentHtml = "<p>Gövde metni</p>",
            SourcePlainText = "Gövde metni",
            SourceUrl = $"https://ornek.com/{wpId}",
            SourcePublishedAt = new DateTime(2026, 8, 10, 9, 0, 0, DateTimeKind.Utc),
            SourceModifiedAt = new DateTime(2026, 8, 10, 9, 0, 0, DateTimeKind.Utc),
            SourceChecksum = $"checksum-{wpId}",
            SourceState = state,
            ReadingMinutes = 2,
            IsArchived = isArchived
        };

        article.ReplaceCategories(new[] { category });
        return article;
    }

    private async Task<JsonElement> GetDataAsync(string url)
    {
        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        // Zarf sözleşmesi (§7 madde 10): {success, data, meta} + her yanıtta traceId.
        document.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        document.RootElement.GetProperty("meta").TryGetProperty("traceId", out _).Should().BeTrue();

        return document.RootElement.GetProperty("data").Clone();
    }

    // ───────────────────────────── Görünürlük ───────────────────────────────────────

    [Fact]
    public async Task List_ReturnsOnlyVisibleArticles()
    {
        var data = await GetDataAsync("/v1/news?limit=50");

        var titles = data.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("title").GetString()!)
            .Where(t => t.Contains(Marker))
            .ToList();

        titles.Should().ContainSingle().Which.Should().Contain("görünen haber");
    }

    [Fact]
    public async Task Detail_Returns404_ForHiddenArticles()
    {
        Guid archivedId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            archivedId = await db.NewsArticles.Where(x => x.WpId == 910002).Select(x => x.Id).SingleAsync();
        }

        (await _client.GetAsync($"/v1/news/{archivedId}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound, "arşivlenmiş habere elde kalan bir bağlantıyla ulaşılamamalı");
    }

    // ───────────────────────────── Kontrat ──────────────────────────────────────────

    /// <summary>
    /// 🔑 Gövde <b>yalnız detayda</b>. 27k kayıtlık bir modülde sayfa başına 20 gövde taşımak
    /// hiç okunmayacak ~40 KB demekti — ama iki ayrı projeksiyon yazmak §7 madde 43'ün
    /// "detay sessizce eksik kalır" hatasının kapısı; bu yüzden tek projeksiyon + parametre.
    /// </summary>
    [Fact]
    public async Task Body_IsAbsentInTheList_ButPresentInTheDetail()
    {
        var list = await GetDataAsync("/v1/news?limit=50");
        var item = list.GetProperty("items").EnumerateArray()
            .First(i => i.GetProperty("title").GetString()!.Contains(Marker));

        item.GetProperty("contentHtml").ValueKind.Should().Be(JsonValueKind.Null);
        item.GetProperty("excerpt").GetString().Should().NotBeNullOrEmpty("özet listede olmalı");
        item.GetProperty("readingMinutes").GetInt32().Should().BeGreaterThan(0);

        var detail = await GetDataAsync($"/v1/news/{item.GetProperty("id").GetString()}");
        detail.GetProperty("contentHtml").GetString().Should().Contain("Gövde metni");
    }

    [Fact]
    public async Task List_IsPaged_WithTheStandardShape()
    {
        var data = await GetDataAsync("/v1/news?page=1&limit=1");

        foreach (var field in new[] { "items", "totalCount", "currentPage", "pageSize", "totalPages" })
            data.TryGetProperty(field, out _).Should().BeTrue("{0} sayfalama sözleşmesinin parçası", field);
    }

    [Fact]
    public async Task Search_MatchesTheTitle()
    {
        var data = await GetDataAsync("/v1/news?search=görünen");

        data.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("title").GetString()!)
            .Should().Contain(t => t.Contains("görünen haber"));
    }

    /// <summary>Dışlanmış kategori public uçta <b>hiç</b> görünmez — boş bir süzgeç yalandır.</summary>
    [Fact]
    public async Task Categories_HideTheExcludedOnes_AndCountOnlyVisibleArticles()
    {
        var data = await GetDataAsync("/v1/news/categories");

        var categories = data.EnumerateArray().ToList();
        categories.Should().NotContain(c => c.GetProperty("slug").GetString() == "clause-news-e-gazete");

        var visible = categories.Single(c => c.GetProperty("slug").GetString() == "clause-news-gundem");
        visible.GetProperty("articleCount").GetInt32().Should().Be(1,
            "sayaç kaynağınki değil, bizde GÖRÜNEN haber sayısı olmalı (3 kayıt gizli)");
    }

    /// <summary>Kategori süzgeci sunucuda: istemci id gönderir, listeyi kendisi elemez.</summary>
    [Fact]
    public async Task CategoryFilter_IsAppliedOnTheServer()
    {
        Guid categoryId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            categoryId = await db.NewsCategories.Where(x => x.Slug == "clause-news-gundem").Select(x => x.Id).SingleAsync();
        }

        var data = await GetDataAsync($"/v1/news?categoryId={categoryId}&limit=50");

        data.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("title").GetString()!)
            .Where(t => t.Contains(Marker))
            .Should().ContainSingle();
    }
}
