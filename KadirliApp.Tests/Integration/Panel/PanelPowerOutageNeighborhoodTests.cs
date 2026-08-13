extern alias WebPanel;

using System.Net;
using FluentAssertions;
using KadirliApp.Application.Features.Notifications.Services;
using KadirliApp.Application.Features.PowerOutages.DTOs;
using KadirliApp.Application.Features.PowerOutages.Queries.GetPowerOutages;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PowerOutagesAdminController = WebPanel::KadirliApp.Web.Controllers.PowerOutagesAdminController;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 12.3 — **kesinti mahalle referansı + mahalle bazlı bildirim.**
///
/// İddia "form açılıyor" değil; üç yeni görünmez sözleşmenin gerçekten tutması:
/// <list type="number">
///   <item><b>#40</b> — <c>power_outages.neighborhood</c> metni FK doluyken <b>sözlükten
///         türetilir</b>, elle yazılmaz. Ayrışırsa panel bir ad, mobil başka bir ad görür
///         ve "sadece mahallem" süzgeci sessizce boş kalır.</item>
///   <item><b>#41</b> — kesinti bildirimi <b>bir duyurudur</b>: kesinti silinince duyurusu
///         ve bildirimleri de gider (#24'ün uzantısı — 11.15c'de 9 ölü bildirim yaşandı).
///         Güncelleme <b>ikinci duyuru üretmez</b>.</item>
///   <item><b>#42</b> — bildirim yalnız <b>FK'sı dolu</b> kesintide gönderilebilir; serbest
///         metinli kayıt sessizce "gönderildi" demez.</item>
/// </list>
///
/// Ayrıca görünmez sözleşme <b>#1</b> (uç sayfalamaz, düz dizi döner) bu fazda kırılmadığı
/// için ayrıca denetleniyor: mahalle alanları eklendi, <b>şekil değişmedi</b>.
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelPowerOutageNeighborhoodTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;

    private const string Marker = "CLAUDE-OUTAGE";

    private Guid _neighborhoodId;
    private string _neighborhoodName = default!;
    private Guid _otherNeighborhoodId;

    private Guid _resident;   // hedef mahallede, bildirimleri açık
    private Guid _elsewhere;  // başka mahallede

    public PanelPowerOutageNeighborhoodTests(WebPanelApplicationFactory factory) => _factory = factory;

    /// <summary>
    /// 🐛 <b>Bu testler KENDİ mahallelerini kurar ve bu bilinçli.</b> İlk yazımda sözlüğün
    /// ilk iki mahallesi (<c>OrderBy(Name).Take(2)</c>) ödünç alınmıştı — <c>PanelPushCampaignTests</c>
    /// de aynı iki satırı kullanıyor ve buradaki iki test kullanıcısı <b>onun alıcı sayımına
    /// karıştı</b>: "2 alıcı bekleniyordu, 3 bulundu". Paylaşılan veritabanında sayı iddia eden
    /// her test kendi kitlesini kurmak zorunda (12.2b'de aynı ders "sabit ön ek + idempotent
    /// kurulum" olarak yazılmıştı).
    ///
    /// ⚠️ Adlar <b>Z</b> ile başlıyor: sözlüğün başına eklenselerdi başka testlerin
    /// <c>OrderBy(Name).Take(2)</c> seçimi kayardı ve kirlilik yön değiştirirdi.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();

            var here = await EnsureNeighborhoodAsync(db, "Zzz Kesinti Testi A");
            var there = await EnsureNeighborhoodAsync(db, "Zzz Kesinti Testi B");

            _neighborhoodId = here.Id;
            _neighborhoodName = here.Name;
            _otherNeighborhoodId = there.Id;

            _resident = await EnsureUserAsync(db, "+905550000931", _neighborhoodId);
            _elsewhere = await EnsureUserAsync(db, "+905550000932", _otherNeighborhoodId);
        });

        await CleanAsync();
    }

    private static async Task<Neighborhood> EnsureNeighborhoodAsync(AppDbContext db, string name)
    {
        var slug = KadirliApp.Application.Common.Utils.SlugHelper.Slugify(name);
        var existing = await db.Neighborhoods.FirstOrDefaultAsync(n => n.Slug == slug);
        if (existing is not null) return existing;

        var created = new Neighborhood { Name = name, Slug = slug, IsActive = true, DisplayOrder = 999 };
        db.Neighborhoods.Add(created);
        await db.SaveChangesAsync();
        return created;
    }

    public async Task DisposeAsync()
    {
        await CleanAsync();
        await CleanUsersAsync();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Kurulum
    // ────────────────────────────────────────────────────────────────────────

    private static async Task<Guid> EnsureUserAsync(AppDbContext db, string phone, Guid neighborhoodId)
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
        user.NotificationPreferences = new NotificationPreferences { Announcements = true };

        await db.SaveChangesAsync();
        return user.Id;
    }

    /// <summary>
    /// ⚠️ Sıra önemli: bildirimler → kampanyalar → duyurular → kesintiler. Ters sırada
    /// FK'lar yetim satır bırakır ve bir sonraki testin sayımına karışır.
    /// </summary>
    private Task CleanAsync() => _factory.WithScopeAsync(async sp =>
    {
        var db = sp.GetRequiredService<AppDbContext>();

        var announcementIds = await db.Announcements.IgnoreQueryFilters()
            .Where(a => a.Title.Contains(Marker)).Select(a => a.Id).ToListAsync();

        await db.Notifications.Where(n => announcementIds.Contains(n.RelatedId!.Value)).ExecuteDeleteAsync();
        await db.PushCampaigns.Where(c => c.SourceId != null && announcementIds.Contains(c.SourceId!.Value))
            .ExecuteDeleteAsync();
        await db.PowerOutages.IgnoreQueryFilters()
            .Where(o => o.AreaDetail != null && o.AreaDetail.Contains(Marker)).ExecuteDeleteAsync();
        await db.Announcements.IgnoreQueryFilters()
            .Where(a => a.Title.Contains(Marker)).ExecuteDeleteAsync();
        // ⚠️ Kullanıcılar burada SİLİNMEZ: bu metot `InitializeAsync`'in sonunda da çağrılıyor
        // (kurulum kendi kendini silerdi — ilk yazımda üç test bu yüzden kırıldı).
        // Kullanıcı temizliği `CleanUsersAsync`'te, yalnız `DisposeAsync`'ten çağrılır.
    });

    /// <summary>🧹 T1 (Faz 0 denetimi): yalnız bu sınıfın vatandaş kullanıcıları.</summary>
    private Task CleanUsersAsync() => _factory.WithScopeAsync(async sp =>
    {
        var db = sp.GetRequiredService<AppDbContext>();
        await db.Users.IgnoreQueryFilters()
            .Where(u => u.Phone == "+905550000931" || u.Phone == "+905550000932")
            .ExecuteDeleteAsync();
    });

    private async Task<T> InDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        T result = default!;
        await _factory.WithScopeAsync(async sp => result = await action(sp.GetRequiredService<AppDbContext>()));
        return result;
    }

    /// <summary>Paneli gerçekten POST'lar — komut yolu uçtan uca koşsun diye.</summary>
    private async Task<HttpResponseMessage> CreateAsync(
        HttpClient admin, string areaDetail, Guid? neighborhoodId,
        bool notify, string? freeText = null, params Guid[] extraTargets)
    {
        var start = DateTime.UtcNow.AddHours(2);
        var parts = new List<string>
        {
            $"AreaDetail={Uri.EscapeDataString(areaDetail)}",
            $"StartTime={start:yyyy-MM-ddTHH:mm}",
            $"EndTime={start.AddHours(3):yyyy-MM-ddTHH:mm}",
            $"Reason={Uri.EscapeDataString("Faz 12.3 testi")}",
            $"SendNotification={notify.ToString().ToLowerInvariant()}"
        };

        if (neighborhoodId is { } id) parts.Add($"NeighborhoodId={id}");
        if (freeText is not null) parts.Add($"Neighborhood={Uri.EscapeDataString(freeText)}");
        parts.AddRange(extraTargets.Select(t => $"TargetNeighborhoodIds={t}"));

        var token = await admin.GetAntiforgeryTokenAsync("/PowerOutagesAdmin/Create");
        parts.Add($"__RequestVerificationToken={Uri.EscapeDataString(token)}");

        return await admin.PostAsync("/PowerOutagesAdmin/Create",
            new StringContent(string.Join("&", parts), System.Text.Encoding.UTF8,
                "application/x-www-form-urlencoded"));
    }

    private Task<PowerOutage> OutageAsync(string areaDetail) =>
        InDbAsync(db => db.PowerOutages.AsNoTracking().FirstAsync(o => o.AreaDetail == areaDetail));

    // ────────────────────────────────────────────────────────────────────────
    // #40 — mahalle adı TÜRETİLMİŞTİR
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 Yönetici serbest metin de gönderse, FK varsa ad <b>sözlükten</b> yazılır.
    /// Ayrışsalardı panel "Cengiz Topel Mah." derken mobil kullanıcının profilindeki
    /// "Cengiz Topel" ile eşleşmez ve "sadece mahallem" süzgeci <b>sessizce boş</b> kalırdı.
    /// </summary>
    [Fact]
    public async Task NeighbourhoodName_IsDerivedFromTheDictionary_NotFromTheForm()
    {
        var admin = await _factory.SuperAdminAsync();
        var area = $"{Marker}-TURETILMIS";

        var response = await CreateAsync(admin, area, _neighborhoodId, notify: false,
            freeText: "TAMAMEN BAŞKA BİR METİN");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var outage = await OutageAsync(area);
        outage.NeighborhoodId.Should().Be(_neighborhoodId);
        outage.Neighborhood.Should().Be(_neighborhoodName,
            "FK doluyken ad sözlükten türetilir — formdaki serbest metin yok sayılır");
    }

    /// <summary>FK verilmezse eski davranış korunur: serbest metin olduğu gibi kalır (kontrat additive).</summary>
    [Fact]
    public async Task FreeTextIsKept_WhenNoDictionaryReferenceIsGiven()
    {
        var admin = await _factory.SuperAdminAsync();
        var area = $"{Marker}-SERBEST";

        await CreateAsync(admin, area, neighborhoodId: null, notify: false, freeText: "Bilinmeyen Bölge");

        var outage = await OutageAsync(area);
        outage.NeighborhoodId.Should().BeNull();
        outage.Neighborhood.Should().Be("Bilinmeyen Bölge");
    }

    // ────────────────────────────────────────────────────────────────────────
    // #41 — kesinti bildirimi BİR DUYURUDUR
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Notification_CreatesAnnouncement_AndReachesOnlyTheTargetedNeighbourhood()
    {
        var admin = await _factory.SuperAdminAsync();
        var area = $"{Marker}-BILDIRIM";

        await CreateAsync(admin, area, _neighborhoodId, notify: true);

        var outage = await OutageAsync(area);
        outage.AnnouncementId.Should().NotBeNull("kesinti bildirimi bir duyurudur — çengel doldurulmalı");

        var announcement = await InDbAsync(db => db.Announcements.IgnoreQueryFilters()
            .AsNoTracking().FirstAsync(a => a.Id == outage.AnnouncementId!.Value));

        announcement.TargetType.Should().Be(PushTargetTypes.Neighborhood);
        announcement.SendPushNotification.Should().BeTrue();
        announcement.Status.Should().Be("active");
        announcement.VisibleUntil.Should().BeCloseTo(outage.EndTime, TimeSpan.FromSeconds(1),
            "duyuru kesinti bitince kendiliğinden görünmez olmalı");

        var notifications = await InDbAsync(db => db.Notifications.AsNoTracking()
            .Where(n => n.RelatedId == announcement.Id).ToListAsync());

        notifications.Select(n => n.UserId).Should().Contain(_resident);
        notifications.Select(n => n.UserId).Should().NotContain(_elsewhere,
            "başka mahalledeki kullanıcıya kesinti bildirimi YAZILMAMALI");

        // Deep-link zinciri değişmedi: mobil `announcement` türünü zaten tanıyor (#18).
        notifications.Should().OnlyContain(
            n => n.RelatedType == AnnouncementNotificationGenerator.RelatedTypeAnnouncement,
            "yeni bir relatedType uydurulsaydı eski sürümler bildirime dokunduğunda hiçbir yere gitmezdi");
    }

    /// <summary>
    /// Teslim panosunda "bu push nereden çıktı?" sorusunun cevabı <b>kesinti</b> olmalı.
    /// <c>announcement</c> yazsaydı yönetici kesinti gönderimlerini hiçbir süzgeçle ayıramazdı.
    /// </summary>
    [Fact]
    public async Task Campaign_IsAttributedToThePowerOutageSource()
    {
        var admin = await _factory.SuperAdminAsync();
        var area = $"{Marker}-KAYNAK";

        await CreateAsync(admin, area, _neighborhoodId, notify: true);
        var outage = await OutageAsync(area);

        var campaign = await InDbAsync(db => db.PushCampaigns.AsNoTracking()
            .FirstAsync(c => c.SourceId == outage.AnnouncementId));

        campaign.Source.Should().Be(PushCampaignSources.PowerOutage);
        campaign.TargetType.Should().Be(PushTargetTypes.Neighborhood);
    }

    /// <summary>
    /// 🔴 Güncelleme <b>ikinci duyuru üretmez</b>. Üretseydi bir yazım düzeltmesi bile
    /// şehre ikinci bir push atardı; kullanıcı aynı kesintiyi iki kez alırdı.
    /// </summary>
    [Fact]
    public async Task Update_RefreshesTheAnnouncement_WithoutSendingASecondNotification()
    {
        var admin = await _factory.SuperAdminAsync();
        var area = $"{Marker}-GUNCELLEME";

        await CreateAsync(admin, area, _neighborhoodId, notify: true);
        var outage = await OutageAsync(area);
        var announcementId = outage.AnnouncementId!.Value;

        var before = await InDbAsync(db => db.Notifications.CountAsync(n => n.RelatedId == announcementId));
        before.Should().BeGreaterThan(0);

        var newEnd = outage.EndTime.AddHours(5);
        var token = await admin.GetAntiforgeryTokenAsync($"/PowerOutagesAdmin/Edit/{outage.Id}");
        var body = string.Join("&",
        [
            $"NeighborhoodId={_neighborhoodId}",
            $"AreaDetail={Uri.EscapeDataString(area)}",
            $"StartTime={outage.StartTime:yyyy-MM-ddTHH:mm}",
            $"EndTime={newEnd:yyyy-MM-ddTHH:mm}",
            $"Reason={Uri.EscapeDataString("Süre uzatıldı")}",
            "SendNotification=true",
            $"__RequestVerificationToken={Uri.EscapeDataString(token)}"
        ]);

        var response = await admin.PostAsync($"/PowerOutagesAdmin/Edit/{outage.Id}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded"));
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var announcements = await InDbAsync(db => db.Announcements.IgnoreQueryFilters()
            .AsNoTracking().Where(a => a.Title.Contains(Marker) || a.Id == announcementId).ToListAsync());

        announcements.Should().ContainSingle(a => a.Id == announcementId);

        var after = await InDbAsync(db => db.Notifications.CountAsync(n => n.RelatedId == announcementId));
        after.Should().Be(before, "güncelleme ikinci kez bildirim ÜRETMEMELİ");

        var refreshed = announcements.Single(a => a.Id == announcementId);
        refreshed.VisibleUntil.Should().BeCloseTo(
            DateTime.SpecifyKind(newEnd, DateTimeKind.Utc), TimeSpan.FromMinutes(1),
            "saat değiştiyse duyurunun görünürlük süresi de tazelenmeli");
    }

    /// <summary>
    /// 🔴 Görünmez sözleşme #24'ün uzantısı. 11.15c'de duyurularda birebir bu yaşandı:
    /// silinen duyurunun 9 bildirimi ayakta kaldı ve kullanıcı dokununca boş sayfaya düştü.
    /// </summary>
    [Fact]
    public async Task Delete_RemovesTheAnnouncementAndItsNotifications()
    {
        var admin = await _factory.SuperAdminAsync();
        var area = $"{Marker}-SILME";

        await CreateAsync(admin, area, _neighborhoodId, notify: true);
        var outage = await OutageAsync(area);
        var announcementId = outage.AnnouncementId!.Value;

        (await InDbAsync(db => db.Notifications.CountAsync(n => n.RelatedId == announcementId)))
            .Should().BeGreaterThan(0);

        var token = await admin.GetAntiforgeryTokenAsync("/PowerOutagesAdmin/Index");
        await admin.PostAsync($"/PowerOutagesAdmin/Delete/{outage.Id}",
            new StringContent($"__RequestVerificationToken={Uri.EscapeDataString(token)}",
                System.Text.Encoding.UTF8, "application/x-www-form-urlencoded"));

        (await InDbAsync(db => db.Notifications.CountAsync(n => n.RelatedId == announcementId)))
            .Should().Be(0, "kaynağı silinen bildirim ayakta kalırsa kullanıcı boş sayfaya düşer");

        var announcement = await InDbAsync(db => db.Announcements.IgnoreQueryFilters()
            .AsNoTracking().FirstOrDefaultAsync(a => a.Id == announcementId));
        announcement!.DeletedAt.Should().NotBeNull("duyuru da gitmeli — vatandaş listesinde ölü kayıt kalmasın");
    }

    // ────────────────────────────────────────────────────────────────────────
    // #42 — FK yoksa bildirim YOK (ve sessizce "gönderildi" denmez)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Notification_IsRefused_WhenTheOutageHasNoDictionaryNeighbourhood()
    {
        var admin = await _factory.SuperAdminAsync();
        var area = $"{Marker}-HEDEFSIZ";

        var response = await CreateAsync(admin, area, neighborhoodId: null, notify: true,
            freeText: "Sözlükte Olmayan Bölge");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var outage = await OutageAsync(area);
        outage.AnnouncementId.Should().BeNull("hedeflenecek mahalle yokken duyuru üretilemez");

        // 🔑 Panel bunu SÖYLEMELİ. Sessiz kalsaydı yönetici gitmeyen bir bildirimi
        // gitmiş sanırdı — bu fazın savaştığı sessiz hasar sınıfı.
        var index = await (await admin.GetAsync("/PowerOutagesAdmin/Index")).ReadDecodedBodyAsync();
        index.Should().Contain("Bildirim gönderilemedi");
    }

    /// <summary>Eşleşmemiş kayıtlar panelde bir şeritle sayılıyor ve süzülebiliyor.</summary>
    [Fact]
    public async Task UnmatchedOutages_AreCountedAndFilterable()
    {
        var admin = await _factory.SuperAdminAsync();
        var area = $"{Marker}-ESLESMEMIS";

        await CreateAsync(admin, area, neighborhoodId: null, notify: false, freeText: $"{Marker} Bilinmeyen");

        var index = await (await admin.GetAsync("/PowerOutagesAdmin/Index")).ReadDecodedBodyAsync();
        index.Should().Contain("mahallesi sözlükle eşleşmiyor");

        var filtered = await (await admin.GetAsync(
            $"/PowerOutagesAdmin/Index?neighborhoodId={PowerOutagesAdminController.UnmatchedNeighborhoodKey}"))
            .ReadDecodedBodyAsync();

        filtered.Should().Contain($"{Marker} Bilinmeyen");
    }

    /// <summary>Sözlük süzgeci gerçekten süzüyor mu (yalnız seçilen mahallenin kayıtları).</summary>
    [Fact]
    public async Task NeighbourhoodIdFilter_ShowsOnlyThatNeighbourhood()
    {
        var admin = await _factory.SuperAdminAsync();

        await CreateAsync(admin, $"{Marker}-SUZGEC-BURADA", _neighborhoodId, notify: false);
        await CreateAsync(admin, $"{Marker}-SUZGEC-ORADA", _otherNeighborhoodId, notify: false);

        var html = await (await admin.GetAsync($"/PowerOutagesAdmin/Index?neighborhoodId={_neighborhoodId}"))
            .ReadDecodedBodyAsync();

        html.Should().Contain($"{Marker}-SUZGEC-BURADA");
        html.Should().NotContain($"{Marker}-SUZGEC-ORADA");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Önizleme ↔ gerçek gönderim paritesi (#38'in bu ekrandaki karşılığı)
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 Panelin "tahmini alıcı" sayısı ile gerçekten yazılan satır sayısı <b>aynı</b>
    /// olmak zorunda. Ayrı bir sayım yazılsaydı panel "342 kişiye gidecek" der, gönderim
    /// 280 satır yazardı ve fark hiçbir yerde görünmezdi (görünmez sözleşme #38).
    /// </summary>
    [Fact]
    public async Task EstimatePreview_MatchesTheActualRecipientCount()
    {
        var admin = await _factory.SuperAdminAsync();
        var area = $"{Marker}-ONIZLEME";

        var json = await (await admin.GetAsync(
            $"/PowerOutagesAdmin/EstimateRecipients?neighborhoodIds={_neighborhoodId}"))
            .Content.ReadAsStringAsync();

        var estimated = System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("count").GetInt32();

        await CreateAsync(admin, area, _neighborhoodId, notify: true);
        var outage = await OutageAsync(area);

        var actual = await InDbAsync(db => db.Notifications.CountAsync(n => n.RelatedId == outage.AnnouncementId));

        actual.Should().Be(estimated, "önizleme ile gönderim AYNI süzgeçten geçmeli");
    }

    /// <summary>
    /// ⚠️ Mahalle seçilmemişken önizleme <b>0</b> demeli, "herkes" değil.
    /// Boş listeyi "tüm şehir" saymak bir form hatasını binlerce kişiye giden bildirime çevirirdi.
    /// </summary>
    [Fact]
    public async Task EstimatePreview_WithoutANeighbourhood_IsZeroNotEveryone()
    {
        var admin = await _factory.SuperAdminAsync();

        var json = await (await admin.GetAsync("/PowerOutagesAdmin/EstimateRecipients"))
            .Content.ReadAsStringAsync();

        System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("count").GetInt32()
            .Should().Be(0);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Görünmez sözleşme #1 KIRILMADI
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Mahalle alanları eklendi ama <c>GET /v1/power-outages</c> hâlâ <b>düz dizi</b>.
    /// Sayfalansaydı mobil süren/planlı ayrımını tam listeden yapamaz ve acil şeritte
    /// kesinti kaybolurdu — hata da görünmezdi.
    /// </summary>
    [Fact]
    public async Task PublicQuery_StillReturnsAFlatList_WithTheNewFields()
    {
        var admin = await _factory.SuperAdminAsync();
        var area = $"{Marker}-KONTRAT";
        await CreateAsync(admin, area, _neighborhoodId, notify: false);
        var outage = await OutageAsync(area);

        List<PowerOutageDto> items = null!;
        await _factory.WithScopeAsync(async sp =>
        {
            var sender = sp.GetRequiredService<MediatR.ISender>();
            var result = await sender.Send(new GetPowerOutagesQuery());

            // 🔴 Dönen tip PagedResult DEĞİL, düz liste. Derleyici de burada tutuyor:
            // sorgu sayfalamaya çevrilirse bu satır **build'i kırar** (görünmez sözleşme #1).
            items = result.Data!;
        });

        var dto = items.Single(o => o.Id == outage.Id);
        dto.Neighborhood.Should().Be(_neighborhoodName);
        dto.NeighborhoodId.Should().Be(_neighborhoodId);
        dto.AreaDetail.Should().Be(area);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Geri doldurma (idempotent, yalnız FK'sı boş satırlara dokunur)
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Serbest metin bir kayıt sözlüğe bağlanır ve adı <b>kanonik hâline</b> yazılır.
    /// ⚠️ İkinci koşuda hiçbir şey değişmez — adım her açılışta koşuyor.
    /// </summary>
    [Fact]
    public async Task Backfill_LinksLegacyFreeTextRows_AndIsIdempotent()
    {
        var area = $"{Marker}-BACKFILL";

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.PowerOutages.Add(new PowerOutage
            {
                // Yıllardır kayıtlarda böyle duruyor: sözlükte "X", kesintide "X Mahallesi".
                Neighborhood = $"{_neighborhoodName} Mahallesi",
                AreaDetail = area,
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(4)
            });
            await db.SaveChangesAsync();
        });

        PowerOutageBackfillReport first = null!;
        await _factory.WithScopeAsync(async sp =>
            first = await PowerOutageNeighborhoodBackfill.RunAsync(sp.GetRequiredService<AppDbContext>()));

        first.Matched.Should().BeGreaterThan(0);

        var linked = await OutageAsync(area);
        linked.NeighborhoodId.Should().Be(_neighborhoodId);
        linked.Neighborhood.Should().Be(_neighborhoodName,
            "geri doldurma adı da kanonikleştirir — mobil eşleşmesi buna dayanıyor");

        // İkinci koşu aynı satırı bir daha TARAMAZ (FK artık dolu) ve hiçbir şeyi değiştirmez.
        await _factory.WithScopeAsync(async sp =>
            await PowerOutageNeighborhoodBackfill.RunAsync(sp.GetRequiredService<AppDbContext>()));

        var afterSecondRun = await OutageAsync(area);
        afterSecondRun.NeighborhoodId.Should().Be(_neighborhoodId);
        afterSecondRun.Neighborhood.Should().Be(_neighborhoodName);
    }

    /// <summary>
    /// 🔴 Yöneticinin panelden bilerek kurduğu bağ, açılışta bir tahminle <b>ezilemez</b>.
    /// Adım yalnız <c>neighborhood_id IS NULL</c> satırlara dokunmalı.
    /// </summary>
    [Fact]
    public async Task Backfill_NeverOverwritesAnExistingReference()
    {
        var admin = await _factory.SuperAdminAsync();
        var area = $"{Marker}-KORUMA";

        await CreateAsync(admin, area, _neighborhoodId, notify: false);

        // Adı bilerek başka bir mahalleye benzet: eşleştirme yeniden koşsaydı FK değişirdi.
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var row = await db.PowerOutages.FirstAsync(o => o.AreaDetail == area);
            var other = await db.Neighborhoods.FirstAsync(n => n.Id == _otherNeighborhoodId);
            row.Neighborhood = other.Name;
            await db.SaveChangesAsync();
        });

        await _factory.WithScopeAsync(async sp =>
            await PowerOutageNeighborhoodBackfill.RunAsync(sp.GetRequiredService<AppDbContext>()));

        (await OutageAsync(area)).NeighborhoodId.Should().Be(_neighborhoodId,
            "FK zaten doluysa geri doldurma o satıra dokunmamalı");
    }
}
