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

namespace KadirliApp.Tests.Integration.Security;

/// <summary>
/// Faz 10.7 doğrulaması: public uçlarda görünürlük kuralları — pending/pasif/doğrulanmamış/süresi geçmiş
/// kayıtlar liste ve detayda dönmez; istemcinin ?status=/?isVerified=/?isActive= parametreleri public uçta
/// etkisizdir; Page/Limit clamp'lenir (?limit=99999 → public 50).
/// </summary>
public class PublicVisibilityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PublicVisibilityTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private async Task<T> InDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(db);
    }

    /// <summary>DevMode OTP akışıyla normal kullanıcı token'ı (PublicEndpointAuthorizationTests deseni).</summary>
    private async Task<(string Token, Guid UserId)> GetUserTokenAsync(string phone)
    {
        var login = await _client.PostAsJsonAsync("/v1/auth/login", new { phone });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var verify = await _client.PostAsJsonAsync("/v1/auth/verify-otp", new { phone, otp = "123456" });
        verify.StatusCode.Should().Be(HttpStatusCode.OK);

        string token;
        using (var verifyDoc = JsonDocument.Parse(await verify.Content.ReadAsStringAsync()))
        {
            var data = verifyDoc.RootElement.GetProperty("data");
            if (data.GetProperty("isNewUser").GetBoolean())
            {
                var neighborhoodId = await InDbAsync(db =>
                    db.Neighborhoods.Where(n => n.IsActive).Select(n => n.Id).FirstAsync());

                var register = await _client.PostAsJsonAsync("/v1/auth/register", new
                {
                    tempToken = data.GetProperty("tempToken").GetString(),
                    username = $"vistest{phone[^7..]}",
                    primaryNeighborhoodId = neighborhoodId
                });
                register.StatusCode.Should().Be(HttpStatusCode.OK);

                using var registerDoc = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
                token = registerDoc.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
            }
            else
            {
                token = data.GetProperty("accessToken").GetString()!;
            }
        }

        var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        using var payloadDoc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
        var userId = Guid.Parse(payloadDoc.RootElement.GetProperty("user_id").GetString()!);

        return (token, userId);
    }

    private static IEnumerable<Guid> ItemIds(JsonElement data)
        => data.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid());

    [Fact]
    public async Task DeathNotices_PublicList_ShouldOnlyReturnApproved_AndClampLimit()
    {
        var (_, userId) = await GetUserTokenAsync("+905077770001");
        var funeralDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        var (approvedId, pendingId, archivedId) = await InDbAsync(async db =>
        {
            var approved = new DeathNotice { DeceasedName = "Onaylı Merhum", FuneralDate = funeralDate, FuneralTime = new TimeSpan(11, 0, 0), AddedBy = userId, Status = "approved" };
            var pending = new DeathNotice { DeceasedName = "Bekleyen Merhum", FuneralDate = funeralDate, FuneralTime = new TimeSpan(11, 0, 0), AddedBy = userId, Status = "pending" };
            var archived = new DeathNotice { DeceasedName = "Arşiv Merhum", FuneralDate = funeralDate, FuneralTime = new TimeSpan(11, 0, 0), AddedBy = userId, Status = "archived" };
            db.DeathNotices.AddRange(approved, pending, archived);
            await db.SaveChangesAsync();
            return (approved.Id, pending.Id, archived.Id);
        });

        // İstemcinin ?status=pending isteği public uçta ETKİSİZ + limit clamp'lenir (99999 → 50)
        var response = await _client.GetAsync("/v1/deaths?status=pending&limit=99999");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("pageSize").GetInt32().Should().Be(50, "public listede limit 50'ye clamp'lenmeli");

        var ids = ItemIds(data).ToList();
        ids.Should().Contain(approvedId);
        ids.Should().NotContain(pendingId, "moderasyondan geçmemiş ilan public listede olmamalı");
        ids.Should().NotContain(archivedId, "arşivlenmiş ilan public listede olmamalı");
    }

    [Fact]
    public async Task DeathNotice_PendingDetail_ShouldReturn404_ExceptForOwner()
    {
        var (token, userId) = await GetUserTokenAsync("+905077770002");
        var funeralDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        var pendingId = await InDbAsync(async db =>
        {
            var pending = new DeathNotice { DeceasedName = "Sahipli Bekleyen", FuneralDate = funeralDate, FuneralTime = new TimeSpan(14, 0, 0), AddedBy = userId, Status = "pending" };
            db.DeathNotices.Add(pending);
            await db.SaveChangesAsync();
            return pending.Id;
        });

        // Anonim istek: id bilinse bile 404
        var anonymous = await _client.GetAsync($"/v1/deaths/{pendingId}");
        anonymous.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Ekleyen kullanıcı kendi pending ilanını görür
        var ownerReq = new HttpRequestMessage(HttpMethod.Get, $"/v1/deaths/{pendingId}");
        ownerReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var owner = await _client.SendAsync(ownerReq);
        owner.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EventAndCampaign_Detail_ShouldHidePendingAndExpired()
    {
        var now = DateTime.UtcNow;

        var (pendingEventId, approvedEventId) = await InDbAsync(async db =>
        {
            var categoryId = await db.EventCategories.Select(c => c.Id).FirstAsync();
            var pending = new Event { Title = "Bekleyen Etkinlik", Description = "x", CategoryId = categoryId, EventDate = now.AddDays(3), EventTime = new TimeSpan(20, 0, 0), Status = "pending", CreatedBy = Guid.NewGuid() };
            var approved = new Event { Title = "Onaylı Etkinlik", Description = "x", CategoryId = categoryId, EventDate = now.AddDays(3), EventTime = new TimeSpan(20, 0, 0), Status = "approved", CreatedBy = Guid.NewGuid() };
            db.Events.AddRange(pending, approved);
            await db.SaveChangesAsync();
            return (pending.Id, approved.Id);
        });

        (await _client.GetAsync($"/v1/events/{pendingEventId}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound, "pending etkinlik id ile okunamamalı");
        (await _client.GetAsync($"/v1/events/{approvedEventId}")).StatusCode
            .Should().Be(HttpStatusCode.OK);

        var (pendingCampaignId, expiredCampaignId, activeCampaignId) = await InDbAsync(async db =>
        {
            var businessCategoryId = await db.BusinessCategories.Select(c => c.Id).FirstAsync();
            var business = new Business { BusinessName = "Görünürlük Test İşletmesi", CategoryId = businessCategoryId };
            db.Businesses.Add(business);

            var pending = new Campaign { Business = business, Title = "Bekleyen Kampanya", Description = "x", StartDate = now.AddDays(-1), EndDate = now.AddDays(5), Status = "pending" };
            var expired = new Campaign { Business = business, Title = "Süresi Geçmiş", Description = "x", StartDate = now.AddDays(-10), EndDate = now.AddDays(-1), Status = "approved" };
            var active = new Campaign { Business = business, Title = "Aktif Kampanya", Description = "x", StartDate = now.AddDays(-1), EndDate = now.AddDays(5), Status = "approved" };
            db.Campaigns.AddRange(pending, expired, active);
            await db.SaveChangesAsync();
            return (pending.Id, expired.Id, active.Id);
        });

        (await _client.GetAsync($"/v1/campaigns/{pendingCampaignId}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound, "pending kampanya id ile okunamamalı");
        (await _client.GetAsync($"/v1/campaigns/{expiredCampaignId}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound, "süresi geçmiş kampanya public detayda dönmemeli");
        (await _client.GetAsync($"/v1/campaigns/{activeCampaignId}")).StatusCode
            .Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Announcement_NonActiveDetail_ShouldNotBeVisibleOnPublicEndpoint()
    {
        var (pendingId, activeId) = await InDbAsync(async db =>
        {
            var typeId = await db.AnnouncementTypes.Select(t => t.Id).FirstAsync();
            var pending = new Announcement { TypeId = typeId, Title = "Bekleyen Duyuru", Body = "x", Status = "pending" };
            var active = new Announcement { TypeId = typeId, Title = "Yayında Duyuru", Body = "x", Status = "active", VisibleUntil = DateTime.UtcNow.AddDays(2) };
            db.Announcements.AddRange(pending, active);
            await db.SaveChangesAsync();
            return (pending.Id, active.Id);
        });

        // Not: Announcements NOT_FOUND'u ApiResponse.FailureResponse ile döner (HTTP 200 + success:false — 10.13 zarf konusu)
        using var pendingDoc = JsonDocument.Parse(await _client.GetStringAsync($"/v1/announcements/{pendingId}"));
        pendingDoc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse("pending duyuru public detayda dönmemeli");
        pendingDoc.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("NOT_FOUND");

        using var activeDoc = JsonDocument.Parse(await _client.GetStringAsync($"/v1/announcements/{activeId}"));
        activeDoc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task TaxiDrivers_PublicEndpoint_ShouldForceVerifiedAndActive()
    {
        var (verifiedId, unverifiedId, inactiveId) = await InDbAsync(async db =>
        {
            var verified = new TaxiDriver { Name = "Doğrulanmış Şoför", Phone = "+905071112233", IsVerified = true, IsActive = true };
            var unverified = new TaxiDriver { Name = "Doğrulanmamış Şoför", Phone = "+905071112234", IsVerified = false, IsActive = true };
            var inactive = new TaxiDriver { Name = "Pasif Şoför", Phone = "+905071112235", IsVerified = true, IsActive = false };
            db.TaxiDrivers.AddRange(verified, unverified, inactive);
            await db.SaveChangesAsync();
            return (verified.Id, unverified.Id, inactive.Id);
        });

        // İstemcinin ?isVerified=false parametresi public uçta ETKİSİZ
        var response = await _client.GetAsync("/v1/taxis/drivers?isVerified=false&isActive=false&limit=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var ids = ItemIds(doc.RootElement.GetProperty("data")).ToList();

        ids.Should().Contain(verifiedId);
        ids.Should().NotContain(unverifiedId, "doğrulanmamış sürücü telefonuyla public listede dönmemeli");
        ids.Should().NotContain(inactiveId, "pasif sürücü public listede dönmemeli");

        (await _client.GetAsync($"/v1/taxis/drivers/{unverifiedId}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound, "doğrulanmamış sürücü detayı da dönmemeli");
    }

    [Fact]
    public async Task InactivePlacePharmacyAndRoutes_ShouldBeHiddenFromPublic()
    {
        var (activePlaceId, inactivePlaceId) = await InDbAsync(async db =>
        {
            var categoryId = await db.PlaceCategories.Select(c => c.Id).FirstAsync();
            var active = new Place { Name = "Aktif Mekan", CategoryId = categoryId, Latitude = 37.4m, Longitude = 36.1m, IsActive = true };
            var inactive = new Place { Name = "Pasif Mekan", CategoryId = categoryId, Latitude = 37.4m, Longitude = 36.1m, IsActive = false };
            db.Places.AddRange(active, inactive);
            await db.SaveChangesAsync();
            return (active.Id, inactive.Id);
        });

        using (var doc = JsonDocument.Parse(await _client.GetStringAsync("/v1/places?limit=50")))
        {
            var ids = ItemIds(doc.RootElement.GetProperty("data")).ToList();
            ids.Should().Contain(activePlaceId);
            ids.Should().NotContain(inactivePlaceId, "pasif mekan public listede dönmemeli");
        }
        (await _client.GetAsync($"/v1/places/{inactivePlaceId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var inactivePharmacyId = await InDbAsync(async db =>
        {
            var inactive = new Pharmacy { Name = "Pasif Eczane", Address = "x", Phone = "+903281112233", IsActive = false };
            db.Pharmacies.Add(inactive);
            await db.SaveChangesAsync();
            return inactive.Id;
        });

        // Pasif eczane: ?isActive=false istemci parametresi de public uçta etkisiz (liste cache'lidir — ilk istek DB'den)
        using (var doc = JsonDocument.Parse(await _client.GetStringAsync("/v1/pharmacies?isActive=false&limit=50")))
        {
            ItemIds(doc.RootElement.GetProperty("data")).Should().NotContain(inactivePharmacyId, "pasif eczane public listede dönmemeli");
        }
        (await _client.GetAsync($"/v1/pharmacies/{inactivePharmacyId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var (inactiveIntercityId, inactiveIntracityId) = await InDbAsync(async db =>
        {
            var intercity = new IntercityRoute { Destination = "Pasif Şehir", IsActive = false };
            var intracity = new IntracityRoute { RouteNumber = "99Z", RouteName = "Pasif Hat", IsActive = false };
            db.IntercityRoutes.Add(intercity);
            db.IntracityRoutes.Add(intracity);
            await db.SaveChangesAsync();
            return (intercity.Id, intracity.Id);
        });

        using (var doc = JsonDocument.Parse(await _client.GetStringAsync("/v1/transport/intercity-routes?limit=50")))
        {
            ItemIds(doc.RootElement.GetProperty("data")).Should().NotContain(inactiveIntercityId, "pasif şehirlerarası hat public listede dönmemeli");
        }
        using (var doc = JsonDocument.Parse(await _client.GetStringAsync("/v1/transport/intracity-routes?limit=50")))
        {
            ItemIds(doc.RootElement.GetProperty("data")).Should().NotContain(inactiveIntracityId, "pasif şehir içi hat public listede dönmemeli");
        }
    }
}
