using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Counters;

/// <summary>
/// Faz 10.12 doğrulaması: duyuru view/click sayaçları (+announcement_views izi ve görünürlük 404'ü),
/// kampanya view-code (kod + kullanıcı başına tek kayıt + code_view_count; kodsuz kampanya 400),
/// taksi çağrısı (taxi_calls izi + total_calls; doğrulanmamış sürücü 404; anonim 401).
/// </summary>
public class CountersTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CountersTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private async Task<T> InDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(db);
    }

    private async Task<(string Access, Guid UserId)> GetUserAsync(string phone, string username)
    {
        (await _client.PostAsJsonAsync("/v1/auth/login", new { phone })).StatusCode.Should().Be(HttpStatusCode.OK);
        var verify = await _client.PostAsJsonAsync("/v1/auth/verify-otp", new { phone, otp = "123456" });
        using var doc = JsonDocument.Parse(await verify.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");

        string access;
        if (data.GetProperty("isNewUser").GetBoolean())
        {
            var neighborhoodId = await InDbAsync(db =>
                db.Neighborhoods.Where(n => n.IsActive).Select(n => n.Id).FirstAsync());
            var register = await _client.PostAsJsonAsync("/v1/auth/register", new
            {
                tempToken = data.GetProperty("tempToken").GetString(),
                username,
                primaryNeighborhoodId = neighborhoodId
            });
            register.StatusCode.Should().Be(HttpStatusCode.OK);
            using var regDoc = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
            access = regDoc.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
        }
        else
        {
            access = data.GetProperty("accessToken").GetString()!;
        }

        var payload = access.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        using var payloadDoc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
        var userId = Guid.Parse(payloadDoc.RootElement.GetProperty("user_id").GetString()!);
        return (access, userId);
    }

    private HttpRequestMessage Authorized(HttpMethod method, string url, string token, HttpContent? content = null)
    {
        var req = new HttpRequestMessage(method, url) { Content = content };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    private async Task<Guid> CreateActiveAnnouncementAsync(string title)
    {
        return await InDbAsync(async db =>
        {
            var ann = new Announcement
            {
                Title = title,
                Body = "sayaç testi",
                TypeId = await db.AnnouncementTypes.Select(t => t.Id).FirstAsync(),
                TargetType = "all",
                Status = "active",
                SentAt = DateTime.UtcNow,
                SendPushNotification = false
            };
            db.Announcements.Add(ann);
            await db.SaveChangesAsync();
            return ann.Id;
        });
    }

    [Fact]
    public async Task AnnouncementViewAndClick_IncrementCounters_And_TrackAuthorizedViewer()
    {
        var annId = await CreateActiveAnnouncementAsync("Sayaç Duyurusu");
        var (token, userId) = await GetUserAsync("+905066660001", "counteruser1");

        // Anonim iki view + bir click
        (await _client.PostAsync($"/v1/announcements/{annId}/view", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PostAsync($"/v1/announcements/{annId}/view", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PostAsync($"/v1/announcements/{annId}/click", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var anon = await InDbAsync(db => db.Announcements.Where(a => a.Id == annId)
            .Select(a => new { a.ViewCount, a.ClickCount }).FirstAsync());
        anon.ViewCount.Should().Be(2);
        anon.ClickCount.Should().Be(1);
        (await InDbAsync(db => db.Set<AnnouncementView>().CountAsync(v => v.AnnouncementId == annId)))
            .Should().Be(0, "anonim view announcement_views'a iz düşmez");

        // Giriş yapmış kullanıcı iki kez view: sayaç her seferinde artar, iz TEK satır kalır
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/announcements/{annId}/view", token)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/announcements/{annId}/view", token)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await InDbAsync(db => db.Announcements.Where(a => a.Id == annId).Select(a => a.ViewCount).FirstAsync()))
            .Should().Be(4);
        (await InDbAsync(db => db.Set<AnnouncementView>()
            .CountAsync(v => v.AnnouncementId == annId && v.UserId == userId))).Should().Be(1);

        // Yayında olmayan duyurunun sayacı artmaz → 404
        var scheduledId = await InDbAsync(async db =>
        {
            var ann = new Announcement
            {
                Title = "Zamanlanmış Sayaç",
                Body = "x",
                TypeId = await db.AnnouncementTypes.Select(t => t.Id).FirstAsync(),
                TargetType = "all",
                Status = "scheduled",
                ScheduledFor = DateTime.UtcNow.AddDays(1),
                SendPushNotification = false
            };
            db.Announcements.Add(ann);
            await db.SaveChangesAsync();
            return ann.Id;
        });
        (await _client.PostAsync($"/v1/announcements/{scheduledId}/view", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CampaignViewCode_ReturnsCode_TracksOncePerUser_And_RejectsCodeless()
    {
        var (campaignId, codelessId) = await InDbAsync(async db =>
        {
            var category = db.BusinessCategories.FirstOrDefault();
            if (category == null)
            {
                category = new BusinessCategory { Name = "Sayaç Test Kategorisi", Slug = "sayac-test-kategorisi" };
                db.BusinessCategories.Add(category);
            }
            var business = new Business { BusinessName = "Sayaç Test İşletmesi", Category = category };
            var withCode = new Campaign
            {
                Business = business,
                Title = "Kodlu Kampanya",
                Description = "x",
                DiscountCode = "KADIRLI10",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = "approved"
            };
            var withoutCode = new Campaign
            {
                Business = business,
                Title = "Kodsuz Kampanya",
                Description = "x",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = "approved"
            };
            db.Campaigns.AddRange(withCode, withoutCode);
            await db.SaveChangesAsync();
            return (withCode.Id, withoutCode.Id);
        });

        // Anonim 401
        (await _client.PostAsync($"/v1/campaigns/{campaignId}/view-code", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var (token, userId) = await GetUserAsync("+905066660001", "counteruser1");

        // İlk istek: kod + iz + sayaç
        var first = await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/campaigns/{campaignId}/view-code", token));
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await first.Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("data").GetProperty("code").GetString().Should().Be("KADIRLI10");

        // İkinci istek: aynı kayıt — yeni satır yok, sayaç artmaz
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/campaigns/{campaignId}/view-code", token)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await InDbAsync(db => db.Set<CampaignCodeView>()
            .CountAsync(v => v.CampaignId == campaignId && v.UserId == userId))).Should().Be(1);
        (await InDbAsync(db => db.Campaigns.Where(c => c.Id == campaignId).Select(c => c.CodeViewCount).FirstAsync()))
            .Should().Be(1);

        // Kodsuz kampanya 400, iz düşülmez
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/campaigns/{codelessId}/view-code", token)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await InDbAsync(db => db.Set<CampaignCodeView>().CountAsync(v => v.CampaignId == codelessId))).Should().Be(0);
    }

    [Fact]
    public async Task TaxiCall_RecordsCall_And_RejectsUnverifiedOrAnonymous()
    {
        var (verifiedId, unverifiedId) = await InDbAsync(async db =>
        {
            var verified = new TaxiDriver { Name = "Sayaç Şoför", Phone = "+905311112233", IsVerified = true, IsActive = true };
            var unverified = new TaxiDriver { Name = "Sayaç Şoför 2", Phone = "+905311112234", IsVerified = false, IsActive = true };
            db.TaxiDrivers.AddRange(verified, unverified);
            await db.SaveChangesAsync();
            return (verified.Id, unverified.Id);
        });

        // Anonim 401
        (await _client.PostAsync($"/v1/taxis/drivers/{verifiedId}/call", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var (token, userId) = await GetUserAsync("+905066660002", "counteruser2");

        var call = await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/taxis/drivers/{verifiedId}/call", token));
        call.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await call.Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("data").GetProperty("phone").GetString().Should().Be("+905311112233");

        // İkinci çağrı yeni satırdır (tekrarlanabilir eylem)
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/taxis/drivers/{verifiedId}/call", token)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await InDbAsync(db => db.Set<TaxiCall>()
            .CountAsync(c => c.DriverId == verifiedId && c.PassengerId == userId))).Should().Be(2);
        (await InDbAsync(db => db.TaxiDrivers.Where(d => d.Id == verifiedId).Select(d => d.TotalCalls).FirstAsync()))
            .Should().Be(2);

        // Doğrulanmamış sürücü aranamaz → 404, sayaç/iz yok
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/taxis/drivers/{unverifiedId}/call", token)))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await InDbAsync(db => db.Set<TaxiCall>().CountAsync(c => c.DriverId == unverifiedId))).Should().Be(0);
    }
}
