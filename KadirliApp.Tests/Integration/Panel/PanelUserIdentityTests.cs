extern alias WebPanel;

using System.Net;
using FluentAssertions;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PanelPermissionFilter = WebPanel::KadirliApp.Web.Authorization.PanelPermissionFilter;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 12.7 — panelin **"Bağlı hesaplar"** kutusu ve bağlantı kaldırma.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 Buradaki asıl iddia *"satır silindi"* değil, <b>"silme kararı doğru yetkiye tabi"</b>:
/// bir giriş yöntemini kaldırmak güvenlik etkili bir işlemdir ve §7 madde 19'un altı kez
/// tekrarlamış tuzağı tam burada yatıyordu — aksiyon <c>Unlink…</c> adlandırılsaydı hiçbir
/// önekle eşleşmez, POST olduğu için sessizce <c>update</c>'e düşer ve <b>yalnız profil
/// düzenleme yetkisi olan</b> bir moderatör kullanıcının Google bağlantısını kaldırabilirdi.
/// </para>
/// <para>
/// ⚠️ Bu, <c>Un…</c> biçiminin listedeki en sinsi hâlidir ve 12.13'te (<c>Unarchive</c>)
/// birebir yaşandı. Bu sefer tuzak <b>doğuşta</b> yakalandı: aksiyon <c>RemoveIdentity</c>
/// adlandırıldı, yani <c>ActionFor</c>'a elle satır eklemek gerekmedi.
/// </para>
/// </remarks>
[Collection(PanelCollection.Name)]
public class PanelUserIdentityTests : IAsyncLifetime
{
    private const string ModeratorUsername = "identity-moderator-test";
    private const string ModeratorPassword = "Moderator123!";
    private const string SubMarker = "panel-identity-test-";

    private readonly WebPanelApplicationFactory _factory;
    private Guid _moderatorId;
    private Guid _citizenId;

