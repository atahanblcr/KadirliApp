extern alias WebPanel;

using System.Net;
using System.Reflection;
using FluentAssertions;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using PanelMenu = WebPanel::KadirliApp.Web.Common.PanelMenu;
using PanelPermissionAttribute = WebPanel::KadirliApp.Web.Authorization.PanelPermissionAttribute;
using PanelPermissionFilter = WebPanel::KadirliApp.Web.Authorization.PanelPermissionFilter;
using StaffAdminController = WebPanel::KadirliApp.Web.Controllers.StaffAdminController;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.15b — **moderatörün izin matrisi panelde gerçekten geçerli mi?**
///
/// <para>🐛 Bu testlerin varlık sebebi gerçek bir hataydı: panel 10.9(e)'den beri
/// "Moderatör (izin matrisine tabi)" rolüyle personel açıyor ve 16 modüllük bir izin
/// matrisi sunuyordu, ama panelin <b>tüm</b> controller'ları
/// <c>[Authorize(Roles = "admin,super_admin")]</c> taşıdığı için oluşturulan moderatör
/// giriş yapabiliyor ve <b>hiçbir sayfayı açamıyordu</b> — açılışta yönlendirildiği
/// Dashboard dâhil. Matris yalnız <c>/v1/admin/*</c> uçlarını etkiliyordu; onları da
/// bugün hiçbir istemci çağırmıyor. Yani panel, karşılığı olmayan bir yetki dağıtıyordu
/// ve moderatör hesabı **çıkmaz sokaktı**.</para>
///
/// <para>11.13'teki Android bildirim izni hatasıyla aynı sınıf: kullanıcıya bir düğme
/// gösteriliyor, düğme hiçbir şey yapmıyor, hata da verilmiyor.</para>
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelModeratorPermissionTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;
    private const string Username = "moderator-test";
    private const string Password = "Moderator123!";
    private Guid _moderatorId;

    public PanelModeratorPermissionTests(WebPanelApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        var moderator = await _factory.EnsureModeratorAsync(Username, Password);
        _moderatorId = moderator.Id;
        // Yalnız "duyurular" modülünde okuma+güncelleme; onay ve diğer modüller kapalı.
        await SetPermissionsAsync(new AdminPermission
        {
            UserId = _moderatorId, Module = "announcements",
            CanRead = true, CanCreate = true, CanUpdate = true, CanDelete = false, CanApprove = false
        });
    }

    public async Task DisposeAsync() => await SetPermissionsAsync();

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

    private async Task<HttpClient> ModeratorClientAsync()
    {
        var client = _factory.CreatePanelClient();
        await client.LoginAsync(Username, Password);
        return client;
    }

    // ─────────────────────── Aksiyon → izin eylemi türetmesi ───────────────────────

    /// <summary>
    /// Panelde izin eylemi aksiyon adından türetiliyor (18 controller × ~8 aksiyona elle
    /// etiket yazılsaydı biri mutlaka unutulurdu). Türetme kuralı burada kilitli.
    /// </summary>
    [Theory]
    [InlineData("Index", "GET", "read")]
    [InlineData("Calendar", "GET", "read")]
    [InlineData("Create", "GET", "create")]
    [InlineData("Create", "POST", "create")]
    [InlineData("CategoryCreate", "POST", "create")]
    [InlineData("Edit", "POST", "update")]
    [InlineData("NeighborhoodUpdate", "POST", "update")]
    [InlineData("Delete", "POST", "delete")]
    [InlineData("PropertyDelete", "POST", "delete")]
    [InlineData("Approve", "POST", "approve")]
    [InlineData("Reject", "POST", "approve")]
    [InlineData("Verify", "POST", "approve")]
    [InlineData("Unverify", "POST", "approve")]
    [InlineData("Ban", "POST", "approve")]
    [InlineData("Unban", "POST", "approve")]
    // ⚠️ Faz 12.10: arşivleme kaydı public listeden düşürür, yani yayından kaldırma
    // kararıdır. Listede olmasaydı POST olduğu için sessizce "update"e düşerdi ve yalnız
    // DÜZENLEME yetkisi olan moderatör bir vefat ilanını yayından kaldırabilirdi
    // (§7 madde 29'daki BulkApprove hatasının aynısı).
    [InlineData("Archive", "POST", "approve")]
    // ⚠️ Faz A (13 Ağu 2026): manşete çıkarma da "içeriği şehre ulaştırma" kararıdır.
    // Önek eklenmeseydi POST olduğu için sessizce "update"e düşerdi (§7 madde 19'un 5. tekrarı).
    [InlineData("Feature", "POST", "approve")]
    // ⚠️ "UpdateStatus" moderasyon kararıdır — "Update" öneki olarak eşleşirse
    // düzenleme yetkisi olan moderatör şikayet SONUÇLANDIRABİLİR hâle gelir.
    [InlineData("UpdateStatus", "POST", "approve")]
    // Adı hiçbir şey söylemeyen aksiyon: yazma her zaman dar tarafa düşer.
    [InlineData("Bilinmeyen", "POST", "update")]
    [InlineData("Bilinmeyen", "GET", "read")]
    public void ActionName_MapsToTheExpectedPermission(string actionName, string method, string expected)
        => PanelPermissionFilter.ActionFor(actionName, method).Should().Be(expected);

    // ─────────────────────── Yapısal denetimler ───────────────────────

    /// <summary>
    /// 🔴 <b>§7 madde 19 (Faz 0 denetimi — B6): sessiz varsayılanı BİLİNÇLİ KARARA çevirir.</b>
    ///
    /// <para>
    /// Yukarıdaki <c>ActionName_MapsToTheExpectedPermission</c> teorisi eşlemeyi kilitliyor
    /// ama <b>kapsamını elle tutuyor</b>: <c>[InlineData]</c> yalnız *aklımıza gelen* adları
    /// içeriyor. Tuzak tam bu yüzden <b>dört kez</b> tekrarladı — <c>BulkApprove</c> (11.18) ·
    /// <c>Archive</c> (12.10) · <c>Unarchive</c> (12.13) · <c>SendNotification</c> (12.15) —
    /// ve her seferinde "çözüm" listeye elle bir satır eklemek oldu. Yani koruma değil,
    /// <b>ritüel</b>: hatırlayan düzeltiyordu, hatırlamayan sessizce yetki açıyordu.
    /// </para>
    ///
    /// <para>
    /// 🔑 <b>Bu test kapsamı TÜRETİR:</b> matrise tabi her panel controller'ının yazma
    /// aksiyonlarını yansımayla toplar ve adı <b>hiçbir şey söylemeyen</b> her aksiyonu
    /// kırmızıya döndürür. Böylece varsayılan artık "sessizce <c>update</c>" değil,
    /// <b>"bir karar ver ve yaz"</b>: ya adı bir önekle eşleşsin (ör. <c>Approve…</c>),
    /// ya öneki <c>ActionFor</c>'a eklensin, ya da bilinçli olarak aşağıdaki listeye girsin.
    /// </para>
    ///
    /// <para>
    /// ⚠️ Ayırt etme hilesi ikinci bir önek listesi yazmadan çalışıyor: adı bir anahtar
    /// kelimeyle eşleşen aksiyon GET'te de aynı eylemi döndürür (<c>Edit</c> → hep
    /// <c>update</c>), adı <b>hiçbir şey söylemeyen</b> aksiyon ise fiile düşer
    /// (GET → <c>read</c>, POST → <c>update</c>). Yani "geri düştü mü?" sorusu
    /// <c>ActionFor</c>'un kendisine sorulur — kopya bir liste tutulmaz (§7 madde 53).
    /// </para>
    /// </summary>
    [Fact]
    public void EveryWriteAction_SaysWhatItIs_InsteadOfSilentlyFallingBackToUpdate()
    {
        // 📌 Bilinçli istisnalar: adı bir şey söylemeyen ama "update" izni DOĞRU olan
        // aksiyonlar. Listeye ad eklemek bir karardır — yorumuyla birlikte yazılır.
        var deliberateFallbacks = new HashSet<string>(StringComparer.Ordinal)
        {
            // 🔍 İkisi de Faz 0 denetiminde (13 Ağu 2026) bu testin İLK KOŞUSUNDA bulundu —
            // yani madde 19'un tuzağı, sayılan dört tekrardan sonra sessizce **beşinci ve
            // altıncı** kez tekrarlamış. İkisi de bugün "update" iznine düşüyor ve karar
            // **bilinçli olarak korundu** (davranış değişikliği bu denetimin kapsamı değil;
            // amaç sessiz varsayılanı yazılı karara çevirmekti):
            //
            // • ResetOverrides — yöneticinin override'larını temizleyip kaydı kaynağın
            //   hâline döndürür. Görünürlüğe DOKUNMAZ (§7 madde 58: görünürlüğün tek yolu
            //   Archive/Unarchive), yalnız başlık/özet/kapak alanlarını yazar. Yani
            //   "Edit"in geri alma hâli → `update` doğru eylem.
            //
            // 📌 `Feature` bu listeden ÇIKARILDI (13 Ağu 2026, kullanıcı kararı): manşet
            //   şeridi vatandaşın **ilk gördüğü yer** ve yalnız başlık düzeltme yetkisi olan
            //   bir moderatörün onu belirlemesi, §7 madde 19'un uyardığı sessiz yetki
            //   yükselmesiydi. Öneki `ActionFor`'a eklendi → artık `approve`.
            "ResetOverrides",
        };

        var matrixControllers = typeof(WebPanel::Program).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true }
                        && typeof(Controller).IsAssignableFrom(t)
                        && t.GetCustomAttribute<PanelPermissionAttribute>() is not null)
            .ToList();

        matrixControllers.Should().NotBeEmpty(
            "kapsam türetiliyor — türetme çalışmıyorsa bu test hiçbir şey denetlemiyor demektir");

        var silent = new List<string>();

        foreach (var controller in matrixControllers)
        {
            var writeActions = controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .Where(m => m.GetCustomAttribute<NonActionAttribute>() is null)
                .Where(m => m.GetCustomAttribute<HttpPostAttribute>() is not null);

            foreach (var action in writeActions)
            {
                var name = action.Name;
                if (deliberateFallbacks.Contains(name)) continue;

                // Adı bir anahtar kelimeyle eşleşiyorsa GET'te de aynı eylemi verir.
                // Vermiyorsa ad hiçbir şey söylememiş, fiile düşülmüş demektir.
                var asPost = PanelPermissionFilter.ActionFor(name, "POST");
                var asGet = PanelPermissionFilter.ActionFor(name, "GET");
                if (asPost == "update" && asGet == "read")
                    silent.Add($"{controller.Name}.{name}");
            }
        }

        silent.Should().BeEmpty(
            "adı hiçbir önekle eşleşmeyen bir yazma aksiyonu sessizce 'update' iznine düşer " +
            "(§7 madde 19). Bu dört kez yaşandı ve en ağırı 12.15'ti: yalnız BAŞLIK DÜZELTME " +
            "yetkisi olan bir moderatör tüm şehre push atabilirdi. Üç seçenek var ve üçü de " +
            "bilinçli: (a) aksiyonu bir önekle adlandır (Approve…/Update…/Delete…), " +
            "(b) yeni öneki PanelPermissionFilter.ActionFor'a ekle, (c) 'update' gerçekten " +
            "doğruysa bu testteki deliberateFallbacks listesine gerekçesiyle yaz. " +
            "Sessizce varsayılana düşmek artık bir seçenek değil. Adı söylemeyen aksiyonlar: {0}",
            string.Join(", ", silent));
    }

    /// <summary>
    /// Moderatöre açılan her controller <c>[PanelPermission]</c> taşımalı. Rol listesine
    /// "moderator" eklenip öznitelik unutulursa moderatör o modülde **sınırsız** yetki
    /// kazanır — sessiz ve tehlikeli.
    /// </summary>
    /// <remarks>
    /// Faz 11.16b: tek istisna sınıfı eklendi — <b>ekranı değil sonucu süzen</b>
    /// controller'lar (global arama gibi tek modüle ait olmayanlar). Bunlar
    /// <c>PanelMenu.PermissionFilteredControllers</c>'da <b>bildirilmiş</b> olmak zorunda;
    /// ⚠️ listeye ad eklemek testi susturmaya yetmez, çünkü o adlar için süzmenin
    /// gerçekten çalıştığını kanıtlayan ayrı davranış testleri var
    /// (<c>GlobalSearchTests</c>). Aksi hâlde "içeride süzüyorum" iddiası çürüyen bir
    /// yorum satırı olurdu.
    /// </remarks>
    [Fact]
    public void EveryControllerOpenToModerators_CarriesAPermissionAttribute()
    {
        var leaking = typeof(WebPanel::Program).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true }
                        && typeof(Controller).IsAssignableFrom(t)
                        && t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .Where(t => t.GetCustomAttribute<AuthorizeAttribute>()?.Roles?.Contains("moderator") == true)
            .Where(t => t.Name != "DashboardController")   // iniş sayfası — bilinçli olarak matris dışı
            .Where(t => t.Name != "AccountController")     // şifre değiştirme/çıkış herkese açık
            .Where(t => !PanelMenu.PermissionFilteredControllers.Contains(
                t.Name.Replace("Controller", "", StringComparison.Ordinal)))
            .Where(t => t.GetCustomAttribute<PanelPermissionAttribute>() is null)
            .Select(t => t.Name)
            .ToList();

        leaking.Should().BeEmpty(
            "moderatöre açık ama [PanelPermission] taşımayan controller'lar: {0}",
            string.Join(", ", leaking));
    }

    /// <summary>
    /// "Sonucu süzen" istisnası <b>dar</b> kalmalı: listeye yazılan her ad gerçekten
    /// var olan, moderatöre açık ve bilinçli olarak <c>[PanelPermission]</c>'sız bir
    /// controller'a karşılık gelmeli.
    /// </summary>
    /// <remarks>
    /// Bu test olmasaydı liste bir "muafiyet çöplüğü"ne dönüşebilirdi: özniteliği unutan
    /// biri adı buraya ekleyip devam eder, kimse fark etmezdi. Şimdi ad eklemek
    /// <b>bilinçli</b> bir eylem: karşılığı olmayan ad testi kırar.
    /// </remarks>
    [Fact]
    public void PermissionFilteredControllers_OnlyListsRealAttributeFreeControllers()
    {
        var controllers = typeof(WebPanel::Program).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true }
                        && typeof(Controller).IsAssignableFrom(t)
                        && t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .ToDictionary(t => t.Name.Replace("Controller", "", StringComparison.Ordinal));

        foreach (var name in PanelMenu.PermissionFilteredControllers)
        {
            controllers.Should().ContainKey(name,
                "listedeki '{0}' karşılığı olmayan bir controller adı", name);

            var type = controllers[name];

            type.GetCustomAttribute<AuthorizeAttribute>()?.Roles.Should().Contain("moderator",
                "'{0}' moderatöre kapalıysa bu listede olmasının bir anlamı yok — " +
                "istisna dar kalmalı", name);

            type.GetCustomAttribute<PanelPermissionAttribute>().Should().BeNull(
                "'{0}' hem [PanelPermission] taşıyor hem muafiyet listesinde; " +
                "ikisinden biri yanlış", name);
        }
    }

    /// <summary>
    /// 🔴 <b>Faz 12.2 — yalnız-admin ekranların HİÇBİRİ izin matrisinde görünmez.</b>
    /// </summary>
    /// <remarks>
    /// 🐛 Bu test bugün yazıldı çünkü <b>bugüne kadar ihlal ediliyordu</b>:
    /// <c>AuditLogsAdmin</c>, <c>TrashAdmin</c> ve <c>ErrorLogsAdmin</c> kurala uyuyordu ama
    /// <c>StaffAdmin</c> hem <c>AdminOnlyControllers</c>'ta hem de <c>Module = "staff"</c>
    /// taşıyordu. <c>StaffAdminController.Modules</c> menüden türediği için "staff" izin
    /// matrisinde bir satır olarak beliriyordu: yönetici moderatöre "Personel: okuma"
    /// veriyor, kutu işaretleniyor, kaydediliyor — ve rol kapısı yüzünden o yetki
    /// <b>asla çalışmıyordu.</b> 11.15b'nin kapattığı "karşılığı olmayan yetki" hatasının
    /// hâlâ ayakta duran örneğiydi.
    ///
    /// 🔑 Üç ekranın uyup dördüncüsünün uymaması, tam olarak testle yakalanması gereken
    /// şeydir: insan gözü "hepsi aynı desende" der ve dördüncüyü fark etmez.
    /// </remarks>
    [Fact]
    public void AdminOnlyControllers_AreOutsideThePermissionMatrix()
    {
        var offenders = PanelMenu.AdminOnlyControllers
            .Select(name => PanelMenu.Items.FirstOrDefault(i => i.Controller == name))
            .Where(item => item is not null && item.Module is not null)
            .Select(item => item!.Controller)
            .ToList();

        offenders.Should().BeEmpty(
            "yalnız-admin ekranların menü satırında Module null olmalı; modül anahtarı " +
            "verildiğinde izin matrisinde moderatöre dağıtılabilen ama rol kapısı yüzünden " +
            "asla çalışmayacak bir yetki belirir. İhlal edenler: {0}",
            string.Join(", ", offenders));
    }

    /// <summary>
    /// Ters yön: <c>AdminOnlyControllers</c>'a yazılan her ad gerçekten var olan, moderatöre
    /// <b>kapalı</b> bir controller olmalı. Aksi hâlde liste (üstteki testi susturmak için)
    /// bir muafiyet çöplüğüne dönüşebilirdi.
    /// </summary>
    [Fact]
    public void AdminOnlyControllers_OnlyListsControllersThatAreActuallyClosedToModerators()
    {
        var controllers = typeof(WebPanel::Program).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true }
                        && typeof(Controller).IsAssignableFrom(t)
                        && t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .ToDictionary(t => t.Name.Replace("Controller", "", StringComparison.Ordinal));

        foreach (var name in PanelMenu.AdminOnlyControllers)
        {
            controllers.Should().ContainKey(name, "listedeki '{0}' karşılığı olmayan bir ad", name);

            var roles = controllers[name].GetCustomAttribute<AuthorizeAttribute>()?.Roles ?? "";
            roles.Should().NotContain("moderator",
                "'{0}' yalnız-admin listesinde ama rol kapısı moderatöre açık — ikisinden biri yanlış", name);
        }
    }

    /// <summary>
    /// 🔑 Tutarsızlığı düzeltmek, düzeltilmemiş hâlinde OLMAYAN yeni bir hata doğurabilirdi:
    /// personel komutları hâlâ <c>AuditModule = "staff"</c> yazıyor ve o anahtar artık
    /// menüde YOK. Karşılığı <c>NonMatrixModules</c>'a eklenmeseydi denetim izi ekranı
    /// "staff" diye <b>ham İngilizce</b> basmaya başlardı (Değişmez Kural #6).
    /// </summary>
    [Fact]
    public void StaffAuditModule_StillHasATurkishLabel_AfterLeavingTheMatrix()
    {
        WebPanel::KadirliApp.Web.Common.PanelDisplay.ModuleLabel("staff")
            .Should().Be("Personel");

        StaffAdminController.Modules.Select(m => m.Key)
            .Should().NotContain("staff", "personel ekranı artık izin matrisinde değil");
    }

    /// <summary>
    /// Menüdeki modül anahtarları, personel ekranındaki izin matrisiyle birebir aynı
    /// olmalı. Ayrışırlarsa yönetici matriste işaretlediği modülü menüde göremez
    /// (ya da tersi) ve sebebini hiçbir yerde bulamaz.
    /// </summary>
    [Fact]
    public void MenuModules_MatchThePermissionMatrixModules()
    {
        var menuModules = PanelMenu.Items
            .Where(i => i.RequiresPermission)
            .Select(i => i.Module!)
            .ToHashSet(StringComparer.Ordinal);

        var matrixModules = StaffAdminController.Modules.Select(m => m.Key).ToHashSet(StringComparer.Ordinal);

        menuModules.Except(matrixModules).Should().BeEmpty(
            "menüde olup izin matrisinde olmayan modül — yöneticiye asla yetki veremezsiniz");
        matrixModules.Except(menuModules).Should().BeEmpty(
            "izin matrisinde olup menüde olmayan modül — verilen yetkinin panelde karşılığı yok");
    }

    // ─────────────────────── Davranış (uçtan uca) ───────────────────────

    /// <summary>
    /// 🔑 Hatanın kendisi: moderatör panele girebiliyor ama iniş sayfası kapalıydı.
    /// Bu test kırmızıya dönerse moderatör hesabı yeniden çıkmaz sokağa düşmüş demektir.
    /// </summary>
    [Fact]
    public async Task Moderator_CanReachTheLandingPage()
    {
        var client = await ModeratorClientAsync();

        var response = await client.GetAsync("/Dashboard/Index");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "moderatör giriş yaptıktan sonra bir yere varabilmeli — aksi hâlde hesap işlevsizdir");
    }

    [Fact]
    public async Task Moderator_CanOpenAPermittedModule()
    {
        var client = await ModeratorClientAsync();

        var response = await client.GetAsync("/AnnouncementsAdmin/Index");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "izin verilen modül açılmalı");
    }

    [Fact]
    public async Task Moderator_CannotOpenAModuleWithoutPermission()
    {
        var client = await ModeratorClientAsync();

        var response = await client.GetAsync("/UsersAdmin/Index");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect, "yetkisiz sayfa engellenmeli");
        response.Headers.Location!.ToString().Should().Contain("/account/denied",
            "yetkisiz erişim açıklayıcı sayfaya gitmeli, boş 403'e değil");
    }

    /// <summary>
    /// Okuma izni verilen modülde bile **onaylama** ayrı bir yetkidir: moderasyon kararı
    /// içeriği yayına alır, düzenlemekten farklı bir güven gerektirir.
    /// </summary>
    [Fact]
    public async Task Moderator_WithoutApprovePermission_CannotModerate()
    {
        var client = await ModeratorClientAsync();

        var response = await client.PostFormAsync("/AnnouncementsAdmin/Delete",
            new Dictionary<string, string> { ["id"] = Guid.NewGuid().ToString() },
            // Token, moderatörün açabildiği bir formdan alınır: liste sayfasında hiç
            // duyuru yoksa silme formu da basılmaz, dolayısıyla token da bulunmaz.
            tokenFromPath: "/AnnouncementsAdmin/Create");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/account/denied",
            "silme izni olmayan moderatör silme aksiyonuna ulaşmamalı");
    }

    /// <summary>
    /// 🔴 <b>Faz 12.10 — yetki yükselmesinin kapandığının uçtan uca kanıtı.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 12.10 öncesinde bu moderatör (ilanlarda <b>yalnız</b> okuma+düzenleme) Düzenle
    /// formundaki durum menüsünden <c>approved</c> seçip kaydedebiliyordu: aksiyon
    /// <c>Edit</c> olduğu için izin <c>update</c>'e düşüyor, kapı açılıyor ve komut durumu
    /// yazıyordu. Yani <b>moderasyon yetkisi olmadan moderasyon kararı</b> — §7 madde
    /// 29'daki <c>BulkApprove</c> hatasının üçüncü biçimi, ama tersi: burada yetki
    /// <i>fazladan</i> çalışıyordu.
    /// </para>
    /// <para>
    /// ⚠️ Test iki yönlü: <c>Edit</c>'in hâlâ AÇIK olduğunu da doğruluyor. Yalnız "Onayla
    /// kapalı" denseydi, ilanlar modülünü tümüyle kapatan bir gerçekleme de yeşil kalırdı.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Moderator_WithOnlyUpdatePermission_CannotModerate_ButCanStillEdit()
    {
        await SetPermissionsAsync(new AdminPermission
        {
            UserId = _moderatorId, Module = "ads",
            CanRead = true, CanCreate = false, CanUpdate = true, CanDelete = false, CanApprove = false
        });

        var client = await ModeratorClientAsync();

        (await client.GetAsync("/AdsAdmin/Index")).StatusCode.Should().Be(HttpStatusCode.OK,
            "okuma izni var — modül açılmalı");

        var approve = await client.PostFormAsync("/AdsAdmin/Approve",
            new Dictionary<string, string> { ["id"] = Guid.NewGuid().ToString() },
            tokenFromPath: "/AdsAdmin/Index");

        approve.StatusCode.Should().Be(HttpStatusCode.Redirect);
        approve.Headers.Location!.ToString().Should().Contain("/account/denied",
            "onay yetkisi olmayan moderatör moderasyon kararı verememeli — 12.10 öncesinde " +
            "Düzenle formundaki durum menüsü tam olarak bu kapıyı atlıyordu");

        // Aynı moderatör Düzenle sayfasını hâlâ açabilmeli (yetki DARALTILDI, kapatılmadı).
        (await client.GetAsync("/AdsAdmin/Index")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>Personel yönetimi matrisin dışında: izin dağıtan ekran moderatöre kapalı.</summary>
    [Fact]
    public async Task Moderator_CannotReachStaffManagement()
    {
        var client = await ModeratorClientAsync();

        var response = await client.GetAsync("/StaffAdmin/Index");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/account/denied",
            "moderatör kendi izinlerini yazabilecek ekrana ulaşmamalı");
    }

    /// <summary>⚠️ Örnek veri basan aksiyon moderatöre kapalı kalmalı.</summary>
    [Fact]
    public async Task Moderator_CannotSeedMockData()
    {
        var client = await ModeratorClientAsync();

        var response = await client.GetAsync("/Dashboard/Seed");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/account/denied");
    }

    /// <summary>
    /// Menü, ulaşılamayacak bağlantı göstermemeli. Aksi hâlde moderatör 17 satır görür,
    /// bir tanesi çalışır — mobildeki "işlevsiz buton yok" kuralının panel karşılığı.
    /// </summary>
    [Fact]
    public async Task ModeratorMenu_ShowsOnlyReachableModules()
    {
        var client = await ModeratorClientAsync();
        var html = await (await client.GetAsync("/Dashboard/Index")).ReadDecodedBodyAsync();

        html.Should().Contain("Duyurular", "izin verilen modül menüde olmalı");
        html.Should().NotContain("Kullanıcılar", "izin verilmeyen modül menüde çizilmemeli");
        html.Should().NotContain("Personel", "personel yönetimi moderatöre hiç gösterilmemeli");
        html.Should().Contain("Dashboard", "iniş sayfası her zaman görünür");
    }

    /// <summary>
    /// 🐛 Canlı testte yakalandı: Dashboard moderatöre açılınca **"Paneli Test Verileriyle
    /// Doldur"** butonu da görünür oldu, ama o aksiyon admin'e kilitli — moderatör tıklayıp
    /// "Yetkiniz yok" sayfasına düşüyordu. Mobildeki "işlevsiz buton yok" kuralının
    /// panel karşılığı: gösterilen her düğme çalışmalı.
    /// </summary>
    [Fact]
    public async Task ModeratorDashboard_HidesActionsItCannotPerform()
    {
        var client = await ModeratorClientAsync();
        var html = await (await client.GetAsync("/Dashboard/Index")).ReadDecodedBodyAsync();

        html.Should().NotContain("/Dashboard/Seed",
            "moderatöre ulaşamayacağı bir düğme gösterilmemeli");
        html.Should().Contain("Sistem Durumu", "sayfanın kendisi yine açılmalı");
    }

    [Fact]
    public async Task SuperAdminDashboard_KeepsTheSeedAction()
    {
        var client = await _factory.SuperAdminAsync();
        var html = await (await client.GetAsync("/Dashboard/Index")).ReadDecodedBodyAsync();

        html.Should().Contain("/Dashboard/Seed", "admin için düğme kaybolmamalı");
    }

    [Fact]
    public async Task SuperAdminMenu_ShowsEveryModule()
    {
        var client = await _factory.SuperAdminAsync();
        var html = await (await client.GetAsync("/Dashboard/Index")).ReadDecodedBodyAsync();

        foreach (var item in PanelMenu.Items)
            html.Should().Contain(item.Label, "süper admin {0} modülünü menüde görmeli", item.Label);
    }
}
