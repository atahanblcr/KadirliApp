using System.Net;
using System.Text;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace KadirliApp.Infrastructure.Jobs;

/// <summary>
/// Hangfire dashboard erişim filtresi.
///
/// Hangfire varsayılan olarak dashboard'ı YALNIZ yerel isteklere açar; ancak reverse-proxy
/// (Nginx/Traefik) arkasında istek uygulamaya localhost'tan geldiği için bu koruma çöker ve
/// <c>/hangfire</c> internete açılır. Bu filtre kararı isteğin kaynağına değil kimlik bilgisine
/// bağlar:
///
/// <list type="number">
///   <item>Kimliği doğrulanmış <c>admin</c>/<c>super_admin</c> rolü varsa geçer.</item>
///   <item>Aksi hâlde HTTP Basic auth: <c>Hangfire:Dashboard:Username</c> + <c>:Password</c>.</item>
///   <item>Kimlik bilgisi YAPILANDIRILMAMIŞSA yalnız gerçekten yerel istekler geçer
///         (geliştirme davranışı korunur), uzak istek 401 alır.</item>
/// </list>
///
/// ⚠️ (3) production'da yeterli DEĞİLDİR — proxy arkasında her istek yerel görünür.
/// Yayına çıkarken <c>Hangfire:Dashboard:Username/Password</c> mutlaka doldurulmalı.
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private const string Realm = "KadirliApp Hangfire";

    private static readonly string[] AllowedRoles = { "admin", "super_admin" };

    private readonly string? _username;
    private readonly string? _password;

    public HangfireDashboardAuthorizationFilter(IConfiguration configuration)
    {
        _username = configuration["Hangfire:Dashboard:Username"];
        _password = configuration["Hangfire:Dashboard:Password"];
    }

    /// <summary>Basic auth kimlik bilgisi yapılandırılmış mı — boş kullanıcı/parola sayılmaz.</summary>
    private bool HasConfiguredCredentials =>
        !string.IsNullOrWhiteSpace(_username) && !string.IsNullOrWhiteSpace(_password);

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // (1) Panel/JWT oturumu zaten varsa rol yeterli.
        if (httpContext.User?.Identity?.IsAuthenticated == true &&
            AllowedRoles.Any(role => httpContext.User.IsInRole(role)))
        {
            return true;
        }

        // (2) Basic auth yapılandırılmışsa tek geçerli yol odur.
        if (HasConfiguredCredentials)
        {
            if (TryValidateBasicAuth(httpContext))
                return true;

            Challenge(httpContext);
            return false;
        }

        // (3) Yapılandırma yoksa yalnız yerel istek — uzaktan erişim kapalı.
        if (IsLocalRequest(httpContext))
            return true;

        Challenge(httpContext);
        return false;
    }

    private bool TryValidateBasicAuth(HttpContext httpContext)
    {
        var header = httpContext.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) ||
            !header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header["Basic ".Length..].Trim()));
        }
        catch (FormatException)
        {
            return false;
        }

        var separator = decoded.IndexOf(':');
        if (separator < 0)
            return false;

        var user = decoded[..separator];
        var pass = decoded[(separator + 1)..];

        // Sabit süreli karşılaştırma — parola uzunluğu/ön eki zamanlamayla sızmasın.
        return FixedTimeEquals(user, _username!) & FixedTimeEquals(pass, _password!);
    }

    private static bool FixedTimeEquals(string left, string right) =>
        System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));

    private static bool IsLocalRequest(HttpContext httpContext)
    {
        var remote = httpContext.Connection.RemoteIpAddress;
        if (remote is null)
            return true; // in-process / test sunucusu — soket yok

        var local = httpContext.Connection.LocalIpAddress;
        return local is not null ? remote.Equals(local) : IPAddress.IsLoopback(remote);
    }

    private static void Challenge(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        httpContext.Response.Headers.WWWAuthenticate = $"Basic realm=\"{Realm}\"";
    }
}
