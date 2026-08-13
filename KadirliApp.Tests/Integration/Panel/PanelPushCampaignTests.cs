extern alias WebPanel;

using System.Net;
using FluentAssertions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.PushCampaigns;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using KadirliApp.Infrastructure.Jobs;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PanelDisplay = WebPanel::KadirliApp.Web.Common.PanelDisplay;
using PanelMenu = WebPanel::KadirliApp.Web.Common.PanelMenu;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 12.2b — **bildirim teslim panosu ve bağımsız gönderim.**
///
/// Bu testlerin iddiası "ekran açılıyor" değil, üç sessiz hasar sınıfının kapandığı:
/// <list type="number">
///   <item><b>#37</b> — <c>FcmSent=true</c> terminaldir: iptal yalnız gönderilmemişe dokunur.</item>
///   <item><b>#38</b> — hedeflemenin tek sahibi var: önizleme ile gerçek gönderim
///         <b>aynı sayıyı</b> verir ve bildirim tercihi manuel gönderimde de geçerlidir.</item>
///   <item><b>#39</b> — sayaçlar artımlı yazılır, ikinci koşuda <b>artmaz</b> ve kampanya
///         token'ı olmayan alıcılar yüzünden sonsuza kadar açık kalmaz.</item>
/// </list>
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelPushCampaignTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;

    // ⚠️ Sabit ön ek + idempotent kurulum: paylaşılan veritabanında her testin yeni
    // kullanıcı üretmesi ilgisiz listeleri kaydırır (12.2'de bu tam olarak yaşandı).
    private const string Marker = "CLAUDE-PUSH";
    private const string ModeratorUsername = "pushcampaign-user";
    private const string ModeratorPassword = "Moderator123!";

    private Guid _neighborhoodId;
    private Guid _otherNeighborhoodId;
    private Guid _withToken;          // hedefte + token'ı var  → push gider
    private Guid _withoutToken;       // hedefte + token'ı yok  → uygulama içinde görür
    private Guid _optedOut;           // hedefte ama bildirimleri KAPALI → hiç satır yazılmaz
    private Guid _elsewhere;          // başka mahalle → hedef dışı

    public PanelPushCampaignTests(WebPanelApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await _factory.EnsureModeratorAsync(ModeratorUsername, ModeratorPassword);
        await _factory.ClearMustChangePasswordAsync(ModeratorUsername);

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();

            var neighborhoods = await db.Neighborhoods.OrderBy(n => n.Name).Take(2).ToListAsync();
            _neighborhoodId = neighborhoods[0].Id;
            _otherNeighborhoodId = neighborhoods[1].Id;

            _withToken = await EnsureUserAsync(db, "+905550000901", _neighborhoodId, "push-token-901", announcements: true);
            _withoutToken = await EnsureUserAsync(db, "+905550000902", _neighborhoodId, null, announcements: true);
            _optedOut = await EnsureUserAsync(db, "+905550000903", _neighborhoodId, "push-token-903", announcements: false);
            _elsewhere = await EnsureUserAsync(db, "+905550000904", _otherNeighborhoodId, "push-token-904", announcements: true);
        });

        await CleanCampaignsAsync();
    }

    public async Task DisposeAsync()
    {
        await CleanCampaignsAsync();
        await CleanUsersAsync();
    }

    /// <summary>
    /// 🧹 <b>T1 (Faz 0 denetimi):</b> bu sınıfın vatandaş kullanıcılarını siler.
    /// </summary>
    /// <remarks>
    /// 🐛 <b>Neden kampanya temizliğinden AYRI:</b> ilk yazımda kullanıcı silme
    /// <c>CleanCampaignsAsync</c>'in içine kondu ve <b>dört test birden kırıldı</b> —
    /// çünkü <c>InitializeAsync</c> kullanıcıları kurduktan <b>sonra</b> aynı temizliği
    /// çağırıyor, yani kurulum kendi kendini siliyordu. Ders küçük ama tam bu maddenin
    /// konusu: <i>temizliğin kapsamı kadar ÇAĞRILDIĞI YER de sözleşmenin parçasıdır.</i>
    /// Kullanıcı silme yalnız <see cref="DisposeAsync"/>'ten çağrılır.
    /// </remarks>
    private Task CleanUsersAsync() => _factory.WithScopeAsync(async sp =>
    {
        var db = sp.GetRequiredService<AppDbContext>();
        await db.Users.IgnoreQueryFilters()
            .Where(u => u.Phone == "+905550000901" || u.Phone == "+905550000902"
                     || u.Phone == "+905550000903" || u.Phone == "+905550000904")
            .ExecuteDeleteAsync();
    });

    // ────────────────────────────────────────────────────────────────────────
    // Kurulum yardımcıları
    // ────────────────────────────────────────────────────────────────────────

    private static async Task<Guid> EnsureUserAsync(
        AppDbContext db, string phone, Guid neighborhoodId, string? token, bool announcements)
    {
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Phone == phone);
        if (user is null)
        {
            user = new User { Phone = phone, Role = UserRole.User };
            db.Users.Add(user);
        }

        user.IsActive = true;
        user.IsBanned = false;
        user.DeletedAt = null;
        user.PrimaryNeighborhoodId = neighborhoodId;
        user.FcmToken = token;
        user.NotificationPreferences = new NotificationPreferences { Announcements = announcements };

        await db.SaveChangesAsync();
        return user.Id;
    }

    /// <summary>
    /// ⚠️ Bildirimler ÖNCE silinir: kampanyaya FK ile bağlılar ve <c>SetNull</c> davranışı
    /// satırı bırakırdı — bir sonraki testin sayımına karışan yetim bildirimler kalırdı.
    /// </summary>
    private Task CleanCampaignsAsync() => _factory.WithScopeAsync(async sp =>
    {
        var db = sp.GetRequiredService<AppDbContext>();
        await db.Notifications.Where(n => n.Title.StartsWith(Marker)).ExecuteDeleteAsync();
        await db.PushCampaigns.Where(c => c.Title.StartsWith(Marker)).ExecuteDeleteAsync();
        // ⚠️ Kullanıcılar burada SİLİNMEZ: bu metot `InitializeAsync`'in sonunda da çağrılıyor
        // (kurulum kendi kendini silerdi). Kullanıcı temizliği `CleanUsersAsync`'te.
    });

    private async Task<T> InDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        T result = default!;
        await _factory.WithScopeAsync(async sp =>
            result = await action(sp.GetRequiredService<AppDbContext>()));
        return result;
    }

    /// <summary>Panel formunu gerçekten POST'lar — komut yolunu uçtan uca koşturmak için.</summary>
    private async Task<HttpResponseMessage> SendAsync(
        HttpClient admin, string title, string targetType, params Guid[] neighborhoodIds)
    {
        var fields = new Dictionary<string, string>
        {
            ["Title"] = title,
            ["Body"] = "Bu bir test bildirimidir.",
            ["TargetType"] = targetType
        };

        // ⚠️ Aynı ada birden çok değer: FormUrlEncodedContent sözlük aldığı için
        // çoklu mahalle seçimi elle kodlanır.
        var extra = string.Concat(neighborhoodIds.Select(id => $"&TargetNeighborhoodIds={id}"));

        var token = await admin.GetAntiforgeryTokenAsync("/PushCampaignsAdmin/Create");
        var body = string.Join("&", fields.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"))
                   + extra
                   + $"&__RequestVerificationToken={Uri.EscapeDataString(token)}";

        return await admin.PostAsync("/PushCampaignsAdmin/Create",
            new StringContent(body, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded"));
    }

    private Task<PushCampaign> CampaignAsync(string title) =>
        InDbAsync(db => db.PushCampaigns.AsNoTracking().FirstAsync(c => c.Title == title));

    private Task<List<Notification>> NotificationsAsync(Guid campaignId) =>
        InDbAsync(db => db.Notifications.AsNoTracking()
            .Where(n => n.CampaignId == campaignId).ToListAsync());

    /// <summary>
    /// Hangfire işleri yalnız <c>KadirliApp.Api</c>'de kayıtlı → panel scope'unda
    /// <c>ActivatorUtilities</c> ile kurulur (<c>BackgroundJobTests</c> deseni).
    /// </summary>
    private Task RunPushJobAsync(IPushService push) => _factory.WithScopeAsync(async sp =>
        await ActivatorUtilities.CreateInstance<SendPushNotificationsJob>(sp, push).RunAsync());

    private sealed class FakePush : IPushService
    {
        private readonly HashSet<string> _invalid;
        public FakePush(params string[] invalidTokens) => _invalid = invalidTokens.ToHashSet();

        public bool IsConfigured => true;
        public int Calls { get; private set; }

        public Task<IReadOnlyList<PushResult>> SendAsync(
            IReadOnlyList<PushMessage> messages, CancellationToken ct = default)
        {
            Calls++;
            IReadOnlyList<PushResult> results = messages
                .Select(m => _invalid.Contains(m.Token)
                    ? PushResult.Failed("Unregistered", tokenInvalid: true)
                    : PushResult.Ok())
                .ToList();
            return Task.FromResult(results);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Yalnız-admin deseni (ARCHITECTURE §3)
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 Bu ekranda gerekçe diğer yalnız-admin ekranlardan <b>ağır</b>: ekran yalnız
    /// göstermiyor, tek tıkla şehrin tamamına push atıyor. Matrise girseydi aksiyon adı
    /// POST olduğu için sessizce <c>update</c>'e düşer (görünmez sözleşme #19) ve yalnız
    /// <i>düzenleme</i> yetkisi olan moderatör herkese bildirim gönderebilirdi.
    /// </summary>
    [Fact]
    public void MenuEntry_IsOutsidePermissionMatrix()
    {
        var item = PanelMenu.Items.Single(i => i.Controller == "PushCampaignsAdmin");

        item.Module.Should().BeNull("bildirim gönderimi izin matrisine dağıtılabilir bir yetki değil");
        item.RequiresPermission.Should().BeFalse();
        PanelMenu.AdminOnlyControllers.Should().Contain("PushCampaignsAdmin");
    }

    [Fact]
    public async Task Moderator_CannotOpenScreen()
    {
        var client = _factory.CreatePanelClient();
        await client.LoginAsync(ModeratorUsername, ModeratorPassword);

        var response = await client.GetAsync("/PushCampaignsAdmin/Index");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Found, HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Moderator_CannotSend()
    {
        var client = _factory.CreatePanelClient();
        await client.LoginAsync(ModeratorUsername, ModeratorPassword);

        var response = await client.PostAsync("/PushCampaignsAdmin/Create",
            new StringContent("Title=x&Body=y&TargetType=all", System.Text.Encoding.UTF8,
                "application/x-www-form-urlencoded"));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Found, HttpStatusCode.Redirect);
        (await InDbAsync(db => db.PushCampaigns.CountAsync(c => c.Title == "x")))
            .Should().Be(0, "ekranın kapısı kapalıysa komutu da çalışmamalı");
    }

    // ────────────────────────────────────────────────────────────────────────
    // #38 — hedeflemenin tek sahibi
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 <b>Bildirim tercihi manuel gönderimde de geçerlidir.</b> "Yönetici elle yolladıysa
    /// herkese gitsin" istisnası açılırsa 10.3'ün tercih ekranı yalan söylemeye başlar:
    /// "bildirimleri kapattım ama geliyor".
    /// </summary>
    [Fact]
    public async Task ManualSend_HonoursNotificationPreferences_AndNeighborhoodTargeting()
    {
        var admin = await _factory.SuperAdminAsync();
        var title = $"{Marker} mahalle hedefi";

        await SendAsync(admin, title, PushTargetTypes.Neighborhood, _neighborhoodId);

        var campaign = await CampaignAsync(title);
        var rows = await NotificationsAsync(campaign.Id);
        var recipients = rows.Select(r => r.UserId).ToHashSet();

        recipients.Should().Contain(_withToken);
        recipients.Should().Contain(_withoutToken, "token'ı olmayan kullanıcı bildirimi uygulama içinde görmeli");
        recipients.Should().NotContain(_optedOut, "bildirimlerini kapatmış kullanıcıya SATIR DA yazılmaz");
        recipients.Should().NotContain(_elsewhere, "hedef dışı mahalledeki kullanıcı almamalı");

        campaign.RecipientCount.Should().Be(rows.Count, "alıcı sayısı yazılan satır sayısıyla aynı olmalı");
        campaign.Source.Should().Be(PushCampaignSources.Manual);
        campaign.CreatedBy.Should().NotBeNull("elle gönderimde 'kim yolladı' kaybolmamalı");
    }

    /// <summary>
    /// 🔴 <b>Önizleme ile gerçek gönderim aynı süzgeçten geçmeli.</b> Ayrı bir sayım
    /// yazılsaydı panel "342 kişiye gidecek" der, gönderim 280 satır yazardı ve aradaki
    /// fark <b>hiçbir yerde görünmezdi</b> — yönetici de yanlış sayıya bakarak onaylardı.
    /// </summary>
    [Fact]
    public async Task EstimatePreview_MatchesWhatIsActuallyWritten()
    {
        var admin = await _factory.SuperAdminAsync();

        var json = await (await admin.GetAsync(
            $"/PushCampaignsAdmin/EstimateRecipients?targetType={PushTargetTypes.Neighborhood}" +
            $"&neighborhoodIds={_neighborhoodId}")).Content.ReadAsStringAsync();

        var estimated = System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("count").GetInt32();

        var title = $"{Marker} onizleme";
        await SendAsync(admin, title, PushTargetTypes.Neighborhood, _neighborhoodId);

        var campaign = await CampaignAsync(title);
        campaign.RecipientCount.Should().Be(estimated,
            "önizleme ile gönderim aynı sorgudan geçmeli — ayrışırsa kimse fark etmez");
    }

    /// <summary>
    /// Mahalle hedeflemesi seçilip hiç mahalle işaretlenmemesi bir <b>doğrulama hatasıdır</b>.
    /// ⚠️ Sessizce "herkes"e düşseydi bir form hatası, tüm şehre giden bildirime dönüşürdü.
    /// </summary>
    [Fact]
    public async Task NeighborhoodTargetingWithoutSelection_IsRejected_NotBroadcast()
    {
        var admin = await _factory.SuperAdminAsync();
        var title = $"{Marker} bos hedef";

        var response = await SendAsync(admin, title, PushTargetTypes.Neighborhood);
        var html = await response.ReadDecodedBodyAsync();

        html.Should().Contain("en az bir mahalle");
        (await InDbAsync(db => db.PushCampaigns.CountAsync(c => c.Title == title)))
            .Should().Be(0, "reddedilen gönderim kampanya satırı üretmemeli");
    }

    // ────────────────────────────────────────────────────────────────────────
    // #39 — artımlı sayaçlar ve tamamlanma
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 <b>Bu fazın en önemli iddiası.</b> İki ayrı sessiz hasar birden kilitleniyor:
    /// <list type="bullet">
    ///   <item>Sayaç yazımı atlanırsa pano sonsuza kadar "Kuyrukta" gösterir — bildirimler
    ///         gider, <c>fcm_sent</c> dolar, hiçbir hata oluşmaz ve <b>yalnız pano yalan söyler</b>.</item>
    ///   <item>Tamamlanma ölçütü "işlenen = alıcı" yapılırsa <b>hiçbir kampanya tamamlanmaz</b>:
    ///         job yalnız token'ı olan satırları alır, token'sız alıcı sonsuza kadar bekler.
    ///         Bu testte alıcılardan birinin bilerek token'ı yok.</item>
    /// </list>
    /// </summary>
    [Fact]
    public async Task Job_WritesCountersIncrementally_AndCompletesDespiteTokenlessRecipients()
    {
        var admin = await _factory.SuperAdminAsync();
        var title = $"{Marker} sayac";
        await SendAsync(admin, title, PushTargetTypes.Neighborhood, _neighborhoodId);

        var before = await CampaignAsync(title);
        before.SentCount.Should().Be(0);
        before.CompletedAt.Should().BeNull("henüz job koşmadı");
        PushCampaignStatus.Of(before.RecipientCount, before.SentCount, before.FailedCount,
                before.CompletedAt, before.CancelledAt)
            .Should().Be(PushCampaignStatuses.Queued);

        await RunPushJobAsync(new FakePush());

        var after = await CampaignAsync(title);
        after.SentCount.Should().Be(1, "token'ı olan tek alıcıya gönderildi");
        after.FailedCount.Should().Be(0);
        after.CompletedAt.Should().NotBeNull(
            "token'ı olmayan alıcı beklerken bile kampanya tamamlanmalı — yoksa hiçbiri kapanmaz");

        // Bekleyen: token'ı olmayan alıcı. Bildirimi uygulama içinde GÖRÜYOR.
        PushCampaignStatus.Pending(after.RecipientCount, after.SentCount, after.FailedCount)
            .Should().Be(1);

        // 🔑 İkinci koşu: FcmSent=true terminal olduğu için satırlar bir daha alınmaz.
        await RunPushJobAsync(new FakePush());

        var twice = await CampaignAsync(title);
        twice.SentCount.Should().Be(1, "sayaçlar ARTIMLI — ikinci koşu aynı satırları tekrar saymamalı");
        twice.CompletedAt.Should().Be(after.CompletedAt, "tamamlanma anı ilk tamamlanmadır, tazelenmez");
    }

    /// <summary>
    /// Geçersiz token sayacı ve başarısızlık kırılımı: "188 başarısız" yazan bir pano
    /// <b>neden</b> sorusuna cevap veremezse yönetici hiçbir şey yapamaz.
    /// </summary>
    [Fact]
    public async Task Job_RecordsFailuresAndClearsInvalidTokens()
    {
        var admin = await _factory.SuperAdminAsync();
        var title = $"{Marker} gecersiz token";
        await SendAsync(admin, title, PushTargetTypes.Neighborhood, _neighborhoodId);

        await RunPushJobAsync(new FakePush("push-token-901"));

        var campaign = await CampaignAsync(title);
        campaign.SentCount.Should().Be(0);
        campaign.FailedCount.Should().Be(1);
        campaign.InvalidTokenCount.Should().Be(1);

        (await InDbAsync(db => db.Users.AsNoTracking().Where(u => u.Id == _withToken)
            .Select(u => u.FcmToken).FirstAsync()))
            .Should().BeNull("kalıcı geçersiz token temizlenmeli (10.11 kuralı)");

        var detail = await (await admin.GetAsync($"/PushCampaignsAdmin/Details?id={campaign.Id}"))
            .ReadDecodedBodyAsync();
        detail.Should().Contain("Unregistered", "başarısızlığın SEBEBİ ekranda görünmeli");
    }

    // ────────────────────────────────────────────────────────────────────────
    // #37 — FcmSent terminaldir
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 <b>İptal, gönderimin tersi değil sınırıdır.</b> İletilmiş mesaj geri alınamaz;
    /// komut ona dokunmaz. Dokunsaydı panel "geri aldım" der, kullanıcının telefonundaki
    /// bildirim yerinde durur ve <b>kimse hata almazdı</b>.
    /// </summary>
    [Fact]
    public async Task Cancel_RemovesOnlyUnsentNotifications()
    {
        var admin = await _factory.SuperAdminAsync();
        var title = $"{Marker} iptal";
        await SendAsync(admin, title, PushTargetTypes.Neighborhood, _neighborhoodId);

        var campaign = await CampaignAsync(title);
        var beforeRows = await NotificationsAsync(campaign.Id);
        beforeRows.Should().HaveCount(2, "token'lı + token'sız iki alıcı");

        // Token'lı alıcının satırı gönderilir → terminal olur.
        await RunPushJobAsync(new FakePush());

        await admin.PostFormAsync($"/PushCampaignsAdmin/Cancel?id={campaign.Id}",
            new Dictionary<string, string>(),
            tokenFromPath: $"/PushCampaignsAdmin/Details?id={campaign.Id}");

        var afterRows = await NotificationsAsync(campaign.Id);
        afterRows.Should().HaveCount(1, "yalnız gönderilmemiş satır geri çekilmeli");
        afterRows[0].FcmSent.Should().BeTrue("kalan satır, iletilmiş olandır");
        afterRows[0].UserId.Should().Be(_withToken);

        var cancelled = await CampaignAsync(title);
        cancelled.CancelledAt.Should().NotBeNull();
        cancelled.RecipientCount.Should().Be(2,
            "alıcı sayısı TARİHÇEDİR, iptalde düşürülmez — 'neden bu kişi görmedi' izi kaybolmamalı");
        PushCampaignStatus.Of(cancelled.RecipientCount, cancelled.SentCount, cancelled.FailedCount,
                cancelled.CompletedAt, cancelled.CancelledAt)
            .Should().Be(PushCampaignStatuses.Cancelled, "iptal 'tamamlandı' diye okunmamalı");
    }

    /// <summary>
    /// Geri çekilecek bir şey kalmamışsa iptal <b>reddedilir</b> ve panel butonu hiç çizmez —
    /// komutun kabul etmeyeceği bir butonu göstermek "işlevsiz buton"un panel karşılığıdır.
    /// </summary>
    [Fact]
    public async Task Cancel_IsRejectedWhenNothingIsLeftToWithdraw()
    {
        var admin = await _factory.SuperAdminAsync();
        var title = $"{Marker} iptal edilemez";
        await SendAsync(admin, title, PushTargetTypes.All);

        var campaign = await CampaignAsync(title);

        // Tamamen teslim edilmiş bir kampanyayı taklit et: her satır iletilmiş, sayaç da
        // job'ın yazacağı hâlde. ⚠️ Yalnız `FcmSent` işaretlenip sayaç bırakılsaydı test
        // gerçekte var olamayacak bir durumu kurar ve hiçbir şey kanıtlamazdı.
        await InDbAsync(async db =>
        {
            await db.Notifications.Where(n => n.CampaignId == campaign.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.FcmSent, true));
            await db.PushCampaigns.Where(c => c.Id == campaign.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.SentCount, c => c.RecipientCount)
                    .SetProperty(c => c.CompletedAt, DateTime.UtcNow));
            return true;
        });

        var detail = await (await admin.GetAsync($"/PushCampaignsAdmin/Details?id={campaign.Id}"))
            .ReadDecodedBodyAsync();
        detail.Should().NotContain("Gönderilmemişleri geri çek",
            "komutun reddedeceği buton ekranda hiç çizilmemeli");

        // Buton çizilmese de uç açık: doğrudan POST edilirse komut yine reddetmeli.
        var response = await admin.PostFormAsync($"/PushCampaignsAdmin/Cancel?id={campaign.Id}",
            new Dictionary<string, string>(),
            tokenFromPath: "/PushCampaignsAdmin/Create");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Found, HttpStatusCode.Redirect);

        var after = await CampaignAsync(title);
        after.CancelledAt.Should().BeNull("geri çekilecek satır kalmamışsa iptal reddedilir");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Panelin görsel dili ve güvenliği
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Değişmez Kural #6: ham İngilizce basılmaz. Yalnız-admin ekranın komutları
    /// <c>AuditModule</c> yazmak <b>zorunda</b>, ama o anahtar menüde <c>Module=null</c>
    /// olduğu için <c>ModuleLabel</c> onu ancak <c>NonMatrixModules</c>'ta bulabilir.
    /// </summary>
    [Fact]
    public void AuditActionsAndModule_HaveTurkishLabels()
    {
        PanelDisplay.ModuleLabel("push-campaigns").Should().Be("Bildirim Gönderimleri");
        PanelDisplay.AuditAction("send-push").Label.Should().Be("Bildirim gönderdi");
        PanelDisplay.AuditAction("cancel-push").Label.Should().Be("Gönderimi iptal etti");
        PanelDisplay.AffectedTypeLabel(nameof(PushCampaign)).Should().Be("Bildirim gönderimi");
    }

    /// <summary>
    /// Kaynak, hedef ve durum DB'de İngilizce sabit — <b>hepsinin</b> Türkçe karşılığı olmalı.
    /// Yeni bir durum eklenip karşılığı yazılmazsa panel ham değeri basardı.
    /// </summary>
    [Fact]
    public void EveryRawValue_HasATurkishBadge()
    {
        foreach (var source in PushCampaignSources.All)
            PanelDisplay.PushSource(source).Label.Should().NotContain("Bilinmeyen", "kaynak: {0}", source);

        foreach (var target in PushTargetTypes.Supported)
            PanelDisplay.PushTarget(target).Label.Should().NotContain("Bilinmeyen", "hedef: {0}", target);

        foreach (var status in PushCampaignStatuses.All)
            PanelDisplay.PushStatus(status).Label.Should().NotContain("Bilinmeyen", "durum: {0}", status);
    }

    /// <summary>
    /// Gönderim başlığı bir forma yazılıyor ve panelde <b>listelenip</b> ayrıntıda basılıyor.
    /// Razor kaçışı kalkarsa (<c>@Html.Raw</c>) yöneticinin tarayıcısında betik koşar —
    /// 12.1'in hata kaydı kararının aynısı.
    /// </summary>
    [Fact]
    public async Task SubmittedTitle_IsEscapedInTheUi()
    {
        var admin = await _factory.SuperAdminAsync();
        var title = $"{Marker} <script>alert(1)</script>";

        await SendAsync(admin, title, PushTargetTypes.All);

        var raw = await (await admin.GetAsync("/PushCampaignsAdmin/Index")).Content.ReadAsStringAsync();

        raw.Should().NotContain("<script>alert(1)</script>", "başlık ham HTML olarak render edilmemeli");
        raw.Should().Contain("&lt;script&gt;", "kaçırılmış hâli görünmeli");
    }

    [Fact]
    public async Task Admin_SeesTheScreen()
    {
        var admin = await _factory.SuperAdminAsync();
        var html = await (await admin.GetAsync("/PushCampaignsAdmin/Index")).ReadDecodedBodyAsync();

        html.Should().Contain("Bildirim Gönderimleri");
    }
}
