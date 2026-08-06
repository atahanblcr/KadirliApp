extern alias WebPanel;

using System.Net;
using FluentAssertions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Security;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PanelDisplay = WebPanel::KadirliApp.Web.Common.PanelDisplay;
using PanelMenu = WebPanel::KadirliApp.Web.Common.PanelMenu;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 12.2 — **giriş denemesi günlüğü.**
///
/// 11.18 hesap kilidini getirdi ama yalnız iki sayaç tutuyordu: kaç kez denendiğini
/// biliyorduk, <b>kimin</b> ve <b>nereden</b> denediğini bilmiyorduk. Bu testler ekranın
/// var olduğunu değil, üç sessiz hasar sınıfının kapandığını kilitliyor: kimlik gerçekten
/// maskeleniyor mu, kilitlenen hesap gerçekten <b>işaretleniyor</b> mu, ve ekran izin
/// matrisinin dışında mı.
///
/// ⚠️ <b>TestServer'da <c>RemoteIpAddress</c> null'dır</b> → IP'ye dayanan R2/R3 burada
/// koşamaz; onlar <c>SuspiciousLoginRulesTests</c>'te saf kural olarak kilitli. Buradaki
/// iddia "kural motoru gerçek giriş akışına <b>bağlı</b> mı" — R1 uçtan uca kanıtlıyor.
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelLoginAttemptTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;

    // 🐛 İlk yazımda kullanıcı adı her testte benzersizdi (GUID ekliydi). xUnit her test
    // metodu için sınıfı YENİDEN kurduğu için bu, tek koşuda 14 yeni kullanıcı demekti ve
    // ilgisiz bir testi kırdı: `PanelUsabilityTests.UsersList_ShowsTurkishRoleLabels`
    // kullanıcı listesinin İLK sayfasında süper admin'i arıyor; sahte moderatörler onu
    // sayfadan aşağı itti. Ders: paylaşılan veritabanında "benzersizlik" kayıt SAYISINI
    // artırmamalı — sabit ad + idempotent kurulum (mevcut `moderator-test` deseni).
    private const string Username = "loginattempt-user";
    private const string Password = "Moderator123!";

    private Guid _userId;
    private string _maskedIdentifier = default!;
    private string _userPhone = default!;

    public PanelLoginAttemptTests(WebPanelApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        var user = await _factory.EnsureModeratorAsync(Username, Password);
        _userId = user.Id;
        _userPhone = user.Phone;
        _maskedIdentifier = LoginIdentifierMasker.MaskIdentifier(Username);
        await _factory.ClearMustChangePasswordAsync(Username);

        // Kullanıcı testler arasında paylaşıldığı için kilit durumu da taşınır; önceki
        // testin bıraktığı kilit, bir sonrakinin girişini "locked_out" yapardı.
        await ClearLockoutAsync();
    }

    public async Task DisposeAsync() =>
        await _factory.WithScopeAsync(async sp =>
            await sp.GetRequiredService<AppDbContext>().LoginAttempts
                .Where(a => a.Identifier == _maskedIdentifier || a.UserId == _userId)
                .ExecuteDeleteAsync());

    private async Task<List<LoginAttempt>> MyAttemptsAsync()
    {
        List<LoginAttempt> rows = new();
        await _factory.WithScopeAsync(async sp =>
            rows = await sp.GetRequiredService<AppDbContext>().LoginAttempts
                .Where(a => a.Identifier == _maskedIdentifier || a.UserId == _userId)
                .OrderBy(a => a.CreatedAt)
                .ToListAsync());
        return rows;
    }

    /// <summary>Giriş formunu gerçekten POST'lar — kayıt yolunu uçtan uca koşturmak için.</summary>
    private async Task AttemptLoginAsync(string password)
    {
        var client = _factory.CreatePanelClient();
        var token = await client.GetAntiforgeryTokenAsync("/account/login");
        await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = Username,
            ["password"] = password,
            ["__RequestVerificationToken"] = token
        }));
    }

    private async Task ClearLockoutAsync() =>
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstAsync(u => u.Id == _userId);
            PanelLockoutPolicy.RegisterSuccess(user);
            await db.SaveChangesAsync();
        });

    // ────────────────────────────────────────────────────────────────────────
    // Yalnız-admin deseni (ARCHITECTURE §3)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void MenuEntry_IsOutsidePermissionMatrix()
    {
        var item = PanelMenu.Items.Single(i => i.Controller == "LoginAttemptsAdmin");

        item.Module.Should().BeNull("giriş denemeleri izin matrisine dağıtılabilir bir modül değil");
        item.RequiresPermission.Should().BeFalse();
        PanelMenu.AdminOnlyControllers.Should().Contain("LoginAttemptsAdmin");
    }

    [Fact]
    public async Task Moderator_CannotOpenScreen()
    {
        var client = _factory.CreatePanelClient();
        await client.LoginAsync(Username, Password);

        var response = await client.GetAsync("/LoginAttemptsAdmin/Index");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Found, HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Admin_SeesTheScreen()
    {
        var client = await _factory.SuperAdminAsync();
        var html = await (await client.GetAsync("/LoginAttemptsAdmin/Index")).ReadDecodedBodyAsync();

        html.Should().Contain("Giriş Denemeleri");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Kayıt yolu — bu alt-fazın varlık sebebi
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 <b>Maskeleme sözleşmesi, uçtan uca.</b> Ham kullanıcı adı tabloya HİÇ girmemeli:
    /// kayıtlar panelde görülüyor, CSV'ye çıkıyor ve 180 gün duruyor.
    /// </summary>
    [Fact]
    public async Task FailedLogin_IsRecorded_WithMaskedIdentifier()
    {
        await AttemptLoginAsync("yanlis-parola");

        var rows = await MyAttemptsAsync();

        rows.Should().HaveCount(1);
        rows[0].Succeeded.Should().BeFalse();
        rows[0].FailureReason.Should().Be(LoginFailureReasons.BadPassword);
        rows[0].Channel.Should().Be(LoginChannels.Panel);
        rows[0].Identifier.Should().NotBe(Username, "ham kimlik saklanamaz");
        rows[0].Identifier.Should().Be(_maskedIdentifier);
        rows[0].UserId.Should().Be(_userId, "hesap biliniyorsa kayıt ona bağlanmalı");
    }

    /// <summary>
    /// Giriş ekranı "kullanıcı adı veya şifre hatalı" diyerek ikisini AYIRMAZ (hesap
    /// sorgulama aracına dönüşmesin diye) — ama <b>kayıt ayırır</b>. "Var olmayan hesaba
    /// 200 deneme" ile "tek hesaba 200 deneme" çok farklı saldırılar; panelde aynı
    /// görünselerdi ekran hangi olayın yaşandığını söyleyemezdi.
    /// </summary>
    [Fact]
    public async Task UnknownUser_IsRecordedSeparatelyFromBadPassword()
    {
        var client = _factory.CreatePanelClient();
        var token = await client.GetAntiforgeryTokenAsync("/account/login");
        var ghost = "hayalet-" + Guid.NewGuid().ToString("N")[..8];

        await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = ghost,
            ["password"] = "her-ne-ise",
            ["__RequestVerificationToken"] = token
        }));

        var masked = LoginIdentifierMasker.MaskIdentifier(ghost);
        LoginAttempt? row = null;
        await _factory.WithScopeAsync(async sp =>
            row = await sp.GetRequiredService<AppDbContext>().LoginAttempts
                .FirstOrDefaultAsync(a => a.Identifier == masked));

        row.Should().NotBeNull();
        row!.FailureReason.Should().Be(LoginFailureReasons.UnknownUser);
        row.UserId.Should().BeNull("olmayan hesabın kimliği de yok");

        await _factory.WithScopeAsync(async sp =>
            await sp.GetRequiredService<AppDbContext>().LoginAttempts
                .Where(a => a.Identifier == masked).ExecuteDeleteAsync());
    }

    [Fact]
    public async Task SuccessfulLogin_IsRecorded()
    {
        await AttemptLoginAsync(Password);

        var rows = await MyAttemptsAsync();

        rows.Should().HaveCount(1);
        rows[0].Succeeded.Should().BeTrue();
        rows[0].FailureReason.Should().BeNull("başarılı denemede sebep alanı dolu kalmamalı");
        rows[0].IsSuspicious.Should().BeFalse("sıradan giriş şüpheli değildir");
    }

    // ────────────────────────────────────────────────────────────────────────
    // R1 — kilit ile uyarının aynı anda tetiklenmesi
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 <b>Bitti kriterinin kalbi:</b> 5 hatalı giriş → 5 kayıt + kilit + şüpheli işareti.
    ///
    /// Eşikler ayrışsaydı hesap kilitlenir ama <b>hiçbir uyarı doğmazdı</b> ve bu hiçbir
    /// hata vermeden olurdu — 11.18'in kilidi çalışmaya devam ettiği için kimse fark etmezdi.
    /// Bu test tam olarak o ayrışmayı yakalar.
    /// </summary>
    [Fact]
    public async Task FiveFailedLogins_LockTheAccount_AndFlagTheLastAttempt()
    {
        for (var i = 0; i < PanelLockoutPolicy.MaxFailedAttempts; i++)
            await AttemptLoginAsync("yanlis-parola");

        var rows = await MyAttemptsAsync();

        rows.Should().HaveCount(PanelLockoutPolicy.MaxFailedAttempts, "her deneme bir satır");
        rows.Should().OnlyContain(r => !r.Succeeded);

        rows[^1].IsSuspicious.Should().BeTrue(
            "kilidi tetikleyen deneme aynı anda uyarı da üretmeli — aksi hâlde hesap " +
            "kilitlenir ve kimseye haber gitmez");
        rows[^1].SuspicionRule.Should().Be(SuspicionRules.RepeatedAccountFailure);

        // Kilit gerçekten kurulmuş olmalı (11.18 davranışı değişmedi).
        await _factory.WithScopeAsync(async sp =>
        {
            var user = await sp.GetRequiredService<AppDbContext>().Users.FirstAsync(u => u.Id == _userId);
            PanelLockoutPolicy.IsLockedOut(user, DateTime.UtcNow).Should().BeTrue();
        });

        await ClearLockoutAsync();
    }

    /// <summary>
    /// Kilitliyken gelen deneme de kaydedilir ve <c>locked_out</c> ile ayrılır: "kilitliyken
    /// kaç kez denendi" saldırının hâlâ sürüp sürmediğini gösteren tek işaret.
    /// </summary>
    [Fact]
    public async Task AttemptWhileLockedOut_IsRecordedWithItsOwnReason()
    {
        for (var i = 0; i < PanelLockoutPolicy.MaxFailedAttempts; i++)
            await AttemptLoginAsync("yanlis-parola");

        await AttemptLoginAsync(Password); // kilitliyken DOĞRU parola

        var rows = await MyAttemptsAsync();

        rows[^1].Succeeded.Should().BeFalse("kilitliyken doğru parola da kabul edilmez");
        rows[^1].FailureReason.Should().Be(LoginFailureReasons.LockedOut);

        await ClearLockoutAsync();
    }

    /// <summary>
    /// 🐛 <b>Canlı doğrulamada bulunan kör nokta.</b>
    /// </summary>
    /// <remarks>
    /// Hız sınırı ara katmanı controller'dan <b>önce</b> çalışıyor; kısılan denemeler
    /// <c>AccountController</c>'a hiç ulaşmıyor ve 12.2'nin ilk hâlinde <c>login_attempts</c>'e
    /// <b>tek satır bile</b> düşmüyordu. Saldırgan dakikada 500 deneme yaparken panel
    /// "5 deneme" gösteriyordu — üstelik kısma ne kadar iyi çalışırsa tablo o kadar çok
    /// yalan söylüyordu.
    ///
    /// ⚠️ Bu test kendi limitini kurar: paylaşılan factory limiti bilerek gevşetiyor
    /// (yoksa 400+ test 429 alırdı), dolayısıyla kısma davranışı ancak <b>kendi</b>
    /// factory'siyle sınanabilir.
    /// </remarks>
    [Fact]
    public async Task RateLimitedAttempts_AreRecorded_NotSilentlyDropped()
    {
        var strict = new WebPanelApplicationFactory();
        try
        {
            await strict.InitializeAsync();

            // 🔑 Sıra kritik: `WebPanelApplicationFactory`'nin KURUCUSU limiti 100000'e
            // çekiyor (paylaşılan süit 429 almasın diye). Ortam değişkenini kurucudan
            // ÖNCE yazsaydık üzerine yazılırdı; host ise ilk `CreateClient()` çağrısında
            // kurulduğu için doğru an tam burası.
            // 🐛 İlk yazımda değişken önce set edilmişti ve test "kısma hiç olmadı" diye
            // kırmızıydı — yani ölçtüğü şey kod değil, kendi kurulum sırasıydı.
            Environment.SetEnvironmentVariable("RateLimiting__PanelLogin__PermitLimit", "2");

            var identifier = LoginIdentifierMasker.MaskIdentifier("hizsiniri-denemesi");

            for (var i = 0; i < 5; i++)
            {
                var client = strict.CreatePanelClient();
                string? token = null;
                try { token = await client.GetAntiforgeryTokenAsync("/account/login"); }
                catch (InvalidOperationException) { /* giriş sayfası da kısıldıysa token yok */ }

                await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["username"] = "hizsiniri-denemesi",
                    ["password"] = "yanlis",
                    ["__RequestVerificationToken"] = token ?? ""
                }));
            }

            List<LoginAttempt> rows = new();
            await strict.WithScopeAsync(async sp =>
                rows = await sp.GetRequiredService<AppDbContext>().LoginAttempts
                    .Where(a => a.Identifier == identifier)
                    .ToListAsync());

            rows.Should().Contain(r => r.FailureReason == LoginFailureReasons.RateLimited,
                "kısılan deneme de bir denemedir; kaydedilmezse panel saldırının boyutunu " +
                "olduğundan KÜÇÜK gösterir");
        }
        finally
        {
            // ⚠️ Değişken süreç geneli: geri alınmazsa aynı koşudaki DİĞER panel testleri
            // 429 almaya başlar ve arıza bu testte değil, uzaktaki bir testte görünür.
            Environment.SetEnvironmentVariable("RateLimiting__PanelLogin__PermitLimit", "100000");
            await strict.DisposeAsync();
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Türkçe görsel dil + XSS
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EveryChannelReasonAndRule_HasTurkishLabel()
    {
        foreach (var channel in LoginChannels.All)
            PanelDisplay.LoginChannel(channel).Label.Should().NotBe(channel,
                $"'{channel}' ekrana ham İngilizce basılmamalı (Değişmez Kural #6)");

        foreach (var reason in LoginFailureReasons.All)
            PanelDisplay.LoginFailureReason(reason).Label.Should().NotBe(reason);

        foreach (var rule in SuspicionRules.All)
            PanelDisplay.SuspicionRule(rule).Label.Should().NotBe(rule);
    }

    [Fact]
    public void UnknownValues_AreFlagged_NotSilentlyPrintedRaw()
    {
        PanelDisplay.LoginChannel("sms").Label.Should().Contain("Bilinmeyen");
        PanelDisplay.LoginFailureReason("captcha").Label.Should().Contain("Bilinmeyen");
        PanelDisplay.SuspicionRule("R9").Label.Should().Contain("Bilinmeyen");
    }

    /// <summary>
    /// 🔴 Kimlik dolaylı olarak <b>istemciden</b> geliyor (giriş formu). Görünüm
    /// <c>@Html.Raw</c> kullanırsa panelde depolanmış XSS olur: yöneticinin tarayıcısında
    /// saldırganın betiği koşar. Maskeleme bunu tek başına engellemez — kısa bir yük
    /// maskelemeden geçse bile kaçırılmalı.
    /// </summary>
    [Fact]
    public async Task ClientSuppliedIdentifier_IsEscaped_NotRenderedAsHtml()
    {
        var payload = "<script>alert('xss')</script>";
        var masked = LoginIdentifierMasker.MaskIdentifier(payload);

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            // Maskeleyici baş harfleri koruduğu için "<sc***" gibi bir değer üretir;
            // testin konusu maskeleme değil, o değerin EKRANDA kaçırılması.
            db.LoginAttempts.Add(new LoginAttempt
            {
                Channel = LoginChannels.Panel,
                Identifier = masked,
                Succeeded = false,
                FailureReason = LoginFailureReasons.UnknownUser,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });

        var client = await _factory.SuperAdminAsync();
        var rawHtml = await (await client.GetAsync("/LoginAttemptsAdmin/Index")).Content.ReadAsStringAsync();

        rawHtml.Should().NotContain("<script>alert", "istemciden gelen metin HTML olarak render edilmemeli");
        rawHtml.Should().Contain("&lt;", "kaçırılmış hâli görünmeli");

        await _factory.WithScopeAsync(async sp =>
            await sp.GetRequiredService<AppDbContext>().LoginAttempts
                .Where(a => a.Identifier == masked).ExecuteDeleteAsync());
    }

    // ────────────────────────────────────────────────────────────────────────
    // Süzgeç, sıralama, dışa aktarma
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🐛 Bu test ilk yazımda <b>HTML gövdesinde</b> "Başarılı" arıyordu ve yanlış sebeple
    /// kırmızıydı: kelime tablo satırında değil, süzgeç formundaki <c>&lt;option&gt;</c>
    /// etiketinde geçiyor. Aynı tuzak <c>search</c> için de var — girilen terim arama
    /// kutusuna geri basılıyor, yani "sonuç yok" sayfası bile terimi <b>içeriyor</b>.
    ///
    /// 🔑 Çözüm: süzgeç iddiaları <b>CSV üzerinden</b> kuruluyor. CSV saf bir veri
    /// projeksiyonu — form yankısı, menü, rozet metni yok; "listede ne var" sorusunun
    /// gürültüsüz tek cevabı.
    /// </summary>
    [Fact]
    public async Task SuspiciousFilter_ShowsOnlyFlaggedAttempts()
    {
        await AttemptLoginAsync(Password);                       // sıradan, şüphesiz
        for (var i = 0; i < PanelLockoutPolicy.MaxFailedAttempts; i++)
            await AttemptLoginAsync("yanlis-parola");            // sonuncusu şüpheli

        var client = await _factory.SuperAdminAsync();
        var csv = await ExportCsvAsync(client, "suspicious=true");

        var rows = DataRows(csv);
        rows.Should().HaveCount(1, "yalnız işaretlenmiş deneme kalmalı");
        rows[0].Should().Contain(PanelDisplay.SuspicionRule(SuspicionRules.RepeatedAccountFailure).Label);
        rows[0].Should().Contain("Başarısız");

        // Süzgeçsiz liste altı satırın hepsini görmeli — yoksa üstteki "1" iddiası,
        // süzgecin çalıştığını değil verinin hiç oluşmadığını kanıtlardı.
        DataRows(await ExportCsvAsync(client)).Should().HaveCount(PanelLockoutPolicy.MaxFailedAttempts + 1);

        await ClearLockoutAsync();
    }

    /// <summary>
    /// Görünmez sözleşme #30: her sıralama anahtarı benzersiz bir ayraçla bitmeli.
    /// Eşit değerli satırlarda kararsız sıra, sayfalı listede <b>sessiz veri kaybı</b> demek.
    /// </summary>
    /// <remarks>
    /// 🐛 İlk yazımda iki HTML gövdesi <b>bütün olarak</b> karşılaştırılıyordu ve test her
    /// zaman kırmızıydı: her sayfada yeni bir antiforgery token basılıyor, yani aynı liste
    /// bile iki farklı gövde üretiyor. Sıralama testinde karşılaştırılacak şey gövde değil
    /// <b>satırların sırası</b>. (<c>PanelErrorLogTests</c> aynı çözümü kullanıyor.)
    /// </remarks>
    [Fact]
    public async Task EverySortKey_ProducesStableOrder_ForTiedRows()
    {
        var tied = DateTime.UtcNow;
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            for (var i = 0; i < 4; i++)
            {
                db.LoginAttempts.Add(new LoginAttempt
                {
                    Channel = LoginChannels.Panel,
                    // Satırları AYIRT ETMEK için kimliğe sıra numarası konuyor: hepsi
                    // birebir aynı olsaydı "sıra kararlı mı" sorusu sorulamazdı.
                    Identifier = $"esit{i}***",
                    UserId = _userId,
                    Succeeded = false,
                    FailureReason = LoginFailureReasons.BadPassword,
                    CreatedAt = tied
                });
            }
            await db.SaveChangesAsync();
        });

        var client = await _factory.SuperAdminAsync();

        foreach (var key in KadirliApp.Application.Common.Sorting.PanelSorts.LoginAttempts.Keys)
        {
            var first = await (await client.GetAsync(
                $"/LoginAttemptsAdmin/Index?search=esit&sort={key}")).ReadDecodedBodyAsync();
            var second = await (await client.GetAsync(
                $"/LoginAttemptsAdmin/Index?search=esit&sort={key}")).ReadDecodedBodyAsync();

            OrderOfMarkers(first).Should().HaveCount(4, "dört eşit satırın hepsi listelenmeli");
            OrderOfMarkers(first).Should().Equal(OrderOfMarkers(second),
                $"'{key}' anahtarı eşit değerli satırlarda kararlı sıra üretmeli");
        }
    }

    private static List<string> OrderOfMarkers(string html) =>
        System.Text.RegularExpressions.Regex.Matches(html, @"esit(\d)\*\*\*")
            .Select(m => m.Groups[1].Value).ToList();

    /// <summary>
    /// 🐛 <b>Bu test gerçek bir sızıntı buldu.</b> İlk koşuda kırmızıydı çünkü dosyada ham
    /// kimlik vardı — <c>Identifier</c>'dan değil, <b>"Kullanıcı" sütunundan</b>: sorgu
    /// projeksiyonu panelin alışılmış <c>Username ?? Phone</c> desenini kullanıyordu ve
    /// kullanıcı adı olmayan bir vatandaş hesabında bu, <b>ham telefon numarasını</b>
    /// CSV'ye yazıyordu. Yani <c>Identifier</c>'ı özenle maskeleyip aynı satırın yanına
    /// ham numarayı basan bir dışa aktarma üretmiştik. Yedek değer artık maskeleniyor.
    /// </summary>
    [Fact]
    public async Task ExportCsv_CarriesTheBom_AndNeverLeaksARawPhoneNumber()
    {
        await AttemptLoginAsync("yanlis-parola");

        var client = await _factory.SuperAdminAsync();
        var response = await client.GetAsync($"/LoginAttemptsAdmin/ExportCsv?search={_maskedIdentifier}");

        response.IsSuccessStatusCode.Should().BeTrue();

        // ⚠️ BOM bayt düzeyinde denetlenmeli: ReadAsStringAsync onu ön ek sayıp yutuyor.
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Take(3).Should().Equal(new byte[] { 0xEF, 0xBB, 0xBF },
            "BOM olmadan Excel Türkçe karakteri bozar");

        var csv = System.Text.Encoding.UTF8.GetString(bytes);
        csv.Should().Contain(_maskedIdentifier, "dosya ekrandaki filtrenin aynısı olmalı");
        csv.Should().NotContain(_userPhone,
            "ham telefon numarası dışa aktarmaya sızamaz — maskeleme yalnız Identifier'ı değil, " +
            "hesaptan türetilen yedek değeri de kapsamalı");
    }

    /// <summary>
    /// 🐛 <b>"Kuralı bilerek boz" denetiminde açılan ikinci boşluk.</b>
    /// </summary>
    /// <remarks>
    /// Üstteki test, düzeltmeyi geri aldığımda <b>yeşil kaldı</b>: sızıntı yalnız
    /// <c>Username</c>'i <b>olmayan</b> hesaplarda oluşuyor (<c>Username ?? Phone</c>
    /// yedeği ancak o zaman devreye girer) ve test kullanıcısı bir moderatördü — adı vardı.
    /// Yani test, düzelttiğim şeyi hiç sınamıyordu.
    ///
    /// 🔑 Kırılgan durum, mobil uygulamanın <b>tipik</b> kullanıcısıdır: vatandaş hesapları
    /// telefonla açılıyor ve çoğunun kullanıcı adı yok. Yani sızıntının asıl hedef kitlesi
    /// tam da bu testin kurduğu hesap.
    /// </remarks>
    [Fact]
    public async Task ExportCsv_MasksThePhone_OfAccountsWithoutAUsername()
    {
        var phone = "+90555" + Random.Shared.Next(1000000, 9999999);
        var identifier = LoginIdentifierMasker.MaskIdentifier(phone);
        Guid citizenId = Guid.Empty;

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var citizen = new KadirliApp.Domain.Entities.User
            {
                Phone = phone,
                Username = null, // 🔑 kırılgan durum: vatandaş hesabının adı yok
                Role = KadirliApp.Domain.Enums.UserRole.User,
                IsActive = true
            };
            db.Users.Add(citizen);
            await db.SaveChangesAsync();
            citizenId = citizen.Id;

            db.LoginAttempts.Add(new LoginAttempt
            {
                Channel = LoginChannels.MobileOtp,
                Identifier = identifier,
                UserId = citizen.Id,
                Succeeded = false,
                FailureReason = LoginFailureReasons.BadOtp,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });

        var client = await _factory.SuperAdminAsync();
        var response = await client.GetAsync($"/LoginAttemptsAdmin/ExportCsv?search={identifier}");
        var csv = System.Text.Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());

        csv.Should().Contain(identifier, "kayıt dosyada olmalı — yoksa test hiçbir şey denetlemiyor");
        csv.Should().NotContain(phone,
            "kullanıcı adı olmayan hesapta 'Kullanıcı' sütunu ham telefona düşüyordu: " +
            "Identifier özenle maskelenirken yanındaki sütun numarayı olduğu gibi basıyordu");

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.LoginAttempts.Where(a => a.UserId == citizenId).ExecuteDeleteAsync();
            await db.Users.Where(u => u.Id == citizenId).ExecuteDeleteAsync();
        });
    }

    /// <summary>
    /// Süzgeç iddialarının gürültüsüz kaynağı: CSV. (HTML gövdesi süzgeç formunu ve
    /// menüyü de içerdiği için "listede X yok" iddiaları orada yanlış sonuç verir.)
    /// </summary>
    private async Task<string> ExportCsvAsync(HttpClient client, string? extraQuery = null)
    {
        var query = $"search={_maskedIdentifier}" + (extraQuery is null ? "" : "&" + extraQuery);
        var response = await client.GetAsync($"/LoginAttemptsAdmin/ExportCsv?{query}");
        response.IsSuccessStatusCode.Should().BeTrue();

        return System.Text.Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());
    }

    /// <summary>Başlık satırını atar, dolu veri satırlarını döndürür.</summary>
    private static List<string> DataRows(string csv) =>
        csv.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.Length > 0)
            .ToList();

    /// <summary>
    /// Geçersiz IP metni süzgeci <b>sessizce yok saymaz</b>. Yok sayılsaydı yönetici
    /// yazdığı IP'yi süzdüğünü sanıp tüm listeye bakardı — güvenlik ekranında
    /// verilebilecek en kötü yanlış cevap.
    /// </summary>
    [Fact]
    public async Task InvalidIpFilter_ReturnsNothing_InsteadOfIgnoringTheFilter()
    {
        await AttemptLoginAsync("yanlis-parola");

        var client = await _factory.SuperAdminAsync();

        // ⚠️ İddia CSV üzerinden: HTML gövdesi arama terimini arama KUTUSUNA geri basıyor,
        // yani "sonuç yok" sayfası bile terimi içeriyor (ilk yazımda test bu yüzden
        // yanlış sebeple kırmızıydı).
        DataRows(await ExportCsvAsync(client, "ip=bu-bir-ip-degil"))
            .Should().BeEmpty("geçersiz IP süzgeci yok sayılmamalı");

        // Ekran da bunu açıkça söylemeli — boş bir tablo "filtre çalışmadı" ile
        // "eşleşen yok" arasındaki farkı anlatmaz.
        var html = await (await client.GetAsync(
            $"/LoginAttemptsAdmin/Index?search={_maskedIdentifier}&ip=bu-bir-ip-degil")).ReadDecodedBodyAsync();
        html.Should().Contain("Bu filtreye uyan giriş denemesi yok.");

        // Süzgeç olmadan kayıt görünüyor — yoksa üstteki "boş" iddiası, süzgecin
        // çalıştığını değil verinin hiç oluşmadığını kanıtlardı.
        DataRows(await ExportCsvAsync(client)).Should().ContainSingle();
    }
}
