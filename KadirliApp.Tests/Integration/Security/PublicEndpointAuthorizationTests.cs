using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Security;

/// <summary>
/// Faz 10.1 doğrulaması: public yazma uçları token'sız 401; user token'ı admin uçlarında 403;
/// başkasının profiline erişim 403 (IDOR); upload magic-byte doğrulaması.
/// </summary>
public class PublicEndpointAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PublicEndpointAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    /// <summary>
    /// DevMode OTP akışıyla normal (rol=user) kullanıcı token'ı alır.
    /// Faz 10.2: verify-otp yeni kullanıcıya tempToken döndüğünden kayıt register ile tamamlanır.
    /// </summary>
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
                Guid neighborhoodId;
                using (var scope = _factory.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    neighborhoodId = await db.Neighborhoods.Where(n => n.IsActive).Select(n => n.Id).FirstAsync();
                }

                var register = await _client.PostAsJsonAsync("/v1/auth/register", new
                {
                    tempToken = data.GetProperty("tempToken").GetString(),
                    username = $"claudetest{phone[^7..]}",
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

        // JWT payload'ından user_id claim'i (base64url)
        var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        using var payloadDoc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
        var userId = Guid.Parse(payloadDoc.RootElement.GetProperty("user_id").GetString()!);

        return (token, userId);
    }

    private HttpRequestMessage AuthorizedRequest(HttpMethod method, string url, string token, HttpContent? content = null)
    {
        var req = new HttpRequestMessage(method, url) { Content = content };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    [Fact]
    public async Task AnonymousWriteRequests_ShouldReturn401()
    {
        // Vefat ilanı gönderme artık [Authorize]
        var deaths = await _client.PostAsJsonAsync("/v1/deaths", new { deceasedName = "x" });
        deaths.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Dosya yükleme artık [Authorize]
        using var form = new MultipartFormDataContent { { new ByteArrayContent(new byte[] { 1, 2, 3 }), "file", "a.png" } };
        var upload = await _client.PostAsync("/v1/files/upload", form);
        upload.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Profil uçları [Authorize] (Faz 10.3: /v1/users/me)
        var profile = await _client.PatchAsJsonAsync("/v1/users/me", new { username = "x" });
        profile.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemovedPublicWriteEndpoints_ShouldNotExist()
    {
        // Admin-işi POST kopyaları silindi (karşılıkları v1/admin/*'ta) → 404 veya 405, asla 2xx
        foreach (var url in new[]
        {
            "/v1/announcements", "/v1/pharmacies", "/v1/events", "/v1/places",
            "/v1/taxis/drivers", "/v1/guide/categories",
            "/v1/transport/intercity-routes", "/v1/transport/intracity-routes",
            "/v1/power-outages"
        })
        {
            var response = await _client.PostAsJsonAsync(url, new { });
            response.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed },
                because: $"POST {url} public yüzeyden kaldırıldı");
        }
    }

    [Fact]
    public async Task UserToken_OnAdminEndpoint_ShouldReturn403()
    {
        var (token, _) = await GetUserTokenAsync("+905011110001");

        var response = await _client.SendAsync(
            AuthorizedRequest(HttpMethod.Get, "/v1/admin/announcements", token));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task IdScopedProfileEndpoints_AreRemoved_AndMeEndpoint_ShouldSucceed()
    {
        var (token, userId) = await GetUserTokenAsync("+905011110002");

        // Faz 10.3: {id}/profile uçları tamamen kaldırıldı — IDOR yüzeyi yok (404), 10.1'deki 403 geçici çözümdü
        var removed = await _client.SendAsync(AuthorizedRequest(
            HttpMethod.Put, $"/v1/users/{Guid.NewGuid()}/profile", token,
            JsonContent.Create(new { username = "hacker" })));
        removed.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);

        // Kendi profili artık claim'den: GET /v1/users/me → 200 + doğru kullanıcı
        var own = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, "/v1/users/me", token));
        own.StatusCode.Should().Be(HttpStatusCode.OK);
        using var ownDoc = JsonDocument.Parse(await own.Content.ReadAsStringAsync());
        ownDoc.RootElement.GetProperty("data").GetProperty("id").GetGuid().Should().Be(userId);
    }

    [Fact]
    public async Task Upload_WithToken_ShouldValidateMagicBytes()
    {
        var (token, _) = await GetUserTokenAsync("+905011110003");

        // Uzantısı .png ama içeriği görsel değil → 400 UNSUPPORTED_FILE_TYPE
        using var fakeForm = new MultipartFormDataContent
        {
            { new ByteArrayContent(Encoding.UTF8.GetBytes("<script>alert(1)</script>")), "file", "evil.png" }
        };
        var fake = await _client.SendAsync(AuthorizedRequest(HttpMethod.Post, "/v1/files/upload", token, fakeForm));
        fake.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var fakeDoc = JsonDocument.Parse(await fake.Content.ReadAsStringAsync());
        fakeDoc.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("UNSUPPORTED_FILE_TYPE");

        // Gerçek PNG imzası → 200
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13 };
        using var pngForm = new MultipartFormDataContent
        {
            { new ByteArrayContent(pngBytes), "file", "gercek.png" }
        };
        var png = await _client.SendAsync(AuthorizedRequest(HttpMethod.Post, "/v1/files/upload", token, pngForm));
        png.StatusCode.Should().Be(HttpStatusCode.OK);
        using var pngDoc = JsonDocument.Parse(await png.Content.ReadAsStringAsync());
        pngDoc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
    }
}
