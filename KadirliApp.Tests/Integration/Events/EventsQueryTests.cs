using System.Net;
using System.Text.Json;
using FluentAssertions;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Events;

/// <summary>
/// Faz 11.10 (mobil): GET /v1/events sıralaması ve tarih filtresi.
/// Mobil "Yaklaşan etkinlikler" listesi ?startDate=bugün&amp;sort=date_asc ile çalışıyor —
/// varsayılan azalan sıralamada ilk sayfa EN UZAK tarihli etkinlikleri getirdiği için
/// sayfalama kullanıcı için anlamsız oluyordu. sort parametresi additive'dir: verilmezse
/// (ve bilinmeyen değer verilirse) eski davranış (date_desc) korunur.
/// </summary>
public class EventsQueryTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly List<Guid> _createdEventIds = new();

    public EventsQueryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    /// <summary>Testin kendi verisi: sıralama iddiası seed'deki tarihlere bağlı kalmasın.</summary>
    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var categoryId = await db.EventCategories.Select(c => c.Id).FirstAsync();
        var today = DateTime.UtcNow.Date;

        // Yakın (+2 gün), uzak (+40 gün) ve geçmiş (-10 gün) birer etkinlik.
        var events = new[]
        {
            NewEvent(categoryId, "ZZ Yakın Etkinlik", today.AddDays(2), new TimeSpan(19, 0, 0)),
            NewEvent(categoryId, "ZZ Uzak Etkinlik", today.AddDays(40), new TimeSpan(10, 0, 0)),
            NewEvent(categoryId, "ZZ Geçmiş Etkinlik", today.AddDays(-10), new TimeSpan(20, 0, 0))
        };

        db.Events.AddRange(events);
        await db.SaveChangesAsync();
        _createdEventIds.AddRange(events.Select(e => e.Id));
    }

    /// <summary>Test verisi kalıcı DB'de kalmasın (oturum sonu temizliği).</summary>
    public async Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Events.Where(e => _createdEventIds.Contains(e.Id)).ExecuteDeleteAsync();
    }

    private static Event NewEvent(Guid categoryId, string title, DateTime date, TimeSpan time) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Description = "11.10 sıralama testi",
        CategoryId = categoryId,
        EventDate = DateTime.SpecifyKind(date, DateTimeKind.Utc),
        EventTime = time,
        IsFree = true,
        IsLocal = true,
        Status = "approved",
        CreatedBy = Guid.Empty,
        CreatedAt = DateTime.UtcNow
    };

    private async Task<List<string>> TitlesAsync(string url)
    {
        var response = await _client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").GetProperty("items")
            .EnumerateArray()
            .Select(x => x.GetProperty("title").GetString()!)
            .Where(t => t.StartsWith("ZZ "))
            .ToList();
    }

    [Fact]
    public async Task Sort_DateAsc_Returns_Nearest_First()
    {
        var titles = await TitlesAsync("/v1/events?sort=date_asc&limit=50");

        titles.Should().Contain("ZZ Geçmiş Etkinlik");
        titles.IndexOf("ZZ Geçmiş Etkinlik").Should().BeLessThan(titles.IndexOf("ZZ Yakın Etkinlik"));
        titles.IndexOf("ZZ Yakın Etkinlik").Should().BeLessThan(titles.IndexOf("ZZ Uzak Etkinlik"));
    }

    [Fact]
    public async Task Default_Sort_Stays_Descending()
    {
        var withoutSort = await TitlesAsync("/v1/events?limit=50");
        var unknownSort = await TitlesAsync("/v1/events?sort=hokus_pokus&limit=50");

        withoutSort.IndexOf("ZZ Uzak Etkinlik").Should().BeLessThan(withoutSort.IndexOf("ZZ Yakın Etkinlik"));
        // Bilinmeyen değer varsayılana düşer (istemci hatası listeyi bozmaz).
        unknownSort.Should().Equal(withoutSort);
    }

    [Fact]
    public async Task StartDate_Filters_Out_Past_Events()
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var titles = await TitlesAsync($"/v1/events?startDate={today}&sort=date_asc&limit=50");

        titles.Should().NotContain("ZZ Geçmiş Etkinlik");
        titles.Should().Contain("ZZ Yakın Etkinlik");
    }

    [Fact]
    public async Task EndDate_Returns_Only_Past_Events()
    {
        var yesterday = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
        var titles = await TitlesAsync($"/v1/events?endDate={yesterday}&limit=50");

        titles.Should().Contain("ZZ Geçmiş Etkinlik");
        titles.Should().NotContain("ZZ Yakın Etkinlik");
    }
}
