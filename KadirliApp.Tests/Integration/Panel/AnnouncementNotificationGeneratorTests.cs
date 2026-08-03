using System.Text.Json;
using FluentAssertions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.15b — **duyuru bildirimi üretimi: handler seviyesinde ilk kez.**
///
/// Bu sınıf projenin en "geniş etkili" iş kuralını taşıyor: bir duyuru yayınlandığında
/// **kime** bildirim yazılacağına karar veriyor. Yanlış davranması iki yönde de ağır:
/// <list type="bullet">
///   <item>fazla hedefleme → bildirim kapatmış kullanıcıya push gider (mağaza şikâyeti,
///         kullanıcının açık tercihinin ezilmesi);</item>
///   <item>eksik hedefleme → mahalle duyurusu o mahalledeki kimseye ulaşmaz ve
///         **hiçbir hata görünmez**.</item>
/// </list>
/// Bugüne dek yalnız uç üzerinden dolaylı deneniyordu.
/// </summary>
[Collection(PanelCollection.Name)]
public class AnnouncementNotificationGeneratorTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;
    private readonly string _marker = "Notif-" + Guid.NewGuid().ToString("N")[..8];

    public AnnouncementNotificationGeneratorTests(WebPanelApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var userIds = await db.Users.IgnoreQueryFilters()
                .Where(u => u.Username!.Contains(_marker)).Select(u => u.Id).ToListAsync();

            await db.Notifications.Where(n => userIds.Contains(n.UserId)).ExecuteDeleteAsync();
            await db.Set<UserNeighborhood>().Where(un => userIds.Contains(un.UserId)).ExecuteDeleteAsync();
            await db.Users.IgnoreQueryFilters().Where(u => userIds.Contains(u.Id)).ExecuteDeleteAsync();
            await db.Announcements.IgnoreQueryFilters().Where(a => a.Title.Contains(_marker)).ExecuteDeleteAsync();
            await db.Neighborhoods.IgnoreQueryFilters().Where(n => n.Name.Contains(_marker)).ExecuteDeleteAsync();
        });
    }

    private async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        T result = default!;
        await _factory.WithScopeAsync(async sp => result = await action(sp.GetRequiredService<AppDbContext>()));
        return result;
    }

    private async Task<Guid> NewNeighborhoodAsync(string suffix) => await WithDbAsync(async db =>
    {
        var n = new Neighborhood { Name = $"{_marker} {suffix}", Slug = $"{_marker}-{suffix}".ToLowerInvariant(), IsActive = true };
        db.Neighborhoods.Add(n);
        await db.SaveChangesAsync();
        return n.Id;
    });

    private async Task<Guid> NewUserAsync(
        string suffix, bool announcementsEnabled = true, bool isActive = true, bool isBanned = false,
        Guid? primaryNeighborhoodId = null) => await WithDbAsync(async db =>
    {
        var user = new User
        {
            Phone = "+90599" + Random.Shared.Next(1000000, 9999999),
            Username = $"{_marker}-{suffix}",
            Role = UserRole.User,
            IsActive = isActive,
            IsBanned = isBanned,
            PrimaryNeighborhoodId = primaryNeighborhoodId
        };
        user.NotificationPreferences.Announcements = announcementsEnabled;
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    });

    private async Task<Announcement> NewAnnouncementAsync(
        string suffix, string targetType = "all", IEnumerable<Guid>? neighborhoods = null, bool push = true)
        => await WithDbAsync(async db =>
        {
            var typeId = await db.AnnouncementTypes.Select(t => t.Id).FirstAsync();
            var announcement = new Announcement
            {
                Title = $"{_marker} {suffix}",
                Body = "Duyuru gövdesi",
                TypeId = typeId,
                Status = "active",
                TargetType = targetType,
                TargetNeighborhoods = neighborhoods is null ? null : JsonSerializer.Serialize(neighborhoods),
                SendPushNotification = push
            };
            db.Announcements.Add(announcement);
            await db.SaveChangesAsync();
            return announcement;
        });

    private async Task<int> GenerateAsync(Announcement announcement)
    {
        var count = 0;
        await _factory.WithScopeAsync(async sp =>
            count = await sp.GetRequiredService<IAnnouncementNotificationGenerator>()
                .GenerateForAnnouncementAsync(announcement));
        return count;
    }

    private async Task<bool> HasNotificationAsync(Guid userId, Guid announcementId) => await WithDbAsync(db =>
        db.Notifications.AnyAsync(n => n.UserId == userId && n.RelatedId == announcementId));

    // ─────────────────────────── Tercihe saygı ───────────────────────────

    /// <summary>
    /// 🔑 Kullanıcı "duyuru bildirimi istemiyorum" dediyse **satır bile yazılmamalı**.
    /// Yazılsaydı 10.11'deki FCM işi "gönderilmemiş her satırı gönder" varsayımıyla onu
    /// da push'lardı — yani ayarlar ekranındaki anahtar hiçbir işe yaramazdı.
    /// </summary>
    [Fact]
    public async Task UsersWhoDisabledAnnouncements_GetNoNotification()
    {
        var wants = await NewUserAsync("istiyor");
        var doesNot = await NewUserAsync("istemiyor", announcementsEnabled: false);
        var announcement = await NewAnnouncementAsync("Tercih");

        await GenerateAsync(announcement);

        (await HasNotificationAsync(wants, announcement.Id)).Should().BeTrue();
        (await HasNotificationAsync(doesNot, announcement.Id)).Should().BeFalse(
            "bildirim tercihini kapatan kullanıcıya satır yazılmamalı");
    }

    /// <summary>Pasif ve engellenmiş kullanıcılar hedeflenmemeli.</summary>
    [Fact]
    public async Task InactiveAndBannedUsers_AreExcluded()
    {
        var active = await NewUserAsync("aktif");
        var inactive = await NewUserAsync("pasif", isActive: false);
        var banned = await NewUserAsync("engelli", isBanned: true);
        var announcement = await NewAnnouncementAsync("Durum");

        await GenerateAsync(announcement);

        (await HasNotificationAsync(active, announcement.Id)).Should().BeTrue();
        (await HasNotificationAsync(inactive, announcement.Id)).Should().BeFalse();
        (await HasNotificationAsync(banned, announcement.Id)).Should().BeFalse(
            "engellenmiş kullanıcıya bildirim gitmemeli");
    }

    [Fact]
    public async Task PushDisabledAnnouncement_GeneratesNothing()
    {
        var user = await NewUserAsync("sessiz");
        var announcement = await NewAnnouncementAsync("Sessiz", push: false);

        (await GenerateAsync(announcement)).Should().Be(0);
        (await HasNotificationAsync(user, announcement.Id)).Should().BeFalse();
    }

    // ─────────────────────────── Mahalle hedeflemesi ───────────────────────────

    /// <summary>
    /// Mahalle hedeflemesi **iki** yoldan eşleşir: kullanıcının birincil mahallesi ya da
    /// ek mahalle listesi. İkincisi unutulursa "birden fazla mahalle takip et" özelliği
    /// sessizce ölür — kullanıcı takip ettiği mahallenin duyurusunu hiç almaz.
    /// </summary>
    [Fact]
    public async Task NeighborhoodTargeting_MatchesPrimaryAndSecondaryNeighborhoods()
    {
        var targetId = await NewNeighborhoodAsync("Hedef");
        var otherId = await NewNeighborhoodAsync("Diger");

        var primaryMatch = await NewUserAsync("birincil", primaryNeighborhoodId: targetId);
        var elsewhere = await NewUserAsync("baska", primaryNeighborhoodId: otherId);
        var secondaryMatch = await NewUserAsync("ikincil", primaryNeighborhoodId: otherId);

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.Set<UserNeighborhood>().Add(new UserNeighborhood { UserId = secondaryMatch, NeighborhoodId = targetId });
            await db.SaveChangesAsync();
        });

        var announcement = await NewAnnouncementAsync("Mahalle", "neighborhood", new[] { targetId });

        await GenerateAsync(announcement);

        (await HasNotificationAsync(primaryMatch, announcement.Id)).Should().BeTrue(
            "birincil mahallesi hedefte olan kullanıcı bildirilmeli");
        (await HasNotificationAsync(secondaryMatch, announcement.Id)).Should().BeTrue(
            "ek mahalle listesinden eşleşen kullanıcı da bildirilmeli");
        (await HasNotificationAsync(elsewhere, announcement.Id)).Should().BeFalse(
            "hedef dışı mahalledeki kullanıcı bildirilmemeli");
    }

    /// <summary>
    /// ⚠️ <c>targetType="neighborhood"</c> ama mahalle listesi boşsa filtre uygulanmaz ve
    /// duyuru **herkese** gider. Panel bu durumu üretmiyor ama kural yazılı olmalı:
    /// aksi hâlde biri "boş liste = kimseye gitmez" varsayar.
    /// </summary>
    [Fact]
    public async Task NeighborhoodTargeting_WithoutAList_FallsBackToEveryone()
    {
        var user = await NewUserAsync("herkes");
        var announcement = await NewAnnouncementAsync("Bos", "neighborhood");

        await GenerateAsync(announcement);

        (await HasNotificationAsync(user, announcement.Id)).Should().BeTrue();
    }

    // ─────────────────────────── İdempotency ve içerik ───────────────────────────

    /// <summary>
    /// 🔑 Aynı duyuru için ikinci üretim **hiçbir satır yazmamalı**. Yayınlama işi
    /// dakikada bir koşuyor ve duyuru güncelleme de üretimi tetikleyebiliyor.
    /// </summary>
    [Fact]
    public async Task GeneratingTwice_ProducesNoDuplicates()
    {
        await NewUserAsync("mukerrer");
        var announcement = await NewAnnouncementAsync("Mukerrer");

        var first = await GenerateAsync(announcement);
        first.Should().BeGreaterThan(0);

        var second = await GenerateAsync(announcement);
        second.Should().Be(0, "aynı duyuru için ikinci üretim atlanmalı");

        var total = await WithDbAsync(db => db.Notifications.CountAsync(n => n.RelatedId == announcement.Id));
        total.Should().Be(first, "toplam bildirim sayısı artmamalı");
    }

    /// <summary>
    /// Bildirim gövdesi 500 karakterde kırpılır (liste için özet yeterli). Kırpma
    /// kalkarsa uzun duyurular bildirim listesini kullanılamaz hâle getirir.
    /// </summary>
    [Fact]
    public async Task LongBody_IsTruncatedForTheNotificationList()
    {
        await NewUserAsync("uzun");
        var announcement = await NewAnnouncementAsync("Uzun");

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var stored = await db.Announcements.FirstAsync(a => a.Id == announcement.Id);
            stored.Body = new string('a', 900);
            announcement.Body = stored.Body;
            await db.SaveChangesAsync();
        });

        await GenerateAsync(announcement);

        var body = await WithDbAsync(db => db.Notifications
            .Where(n => n.RelatedId == announcement.Id).Select(n => n.Body).FirstAsync());

        body.Length.Should().BeLessThan(900, "uzun gövde kırpılmalı");
        body.Should().EndWith("…", "kırpıldığı kullanıcıya belli olmalı");
    }

    /// <summary>
    /// ⚠️ <c>relatedType</c>/<c>relatedId</c> mobil derin bağlantının tek girdisi
    /// (görünmez sözleşme §16/§18). Değişirse bildirime dokunan kullanıcı hiçbir yere
    /// gitmez ve hata da görmez.
    /// </summary>
    [Fact]
    public async Task GeneratedNotification_CarriesTheDeepLinkContract()
    {
        await NewUserAsync("baglanti");
        var announcement = await NewAnnouncementAsync("Baglanti");

        await GenerateAsync(announcement);

        var notification = await WithDbAsync(db =>
            db.Notifications.FirstAsync(n => n.RelatedId == announcement.Id));

        notification.RelatedType.Should().Be("announcement", "mobil rota eşlemesi bu değere bakıyor");
        notification.Type.Should().Be("announcement");
        notification.RelatedId.Should().Be(announcement.Id);
        notification.Title.Should().Be(announcement.Title);
        notification.IsRead.Should().BeFalse("yeni bildirim okunmamış doğmalı — rozet sayacı buna bakıyor");
    }
}
