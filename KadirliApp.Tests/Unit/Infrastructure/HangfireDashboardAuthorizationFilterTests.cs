using System.Net;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Hangfire;
using Hangfire.AspNetCore;
using Hangfire.Dashboard;
using KadirliApp.Infrastructure.Jobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace KadirliApp.Tests.Unit.Infrastructure;

/// <summary>
/// <see cref="HangfireDashboardAuthorizationFilter"/> birim testleri.
/// Kritik senaryo: reverse-proxy arkasında UZAK bir istek "yerel" görünmediği sürece reddedilmeli,
/// kimlik bilgisi yapılandırıldıysa yerellik tek başına yetmemeli.
/// </summary>
public class HangfireDashboardAuthorizationFilterTests
{
    private static HangfireDashboardAuthorizationFilter BuildFilter(
        string? username = null, string? password = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Hangfire:Dashboard:Username"] = username,
            ["Hangfire:Dashboard:Password"] = password
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new HangfireDashboardAuthorizationFilter(configuration);
    }

    /// <summary>
    /// AspNetCoreDashboardContext ctor'u RequestServices üzerinden servis çözer —
    /// çıplak DefaultHttpContext'te bu null olduğu için boş bir provider bağlanır.
    /// </summary>
    private static DefaultHttpContext NewHttpContext() =>
        new() { RequestServices = new ServiceCollection().BuildServiceProvider() };

    private static DashboardContext BuildContext(
        IPAddress? remoteIp,
        IPAddress? localIp = null,
        string? basicAuth = null,
        ClaimsPrincipal? user = null)
    {
        var httpContext = NewHttpContext();
        httpContext.Connection.RemoteIpAddress = remoteIp;
        httpContext.Connection.LocalIpAddress = localIp;

        if (basicAuth is not null)
            httpContext.Request.Headers.Authorization = $"Basic {Base64(basicAuth)}";

        if (user is not null)
            httpContext.User = user;

        return new AspNetCoreDashboardContext(
            new Mock<JobStorage>().Object, new DashboardOptions(), httpContext);
    }

    private static string Base64(string raw) => Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));

    private static ClaimsPrincipal UserWithRole(string role) =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "TestAuth"));

    // ------------------------------------------------- Kimlik bilgisi YOK (geliştirme davranışı)

    [Fact]
    public void Authorize_WithoutCredentials_ShouldAllowLoopbackRequest()
    {
        var filter = BuildFilter();

        filter.Authorize(BuildContext(IPAddress.Loopback)).Should().BeTrue();
    }

    [Fact]
    public void Authorize_WithoutCredentials_ShouldDenyRemoteRequest()
    {
        var filter = BuildFilter();
        var context = BuildContext(IPAddress.Parse("203.0.113.10"), IPAddress.Parse("10.0.0.5"));

        filter.Authorize(context).Should().BeFalse("dashboard yapılandırma olmadan dışarı açılmamalı");
        context.GetHttpContext().Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    // ------------------------------------------------- Basic auth yapılandırılmış

    [Fact]
    public void Authorize_WithCredentials_ShouldAllowCorrectBasicAuth()
    {
        var filter = BuildFilter("hfadmin", "s3cret");
        var context = BuildContext(IPAddress.Parse("203.0.113.10"), basicAuth: "hfadmin:s3cret");

        filter.Authorize(context).Should().BeTrue();
    }

    [Theory]
    [InlineData("hfadmin:wrong")]
    [InlineData("wrong:s3cret")]
    [InlineData("hfadmin")] // ayraç yok
    [InlineData(":")]       // boş kullanıcı + boş parola
    public void Authorize_WithCredentials_ShouldDenyBadBasicAuth(string raw)
    {
        var filter = BuildFilter("hfadmin", "s3cret");
        var context = BuildContext(IPAddress.Parse("203.0.113.10"), basicAuth: raw);

        filter.Authorize(context).Should().BeFalse();
        context.GetHttpContext().Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public void Authorize_WithCredentials_ShouldDenyMalformedBase64()
    {
        var filter = BuildFilter("hfadmin", "s3cret");
        var httpContext = NewHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
        httpContext.Request.Headers.Authorization = "Basic ****not-base64****";

        var context = new AspNetCoreDashboardContext(
            new Mock<JobStorage>().Object, new DashboardOptions(), httpContext);

        filter.Authorize(context).Should().BeFalse("bozuk başlık istisna değil 401 üretmeli");
    }

    [Fact]
    public void Authorize_WithCredentials_ShouldDenyLocalRequestWithoutBasicAuth()
    {
        var filter = BuildFilter("hfadmin", "s3cret");
        var context = BuildContext(IPAddress.Loopback);

        // Kimlik bilgisi yapılandırıldıysa "yerelim" demek artık yeterli DEĞİL.
        filter.Authorize(context).Should().BeFalse();
        context.GetHttpContext().Response.Headers.WWWAuthenticate.ToString()
            .Should().StartWith("Basic realm=");
    }

    // ------------------------------------------------- Oturum rolü

    [Theory]
    [InlineData("admin")]
    [InlineData("super_admin")]
    public void Authorize_WithAdminRole_ShouldAllowEvenWhenRemote(string role)
    {
        var filter = BuildFilter("hfadmin", "s3cret");
        var context = BuildContext(IPAddress.Parse("203.0.113.10"), user: UserWithRole(role));

        filter.Authorize(context).Should().BeTrue();
    }

    [Fact]
    public void Authorize_WithNonAdminRole_ShouldDeny()
    {
        var filter = BuildFilter("hfadmin", "s3cret");
        var context = BuildContext(IPAddress.Parse("203.0.113.10"), user: UserWithRole("moderator"));

        filter.Authorize(context).Should().BeFalse();
    }
}
