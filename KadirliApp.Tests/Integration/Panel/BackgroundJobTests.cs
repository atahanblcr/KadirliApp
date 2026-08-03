using FluentAssertions;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Jobs;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.15b — **arka plan işleri: kimsenin bakmadığı yerde çalışan kod.**
///
/// Hangfire işleri dakikada/saatte bir, kullanıcı olmadan koşar. Bir iş yanlış davranırsa
/// (mükerrer bildirim üretirse, sınır tarihindeki kaydı atlarsa, iki kez koşunca satırları
/// bozarsa) kimse hata almaz — yalnız veri sessizce yanlışlaşır. Kullanıcı bunu
/// "aynı duyuru iki kez geldi" ya da "vefat ilanı hâlâ listede" diye fark eder.
///
/// <c>ExpireAdsJob</c> ve <c>SendPushNotificationsJob</c> 10.x'te kaplanmıştı;
/// burada kalan ikisi kilitleniyor.
/// </summary>
[Collection(PanelCollection.Name)]
public class BackgroundJobTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;
    private readonly string _marker = "Job-" + Guid.NewGuid().ToString("N")[..8];

    public BackgroundJobTests(WebPanelApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var announcementIds = await db.Announcements.IgnoreQueryFilters()
                .Where(a => a.Title.Contains(_marker)).Select(a => a.Id).ToListAsync();

            await db.Notifications.Where(n => announcementIds.Contains(n.RelatedId!.Value)).ExecuteDeleteAsync();
            await db.Announcements.IgnoreQueryFilters().Where(a => a.Title.Contains(_marker)).ExecuteDeleteAsync();
            await db.DeathNotices.IgnoreQueryFilters().Where(d => d.DeceasedName.Contains(_marker)).ExecuteDeleteAsync();
        });
    }

    /// <summary>
    /// ⚠️ Hangfire işleri yalnız <c>KadirliApp.Api</c>'de kaydediliyor (panel iş çalıştırmaz),
    /// bu yüzden <c>GetRequiredService</c> ile çözülemezler. <c>ActivatorUtilities</c>
    /// bağımlılıkları scope'tan alıp nesneyi elle kurar — testin ilgilendiği şey zaten
    /// Hangfire kaydı değil, işin **gövdesi**.
    /// </summary>
    private async Task RunAsync<TJob>(Func<TJob, Task> run) where TJob : notnull
        => await _factory.WithScopeAsync(async sp =>
            await run(ActivatorUtilities.CreateInstance<TJob>(sp)));

    private async Task<T> QueryAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        T result = default!;
        await _factory.WithScopeAsync(async sp => result = await query(sp.GetRequiredService<AppDbContext>()));
        return result;
    }

    private async Task<Guid> InsertAsync<T>(T entity) where T : Domain.Common.BaseEntity
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.Set<T>().Add(entity);
            await db.SaveChangesAsync();
        });
        return entity.Id;
    }

    // ─────────────────────────── ArchiveDeathsJob ───────────────────────────

    private DeathNotice DeathNotice(string suffix, string status, DateTime? autoArchiveAt) => new()
    {
        DeceasedName = $"{_marker} {suffix}",
        FuneralDate = DateTime.UtcNow.Date.AddDays(-3),
        Status = status,
        AutoArchiveAt = autoArchiveAt
    };

    /// <summary>
    /// İş yalnız **süresi geçmiş ve onaylı** ilanı arşivlemeli. Sınır davranışı önemli:
    /// bir gün sonra arşivlenecek ilan bugün arşivlenirse aile duyurusu erken kaybolur.
    /// </summary>
    [Fact]
    public async Task ArchiveDeathsJob_ArchivesOnlyExpiredApprovedNotices()
    {
        var now = DateTime.UtcNow;
        var expired = await InsertAsync(DeathNotice("Süresi Geçmiş", "approved", now.AddHours(-1)));
        var future = await InsertAsync(DeathNotice("Gelecek", "approved", now.AddDays(1)));
        var pending = await InsertAsync(DeathNotice("Onaysız", "pending", now.AddHours(-1)));
        var noDate = await InsertAsync(DeathNotice("Tarihsiz", "approved", null));

        await RunAsync<ArchiveDeathsJob>(job => job.RunAsync());

        (await StatusOf(expired)).Should().Be("archived", "süresi geçmiş onaylı ilan arşivlenmeli");
        (await StatusOf(future)).Should().Be("approved", "süresi gelmemiş ilan erken arşivlenmemeli");
        (await StatusOf(pending)).Should().Be("pending",
            "onaysız ilan arşivlenmemeli — hiç yayınlanmamış içerik 'arşiv' durumuna geçemez");
        (await StatusOf(noDate)).Should().Be("approved",
            "auto_archive_at boşsa ilan süresizdir, iş ona dokunmamalı");
    }

    /// <summary>
    /// 🔑 İş dakikalık/günlük koşuyor — **ikinci koşu birinci koşunun sonucunu bozmamalı.**
    /// Bozsaydı her koşuda satırlar yeniden yazılır, <c>updated_at</c> kayar ve
    /// "ne zaman arşivlendi?" sorusu cevapsız kalırdı.
    /// </summary>
    [Fact]
    public async Task ArchiveDeathsJob_IsIdempotent()
    {
        var id = await InsertAsync(DeathNotice("Mükerrer", "approved", DateTime.UtcNow.AddHours(-1)));

        await RunAsync<ArchiveDeathsJob>(job => job.RunAsync());
        var afterFirst = await QueryAsync(db => db.DeathNotices.IgnoreQueryFilters()
            .Where(d => d.Id == id).Select(d => new { d.Status, d.UpdatedAt }).FirstAsync());

        await RunAsync<ArchiveDeathsJob>(job => job.RunAsync());
        var afterSecond = await QueryAsync(db => db.DeathNotices.IgnoreQueryFilters()
            .Where(d => d.Id == id).Select(d => new { d.Status, d.UpdatedAt }).FirstAsync());

        afterSecond.Status.Should().Be("archived");
        afterSecond.UpdatedAt.Should().Be(afterFirst.UpdatedAt,
            "ikinci koşu zaten arşivlenmiş satıra dokunmamalı");
    }

    /// <summary>Arşivlenmiş ilan silinmiş değildir — kayıt kalır, yalnız yayından çıkar.</summary>
    [Fact]
    public async Task ArchiveDeathsJob_DoesNotDeleteRows()
    {
        var id = await InsertAsync(DeathNotice("Kalıcı", "approved", DateTime.UtcNow.AddHours(-1)));

        await RunAsync<ArchiveDeathsJob>(job => job.RunAsync());

        var row = await QueryAsync(db => db.DeathNotices.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == id));
        row.Should().NotBeNull("arşivleme silme değildir");
        row!.DeletedAt.Should().BeNull();
    }

    private async Task<string> StatusOf(Guid id) => await QueryAsync(db =>
        db.DeathNotices.IgnoreQueryFilters().Where(d => d.Id == id).Select(d => d.Status).FirstAsync());

    // ───────────────────── PublishScheduledAnnouncementsJob ─────────────────────

    /// <summary>⚠️ Duyuru seed'lenmiş bir duyuru türüne FK ile bağlı — tür oradan alınır.</summary>
    private async Task<Guid> InsertAnnouncementAsync(string suffix, string status, DateTime? scheduledFor, bool push = true)
    {
        Guid typeId = Guid.Empty;
        await _factory.WithScopeAsync(async sp =>
            typeId = await sp.GetRequiredService<AppDbContext>().AnnouncementTypes.Select(t => t.Id).FirstAsync());

        return await InsertAsync(new Announcement
        {
            Title = $"{_marker} {suffix}",
            Body = "Test duyurusu gövdesi",
            TypeId = typeId,
            Status = status,
            ScheduledFor = scheduledFor,
            SendPushNotification = push,
            TargetType = "all"
        });
    }

    [Fact]
    public async Task PublishJob_PublishesOnlyAnnouncementsWhoseTimeHasCome()
    {
        var now = DateTime.UtcNow;
        var due = await InsertAnnouncementAsync("Vakti Gelmiş", "scheduled", now.AddMinutes(-1));
        var later = await InsertAnnouncementAsync("Sonra", "scheduled", now.AddHours(2));
        var draft = await InsertAnnouncementAsync("Taslak", "draft", now.AddMinutes(-1));

        await RunAsync<PublishScheduledAnnouncementsJob>(job => job.RunAsync());

        (await AnnouncementStatusOf(due)).Should().Be("active", "zamanı gelen duyuru yayınlanmalı");
        (await AnnouncementStatusOf(later)).Should().Be("scheduled", "zamanı gelmeyen duyuru beklemeli");
        (await AnnouncementStatusOf(draft)).Should().Be("draft",
            "taslak durumundaki duyuru zamanlanmış sayılmamalı — yönetici henüz bitirmemiştir");
    }

    /// <summary>Yayınlanan duyuruya gönderim zamanı damgası düşmeli.</summary>
    [Fact]
    public async Task PublishJob_StampsTheSentTime()
    {
        var id = await InsertAnnouncementAsync("Damga", "scheduled", DateTime.UtcNow.AddMinutes(-1));

        await RunAsync<PublishScheduledAnnouncementsJob>(job => job.RunAsync());

        var sentAt = await QueryAsync(db => db.Announcements.IgnoreQueryFilters()
            .Where(a => a.Id == id).Select(a => a.SentAt).FirstAsync());
        sentAt.Should().NotBeNull();
    }

    /// <summary>
    /// 🔑 **Mükerrer bildirim, kullanıcının doğrudan gördüğü hatadır.** İş dakikada bir
    /// koşuyor; ikinci koşu aynı duyuru için ikinci bir bildirim satırı üretirse herkes
    /// aynı duyuruyu iki kez alır. İdempotency <c>related_type + related_id</c>
    /// işaretiyle sağlanıyor.
    /// </summary>
    [Fact]
    public async Task PublishJob_RunningTwice_DoesNotDuplicateNotifications()
    {
        var id = await InsertAnnouncementAsync("Tekrar", "scheduled", DateTime.UtcNow.AddMinutes(-1));

        await RunAsync<PublishScheduledAnnouncementsJob>(job => job.RunAsync());
        var afterFirst = await NotificationCountFor(id);

        await RunAsync<PublishScheduledAnnouncementsJob>(job => job.RunAsync());
        var afterSecond = await NotificationCountFor(id);

        afterSecond.Should().Be(afterFirst,
            "ikinci koşu mükerrer bildirim üretmemeli — kullanıcı aynı duyuruyu iki kez alır");
    }

    /// <summary>
    /// ⚠️ 10.10 kararı: <c>SendPushNotification=false</c> ise bildirim **satırı da**
    /// yazılmaz. Yazılsaydı 10.11'deki FCM işi "gönderilmemiş her satırı gönder"
    /// varsayımıyla onu da push'lar ve yönetici bilinçli kararı ezilirdi.
    /// </summary>
    [Fact]
    public async Task PublishJob_WithPushDisabled_WritesNoNotificationRow()
    {
        var id = await InsertAnnouncementAsync("Sessiz", "scheduled", DateTime.UtcNow.AddMinutes(-1), push: false);

        await RunAsync<PublishScheduledAnnouncementsJob>(job => job.RunAsync());

        (await AnnouncementStatusOf(id)).Should().Be("active", "duyuru yine de yayınlanmalı");
        (await NotificationCountFor(id)).Should().Be(0,
            "push kapalıyken bildirim satırı yazılmamalı");
    }

    /// <summary>Yapacak iş yoksa sessizce çıkmalı — boş koşu hata üretmemeli.</summary>
    [Fact]
    public async Task PublishJob_WithNothingDue_CompletesQuietly()
    {
        var run = async () => await RunAsync<PublishScheduledAnnouncementsJob>(job => job.RunAsync());
        await run.Should().NotThrowAsync();
    }

    private async Task<string> AnnouncementStatusOf(Guid id) => await QueryAsync(db =>
        db.Announcements.IgnoreQueryFilters().Where(a => a.Id == id).Select(a => a.Status).FirstAsync());

    private async Task<int> NotificationCountFor(Guid announcementId) => await QueryAsync(db =>
        db.Notifications.CountAsync(n => n.RelatedType == "announcement" && n.RelatedId == announcementId));
}
