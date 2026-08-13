using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Jobs;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Notifications;

/// <summary>
/// Faz 10.10 doğrulaması: duyuru yayınında bildirim üretimi (hedefleme + tercih + push bayrağı),
/// GET /v1/notifications (+unreadOnly/unreadCount), PATCH {id}/read (sahiplik), POST read-all,
/// PublishScheduledAnnouncementsJob idempotency'si (iki koşu → mükerrer bildirim yok).
/// </summary>
public class NotificationsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NotificationsTests(CustomWebApplicationFactory factory)
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

    /// <summary>OTP+register akışıyla kullanıcı — mahallesi parametreyle kontrol edilir.</summary>
    private async Task<(string Access, Guid UserId)> GetUserAsync(string phone, string username, Guid neighborhoodId)
    {
        (await _client.PostAsJsonAsync("/v1/auth/login", new { phone })).StatusCode.Should().Be(HttpStatusCode.OK);
        var verify = await _client.PostAsJsonAsync("/v1/auth/verify-otp", new { phone, otp = "123456" });
        using var doc = JsonDocument.Parse(await verify.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");

        string access;
        if (data.GetProperty("isNewUser").GetBoolean())
        {
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

    private async Task<Guid[]> GetTwoNeighborhoodIdsAsync() =>
        await InDbAsync(db => db.Neighborhoods.Where(n => n.IsActive)
            .OrderBy(n => n.Name).Select(n => n.Id).Take(2).ToArrayAsync());

    private async Task<Guid> GetAnnouncementTypeIdAsync() =>
        await InDbAsync(db => db.AnnouncementTypes.Select(t => t.Id).FirstAsync());

    private async Task<Guid> CreateAnnouncementAsync(string adminToken, object body)
    {
        var response = await _client.SendAsync(
            Authorized(HttpMethod.Post, "/v1/admin/announcements", adminToken, JsonContent.Create(body)));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // CreateAnnouncementCommand eski ApiResponse<Guid> desenini döner (filter çift sarmaz) → data doğrudan guid.
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").GetGuid();
    }

    private async Task<int> NotificationCountAsync(Guid announcementId, Guid? userId = null) =>
        await InDbAsync(db => db.Notifications.CountAsync(n =>
            n.RelatedId == announcementId && (userId == null || n.UserId == userId)));

    [Fact]
    public async Task ImmediateAnnouncement_GeneratesRows_RespectsPreference_And_ReadFlowsWork()
    {
        var admin = await GetAdminTokenAsync();
        var hoods = await GetTwoNeighborhoodIdsAsync();
        var typeId = await GetAnnouncementTypeIdAsync();

        var (tokenA, userA) = await GetUserAsync("+905077770001", "notifuser1", hoods[0]);
        var (tokenC, userC) = await GetUserAsync("+905077770003", "notifuser3", hoods[0]);

        // C duyuru bildirimlerini kapatır — üretim anında tercih uygulanmalı
        (await _client.SendAsync(Authorized(HttpMethod.Patch, "/v1/users/me/notifications", tokenC,
            JsonContent.Create(new { announcements = false })))).StatusCode.Should().Be(HttpStatusCode.OK);

        var annId = await CreateAnnouncementAsync(admin, new
        {
            title = "Bildirim Testi Herkese",
            body = "Anında yayınlanan duyuru",
            typeId,
            targetType = "all",
            sendPushNotification = true
        });

        (await NotificationCountAsync(annId, userA)).Should().Be(1);
        (await NotificationCountAsync(annId, userC)).Should().Be(0, "tercihi kapalı kullanıcıya satır yazılmamalı");

        // Anonim istek 401
        (await _client.GetAsync("/v1/notifications")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // A listesinde bildirim + unreadCount
        var list = await _client.SendAsync(Authorized(HttpMethod.Get, "/v1/notifications?unreadOnly=true", tokenA));
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        Guid notifId;
        using (var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync()))
        {
            var data = doc.RootElement.GetProperty("data");
            data.GetProperty("unreadCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
            var item = data.GetProperty("items").EnumerateArray()
                .First(i => i.GetProperty("relatedId").GetGuid() == annId);
            item.GetProperty("title").GetString().Should().Be("Bildirim Testi Herkese");
            item.GetProperty("isRead").GetBoolean().Should().BeFalse();
            item.GetProperty("relatedType").GetString().Should().Be("announcement");
            notifId = item.GetProperty("id").GetGuid();
        }

        // Sahiplik: C başkasının bildirimini okuyamaz → 404
        (await _client.SendAsync(Authorized(HttpMethod.Patch, $"/v1/notifications/{notifId}/read", tokenC)))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        // A okur; ikinci çağrı idempotent
        (await _client.SendAsync(Authorized(HttpMethod.Patch, $"/v1/notifications/{notifId}/read", tokenA)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.SendAsync(Authorized(HttpMethod.Patch, $"/v1/notifications/{notifId}/read", tokenA)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var isRead = await InDbAsync(db => db.Notifications.Where(n => n.Id == notifId)
            .Select(n => new { n.IsRead, n.ReadAt }).FirstAsync());
        isRead.IsRead.Should().BeTrue();
        isRead.ReadAt.Should().NotBeNull();

        // İkinci duyuru → read-all hepsini kapatır
        await CreateAnnouncementAsync(admin, new
        {
            title = "Bildirim Testi 2",
            body = "x",
            typeId,
            targetType = "all",
            sendPushNotification = true
        });
        // 🔴 §7 madde 17 (Faz 0 denetimi — B4): `unreadCount` rozeti SAYFADAN ve SÜZGEÇTEN
        // BAĞIMSIZ bir toplamdır — sayfalı gövdenin İÇİNDE taşınır ama gövdeye ait değildir.
        //
        // ⚠️ İddianın şekli özenle seçildi: yalnız "süzgeçli ve süzgeçsiz istek aynı sayacı
        // versin" demek bu uçta neredeyse TOTOLOJİDİR — süzgeç zaten "okunmamışlar" olduğu
        // için içindeki okunmamış sayısı toplamla matematiksel olarak eşittir ve hiçbir
        // makul bozma onu kırmazdı (yani "iddiası zayıf test" sınıfının ta kendisi olurdu).
        // Gerçekten kırılabilen iddia SAYFALAMADIR: sayaç listeden türetilirse `limit=1`
        // isteğinde rozet "1 okunmamış" der, oysa kullanıcının 2 okunmamış bildirimi vardır.
        var thirdAnnId = await CreateAnnouncementAsync(admin, new
        {
            title = "Bildirim Testi 3",
            body = "x",
            typeId,
            targetType = "all",
            sendPushNotification = true
        });
        (await NotificationCountAsync(thirdAnnId, userA)).Should().Be(1);

        // A'nın durumu: 1 okunmuş + 2 okunmamış bildirim.
        using (var doc = JsonDocument.Parse(await (await _client.SendAsync(
                   Authorized(HttpMethod.Get, "/v1/notifications?limit=1", tokenA))).Content.ReadAsStringAsync()))
        {
            var data = doc.RootElement.GetProperty("data");
            data.GetProperty("items").GetArrayLength().Should().Be(1, "sayfa boyutu 1 istendi");
            data.GetProperty("unreadCount").GetInt32().Should().Be(2,
                "unreadCount SAYFADAN bağımsızdır (§7 madde 17): sayaç listeden türetilirse " +
                "rozet sayfa boyutuna göre küçülür ve kullanıcı bildirimlerinin bir kısmını " +
                "hiç görmediğini anlamaz — hata da almaz");
        }

        // Süzgeç ayağı: liste daralır, sayaç daralmaz.
        using (var doc = JsonDocument.Parse(await (await _client.SendAsync(
                   Authorized(HttpMethod.Get, "/v1/notifications?unreadOnly=true&limit=1", tokenA))).Content.ReadAsStringAsync()))
        {
            var data = doc.RootElement.GetProperty("data");
            data.GetProperty("unreadCount").GetInt32().Should().Be(2,
                "unreadOnly süzgeci rozeti değiştirmez — sayaç filtreden bağımsız toplamdır");
        }

        var readAll = await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/notifications/read-all", tokenA));
        readAll.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await readAll.Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("data").GetProperty("markedCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);

        var after = await _client.SendAsync(Authorized(HttpMethod.Get, "/v1/notifications", tokenA));
        using (var doc = JsonDocument.Parse(await after.Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("data").GetProperty("unreadCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task NeighborhoodTargeting_OnlyMatchingUsersGetRows()
    {
        var admin = await GetAdminTokenAsync();
        var hoods = await GetTwoNeighborhoodIdsAsync();
        var typeId = await GetAnnouncementTypeIdAsync();

        var (_, userA) = await GetUserAsync("+905077770001", "notifuser1", hoods[0]);
        var (_, userB) = await GetUserAsync("+905077770002", "notifuser2", hoods[1]);

        var annId = await CreateAnnouncementAsync(admin, new
        {
            title = "Mahalle Hedefli",
            body = "Sadece ilk mahalle",
            typeId,
            targetType = "neighborhood",
            targetNeighborhoodIds = new[] { hoods[0] },
            sendPushNotification = true
        });

        (await NotificationCountAsync(annId, userA)).Should().Be(1);
        (await NotificationCountAsync(annId, userB)).Should().Be(0, "başka mahalledeki kullanıcıya satır yazılmamalı");
    }

    [Fact]
    public async Task PushDisabledAnnouncement_WritesNoRows()
    {
        var admin = await GetAdminTokenAsync();
        var typeId = await GetAnnouncementTypeIdAsync();

        var annId = await CreateAnnouncementAsync(admin, new
        {
            title = "Sessiz Duyuru",
            body = "Push kapalı",
            typeId,
            targetType = "all",
            sendPushNotification = false
        });

        (await NotificationCountAsync(annId)).Should().Be(0, "sendPushNotification=false ise satır da yazılmamalı (10.10 kararı)");
    }

    [Fact]
    public async Task ScheduledJob_PublishesAndGeneratesOnce_EvenWhenRunTwice()
    {
        var hoods = await GetTwoNeighborhoodIdsAsync();
        var typeId = await GetAnnouncementTypeIdAsync();
        await GetUserAsync("+905077770001", "notifuser1", hoods[0]); // en az bir hedef kullanıcı olsun

        var annId = await InDbAsync(async db =>
        {
            var ann = new Announcement
            {
                Title = "Zamanlanmış Duyuru",
                Body = "Job testi",
                TypeId = typeId,
                TargetType = "all",
                Status = "scheduled",
                ScheduledFor = DateTime.UtcNow.AddMinutes(-5),
                SendPushNotification = true
            };
            db.Announcements.Add(ann);
            await db.SaveChangesAsync();
            return ann.Id;
        });

        async Task RunJobAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var job = ActivatorUtilities.CreateInstance<PublishScheduledAnnouncementsJob>(scope.ServiceProvider);
            await job.RunAsync();
        }

        await RunJobAsync();
        var status = await InDbAsync(db => db.Announcements.Where(a => a.Id == annId).Select(a => a.Status).FirstAsync());
        status.Should().Be("active");
        var countAfterFirst = await NotificationCountAsync(annId);
        countAfterFirst.Should().BeGreaterThanOrEqualTo(1);

        await RunJobAsync();
        (await NotificationCountAsync(annId)).Should().Be(countAfterFirst, "job ikinci kez koşunca mükerrer bildirim üretmemeli");
    }
}
