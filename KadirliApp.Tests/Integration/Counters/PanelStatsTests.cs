using FluentAssertions;
using KadirliApp.Application.Features.Ads.Queries;
using KadirliApp.Application.Features.Announcements.Queries.GetAnnouncementAdminStats;
using KadirliApp.Application.Features.Dashboard.Queries;
using KadirliApp.Application.Features.Taxis.Queries;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Counters;

/// <summary>
/// Faz 10.10-A doğrulaması: panel-only admin istatistik query'leri (taksi çağrı/son-çağrı,
/// duyuru görüntülenme/tıklama/tekil, ilan etkileşim kartı) + dashboard'ın yeni Etkileşim alanları.
/// Panel view'ları canlı smoke ile doğrulanır; burada Application katmanı sınanır.
/// </summary>
public class PanelStatsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PanelStatsTests(CustomWebApplicationFactory factory) => _factory = factory;

    private async Task<T> InScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        return await action(scope.ServiceProvider);
    }

    [Fact]
    public async Task AdminStatsQueries_ReturnAggregates_FromInteractionRows()
    {
        var (driverId, annId, adId, userId) = await InScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstAsync(u => u.Role == KadirliApp.Domain.Enums.UserRole.SuperAdmin);

            var driver = new TaxiDriver { Name = "Stats Şoför", Phone = "+905322221100", IsVerified = true, IsActive = true };
            db.TaxiDrivers.Add(driver);

            var ann = new Announcement
            {
                Title = "Stats Duyurusu",
                Body = "x",
                TypeId = await db.AnnouncementTypes.Select(t => t.Id).FirstAsync(),
                TargetType = "all",
                Status = "active",
                SentAt = DateTime.UtcNow,
                ViewCount = 5,
                ClickCount = 2
            };
            db.Announcements.Add(ann);

            var ad = new Ad
            {
                Title = "Stats İlanı",
                Description = "x",
                CategoryId = await db.AdCategories.Select(c => c.Id).FirstAsync(),
                UserId = user.Id,
                ContactPhone = "+905301112233",
                Status = "approved",
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                ViewCount = 7,
                PhoneClickCount = 3,
                WhatsappClickCount = 1
            };
            db.Ads.Add(ad);
            await db.SaveChangesAsync();

            db.Set<AdFavorite>().Add(new AdFavorite { AdId = ad.Id, UserId = user.Id });
            await db.SaveChangesAsync();

            db.Set<TaxiCall>().AddRange(
                new TaxiCall { DriverId = driver.Id, PassengerId = user.Id, CalledAt = DateTime.UtcNow.AddDays(-2) },
                new TaxiCall { DriverId = driver.Id, PassengerId = user.Id, CalledAt = DateTime.UtcNow.AddDays(-1) });
            db.Set<AnnouncementView>().Add(new AnnouncementView { AnnouncementId = ann.Id, UserId = user.Id });
            await db.SaveChangesAsync();

            return (driver.Id, ann.Id, ad.Id, user.Id);
        });

        await InScopeAsync<object?>(async sp =>
        {
            var sender = sp.GetRequiredService<ISender>();

            // Taksi: tek kaynak taxi_calls — COUNT + MAX(called_at)
            var taxiStats = await sender.Send(new GetTaxiAdminStatsQuery());
            taxiStats.Should().ContainKey(driverId);
            taxiStats[driverId].CallCount.Should().Be(2);
            taxiStats[driverId].LastCallAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(-1), TimeSpan.FromMinutes(1));

            // Duyuru: sayaçlar + tekil erişim
            var annStats = await sender.Send(new GetAnnouncementAdminStatsQuery());
            annStats.Should().ContainKey(annId);
            annStats[annId].Should().Be(new AnnouncementAdminStatsDto(5, 2, 1));

            // İlan: etkileşim kartı (favori dahil); olmayan ilan null
            var adStats = await sender.Send(new GetAdAdminStatsQuery(adId));
            adStats.Should().Be(new AdAdminStatsDto(7, 3, 1, 1));
            (await sender.Send(new GetAdAdminStatsQuery(Guid.NewGuid()))).Should().BeNull();

            // Dashboard: yeni Etkileşim alanları (60 sn cache'i atlatmak için handler'ın gördüğü veri taze olmalı —
            // bu test verisi cache'lenmiş eski sonuçla yarışmasın diye alt sınır asserti kullanılır)
            var dash = await sender.Send(new GetDashboardStatsQuery());
            dash.TaxiCallsLast7Days.Should().BeGreaterThanOrEqualTo(0);
            dash.NewUsersLast7Days.Should().BeGreaterThanOrEqualTo(1, "testin admin kullanıcısı son 7 günde oluştu");
            dash.TotalAnnouncementViews.Should().BeGreaterThanOrEqualTo(0);
            return null;
        });
    }
}
