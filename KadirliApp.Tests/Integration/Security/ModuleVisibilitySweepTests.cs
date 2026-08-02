using System.Text.Json;
using FluentAssertions;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Security;

/// <summary>
/// Faz 11.14 — **Liste seviyesinde** görünürlük süpürmesi. <see cref="PublicVisibilityTests"/>
/// çoğunlukla DETAY ucunu (404 mü?) deniyor; oysa sızıntının asıl acıttığı yer listedir:
/// onaylanmamış bir vefat ilanı ya da süresi dolmuş bir kampanya listede görünürse kimse
/// hata almaz, sadece **görünmemesi gereken içerik görünür**.
///
/// 📌 Kural (API_CONTRACT §11): public uçlar YALNIZ onaylı + aktif + silinmemiş + süresi
/// geçmemiş kaydı döndürür. Her modül için "gizli" ve "görünür" birer satır kurulur, listenin
/// yalnız görüneni verdiği doğrulanır.
/// </summary>
public class ModuleVisibilitySweepTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string Marker = "CLAUDE-VIS-11.14";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ModuleVisibilitySweepTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Campaigns.IgnoreQueryFilters().Where(c => c.Title.Contains(Marker)).ExecuteDeleteAsync();
        await db.Businesses.Where(b => b.BusinessName.Contains(Marker)).ExecuteDeleteAsync();
        await db.Events.IgnoreQueryFilters().Where(e => e.Title.Contains(Marker)).ExecuteDeleteAsync();
        await db.GuideItems.Where(g => g.Name.Contains(Marker)).ExecuteDeleteAsync();
        await db.DeathNotices.IgnoreQueryFilters().Where(d => d.DeceasedName.Contains(Marker)).ExecuteDeleteAsync();
        await db.Places.Where(p => p.Name.Contains(Marker)).ExecuteDeleteAsync();
    }

    private async Task<List<string>> ListNamesAsync(string url, string nameField)
    {
        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");
        var items = data.ValueKind == JsonValueKind.Array ? data : data.GetProperty("items");
        return items.EnumerateArray().Select(i => i.GetProperty(nameField).GetString()!).ToList();
    }

    private async Task WithDbAsync(Func<AppDbContext, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    // ------------------------------------------------------------------------ Rehber

    [Fact]
    public async Task GuideItems_List_HidesInactiveEntries()
    {
        await WithDbAsync(async db =>
        {
            var categoryId = await db.GuideCategories.Select(c => c.Id).FirstAsync();
            db.GuideItems.AddRange(
                new GuideItem { CategoryId = categoryId, Name = $"{Marker} Açık Kurum", IsActive = true },
                new GuideItem { CategoryId = categoryId, Name = $"{Marker} Kapalı Kurum", IsActive = false });
            await db.SaveChangesAsync();
        });

        var names = await ListNamesAsync("/v1/guide/items?limit=100", "name");

        names.Should().Contain($"{Marker} Açık Kurum");
        names.Should().NotContain($"{Marker} Kapalı Kurum",
            "controller dto.IsActive=true'yu zorluyor — kaldırılırsa kapalı kayıtlar rehbere sızar");
    }

    // ------------------------------------------------------------------------ Mekan

    [Fact]
    public async Task Places_List_HidesInactiveEntries()
    {
        await WithDbAsync(async db =>
        {
            var categoryId = await db.PlaceCategories.Select(c => c.Id).FirstAsync();
            db.Places.AddRange(
                new Place { CategoryId = categoryId, Name = $"{Marker} Açık Mekan", IsActive = true },
                new Place { CategoryId = categoryId, Name = $"{Marker} Kapalı Mekan", IsActive = false });
            await db.SaveChangesAsync();
        });

        var names = await ListNamesAsync("/v1/places?limit=100", "name");

        names.Should().Contain($"{Marker} Açık Mekan");
        names.Should().NotContain($"{Marker} Kapalı Mekan");
    }

    // --------------------------------------------------------------------- Etkinlik

    [Fact]
    public async Task Events_List_ShowsOnlyApproved_AndHidesSoftDeleted()
    {
        await WithDbAsync(async db =>
        {
            var categoryId = await db.EventCategories.Select(c => c.Id).FirstAsync();
            var day = DateTime.UtcNow.Date.AddDays(10);

            db.Events.AddRange(
                NewEvent($"{Marker} Onaylı Etkinlik", categoryId, day, "approved"),
                NewEvent($"{Marker} Bekleyen Etkinlik", categoryId, day, "pending"),
                NewEvent($"{Marker} Reddedilen Etkinlik", categoryId, day, "rejected"));
            await db.SaveChangesAsync();

            var deleted = NewEvent($"{Marker} Silinmiş Etkinlik", categoryId, day, "approved");
            deleted.DeletedAt = DateTime.UtcNow;
            db.Events.Add(deleted);
            await db.SaveChangesAsync();
        });

        var titles = await ListNamesAsync("/v1/events?limit=100", "title");

        titles.Should().Contain($"{Marker} Onaylı Etkinlik");
        titles.Should().NotContain($"{Marker} Bekleyen Etkinlik", "moderasyondan geçmemiş etkinlik listede olmamalı");
        titles.Should().NotContain($"{Marker} Reddedilen Etkinlik");
        titles.Should().NotContain($"{Marker} Silinmiş Etkinlik", "global soft-delete filtresi listede de geçerli");
    }

    private static Event NewEvent(string title, Guid categoryId, DateTime date, string status) => new()
    {
        Title = title,
        Description = "Görünürlük süpürmesi.",
        CategoryId = categoryId,
        EventDate = date,
        EventTime = new TimeSpan(19, 0, 0),
        Status = status,
        IsFree = true,
        CreatedBy = Guid.Empty
    };

    // --------------------------------------------------------------------- Vefat

    [Fact]
    public async Task DeathNotices_List_ShowsOnlyApproved()
    {
        await WithDbAsync(async db =>
        {
            db.DeathNotices.AddRange(
                NewDeath($"{Marker} Onaylı Merhum", "approved"),
                NewDeath($"{Marker} Bekleyen Merhum", "pending"));
            await db.SaveChangesAsync();
        });

        var names = await ListNamesAsync("/v1/deaths?limit=100", "deceasedName");

        names.Should().Contain($"{Marker} Onaylı Merhum");
        names.Should().NotContain($"{Marker} Bekleyen Merhum",
            "vefat ilanı hassas içerik — moderasyondan geçmeden yayınlanmamalı");
    }

    private static DeathNotice NewDeath(string name, string status) => new()
    {
        DeceasedName = name,
        FuneralDate = DateTime.UtcNow.Date.AddDays(1),
        FuneralTime = new TimeSpan(13, 0, 0),
        AddedBy = Guid.Empty,
        Status = status
    };

    // ------------------------------------------------------------------ Kampanya

    /// <summary>
    /// Kampanyanın "yürürlükte" olması iki koşula bağlı: onaylı olmak **ve** tarih aralığında
    /// olmak. Süresi dolan kampanya listede kalırsa kullanıcı kasada geçersiz kod gösterir.
    /// </summary>
    [Fact]
    public async Task Campaigns_List_HidesPendingAndExpiredOnes()
    {
        await WithDbAsync(async db =>
        {
            var categoryId = await db.BusinessCategories.Select(c => c.Id).FirstAsync();
            var business = new Business
            {
                CategoryId = categoryId, BusinessName = $"{Marker} Esnaf", Phone = "+903281110002", IsVerified = true
            };
            db.Businesses.Add(business);
            await db.SaveChangesAsync();

            var now = DateTime.UtcNow;
            db.Campaigns.AddRange(
                NewCampaign($"{Marker} Yürürlükteki Kampanya", business.Id, now.AddDays(-1), now.AddDays(10), "approved"),
                NewCampaign($"{Marker} Süresi Dolmuş Kampanya", business.Id, now.AddDays(-30), now.AddDays(-2), "approved"),
                NewCampaign($"{Marker} Bekleyen Kampanya", business.Id, now.AddDays(-1), now.AddDays(10), "pending"));
            await db.SaveChangesAsync();
        });

        var titles = await ListNamesAsync("/v1/campaigns?limit=100", "title");

        titles.Should().Contain($"{Marker} Yürürlükteki Kampanya");
        titles.Should().NotContain($"{Marker} Süresi Dolmuş Kampanya", "bitiş tarihi geçen kampanya listelenmemeli");
        titles.Should().NotContain($"{Marker} Bekleyen Kampanya");
    }

    private static Campaign NewCampaign(string title, Guid businessId, DateTime start, DateTime end, string status) => new()
    {
        BusinessId = businessId,
        Title = title,
        Description = "Görünürlük süpürmesi.",
        StartDate = start,
        EndDate = end,
        Status = status
    };
}
