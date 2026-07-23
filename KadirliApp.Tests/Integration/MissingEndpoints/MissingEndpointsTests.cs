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

namespace KadirliApp.Tests.Integration.MissingEndpoints;

/// <summary>
/// Faz 10.8 doğrulaması: eksik public uçlar — complaints/my, files DELETE (sahiplik + referans),
/// hesap silme (anonimleştirme + token iptali), transport saat/durak CRUD + public DTO,
/// ads ?sort= whitelist + açıklama araması, announcements sayfalama + ?typeId=.
/// </summary>
public class MissingEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MissingEndpointsTests(CustomWebApplicationFactory factory)
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

    private async Task<string> GetAdminTokenAsync()
    {
        const string phone = "+905000000001";
        (await _client.PostAsJsonAsync("/v1/auth/login", new { phone })).StatusCode.Should().Be(HttpStatusCode.OK);
        var verify = await _client.PostAsJsonAsync("/v1/auth/verify-otp", new { phone, otp = "123456" });
        using var doc = JsonDocument.Parse(await verify.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    /// <summary>OTP+register akışıyla kullanıcı; access + refresh token ve userId döner.</summary>
    private async Task<(string Access, string Refresh, Guid UserId)> GetUserAsync(string phone, string username)
    {
        (await _client.PostAsJsonAsync("/v1/auth/login", new { phone })).StatusCode.Should().Be(HttpStatusCode.OK);
        var verify = await _client.PostAsJsonAsync("/v1/auth/verify-otp", new { phone, otp = "123456" });
        using var doc = JsonDocument.Parse(await verify.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");

        string access, refresh;
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
            var regData = regDoc.RootElement.GetProperty("data");
            access = regData.GetProperty("accessToken").GetString()!;
            refresh = regData.GetProperty("refreshToken").GetString()!;
        }
        else
        {
            access = data.GetProperty("accessToken").GetString()!;
            refresh = data.GetProperty("refreshToken").GetString()!;
        }

        var payload = access.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        using var payloadDoc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
        var userId = Guid.Parse(payloadDoc.RootElement.GetProperty("user_id").GetString()!);
        return (access, refresh, userId);
    }

    private HttpRequestMessage Authorized(HttpMethod method, string url, string token, HttpContent? content = null)
    {
        var req = new HttpRequestMessage(method, url) { Content = content };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    private async Task<Guid> UploadPngAsync(string token, string name)
    {
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13 };
        using var form = new MultipartFormDataContent { { new ByteArrayContent(pngBytes), "file", name } };
        var response = await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/files/upload", token, form));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task MyComplaints_RequiresAuth_And_ReturnsOnlyOwnComplaints()
    {
        var (tokenA, _, _) = await GetUserAsync("+905088880001", "endpuser1");
        var (tokenB, _, _) = await GetUserAsync("+905088880002", "endpuser2");

        (await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/complaints", tokenA,
            JsonContent.Create(new { subject = "A'nın şikayeti", message = "x" })))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/complaints", tokenB,
            JsonContent.Create(new { subject = "B'nin şikayeti", message = "x" })))).StatusCode.Should().Be(HttpStatusCode.OK);

        // Anonim istek 401
        (await _client.GetAsync("/v1/complaints/my")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // A yalnız kendi şikayetini görür
        var mine = await _client.SendAsync(Authorized(HttpMethod.Get, "/v1/complaints/my", tokenA));
        mine.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await mine.Content.ReadAsStringAsync());
        var subjects = doc.RootElement.GetProperty("data").GetProperty("items")
            .EnumerateArray().Select(i => i.GetProperty("subject").GetString()).ToList();
        subjects.Should().Contain("A'nın şikayeti");
        subjects.Should().NotContain("B'nin şikayeti", "başkasının şikayeti listelenmemeli");
    }

    [Fact]
    public async Task FileDelete_EnforcesOwnership_ReferenceCheck_And_SoftDeletes()
    {
        var (owner, _, ownerId) = await GetUserAsync("+905088880003", "endpuser3");
        var (other, _, _) = await GetUserAsync("+905088880004", "endpuser4");

        // Serbest (hiçbir kayda bağlı olmayan) dosya: başkası 403, sahibi 200, ikinci silme 404
        var freeFileId = await UploadPngAsync(owner, "serbest.png");
        (await _client.SendAsync(Authorized(HttpMethod.Delete, $"/v1/files/{freeFileId}", other)))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await _client.SendAsync(Authorized(HttpMethod.Delete, $"/v1/files/{freeFileId}", owner)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.SendAsync(Authorized(HttpMethod.Delete, $"/v1/files/{freeFileId}", owner)))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Global soft-delete filtresi (FileConfiguration.HasQueryFilter) silinen satırı gizler — IgnoreQueryFilters şart.
        var deletedAt = await InDbAsync(db =>
            db.Files.IgnoreQueryFilters().Where(f => f.Id == freeFileId).Select(f => f.DeletedAt).FirstAsync());
        deletedAt.Should().NotBeNull("soft delete: files.deleted_at dolmalı");

        // İlana bağlı dosya silinemez → 409 CONFLICT
        var usedFileId = await UploadPngAsync(owner, "ilanda.png");
        var categoryId = await InDbAsync(db =>
            db.AdCategories.Where(c => c.Name == "Motosiklet").Select(c => c.Id).FirstAsync());
        var createAd = await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/ads", owner, JsonContent.Create(new
        {
            categoryId,
            title = "Görsel referans testi",
            description = "dosya referans denemesi",
            price = 100,
            sellerName = "Endp User",
            contactPhone = "+905088880003",
            imageFileIds = new[] { usedFileId }
        })));
        createAd.StatusCode.Should().Be(HttpStatusCode.Created);

        var conflict = await _client.SendAsync(Authorized(HttpMethod.Delete, $"/v1/files/{usedFileId}", owner));
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict, "ilana bağlı dosya silinememeli");
        using var conflictDoc = JsonDocument.Parse(await conflict.Content.ReadAsStringAsync());
        conflictDoc.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("CONFLICT");
    }

    [Fact]
    public async Task AccountDeletion_AnonymizesUser_SoftDeletesAds_AndRevokesRefreshToken()
    {
        var (access, refresh, userId) = await GetUserAsync("+905088880005", "endpuser5");

        var categoryId = await InDbAsync(db =>
            db.AdCategories.Where(c => c.Name == "Motosiklet").Select(c => c.Id).FirstAsync());
        var createAd = await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/ads", access, JsonContent.Create(new
        {
            categoryId,
            title = "Silinecek hesabın ilanı",
            description = "hesap silme testi",
            price = 50,
            sellerName = "Endp User5",
            contactPhone = "+905088880005"
        })));
        createAd.StatusCode.Should().Be(HttpStatusCode.Created);

        // Anonim istek 401; admin/staff hesabı 403
        (await _client.DeleteAsync("/v1/users/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var adminToken = await GetAdminTokenAsync();
        (await _client.SendAsync(Authorized(HttpMethod.Delete, "/v1/users/me", adminToken)))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden, "admin hesabı mobil uçtan silinememeli");

        // Hesap silme (refresh token gövdede → iptal edilir)
        var delete = await _client.SendAsync(Authorized(HttpMethod.Delete, "/v1/users/me", access,
            JsonContent.Create(new { refreshToken = refresh })));
        delete.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await InDbAsync(db => db.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == userId));
        user.DeletedAt.Should().NotBeNull();
        user.IsActive.Should().BeFalse();
        user.Phone.Should().StartWith("del", "telefon anonimleştirilmeli (yeniden kayda açılır)");
        user.Username.Should().BeNull();
        user.FcmToken.Should().BeNull();

        var adDeleted = await InDbAsync(db =>
            db.Ads.IgnoreQueryFilters().Where(a => a.UserId == userId).Select(a => a.DeletedAt).FirstAsync());
        adDeleted.Should().NotBeNull("kullanıcının ilanı yayından düşmeli (soft delete)");

        // İptal edilen refresh 401; silinen hesapla tekrar silme 404
        (await _client.PostAsJsonAsync("/v1/auth/refresh", new { refreshToken = refresh }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await _client.SendAsync(Authorized(HttpMethod.Delete, "/v1/users/me", access)))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TransportSchedulesAndStops_AdminCrud_And_PublicDtos()
    {
        var admin = await GetAdminTokenAsync();

        var (routeId, intraRouteId) = await InDbAsync(async db =>
        {
            var intercity = new IntercityRoute { Destination = "Test Şehri", Price = 100, IsActive = true };
            var intracity = new IntracityRoute { RouteNumber = "T1", RouteName = "Test Hattı", IsActive = true };
            db.IntercityRoutes.Add(intercity);
            db.IntracityRoutes.Add(intracity);
            await db.SaveChangesAsync();
            return (intercity.Id, intracity.Id);
        });

        // Saat ekleme: geçerli 200, aynı saat 409, bozuk format 400, olmayan hat 404
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/admin/transport/intercity/{routeId}/schedules", admin,
            JsonContent.Create(new { departureTime = "07:30" })))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/admin/transport/intercity/{routeId}/schedules", admin,
            JsonContent.Create(new { departureTime = "07:30" })))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/admin/transport/intercity/{routeId}/schedules", admin,
            JsonContent.Create(new { departureTime = "kaçta" })))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/admin/transport/intercity/{Guid.NewGuid()}/schedules", admin,
            JsonContent.Create(new { departureTime = "09:00" })))).StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Durak ekleme: geçerli 200, aynı sıra 409
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/admin/transport/intracity/{intraRouteId}/stops", admin,
            JsonContent.Create(new { stopName = "Meydan", stopOrder = 1, timeFromStart = 0 })))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/admin/transport/intracity/{intraRouteId}/stops", admin,
            JsonContent.Create(new { stopName = "Başka Durak", stopOrder = 1 })))).StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Public DTO'lar: saatler "HH:mm" formatlı, duraklar sıralı
        using (var doc = JsonDocument.Parse(await _client.GetStringAsync("/v1/transport/intercity-routes?searchTerm=Test")))
        {
            var route = doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
                .First(r => r.GetProperty("id").GetGuid() == routeId);
            route.GetProperty("schedules").EnumerateArray()
                .Select(s => s.GetProperty("departureTime").GetString())
                .Should().ContainSingle().Which.Should().Be("07:30");
        }
        using (var doc = JsonDocument.Parse(await _client.GetStringAsync("/v1/transport/intracity-routes?searchTerm=Test")))
        {
            var route = doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
                .First(r => r.GetProperty("id").GetGuid() == intraRouteId);
            var stop = route.GetProperty("stops").EnumerateArray().Single();
            stop.GetProperty("stopName").GetString().Should().Be("Meydan");
            stop.GetProperty("stopOrder").GetInt32().Should().Be(1);
        }

        // Silme uçları — sonrasında public yanıttan da düşer
        var (scheduleId, stopId) = await InDbAsync(async db => (
            await db.IntercitySchedules.Where(s => s.RouteId == routeId).Select(s => s.Id).FirstAsync(),
            await db.IntracityStops.Where(s => s.RouteId == intraRouteId).Select(s => s.Id).FirstAsync()));
        (await _client.SendAsync(Authorized(HttpMethod.Delete, $"/v1/admin/transport/intercity/schedules/{scheduleId}", admin)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.SendAsync(Authorized(HttpMethod.Delete, $"/v1/admin/transport/intracity/stops/{stopId}", admin)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await _client.GetStringAsync("/v1/transport/intercity-routes?searchTerm=Test")))
        {
            doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
                .First(r => r.GetProperty("id").GetGuid() == routeId)
                .GetProperty("schedules").GetArrayLength().Should().Be(0);
        }
    }

    [Fact]
    public async Task AdsSort_Whitelist_And_SearchIncludesDescription()
    {
        var (_, _, userId) = await GetUserAsync("+905088880006", "endpuser6");
        var categoryId = await InDbAsync(db =>
            db.AdCategories.Where(c => c.Name == "Motosiklet").Select(c => c.Id).FirstAsync());

        await InDbAsync(async db =>
        {
            db.Ads.AddRange(
                new Ad { CategoryId = categoryId, Title = "Ucuz motosiklet", Description = "sorunsuz", Price = 100, UserId = userId, ContactPhone = "+905088880006", Status = "approved", ExpiresAt = DateTime.UtcNow.AddDays(30) },
                new Ad { CategoryId = categoryId, Title = "Pahalı motosiklet", Description = "çantaları hediyedir", Price = 900, UserId = userId, ContactPhone = "+905088880006", Status = "approved", ExpiresAt = DateTime.UtcNow.AddDays(30) });
            await db.SaveChangesAsync();
            return 0;
        });

        using (var doc = JsonDocument.Parse(await _client.GetStringAsync("/v1/ads?sort=price_asc&limit=50")))
        {
            var prices = doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
                .Select(i => i.GetProperty("price").GetDecimal()).ToList();
            prices.Should().BeInAscendingOrder();
        }
        using (var doc = JsonDocument.Parse(await _client.GetStringAsync("/v1/ads?sort=price_desc&limit=50")))
        {
            var prices = doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
                .Select(i => i.GetProperty("price").GetDecimal()).ToList();
            prices.Should().BeInDescendingOrder();
        }

        // Whitelist dışı sort 400 VALIDATION_ERROR
        var invalid = await _client.GetAsync("/v1/ads?sort=en_pahali");
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Arama açıklamada da çalışır ("hediyedir" yalnız description'da geçiyor)
        using (var doc = JsonDocument.Parse(await _client.GetStringAsync("/v1/ads?search=hediyedir")))
        {
            doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
                .Select(i => i.GetProperty("title").GetString())
                .Should().ContainSingle().Which.Should().Be("Pahalı motosiklet");
        }
    }

    [Fact]
    public async Task Announcements_Paging_And_TypeFilter()
    {
        var (typeAId, typeBId) = await InDbAsync(async db =>
        {
            var types = await db.AnnouncementTypes.OrderBy(t => t.DisplayOrder).Take(2).ToListAsync();
            db.Announcements.AddRange(
                new Announcement { TypeId = types[0].Id, Title = "Sayfalama 1", Body = "x", Status = "active" },
                new Announcement { TypeId = types[0].Id, Title = "Sayfalama 2", Body = "x", Status = "active" },
                new Announcement { TypeId = types[1].Id, Title = "Diğer tür", Body = "x", Status = "active" });
            await db.SaveChangesAsync();
            return (types[0].Id, types[1].Id);
        });

        // Sayfalama: limit=1 → tek kayıt, totalCount hepsi, totalPages tutarlı (PagedResult zarfı)
        using (var doc = JsonDocument.Parse(await _client.GetStringAsync("/v1/announcements?limit=1&page=1")))
        {
            var data = doc.RootElement.GetProperty("data");
            data.GetProperty("items").GetArrayLength().Should().Be(1);
            data.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(3);
            data.GetProperty("pageSize").GetInt32().Should().Be(1);
        }

        // Tür filtresi: yalnız o türün duyuruları
        using (var doc = JsonDocument.Parse(await _client.GetStringAsync($"/v1/announcements?typeId={typeBId}")))
        {
            var titles = doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
                .Select(i => i.GetProperty("title").GetString()).ToList();
            titles.Should().Contain("Diğer tür");
            titles.Should().NotContain("Sayfalama 1");
        }

        // Clamp public listede de geçerli (?limit=99999 → 50)
        using (var doc = JsonDocument.Parse(await _client.GetStringAsync("/v1/announcements?limit=99999")))
        {
            doc.RootElement.GetProperty("data").GetProperty("pageSize").GetInt32().Should().Be(50);
        }
    }
}
