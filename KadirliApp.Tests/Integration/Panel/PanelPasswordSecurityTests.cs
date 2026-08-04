extern alias WebPanel;

using System.Net;
using FluentAssertions;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Security;
using KadirliApp.Application.Features.Staff.Commands;
using KadirliApp.Application.Features.Users.Commands.ChangeMyPassword;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using KadirliApp.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using KadirliApp.Application.Common.Interfaces;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.18 — **panel güvenlik kapanışı** (11.15c C grubunun kalanı).
///
/// Üç ayrı açık, tek bir sınıfta kilitleniyor:
///
/// 1. 🔴 <b>Oturum iptal edilemiyordu.</b> Çerez 8 saatlik ve <c>OnValidatePrincipal</c>
///    yoktu → silinen/banlanan/pasife alınan personelin oturumu yaşamaya devam ediyordu.
///    "Yetkiyi geri aldım" diyen yönetici aslında hiçbir şey geri almamış oluyordu.
/// 2. 🔴 <b>Varsayılan parola zorla değiştirilmiyordu.</b> <c>admin / Admin123!</c> kaynakta
///    yazılı; 11.15c sızıntıyı kapattı ama parolanın kendisi çalışmaya devam ediyordu.
/// 3. 🟡 <b>Parola politikası 6 karakterdi</b> ve kural üç ayrı handler'da kopyalanmıştı;
///    hesap kilidi hiç yoktu (hız sınırı IP'yi kısıtlıyor, hesabı değil).
///
/// ⚠️ Bu sınıf paylaşılan süper admin oturumunu KULLANMAZ ve onu bozmamaya dikkat eder:
/// kendi personelini üretir, kendi istemcisiyle girer, sonunda temizler. Paylaşılan
/// admin'in parolasını değiştiren bir test, ardından koşan 400+ testi düşürürdü.
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelPasswordSecurityTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;
    private readonly string _marker = "PwSec" + Guid.NewGuid().ToString("N")[..8];

    public PanelPasswordSecurityTests(WebPanelApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.Users.IgnoreQueryFilters()
                .Where(u => u.Username != null && u.Username.StartsWith(_marker))
                .ExecuteDeleteAsync();
        });
    }

    // ————————————————————————————————————————————————————————————————
    // Yardımcılar
    // ————————————————————————————————————————————————————————————————

    private const string GoodPassword = "Kadirli2026Panel";

    /// <summary>Panelde oturum açabilen bir moderatör üretir; bayrakları test belirler.</summary>
    private async Task<(Guid Id, string Username)> SeedStaffAsync(
        string password = GoodPassword,
        bool mustChangePassword = false,
        UserRole role = UserRole.Moderator,
        bool isActive = true)
    {
        var username = _marker + "-" + Guid.NewGuid().ToString("N")[..6];
        Guid id = Guid.Empty;

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var hasher = sp.GetRequiredService<IPasswordHasher>();

            var user = new User
            {
                Phone = "+9059" + Random.Shared.Next(10000000, 99999999),
                Username = username,
                Password = hasher.HashPassword(password),
                Role = role,
                IsActive = isActive,
                MustChangePassword = mustChangePassword
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            id = user.Id;
        });

        return (id, username);
    }

    private async Task MutateUserAsync(Guid id, Action<User> mutate)
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var user = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == id);
            mutate(user);
            await db.SaveChangesAsync();
        });
    }

    private async Task<User> ReadUserAsync(Guid id)
    {
        User result = null!;
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            result = await db.Users.IgnoreQueryFilters().AsNoTracking().FirstAsync(u => u.Id == id);
        });
        return result;
    }

    private async Task<T> SendAsync<T>(IRequest<T> request)
    {
        T result = default!;
        await _factory.WithScopeAsync(async sp =>
            result = await sp.GetRequiredService<ISender>().Send(request));
        return result;
    }

    // ————————————————————————————————————————————————————————————————
    // 1. Oturum tazeleme (OnValidatePrincipal)
    // ————————————————————————————————————————————————————————————————

    /// <summary>
    /// 🔴 Kapanan açığın ta kendisi: oturum açmış personel **pasife alınınca** elindeki
    /// çerez derhal ölmeli. Öncesinde 8 saat daha çalışıyordu.
    /// </summary>
    [Fact]
    public async Task DeactivatedStaff_SessionDiesOnNextRequest()
    {
        var (id, username) = await SeedStaffAsync();
        var client = _factory.CreatePanelClient();
        await client.LoginAsync(username, GoodPassword);

        // Girişten hemen sonra oturum yaşıyor (Dashboard'a erişim izinden bağımsız olarak
        // en azından giriş sayfasına ATILMIYOR).
        var before = await client.GetAsync("/Dashboard/Index");
        before.Headers.Location?.ToString().Should().NotContain("/account/login",
            "yeni açılmış oturum ilk istekte düşmemeli");

        await MutateUserAsync(id, u => u.IsActive = false);

        var after = await client.GetAsync("/Dashboard/Index");
        after.StatusCode.Should().Be(HttpStatusCode.Redirect);
        after.Headers.Location!.ToString().Should().Contain("/account/login",
            "pasife alınan personelin çerezi bir sonraki istekte geçersiz olmalı");
    }

    /// <summary>Banlanan personel için de aynı kural geçerli.</summary>
    [Fact]
    public async Task BannedStaff_SessionDiesOnNextRequest()
    {
        var (id, username) = await SeedStaffAsync();
        var client = _factory.CreatePanelClient();
        await client.LoginAsync(username, GoodPassword);

        await MutateUserAsync(id, u => u.IsBanned = true);

        var after = await client.GetAsync("/Dashboard/Index");
        after.Headers.Location!.ToString().Should().Contain("/account/login");
    }

    /// <summary>
    /// Silinen (soft delete) personelin oturumu da ölmeli.
    /// ⚠️ Burada <c>IgnoreQueryFilters()</c> **kullanılmaması** doğrunun kendisi:
    /// doğrulayıcı silinmiş kullanıcıyı bulamayınca oturumu düşürür. Filtre atlansaydı
    /// silinmiş personelin oturumu ayakta kalırdı.
    /// </summary>
    [Fact]
    public async Task DeletedStaff_SessionDiesOnNextRequest()
    {
        var (id, username) = await SeedStaffAsync();
        var client = _factory.CreatePanelClient();
        await client.LoginAsync(username, GoodPassword);

        await MutateUserAsync(id, u => u.DeletedAt = DateTime.UtcNow);

        var after = await client.GetAsync("/Dashboard/Index");
        after.Headers.Location!.ToString().Should().Contain("/account/login");
    }

    /// <summary>
    /// Rolü artık panele girmeye yetmeyen (normal kullanıcıya düşürülmüş) personel dışarı atılır.
    /// </summary>
    [Fact]
    public async Task StaffDemotedToPlainUser_SessionDies()
    {
        var (id, username) = await SeedStaffAsync();
        var client = _factory.CreatePanelClient();
        await client.LoginAsync(username, GoodPassword);

        await MutateUserAsync(id, u => u.Role = UserRole.User);

        var after = await client.GetAsync("/Dashboard/Index");
        after.Headers.Location!.ToString().Should().Contain("/account/login");
    }

    /// <summary>
    /// 🔑 Parolanın çerezden **sonra** değişmiş olması, o çerezi öldürür. Yöneticinin
    /// "şifresini sıfırladım" dediğinde beklediği şey tam olarak budur.
    ///
    /// ⚠️ Damga bilerek elle, çerezin düzenlenme anından **açıkça sonraya** konuyor.
    /// İlk hâli sıfırlama komutunu çağırıp sonucu ölçüyordu ve **yarış içeriyordu**:
    /// test milisaniyeler içinde koştuğu için sıfırlama girişle aynı saniyeye düşüyor,
    /// karşılaştırma ise (çerezin taşıyabildiği hassasiyet olan) saniyeye yuvarlandığı
    /// için oturum hayatta kalıyordu. Kuralın kendisi burada, komutun damgayı gerçekten
    /// attığı ise <see cref="PasswordReset_StampsPasswordChangedAt"/>'te denetleniyor.
    /// </summary>
    [Fact]
    public async Task PasswordChangedAfterCookieIssued_KillsSession()
    {
        var (id, username) = await SeedStaffAsync();
        var client = _factory.CreatePanelClient();
        await client.LoginAsync(username, GoodPassword);

        await MutateUserAsync(id, u => u.PasswordChangedAt = DateTime.UtcNow.AddSeconds(5));

        var after = await client.GetAsync("/Dashboard/Index");
        after.Headers.Location!.ToString().Should().Contain("/account/login",
            "parola çerezden sonra değiştiyse o çerez geçersiz olmalı");
    }

    /// <summary>Sıfırlama komutu damgayı gerçekten atıyor mu (yukarıdaki kuralın diğer yarısı).</summary>
    [Fact]
    public async Task PasswordReset_StampsPasswordChangedAt()
    {
        var (id, _) = await SeedStaffAsync();
        var before = DateTime.UtcNow;

        await SendAsync(new ResetStaffPasswordCommand { Id = id, NewPassword = "YeniParola2026x" });

        (await ReadUserAsync(id)).PasswordChangedAt.Should().NotBeNull()
            .And.Subject.As<DateTime>().Should().BeOnOrAfter(before.AddSeconds(-1),
                "damga olmadan açık oturumlar düşmez");
    }

    /// <summary>
    /// Sağlıklı bir oturum, hiçbir şey değişmediğinde **düşmemeli**. Bu test olmadan
    /// "her isteği reddet" gibi bir gerçekleme de diğer testleri yeşil geçerdi.
    /// </summary>
    [Fact]
    public async Task HealthyStaff_SessionSurvivesRepeatedRequests()
    {
        var (_, username) = await SeedStaffAsync();
        var client = _factory.CreatePanelClient();
        await client.LoginAsync(username, GoodPassword);

        for (var i = 0; i < 3; i++)
        {
            var response = await client.GetAsync("/Dashboard/Index");
            (response.Headers.Location?.ToString() ?? "").Should().NotContain("/account/login",
                "değişmeyen sağlıklı oturum düşmemeli (aksi hâlde panel kullanılamaz olurdu)");
        }
    }

    // ————————————————————————————————————————————————————————————————
    // 2. İlk girişte zorunlu parola değişimi
    // ————————————————————————————————————————————————————————————————

    /// <summary>
    /// 🔴 Bayrak açıkken panelin **hiçbir** sayfası açılmaz — tek çıkış parolayı değiştirmek.
    /// </summary>
    [Theory]
    [InlineData("/Dashboard/Index")]
    [InlineData("/AdsAdmin/Index")]
    [InlineData("/AnnouncementsAdmin/Index")]
    public async Task MustChangePassword_RedirectsEveryPanelPage(string path)
    {
        var (_, username) = await SeedStaffAsync(mustChangePassword: true);
        var client = _factory.CreatePanelClient();
        await client.LoginAsync(username, GoodPassword);

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("ChangePassword",
            $"{path} zorunlu parola değişimi borcu varken açılmamalı");
    }

    /// <summary>
    /// ⚠️ Yönlendirmenin hedefi muaf olmalı, yoksa tarayıcı sonsuz döngüye girer.
    /// Bu, "hepsini yönlendir" gibi kestirme bir gerçeklemeyi eleyen test.
    /// </summary>
    [Fact]
    public async Task MustChangePassword_ChangePasswordPageItselfOpens()
    {
        var (_, username) = await SeedStaffAsync(mustChangePassword: true);
        var client = _factory.CreatePanelClient();
        await client.LoginAsync(username, GoodPassword);

        var response = await client.GetAsync("/Account/ChangePassword");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "yönlendirmenin hedefi muaf olmalı — değilse sonsuz yönlendirme döngüsü oluşur");

        var body = await response.ReadDecodedBodyAsync();
        body.Should().Contain("şifrenizi değiştirmelisiniz",
            "kullanıcıya neden bu ekranda olduğu söylenmeli");
    }

    /// <summary>Çıkış yolu da açık kalmalı (kullanıcı sıkışmamalı).</summary>
    [Fact]
    public async Task MustChangePassword_LogoutStillWorks()
    {
        var (_, username) = await SeedStaffAsync(mustChangePassword: true);
        var client = _factory.CreatePanelClient();
        await client.LoginAsync(username, GoodPassword);

        var response = await client.GetAsync("/Account/Logout");

        response.Headers.Location!.ToString().Should().Contain("Login");
    }

    /// <summary>
    /// Parola değiştirilince borç kapanır **ve** kullanıcı kendi oturumundan atılmaz
    /// (çerez yeniden düzenlenir). Bu ikinci kısım olmadan akış kullanılamaz olurdu:
    /// parola değişimi tüm oturumları düşürdüğü için kişi kendi işlemiyle dışarı düşerdi.
    /// </summary>
    [Fact]
    public async Task ChangingPassword_ClearsFlagAndKeepsOwnSession()
    {
        var (id, username) = await SeedStaffAsync(mustChangePassword: true);
        var client = _factory.CreatePanelClient();
        await client.LoginAsync(username, GoodPassword);

        var response = await client.PostFormAsync("/Account/ChangePassword", new Dictionary<string, string>
        {
            ["currentPassword"] = GoodPassword,
            ["newPassword"] = "BambaskaParola2026",
            ["newPasswordConfirm"] = "BambaskaParola2026"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        // ⚠️ Dashboard/Index varsayılan rota olduğu için üretilen adres "Dashboard" değil "/".
        response.Headers.Location!.ToString().Should().NotContain("ChangePassword",
            "borç kapandıysa kullanıcı parola ekranında tutulmamalı");

        (await ReadUserAsync(id)).MustChangePassword.Should().BeFalse("kendi seçtiği parola borcu kapatır");

        var next = await client.GetAsync("/Dashboard/Index");
        (next.Headers.Location?.ToString() ?? "").Should().NotContain("/account/login",
            "parolasını değiştiren kişi kendi oturumundan atılmamalı");
    }

    /// <summary>
    /// 🔑 Seed'lenen süper admin **varsayılan parolayla** doğduğu için bayrağı taşımalı.
    /// Bu test doğrudan DbSeeder'ın kararını kilitler.
    /// </summary>
    [Fact]
    public async Task SeededAdmin_IsBornWithMustChangePassword()
    {
        // Test süiti paylaşılan admin'in bayrağını temizliyor (bkz. WebPanelApplicationFactory),
        // bu yüzden iddia "şu an açık" değil, **seed kuralının kendisi** üzerinden kurulur:
        // varsayılan parolayı geri koyup seeder'ı yeniden çalıştırınca bayrak geri gelmeli.
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var hasher = sp.GetRequiredService<IPasswordHasher>();

            var admin = await db.Users.FirstAsync(u => u.Username == DbSeeder.AdminUsername);
            admin.Password = hasher.HashPassword(DbSeeder.AdminPassword);
            admin.MustChangePassword = false;
            await db.SaveChangesAsync();
        });

        await DbSeeder.SeedAsync(_factory.Services);

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var admin = await db.Users.AsNoTracking().FirstAsync(u => u.Username == DbSeeder.AdminUsername);
            admin.MustChangePassword.Should().BeTrue(
                "varsayılan parola kaynakta yazılı — onu kullanan hesap paneli kullanamamalı");
        });

        // Süiti bozmamak için borcu tekrar kapat (paylaşılan oturum bu hesapla çalışıyor).
        await _factory.ClearMustChangePasswordAsync(DbSeeder.AdminUsername);
    }

    /// <summary>
    /// ⚠️ Seed'in ölçütü "super_admin" değil, **"hâlâ varsayılan parolayı kullanıyor"**.
    /// Parolasını çoktan değiştirmiş yönetici her açılışta parola ekranına düşmemeli.
    /// </summary>
    [Fact]
    public async Task Seeder_DoesNotForceChange_WhenAdminAlreadyChosenOwnPassword()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var hasher = sp.GetRequiredService<IPasswordHasher>();

            var admin = await db.Users.FirstAsync(u => u.Username == DbSeeder.AdminUsername);
            admin.Password = hasher.HashPassword("YoneticininKendiParolasi2026");
            admin.MustChangePassword = false;
            await db.SaveChangesAsync();
        });

        await DbSeeder.SeedAsync(_factory.Services);

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var admin = await db.Users.AsNoTracking().FirstAsync(u => u.Username == DbSeeder.AdminUsername);
            admin.MustChangePassword.Should().BeFalse(
                "kendi parolasını seçmiş yönetici zorunlu değişime tabi tutulmamalı");
        });

        // Süiti eski hâline getir: paylaşılan oturum seed parolasıyla giriyor.
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var hasher = sp.GetRequiredService<IPasswordHasher>();
            var admin = await db.Users.FirstAsync(u => u.Username == DbSeeder.AdminUsername);
            admin.Password = hasher.HashPassword(DbSeeder.AdminPassword);
            admin.MustChangePassword = false;
            await db.SaveChangesAsync();
        });
    }

    /// <summary>Yöneticinin oluşturduğu personel, parolayı kendisi seçmediği için borçlu doğar.</summary>
    [Fact]
    public async Task CreateStaff_MarksMustChangePassword()
    {
        var username = _marker + "-created";
        var id = await SendAsync(new CreateStaffCommand
        {
            Phone = "+9059" + Random.Shared.Next(10000000, 99999999),
            Username = username,
            Password = GoodPassword,
            Role = "moderator"
        });

        (await ReadUserAsync(id)).MustChangePassword.Should().BeTrue(
            "parolayı personelin kendisi değil yönetici belirledi");
    }

    /// <summary>Parola sıfırlaması da aynı borcu doğurur.</summary>
    [Fact]
    public async Task ResetStaffPassword_MarksMustChangePassword()
    {
        var (id, _) = await SeedStaffAsync();

        await SendAsync(new ResetStaffPasswordCommand { Id = id, NewPassword = "SifirlananParola26" });

        (await ReadUserAsync(id)).MustChangePassword.Should().BeTrue();
    }

    // ————————————————————————————————————————————————————————————————
    // 3. Parola politikası
    // ————————————————————————————————————————————————————————————————

    [Theory]
    [InlineData("kisa1", "en az 10")]                 // uzunluk
    [InlineData("parolaparola", "en az bir rakam")]   // rakam yok
    [InlineData("1234567890", "en az bir harf")]      // harf yok
    public void Policy_RejectsWeakPasswords(string password, string expectedFragment)
    {
        var error = PanelPasswordPolicy.Validate(password);

        error.Should().NotBeNull("zayıf parola kabul edilmemeli");
        error!.Should().Contain(expectedFragment);
    }

    [Fact]
    public void Policy_AcceptsStrongPassword() =>
        PanelPasswordPolicy.Validate("Kadirli2026Panel").Should().BeNull();

    [Fact]
    public void Policy_RejectsPasswordEqualToUsername() =>
        PanelPasswordPolicy.Validate("Yonetici2026", username: "Yonetici2026")
            .Should().Contain("kullanıcı adınızla aynı olamaz");

    /// <summary>
    /// 🔑 Politika **tek sahipten** uygulanmalı. Bu test üç kapının üçünü birden dener:
    /// biri elle "&lt; 6" denetimine geri dönerse buradan kırmızıya döner.
    /// </summary>
    [Fact]
    public async Task WeakPassword_RejectedByEveryEntryPoint()
    {
        var weak = "abc123"; // 6 karakter: eski politikaya UYUYOR, yenisine uymuyor

        // (a) Personel oluşturma
        var create = async () => await SendAsync(new CreateStaffCommand
        {
            Phone = "+9059" + Random.Shared.Next(10000000, 99999999),
            Username = _marker + "-weak",
            Password = weak,
            Role = "moderator"
        });
        await create.Should().ThrowAsync<AppException>();

        // (b) Parola sıfırlama
        var (id, _) = await SeedStaffAsync();
        var reset = async () => await SendAsync(new ResetStaffPasswordCommand { Id = id, NewPassword = weak });
        await reset.Should().ThrowAsync<AppException>();

        // (c) Kendi parolasını değiştirme
        var change = async () => await SendAsync(new ChangeMyPasswordCommand(id, GoodPassword, weak));
        await change.Should().ThrowAsync<AppException>();
    }

    // ————————————————————————————————————————————————————————————————
    // 4. Hesap kilidi
    // ————————————————————————————————————————————————————————————————

    /// <summary>
    /// 🔴 Art arda hatalı denemeden sonra hesap kilitlenir ve **doğru parola da** reddedilir.
    /// İkinci kısım kritik: kilit sonrası doğru parola kabul edilseydi, kilit yalnızca
    /// yanlış tahminleri yavaşlatır, doğru tahmini hiç engellemezdi.
    /// </summary>
    [Fact]
    public async Task RepeatedFailures_LockAccount_AndCorrectPasswordIsAlsoRejected()
    {
        var (id, username) = await SeedStaffAsync();
        var client = _factory.CreatePanelClient();

        for (var i = 0; i < PanelLockoutPolicy.MaxFailedAttempts; i++)
        {
            await client.PostFormAsync("/account/login", new Dictionary<string, string>
            {
                ["username"] = username,
                ["password"] = "yanlisparola" + i
            }, tokenFromPath: "/account/login");
        }

        var user = await ReadUserAsync(id);
        user.LockedOutUntil.Should().NotBeNull("hak dolduğunda hesap kilitlenmeli");
        user.FailedLoginAttempts.Should().BeGreaterThanOrEqualTo(PanelLockoutPolicy.MaxFailedAttempts);

        var withCorrectPassword = await client.PostFormAsync("/account/login", new Dictionary<string, string>
        {
            ["username"] = username,
            ["password"] = GoodPassword
        }, tokenFromPath: "/account/login");

        withCorrectPassword.StatusCode.Should().Be(HttpStatusCode.OK,
            "kilitliyken doğru parola da girişi sağlamamalı (200 = form hatayla geri döndü)");
        (await withCorrectPassword.ReadDecodedBodyAsync()).Should().Contain("kilitlendi");
    }

    /// <summary>Başarılı giriş sayacı sıfırlar — kısmi denemeler birikip sonradan patlamaz.</summary>
    [Fact]
    public async Task SuccessfulLogin_ResetsFailureCounter()
    {
        var (id, username) = await SeedStaffAsync();
        var client = _factory.CreatePanelClient();

        await client.PostFormAsync("/account/login", new Dictionary<string, string>
        {
            ["username"] = username,
            ["password"] = "yanlis"
        }, tokenFromPath: "/account/login");

        (await ReadUserAsync(id)).FailedLoginAttempts.Should().Be(1);

        await client.LoginAsync(username, GoodPassword);

        (await ReadUserAsync(id)).FailedLoginAttempts.Should().Be(0, "başarılı giriş sayacı sıfırlamalı");
    }

    /// <summary>Kilit süresi dolunca hesap kendiliğinden açılır (yönetici müdahalesi gerekmez).</summary>
    [Fact]
    public async Task ExpiredLockout_AllowsLoginAgain()
    {
        var (id, username) = await SeedStaffAsync();

        await MutateUserAsync(id, u =>
        {
            u.FailedLoginAttempts = PanelLockoutPolicy.MaxFailedAttempts;
            u.LockedOutUntil = DateTime.UtcNow.AddMinutes(-1); // süresi geçmiş kilit
        });

        var client = _factory.CreatePanelClient();
        await client.LoginAsync(username, GoodPassword); // fırlatmazsa giriş başarılı

        (await ReadUserAsync(id)).LockedOutUntil.Should().BeNull("süresi dolan kilit temizlenmeli");
    }

    /// <summary>Parola sıfırlaması kilidi de açar — "hesabım kilitlendi" çağrısının çözümü budur.</summary>
    [Fact]
    public async Task PasswordReset_ClearsLockout()
    {
        var (id, _) = await SeedStaffAsync();
        await MutateUserAsync(id, u =>
        {
            u.FailedLoginAttempts = PanelLockoutPolicy.MaxFailedAttempts;
            u.LockedOutUntil = DateTime.UtcNow.AddMinutes(30);
        });

        await SendAsync(new ResetStaffPasswordCommand { Id = id, NewPassword = "AcilanParola2026" });

        var user = await ReadUserAsync(id);
        user.LockedOutUntil.Should().BeNull();
        user.FailedLoginAttempts.Should().Be(0);
    }
}