    public PanelUserIdentityTests(WebPanelApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        var moderator = await _factory.EnsureModeratorAsync(ModeratorUsername, ModeratorPassword);
        _moderatorId = moderator.Id;
        await _factory.ClearMustChangePasswordAsync(ModeratorUsername);

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();

            var citizen = new User
            {
                Phone = "+9059100" + Random.Shared.Next(10000, 99999),
                Username = "identity-citizen-" + Guid.NewGuid().ToString("N")[..8],
                Role = UserRole.User,
                IsActive = true
            };
            db.Users.Add(citizen);
            await db.SaveChangesAsync();
            _citizenId = citizen.Id;
        });
    }

    /// <summary>
    /// ⚠️ Test kendi satırlarını **siliyor** — T1'in dersi (biriken test kullanıcıları
    /// ilgisiz testleri sayfalamayla kaydırıyordu, 12.15b'de birebir yaşandı).
    /// </summary>
    public async Task DisposeAsync()
    {
        await SetPermissionsAsync();
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.Set<UserIdentity>().Where(x => x.ProviderUserId.StartsWith(SubMarker)).ExecuteDeleteAsync();
            await db.Users.IgnoreQueryFilters().Where(u => u.Id == _citizenId).ExecuteDeleteAsync();
        });
    }

    // ─────────────────────────── izin ───────────────────────────

    /// <summary>
    /// 🔴 §7 madde 19. <c>RemoveIdentity</c> → <c>delete</c>; <c>Unlink…</c> yazılsaydı
    /// <c>update</c>'e düşerdi. Teori ayağı — davranış ayağı aşağıda.
    /// </summary>
    [Theory]
    [InlineData("RemoveIdentity", "delete")]
    [InlineData("Edit", "update")]
    public void RemoveIdentity_MapsToDelete_NotUpdate(string actionName, string expected)
        => PanelPermissionFilter.ActionFor(actionName, "POST").Should().Be(expected);

    /// <summary>
    /// 🔑 Teoriden ayrı olmak zorunda: eşlemeyi doğru yazıp aksiyonu yanlış adlandırmak
    /// (ya da controller'a <c>[PanelPermission]</c> koymayı unutmak) mümkün ve o durumda
    /// yukarıdaki teori **yeşil kalırdı**.
    /// </summary>
    [Fact]
    public async Task AModeratorWithOnlyUpdatePermission_CannotRemoveAnIdentity()
    {
        await SeedIdentityAsync();
        await SetPermissionsAsync(new AdminPermission
        {
            UserId = _moderatorId, Module = "users",
            CanRead = true, CanUpdate = true, CanDelete = false
        });

        var client = _factory.CreatePanelClient();
        await client.LoginAsync(ModeratorUsername, ModeratorPassword);

        var response = await client.PostFormAsync("/UsersAdmin/RemoveIdentity",
            new Dictionary<string, string>
            {
                ["id"] = _citizenId.ToString(),
                ["provider"] = SocialProviders.Google
            },
            $"/UsersAdmin/Edit/{_citizenId}");

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);

        (await IdentityCountAsync()).Should().Be(1,
            "bir giriş yöntemini kaldırmak 'profil düzenleme' değil, güvenlik etkili bir işlemdir");
    }

    // ─────────────────────────── davranış ───────────────────────────

    [Fact]
    public async Task TheEditScreen_ShowsTheLinkedAccount_WithoutLeakingTheProviderUserId()
    {
        var sub = await SeedIdentityAsync();
        var client = await _factory.SuperAdminAsync();

        var html = await (await client.GetAsync($"/UsersAdmin/Edit/{_citizenId}")).ReadDecodedBodyAsync();

        html.Should().Contain("Bağlı hesaplar");
        html.Should().Contain("Google", "ham 'google' değil Türkçe/marka rozeti basılmalı (Değişmez Kural #6)");
        html.Should().Contain("vatandas@ornek.com");

        // ⚠️ Sağlayıcı kimliği ekrana ÇIKMAZ: dışarı verilen her kimlik değeri, ileride
        // birinin onunla eşleştirme yapmaya kalkışabileceği bir yüzeydir (§7 madde 69).
        html.Should().NotContain(sub);
    }

    /// <summary>
    /// Bağlantısı olmayan hesapta kutu yine çizilir ve <b>"yok"</b> der. Hiç çizilmemesi
    /// yöneticiye *"böyle bir bilgi yok"* izlenimi verirdi (`_RecentLoginAttempts` deseni).
    /// </summary>
    [Fact]
    public async Task WithNoLinkedAccount_TheBoxStillRenders_AndSaysSo()
    {
        var client = await _factory.SuperAdminAsync();

        var html = await (await client.GetAsync($"/UsersAdmin/Edit/{_citizenId}")).ReadDecodedBodyAsync();

        html.Should().Contain("Bağlı hesaplar");
        html.Should().Contain("Bağlı sosyal hesap yok");
    }

    [Fact]
    public async Task SuperAdmin_RemovesTheIdentity_AndItIsRecordedInTheAuditTrail()
    {
        await SeedIdentityAsync();
        var client = await _factory.SuperAdminAsync();

        var response = await client.PostFormAsync("/UsersAdmin/RemoveIdentity",
            new Dictionary<string, string>
            {
                ["id"] = _citizenId.ToString(),
                ["provider"] = SocialProviders.Google
            },
            $"/UsersAdmin/Edit/{_citizenId}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);

        (await IdentityCountAsync()).Should().Be(0);

        // 🔑 Denetim izi bu komutun VAROLMA SEBEBİ: vatandaşın kendi ucu ize düşmez
        // (audit_logs yönetici defteridir), yöneticinin başkasının hesabında yaptığı
        // işlem düşmek ZORUNDA.
        var audited = await AuditedAsync();
        audited.Should().BeTrue("yöneticinin başkasının hesabına dokunması izlenebilir olmalı");
    }

    /// <summary>
    /// 🔑 Kaldırma kullanıcıyı hesabından **kilitlemez** — telefon çıpa olarak duruyor.
    /// Telefonsuz bir kimlik modelinde bu düğme bir tuzak olurdu (§7 madde 70).
    /// </summary>
    [Fact]
    public async Task RemovingTheLastIdentity_DoesNotDisableTheAccount()
    {
        await SeedIdentityAsync();
        var client = await _factory.SuperAdminAsync();

        await client.PostFormAsync("/UsersAdmin/RemoveIdentity",
            new Dictionary<string, string>
            {
                ["id"] = _citizenId.ToString(),
                ["provider"] = SocialProviders.Google
            },
            $"/UsersAdmin/Edit/{_citizenId}");

        User? citizen = null;
        await _factory.WithScopeAsync(async sp =>
            citizen = await sp.GetRequiredService<AppDbContext>().Users
                .FirstOrDefaultAsync(u => u.Id == _citizenId));

        citizen.Should().NotBeNull();
        citizen!.IsActive.Should().BeTrue();
        citizen.Phone.Should().NotBeNullOrWhiteSpace("telefon çıpadır; kullanıcı OTP ile girmeye devam eder");
    }

    // ─────────────────────────── yardımcılar ───────────────────────────

    private async Task<string> SeedIdentityAsync()
    {
        var sub = SubMarker + Guid.NewGuid().ToString("N")[..8];
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.Set<UserIdentity>().Add(new UserIdentity
            {
                UserId = _citizenId,
                Provider = SocialProviders.Google,
                ProviderUserId = sub,
                Email = "vatandas@ornek.com",
                EmailVerified = true,
                DisplayName = "Ayşe Yılmaz",
                LinkedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });
        return sub;
    }

    private async Task<int> IdentityCountAsync()
    {
        var count = 0;
        await _factory.WithScopeAsync(async sp =>
            count = await sp.GetRequiredService<AppDbContext>().Set<UserIdentity>()
                .CountAsync(x => x.UserId == _citizenId));
        return count;
    }

    private async Task<bool> AuditedAsync()
    {
        var found = false;
        await _factory.WithScopeAsync(async sp =>
            found = await sp.GetRequiredService<AppDbContext>().AuditLogs
                .AnyAsync(a => a.Action == "unlink_identity" && a.AffectedId == _citizenId));
        return found;
    }

    private async Task SetPermissionsAsync(params AdminPermission[] permissions)
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.Set<AdminPermission>().Where(p => p.UserId == _moderatorId).ExecuteDeleteAsync();
            if (permissions.Length > 0) db.Set<AdminPermission>().AddRange(permissions);
            await db.SaveChangesAsync();
        });
    }
}
