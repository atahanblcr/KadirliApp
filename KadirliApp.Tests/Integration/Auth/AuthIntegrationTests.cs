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

namespace KadirliApp.Tests.Integration.Auth;

/// <summary>
/// Faz 10.2: OTP → verify-otp (yeni kullanıcı: tempToken / kayıtlı: access+refresh) →
/// register → refresh rotasyonu → logout iptali akışlarının uçtan uca doğrulaması.
/// </summary>
public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // ---- yardımcılar ----

    private async Task<Guid> GetNeighborhoodIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Neighborhoods.Where(n => n.IsActive).Select(n => n.Id).FirstAsync();
    }

    /// <summary>DevMode OTP (sabit 123456) ile login+verify yapar, yeni kullanıcı tempToken'ı döner.</summary>
    private async Task<string> GetTempTokenAsync(string phone)
    {
        var login = await _client.PostAsJsonAsync("/v1/auth/login", new { phone });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var verify = await _client.PostAsJsonAsync("/v1/auth/verify-otp", new { phone, otp = "123456" });
        verify.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await verify.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("isNewUser").GetBoolean().Should().BeTrue();
        return data.GetProperty("tempToken").GetString()!;
    }

    private async Task<(string Access, string Refresh)> RegisterNewUserAsync(string phone, string username)
    {
        var tempToken = await GetTempTokenAsync(phone);

        var register = await _client.PostAsJsonAsync("/v1/auth/register", new
        {
            tempToken,
            username,
            primaryNeighborhoodId = await GetNeighborhoodIdAsync(),
            age = 30
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");
        return (data.GetProperty("accessToken").GetString()!, data.GetProperty("refreshToken").GetString()!);
    }

    private static Guid DecodeUserId(string jwt)
    {
        var payload = jwt.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
        return Guid.Parse(doc.RootElement.GetProperty("user_id").GetString()!);
    }

    private HttpRequestMessage AuthorizedRequest(HttpMethod method, string url, string token, HttpContent? content = null)
    {
        var req = new HttpRequestMessage(method, url) { Content = content };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    // ---- testler ----

    [Fact]
    public async Task NewUser_Flow_TempToken_Register_ThenProtectedEndpoint()
    {
        const string phone = "+905001234567";

        // 1. Yeni kullanıcı: verify-otp tam token DEĞİL tempToken döner (RegisterNewUserAsync doğruluyor)
        var (access, refresh) = await RegisterNewUserAsync(phone, "claudetest_yeni");
        access.Should().NotBeNullOrEmpty();
        refresh.Should().NotBeNullOrEmpty();

        // 2. Access token korumalı uçta çalışır (kendi profili)
        var userId = DecodeUserId(access);
        var profile = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, "/v1/users/me", access));
        profile.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Artık kayıtlı: ikinci verify-otp isNewUser=false + access+refresh çifti döner
        await _client.PostAsJsonAsync("/v1/auth/login", new { phone });
        var verify = await _client.PostAsJsonAsync("/v1/auth/verify-otp", new { phone, otp = "123456" });
        verify.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await verify.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("isNewUser").GetBoolean().Should().BeFalse();
        data.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        data.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        data.GetProperty("expiresIn").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Register_WithTakenUsername_ShouldReturn409()
    {
        await RegisterNewUserAsync("+905001234568", "claudetest_dolu");

        var tempToken = await GetTempTokenAsync("+905001234569");
        var response = await _client.PostAsJsonAsync("/v1/auth/register", new
        {
            tempToken,
            username = "CLAUDETEST_DOLU", // case-insensitive kontrol
            primaryNeighborhoodId = await GetNeighborhoodIdAsync()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("CONFLICT");
    }

    [Fact]
    public async Task TempToken_ShouldNotWork_OnProtectedEndpoints_OrAsRefresh()
    {
        var tempToken = await GetTempTokenAsync("+905001234570");

        // Temp token access token yerine geçemez ([Authorize] → 401)
        var profile = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, "/v1/users/me", tempToken));
        profile.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Temp token refresh token yerine de geçemez (token_type ayrımı)
        var refresh = await _client.PostAsJsonAsync("/v1/auth/refresh", new { refreshToken = tempToken });
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ShouldRotate_AndOldRefreshShouldBeRevoked()
    {
        var (_, refresh) = await RegisterNewUserAsync("+905001234571", "claudetest_refresh");

        // 1. Refresh → yeni çift
        var first = await _client.PostAsJsonAsync("/v1/auth/refresh", new { refreshToken = refresh });
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        string newAccess, newRefresh;
        using (var doc = JsonDocument.Parse(await first.Content.ReadAsStringAsync()))
        {
            var data = doc.RootElement.GetProperty("data");
            newAccess = data.GetProperty("accessToken").GetString()!;
            newRefresh = data.GetProperty("refreshToken").GetString()!;
        }
        newRefresh.Should().NotBe(refresh, "rotasyon yeni jti'li yeni refresh üretmeli");

        // 2. Yeni access korumalı uçta geçerli
        var userId = DecodeUserId(newAccess);
        var profile = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, "/v1/users/me", newAccess));
        profile.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. ESKİ refresh artık iptal listesinde → 401
        var replay = await _client.PostAsJsonAsync("/v1/auth/refresh", new { refreshToken = refresh });
        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 4. Yeni refresh çalışmaya devam eder
        var second = await _client.PostAsJsonAsync("/v1/auth/refresh", new { refreshToken = newRefresh });
        second.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_ShouldRevokeRefreshToken_AndRequireAuth()
    {
        var (access, refresh) = await RegisterNewUserAsync("+905001234572", "claudetest_logout");

        // Token'sız logout → 401
        var anonymous = await _client.PostAsJsonAsync("/v1/auth/logout", new { refreshToken = refresh });
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Logout → refresh iptal edilir
        var logout = await _client.SendAsync(AuthorizedRequest(
            HttpMethod.Post, "/v1/auth/logout", access, JsonContent.Create(new { refreshToken = refresh })));
        logout.StatusCode.Should().Be(HttpStatusCode.OK);

        var replay = await _client.PostAsJsonAsync("/v1/auth/refresh", new { refreshToken = refresh });
        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithGarbageToken_ShouldReturn401()
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/refresh", new { refreshToken = "bozuk.token.degeri" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("UNAUTHORIZED");
    }

    [Fact]
    public async Task VerifyOtp_WithInvalidOtp_ShouldReturn400()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/v1/auth/verify-otp", new { phone = "+905001234599", otp = "000000" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var success = doc.RootElement.GetProperty("success").GetBoolean();
        success.Should().BeFalse();

        var errCode = doc.RootElement.GetProperty("error").GetProperty("code").GetString();
        errCode.Should().Be("INVALID_OTP");
    }
}
