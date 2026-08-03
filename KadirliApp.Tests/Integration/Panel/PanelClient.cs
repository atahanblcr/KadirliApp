using System.Net;
using System.Text.RegularExpressions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.15b — panel testlerinin ortak yardımcıları.
///
/// Panelde bir POST atmak API'dekinden daha zahmetli: <c>AutoValidateAntiforgeryToken</c>
/// global olduğu için her POST hem **çerez** hem de **form alanı** olarak antiforgery
/// token'ı ister ve ikisi eşleşmek zorundadır. Token her zaman formun bulunduğu sayfanın
/// GET'inden alınır. (10.x oturumlarında bu elle <c>curl -b -c</c> ile yapılıyordu.)
/// </summary>
public static class PanelClient
{
    private static readonly Regex TokenRegex = new(
        "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Yönlendirmeleri İZLEMEYEN istemci — "302 → /account/login" iddiası ancak böyle kurulabilir.</summary>
    public static HttpClient CreatePanelClient(this WebPanelApplicationFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>
    /// Seed'lenmiş süper admin ile **yeni** bir oturum açar; çerez istemcide kalır.
    /// Oturumun kendisini deneyen testler (çıkış, hatalı şifre) bunu kullanmalı.
    /// Yalnız sayfa okuyan testler <see cref="WebPanelApplicationFactory.SuperAdminAsync"/>
    /// ile paylaşılan oturumu kullanır — her testte yeniden giriş yapmak hem yavaştır
    /// hem de giriş hız sınırını gereksiz yere tüketir.
    /// </summary>
    public static async Task<HttpClient> LoginAsSuperAdminAsync(this WebPanelApplicationFactory factory)
    {
        var client = factory.CreatePanelClient();
        await client.LoginAsync(DbSeeder.AdminUsername, DbSeeder.AdminPassword);
        return client;
    }

    public static async Task LoginAsync(this HttpClient client, string username, string password)
    {
        var token = await client.GetAntiforgeryTokenAsync("/account/login");

        var response = await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = username,
            ["password"] = password,
            ["__RequestVerificationToken"] = token
        }));

        // Başarılı giriş yönlendirir; başarısız giriş formu 200 ile geri döndürür.
        if (response.StatusCode != HttpStatusCode.Redirect && response.StatusCode != HttpStatusCode.Found)
            throw new InvalidOperationException(
                $"Panel girişi başarısız ({username}): {response.StatusCode}. " +
                "Gövde: " + await response.Content.ReadAsStringAsync());
    }

    /// <summary>Verilen sayfayı GET'ler ve içindeki antiforgery token'ı çıkarır.</summary>
    public static async Task<string> GetAntiforgeryTokenAsync(this HttpClient client, string path)
    {
        var page = await client.GetAsync(path);
        var html = await page.Content.ReadAsStringAsync();

        var match = TokenRegex.Match(html);
        if (!match.Success)
            throw new InvalidOperationException(
                $"{path} sayfasında __RequestVerificationToken bulunamadı (durum: {page.StatusCode}). " +
                "Form tag helper'ı kaldırılmış olabilir — panel POST'ları o token olmadan 400 alır.");

        return match.Groups[1].Value;
    }

    /// <summary>Antiforgery token'ı ilgili sayfadan alıp form POST'u atar.</summary>
    public static async Task<HttpResponseMessage> PostFormAsync(
        this HttpClient client, string path, IDictionary<string, string> fields, string? tokenFromPath = null)
    {
        var token = await client.GetAntiforgeryTokenAsync(tokenFromPath ?? path);

        var payload = new Dictionary<string, string>(fields) { ["__RequestVerificationToken"] = token };
        return await client.PostAsync(path, new FormUrlEncodedContent(payload));
    }

    /// <summary>
    /// ⚠️ **Panelin en sinsi test tuzağı.** Razor, model/ViewBag üzerinden gelen Türkçe metni
    /// HTML varlığına çevirir (<c>hatalı</c> → <c>hatal&amp;#x131;</c>) ama görünümün içine
    /// düz yazılmış statik metni ham UTF-8 bırakır. Yani gövdede "hatalı" aramak
    /// **dinamik metinlerde her zaman başarısız olur** ve test sanki özellik yokmuş gibi
    /// kırmızıya döner. Gövdeyi her zaman bu metotla okuyun.
    /// </summary>
    public static async Task<string> ReadDecodedBodyAsync(this HttpResponseMessage response) =>
        WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

    /// <summary>Testin kendi verisini kurabilmesi için scoped servis erişimi.</summary>
    public static async Task WithScopeAsync(this WebPanelApplicationFactory factory, Func<IServiceProvider, Task> action)
    {
        using var scope = factory.Services.CreateScope();
        await action(scope.ServiceProvider);
    }

    /// <summary>
    /// Panelde oturum açabilen ama hiçbir izni olmayan bir <c>moderator</c> üretir.
    /// Aynı telefon numarası varsa yeniden kullanılır (testler arası idempotent).
    /// </summary>
    public static async Task<User> EnsureModeratorAsync(
        this WebPanelApplicationFactory factory, string username, string password)
    {
        User created = null!;
        await factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var hasher = sp.GetRequiredService<IPasswordHasher>();

            var existing = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (existing is not null) { created = existing; return; }

            created = new User
            {
                Phone = "+9059000" + Random.Shared.Next(10000, 99999),
                Username = username,
                Password = hasher.HashPassword(password),
                Role = UserRole.Moderator,
                IsActive = true
            };
            db.Users.Add(created);
            await db.SaveChangesAsync();
        });
        return created;
    }
}
