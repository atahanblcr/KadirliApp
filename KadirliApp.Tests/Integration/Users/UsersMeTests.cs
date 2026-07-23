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

namespace KadirliApp.Tests.Integration.Users;

/// <summary>
/// Faz 10.3 doğrulaması: /v1/users/me izolasyonu, username/mahalle 30 gün değişim kuralları,
/// bildirim tercihleri PATCH semantiği, FCM token kaydı + başka kullanıcıdan token devralma.
/// </summary>
public class UsersMeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UsersMeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private async Task<string> RegisterUserAsync(string phone, string username)
    {
        var login = await _client.PostAsJsonAsync("/v1/auth/login", new { phone });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var verify = await _client.PostAsJsonAsync("/v1/auth/verify-otp", new { phone, otp = "123456" });
        verify.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verifyDoc = JsonDocument.Parse(await verify.Content.ReadAsStringAsync());
        var data = verifyDoc.RootElement.GetProperty("data");
        if (!data.GetProperty("isNewUser").GetBoolean())
            return data.GetProperty("accessToken").GetString()!;

        Guid neighborhoodId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            neighborhoodId = await db.Neighborhoods.Where(n => n.IsActive).Select(n => n.Id).FirstAsync();
        }

        var register = await _client.PostAsJsonAsync("/v1/auth/register", new
        {
            tempToken = data.GetProperty("tempToken").GetString(),
            username,
            primaryNeighborhoodId = neighborhoodId
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        using var registerDoc = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        return registerDoc.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
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

    private async Task<JsonDocument> GetMeAsync(string token)
    {
        var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/v1/users/me", token));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetMe_IsIsolatedPerUser_AndIncludesNeighborhoodAndPreferences()
    {
        var tokenA = await RegisterUserAsync("+905022220001", "claudetest_me_a");
        var tokenB = await RegisterUserAsync("+905022220002", "claudetest_me_b");

        using var meA = await GetMeAsync(tokenA);
        using var meB = await GetMeAsync(tokenB);

        meA.RootElement.GetProperty("data").GetProperty("phone").GetString().Should().Be("+905022220001");
        meB.RootElement.GetProperty("data").GetProperty("phone").GetString().Should().Be("+905022220002");
        meA.RootElement.GetProperty("data").GetProperty("username").GetString().Should().Be("claudetest_me_a");

        // Mahalle adı join'i + varsayılan bildirim tercihleri yanıtta
        meA.RootElement.GetProperty("data").GetProperty("primaryNeighborhoodName").GetString().Should().NotBeNullOrEmpty();
        meA.RootElement.GetProperty("data").GetProperty("notificationPreferences")
            .GetProperty("announcements").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task PatchMe_Username_FirstChangeSucceeds_SecondBlocked_ConflictIs409()
    {
        var token = await RegisterUserAsync("+905022220003", "claudetest_un_1");
        await RegisterUserAsync("+905022220004", "claudetest_un_2");

        // İlk değişiklik serbest (kayıt anı sayaç başlatmaz)
        var first = await _client.SendAsync(Authorized(
            HttpMethod.Patch, "/v1/users/me", token, new { username = "claudetest_un_1b" }));
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await first.Content.ReadAsStringAsync()))
        {
            doc.RootElement.GetProperty("data").GetProperty("username").GetString().Should().Be("claudetest_un_1b");
            doc.RootElement.GetProperty("data").GetProperty("usernameLastChangedAt").ValueKind
                .Should().NotBe(JsonValueKind.Null);
        }

        // 30 gün dolmadan ikinci değişiklik → 400 USERNAME_CHANGE_LIMIT
        var second = await _client.SendAsync(Authorized(
            HttpMethod.Patch, "/v1/users/me", token, new { username = "claudetest_un_1c" }));
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using (var doc = JsonDocument.Parse(await second.Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("USERNAME_CHANGE_LIMIT");

        // Aynı username tekrar gönderilirse no-op → limit tetiklenmez
        var noop = await _client.SendAsync(Authorized(
            HttpMethod.Patch, "/v1/users/me", token, new { username = "claudetest_un_1b" }));
        noop.StatusCode.Should().Be(HttpStatusCode.OK);

        // Başkasının username'i (case-insensitive) → 409 CONFLICT (limit'ten önce format+no-op koşulları geçen taze kullanıcıyla)
        var tokenFresh = await RegisterUserAsync("+905022220005", "claudetest_un_3");
        var conflict = await _client.SendAsync(Authorized(
            HttpMethod.Patch, "/v1/users/me", tokenFresh, new { username = "CLAUDETEST_UN_2" }));
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PatchMe_Neighborhood_FirstChangeSucceeds_SecondBlocked()
    {
        var token = await RegisterUserAsync("+905022220006", "claudetest_nh");

        // Kullanıcının kayıt olduğu mahalleyi dışlayıp iki FARKLI mahalle seç (no-op'a düşmesin)
        Guid currentNeighborhood;
        using (var me = await GetMeAsync(token))
            currentNeighborhood = me.RootElement.GetProperty("data").GetProperty("primaryNeighborhoodId").GetGuid();

        Guid otherNeighborhood, thirdNeighborhood;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ids = await db.Neighborhoods.Where(n => n.IsActive && n.Id != currentNeighborhood)
                .OrderBy(n => n.Name).Select(n => n.Id).Take(2).ToListAsync();
            otherNeighborhood = ids[0];
            thirdNeighborhood = ids[1];
        }

        var first = await _client.SendAsync(Authorized(
            HttpMethod.Patch, "/v1/users/me", token, new { primaryNeighborhoodId = otherNeighborhood }));
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await _client.SendAsync(Authorized(
            HttpMethod.Patch, "/v1/users/me", token, new { primaryNeighborhoodId = thirdNeighborhood }));
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var doc = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("NEIGHBORHOOD_CHANGE_LIMIT");
    }

    [Fact]
    public async Task PatchMeNotifications_UpdatesOnlyProvidedKeys()
    {
        var token = await RegisterUserAsync("+905022220007", "claudetest_prefs");

        var patch = await _client.SendAsync(Authorized(
            HttpMethod.Patch, "/v1/users/me/notifications", token, new { deaths = false, ads = true }));
        patch.StatusCode.Should().Be(HttpStatusCode.OK);

        using var me = await GetMeAsync(token);
        var prefs = me.RootElement.GetProperty("data").GetProperty("notificationPreferences");
        prefs.GetProperty("deaths").GetBoolean().Should().BeFalse();
        prefs.GetProperty("ads").GetBoolean().Should().BeTrue();
        // Gönderilmeyen anahtarlar varsayılanında kaldı
        prefs.GetProperty("announcements").GetBoolean().Should().BeTrue();
        prefs.GetProperty("campaigns").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task FcmToken_IsSaved_AndTakenOverFromPreviousAccountOnSameDevice()
    {
        var tokenA = await RegisterUserAsync("+905022220008", "claudetest_fcm_a");
        var tokenB = await RegisterUserAsync("+905022220009", "claudetest_fcm_b");
        const string deviceToken = "claudetest-fcm-device-token-123";

        var saveA = await _client.SendAsync(Authorized(
            HttpMethod.Post, "/v1/notifications/fcm-token", tokenA, new { token = deviceToken }));
        saveA.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Users.SingleAsync(u => u.Phone == "+905022220008")).FcmToken.Should().Be(deviceToken);
        }

        // Aynı cihaz logout'suz B hesabına geçti → token B'ye yazılır, A'dan temizlenir
        var saveB = await _client.SendAsync(Authorized(
            HttpMethod.Post, "/v1/notifications/fcm-token", tokenB, new { token = deviceToken }));
        saveB.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Users.SingleAsync(u => u.Phone == "+905022220008")).FcmToken.Should().BeNull();
            (await db.Users.SingleAsync(u => u.Phone == "+905022220009")).FcmToken.Should().Be(deviceToken);
        }

        // Boş token → 400
        var invalid = await _client.SendAsync(Authorized(
            HttpMethod.Post, "/v1/notifications/fcm-token", tokenB, new { token = "" }));
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Token'sız istek → 401
        var anon = await _client.PostAsJsonAsync("/v1/notifications/fcm-token", new { token = deviceToken });
        anon.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
