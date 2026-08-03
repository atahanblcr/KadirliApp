using FluentAssertions;
using KadirliApp.Application.Features.Announcements.Commands.DeleteAnnouncement;
using KadirliApp.Application.Features.Ads.Commands.ApproveAd;
using KadirliApp.Application.Features.Dashboard.Queries;
using KadirliApp.Application.Features.Notifications.Queries.GetMyNotifications;
using KadirliApp.Application.Features.Notifications.Services;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using KadirliApp.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.15c — canlı panel gezisinde bulunan **iş kuralı hatalarının** kilidi.
///
/// Bu üç hatanın ortak yanı: hiçbiri hata vermiyordu. Panel "başarılı" diyor, log temiz,
/// istisna yok — ama yönetici ile vatandaş farklı gerçeklik görüyordu. 11.13'ün
/// "yapılandırma bayrağıyla kapatılmış kod yolu hiç test edilmiyor" dersinin akrabası:
/// **iki tarafın aynı şeyi gördüğünü kimse doğrulamıyordu.**
///
/// Panel koleksiyonundadır çünkü gerçek Postgres ister; test ettiği kurallar
/// <c>Application</c> katmanındadır ve API tarafını da aynen etkiler.
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelBusinessRuleTests
{
    private readonly WebPanelApplicationFactory _factory;

    public PanelBusinessRuleTests(WebPanelApplicationFactory factory) => _factory = factory;

    // ── A5: süresi geçmiş ilanın onayı ─────────────────────────────────────────

    /// <summary>
    /// 🔴 Canlı çelişki: süresi geçmiş bir ilan panelden onaylanınca panel
    /// "İlan başarıyla onaylandı." diyordu, ama <c>ExpiresAt</c> geçmişte kaldığı için
    /// <c>GET /v1/ads</c> onu HİÇ döndürmüyordu ve saatlik <c>ExpireAdsJob</c> durumu
    /// sessizce yeniden <c>expired</c> yapıyordu.
    ///
    /// Karar: yayın penceresi ilanın gönderildiği an değil, **görünür olduğu an** başlar.
    /// </summary>
    [Fact]
    public async Task ApprovingExpiredAd_GivesItFreshPublishWindow()
    {
        var adId = await SeedAdAsync("Suresi dolmus onay testi", "expired", DateTime.UtcNow.AddDays(-3));

        await SendAsync(new ApproveAdCommand(adId, await AdminIdAsync()));

        var ad = await LoadAdAsync(adId);
        ad.Status.Should().Be("approved");
        ad.ExpiresAt.Should().BeAfter(DateTime.UtcNow,
            "onaylanan ilan mobilde GERÇEKTEN görünmeli — aksi hâlde panel 'onaylandı' derken " +
            "vatandaş hiçbir şey görmez ve ExpireAdsJob bir saat içinde durumu geri alır");

        await DeleteAdAsync(adId);
    }

    /// <summary>
    /// Aynı sessiz hatanın <c>expired</c> olmayan biçimi: onay kuyruğunda 30 günden fazla
    /// bekleyen bir <c>pending</c> ilan, onaylandığı anda süresi dolmuş oluyordu.
    /// Bu yüzden düzeltmenin koşulu duruma değil TARİHE bakıyor.
    /// </summary>
    [Fact]
    public async Task ApprovingLongPendingAd_AlsoGetsFreshWindow()
    {
        var adId = await SeedAdAsync("Uzun suredir bekleyen ilan", "pending", DateTime.UtcNow.AddDays(-1));

        await SendAsync(new ApproveAdCommand(adId, await AdminIdAsync()));

        (await LoadAdAsync(adId)).ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        await DeleteAdAsync(adId);
    }

    /// <summary>
    /// Süresi GEÇMEMİŞ ilanın süresi onayla uzatılmamalı — onay bir uzatma aracı değil.
    /// (Bu iddia olmasaydı "her onayda +30 gün" gibi bir düzeltme de testi geçerdi.)
    /// </summary>
    [Fact]
    public async Task ApprovingValidAd_DoesNotChangeItsExpiry()
    {
        var expires = DateTime.UtcNow.AddDays(10);
        var adId = await SeedAdAsync("Suresi devam eden ilan", "pending", expires);

        await SendAsync(new ApproveAdCommand(adId, await AdminIdAsync()));

        (await LoadAdAsync(adId)).ExpiresAt.Should().BeCloseTo(expires, TimeSpan.FromSeconds(5));

        await DeleteAdAsync(adId);
    }

    // ── A6: Dashboard sayaçları vatandaşın gördüğüyle aynı olmalı ───────────────

    /// <summary>
    /// 🐛 Canlı: panel "Aktif İlanlar: 1" derken <c>GET /v1/ads</c> 0 döndürdü.
    /// <c>GetDashboardStatsQueryHandler</c> yalnız <c>Status == "approved"</c> sayıyor,
    /// <c>ExpiresAt</c>'i yok sayıyordu.
    /// </summary>
    [Fact]
    public async Task DashboardActiveAds_ExcludesExpiredOnes()
    {
        var before = (await FreshStatsAsync()).ActiveAds;

        // "approved" ama süresi GEÇMİŞ: mobilde görünmez, panelde de sayılmamalı.
        var staleId = await SeedAdAsync("Onayli ama suresi gecmis", "approved", DateTime.UtcNow.AddDays(-1));
        (await FreshStatsAsync()).ActiveAds.Should().Be(before,
            "süresi geçmiş ilan 'aktif' sayılmamalı — vatandaş onu görmüyor");

        // "approved" ve süresi devam eden: sayılmalı (aksi hâlde test, sayaç tümüyle
        // bozuk olsa bile yeşil kalırdı).
        var liveId = await SeedAdAsync("Onayli ve yayinda", "approved", DateTime.UtcNow.AddDays(10));
        (await FreshStatsAsync()).ActiveAds.Should().Be(before + 1);

        await DeleteAdAsync(staleId);
        await DeleteAdAsync(liveId);
    }

    /// <summary>Duyuru sayacı da yayınlanmamış (taslak/zamanlanmış) kayıtları saymamalı.</summary>
    [Fact]
    public async Task DashboardAnnouncements_CountsOnlyPublishedOnes()
    {
        var before = (await FreshStatsAsync()).TotalAnnouncements;

        var draftId = await SeedAnnouncementAsync("Taslak duyuru", status: "draft");
        (await FreshStatsAsync()).TotalAnnouncements.Should().Be(before,
            "taslak duyuru yayında değil — 'yayındaki duyuru' sayacına girmemeli");

        var activeId = await SeedAnnouncementAsync("Yayindaki duyuru", status: "active");
        (await FreshStatsAsync()).TotalAnnouncements.Should().Be(before + 1);

        await DeleteAnnouncementRowAsync(draftId);
        await DeleteAnnouncementRowAsync(activeId);
    }

    // ── A4: ölü bildirim ───────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 **Canlı kanıtlı hata:** panelden push'lu duyuru oluşturuldu → 9 bildirim satırı
    /// üretildi → duyuru panelden silindi → **9 satır aynen durdu**. Kullanıcı bildirimi
    /// görüyor, dokunuyor ve boş sayfaya düşüyordu.
    ///
    /// Birinci katman: silme komutu bildirimleri de temizler.
    /// </summary>
    [Fact]
    public async Task DeletingAnnouncement_AlsoRemovesItsNotifications()
    {
        var userId = await AdminIdAsync();
        var announcementId = await SeedAnnouncementAsync("Silinecek duyuru", status: "active");
        await SeedNotificationAsync(userId, announcementId);

        await CountNotificationsAsync(announcementId).ContinueWith(t => t.Result.Should().Be(1));

        await SendAsync(new DeleteAnnouncementCommand { Id = announcementId });

        (await CountNotificationsAsync(announcementId)).Should().Be(0,
            "duyurunun bildirimleri onunla birlikte gitmeli — yoksa mobilde ölü bağlantı kalır");
    }

    /// <summary>
    /// İkinci katman (emniyet ağı): silme DIŞINDAKİ görünmezleşme yolları.
    /// Duyuru "draft"a çekilirse public uç onu NOT_FOUND döndürür; bildirimin de
    /// listede kalmaması gerekir. Bu katman olmasaydı yalnız silme kapatılmış olurdu.
    /// </summary>
    [Fact]
    public async Task NotificationList_HidesNotificationsWhoseTargetIsNoLongerPublished()
    {
        var userId = await AdminIdAsync();
        var announcementId = await SeedAnnouncementAsync("Yayindan kaldirilacak duyuru", status: "active");
        await SeedNotificationAsync(userId, announcementId);

        // Önce GÖRÜNDÜĞÜNÜ gösteriyoruz — bu adım olmadan aşağıdaki iddia,
        // süzgeç her şeyi elese bile yeşil kalırdı.
        var visible = await SendAsync(new GetMyNotificationsQuery(userId, Limit: 100));
        visible.Items.Should().Contain(n => n.RelatedId == announcementId);

        await UpdateAnnouncementStatusAsync(announcementId, "draft");

        var afterUnpublish = await SendAsync(new GetMyNotificationsQuery(userId, Limit: 100));
        afterUnpublish.Items.Should().NotContain(n => n.RelatedId == announcementId,
            "hedefi artık yayında olmayan bildirim kullanıcıyı boş sayfaya götürür");

        await DeleteAnnouncementRowAsync(announcementId);
    }

    /// <summary>
    /// ⚠️ Rozet ile liste ayrışmamalı: <c>unreadCount</c> süzgeci atlarsa kullanıcı
    /// "3 okunmamış" rozeti görür ama listede 1 satır bulur — sessiz tutarsızlık.
    /// </summary>
    [Fact]
    public async Task UnreadCount_UsesSameLivenessFilterAsTheList()
    {
        var userId = await AdminIdAsync();
        await ClearNotificationsAsync(userId);

        var deadTarget = await SeedAnnouncementAsync("Olu hedef", status: "draft");
        var liveTarget = await SeedAnnouncementAsync("Canli hedef", status: "active");
        await SeedNotificationAsync(userId, deadTarget);
        await SeedNotificationAsync(userId, liveTarget);

        var result = await SendAsync(new GetMyNotificationsQuery(userId, Limit: 100));

        result.Items.Should().HaveCount(1);
        result.UnreadCount.Should().Be(1,
            "okunmamış sayacı listeyle aynı süzgeci kullanmalı");

        await DeleteAnnouncementRowAsync(deadTarget);
        await DeleteAnnouncementRowAsync(liveTarget);
        await ClearNotificationsAsync(userId);
    }

    // ── Yardımcılar ────────────────────────────────────────────────────────────

    private async Task<T> SendAsync<T>(IRequest<T> request)
    {
        T result = default!;
        await _factory.WithScopeAsync(async sp =>
            result = await sp.GetRequiredService<ISender>().Send(request));
        return result;
    }

    /// <summary>
    /// ⚠️ Dashboard sorgusu <c>ICacheableQuery</c> (60 sn TTL). Testte önbellekten okumak,
    /// düzeltmeyi değil bayat değeri denetlemek olurdu → handler'ı DOĞRUDAN çağırıyoruz.
    /// </summary>
    private async Task<DashboardStatsDto> FreshStatsAsync()
    {
        DashboardStatsDto result = null!;
        await _factory.WithScopeAsync(async sp =>
        {
            var handler = new GetDashboardStatsQueryHandler(
                sp.GetRequiredService<KadirliApp.Application.Common.Interfaces.IUnitOfWork>());
            result = await handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);
        });
        return result;
    }

    private async Task<Guid> AdminIdAsync()
    {
        Guid id = Guid.Empty;
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            id = (await db.Users.FirstAsync(u => u.Role == UserRole.SuperAdmin)).Id;
        });
        return id;
    }

    private async Task<Guid> SeedAdAsync(string title, string status, DateTime expiresAt)
    {
        Guid id = Guid.Empty;
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var ad = new Ad
            {
                UserId = (await db.Users.FirstAsync()).Id,
                CategoryId = (await db.AdCategories.FirstAsync()).Id,
                Title = title,
                Description = "11.15c testi.",
                Price = 100m,
                ContactPhone = "+905550000000",
                Status = status,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };
            db.Ads.Add(ad);
            await db.SaveChangesAsync();
            id = ad.Id;
        });
        return id;
    }

    private async Task<Ad> LoadAdAsync(Guid id)
    {
        Ad ad = null!;
        await _factory.WithScopeAsync(async sp =>
            ad = await sp.GetRequiredService<AppDbContext>().Ads.AsNoTracking().FirstAsync(a => a.Id == id));
        return ad;
    }

    private Task DeleteAdAsync(Guid id) => _factory.WithScopeAsync(async sp =>
        await sp.GetRequiredService<AppDbContext>().Ads.Where(a => a.Id == id).ExecuteDeleteAsync());

    private async Task<Guid> SeedAnnouncementAsync(string title, string status)
    {
        Guid id = Guid.Empty;
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var announcement = new Announcement
            {
                Title = title,
                Body = "11.15c testi.",
                // ⚠️ 11.15b tuzağı: announcements.type_id seed'den alınmalı (FK).
                TypeId = (await db.AnnouncementTypes.FirstAsync()).Id,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };
            db.Announcements.Add(announcement);
            await db.SaveChangesAsync();
            id = announcement.Id;
        });
        return id;
    }

    private Task UpdateAnnouncementStatusAsync(Guid id, string status) => _factory.WithScopeAsync(async sp =>
        await sp.GetRequiredService<AppDbContext>().Announcements
            .Where(a => a.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, status)));

    private Task DeleteAnnouncementRowAsync(Guid id) => _factory.WithScopeAsync(async sp =>
    {
        var db = sp.GetRequiredService<AppDbContext>();
        await db.Notifications.Where(n => n.RelatedId == id).ExecuteDeleteAsync();
        await db.Announcements.Where(a => a.Id == id).ExecuteDeleteAsync();
    });

    private Task SeedNotificationAsync(Guid userId, Guid announcementId) => _factory.WithScopeAsync(async sp =>
    {
        var db = sp.GetRequiredService<AppDbContext>();
        db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = "Test bildirimi",
            Body = "11.15c testi.",
            Type = AnnouncementNotificationGenerator.RelatedTypeAnnouncement,
            RelatedId = announcementId,
            RelatedType = AnnouncementNotificationGenerator.RelatedTypeAnnouncement,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    });

    private async Task<int> CountNotificationsAsync(Guid announcementId)
    {
        int count = 0;
        await _factory.WithScopeAsync(async sp =>
            count = await sp.GetRequiredService<AppDbContext>().Notifications
                .CountAsync(n => n.RelatedId == announcementId));
        return count;
    }

    private Task ClearNotificationsAsync(Guid userId) => _factory.WithScopeAsync(async sp =>
        await sp.GetRequiredService<AppDbContext>().Notifications
            .Where(n => n.UserId == userId).ExecuteDeleteAsync());
}
