extern alias WebPanel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.News;
using KadirliApp.Application.Features.News.Commands;
using KadirliApp.Application.Features.News.Dtos;
using KadirliApp.Application.Features.News.Queries;
using KadirliApp.Application.Features.News.Services;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using KadirliApp.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PanelDisplay = WebPanel::KadirliApp.Web.Common.PanelDisplay;
using PanelPermissionFilter = WebPanel::KadirliApp.Web.Authorization.PanelPermissionFilter;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 12.15 — <b>haber bildirimi.</b>
///
/// Bu testlerin iddiası "buton çalışıyor" değil; 12.15'in kapattığı sessiz hasar sınıfları:
/// <list type="number">
///   <item><b>Aynı haber ikinci kez gönderilemez</b> — komut reddeder <b>ve</b> veritabanında
///         kısmi unique indeks var (§7 madde 37: gönderim terminaldir).</item>
///   <item><b>Görünmeyen haber bildirilemez</b> — üç görünmezlik ekseninin <b>üçü de</b>
///         (arşiv · <c>gone</c> · <b>dışlanmış kategori</b>). Sonuncusu planın listesinde
///         yoktu; gönderilseydi vatandaş boş sayfaya düşerdi (§7 madde 24).</item>
///   <item><b>Görünmez olan haberin bildirimleri düşer</b> — 11.15c'nin 9 ölü bildirimi.</item>
///   <item><b>İzin eylemi <c>approve</c></b> — "SendNotification" öneki elle eklendi, yoksa
///         yalnız düzenleme yetkisi olan moderatör <b>tüm şehre push atardı</b>
///         (§7 madde 19'un dördüncü tekrarı).</item>
///   <item><b>Önizleme = gerçek</b> — metin de alıcı sayısı da gönderimin kendi
///         kaynağından (§7 madde 38).</item>
/// </list>
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelNewsNotificationTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;

    private const string Marker = "CLAUDE-NEWSPUSH";
    private const int WpIdFloor = 992000;
    private const int WpIdCeiling = 993000;
    private const string ModeratorUsername = "newspush-moderator";
    private const string ModeratorPassword = "Moderator123!";

    private Guid _moderatorId;
    private Guid _articleId;          // yayında, görünür kategoride
    private Guid _excludedArticleId;  // yayında ama kategorisi DIŞLANMIŞ
    private Guid _recipientId;        // aktif + bildirimleri açık
    private Guid _optedOutId;         // bildirimleri KAPALI

    public PanelNewsNotificationTests(WebPanelApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        var moderator = await _factory.EnsureModeratorAsync(ModeratorUsername, ModeratorPassword);
        _moderatorId = moderator.Id;
        await _factory.ClearMustChangePasswordAsync(ModeratorUsername);

        await CleanAsync();

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();

            var visible = new NewsCategory { WpId = 992001 };
            visible.ApplySourceSnapshot($"{Marker}-GORUNUR", $"{Marker.ToLowerInvariant()}-gorunur", 10);

            var excluded = new NewsCategory { WpId = 992002 };
            excluded.ApplySourceSnapshot($"{Marker}-DISLANMIS", $"{Marker.ToLowerInvariant()}-dislanmis", 10);
            excluded.SetVisibility(isExcluded: true, showInFilterStrip: false, displayOrder: 0);

            db.Set<NewsCategory>().AddRange(visible, excluded);
            await db.SaveChangesAsync();

            var article = NewArticle(992101, $"{Marker} gönderilebilir haber");
            article.ReplaceCategories(new[] { visible });

            var hidden = NewArticle(992102, $"{Marker} dışlanmış kategorideki haber");
            hidden.ReplaceCategories(new[] { excluded });

            db.Set<NewsArticle>().AddRange(article, hidden);
            await db.SaveChangesAsync();

            _articleId = article.Id;
            _excludedArticleId = hidden.Id;

            _recipientId = await EnsureUserAsync(db, "+905550000951", news: true);
            _optedOutId = await EnsureUserAsync(db, "+905550000952", news: false);
        });
    }

    public Task DisposeAsync() => CleanAsync();

    // ────────────────────────────────────────────────────────────────────────
    // 1) Mutlu yol: gönderim zinciri uçtan uca
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Sending_OpensACampaign_WritesNotifications_AndMarksTheArticle()
    {
        var client = await _factory.SuperAdminAsync();

        var response = await SendAsync(client, _articleId);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);

        var article = await LoadAsync(_articleId);
        article.NotificationSent.Should().BeTrue();
        article.NotificationCampaignId.Should().NotBeNull();
        article.NotificationRecipientCount.Should().BeGreaterThan(0);

        var campaign = await InDbAsync(db =>
            db.PushCampaigns.AsNoTracking().FirstAsync(c => c.Id == article.NotificationCampaignId!.Value));

        // 🔑 Kaynak `news`, `manual` DEĞİL — ayrım `SourceId`'nin varlığı ve o kimlik
        // "ikinci kez gönderilemez" kuralının veritabanındaki çıpası.
        campaign.Source.Should().Be(PushCampaignSources.News);
        campaign.SourceId.Should().Be(_articleId);
        campaign.TargetType.Should().Be(PushTargetTypes.All);

        var notifications = await InDbAsync(db => db.Notifications.AsNoTracking()
            .Where(n => n.RelatedType == NewsNotifications.RelatedType && n.RelatedId == _articleId)
            .ToListAsync());

        notifications.Should().NotBeEmpty();

        // 🔴 §7 madde 18: mobil ROTAYI bu iki alandan üretiyor (`app_notification.dart`,
        // 12.14'te yazıldı). Değer değişirse deep-link SESSİZCE ölür.
        notifications.Should().OnlyContain(n => n.RelatedType == "news");
        notifications.Should().OnlyContain(n => n.RelatedId == _articleId);

        // Bildirim tercihi gönderimde de geçerli (§7 madde 38) — ve 12.15b'den beri
        // haberin KENDİ ekseninden: "Duyurular"ı kapatan kullanıcı haberi almaya devam eder.
        notifications.Should().Contain(n => n.UserId == _recipientId);
        notifications.Should().NotContain(n => n.UserId == _optedOutId);

        // Gövde kendi kendine yeterli olmak zorunda — eski sürümler dokununca hiçbir yere
        // gitmiyor (kabul edilen sınır), ama bilgiyi almış olmalılar.
        notifications.First().Body.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Sending_LeavesAnAuditTrailWithATurkishLabel()
    {
        var client = await _factory.SuperAdminAsync();
        await SendAsync(client, _articleId);

        var action = await InDbAsync(db => db.AuditLogs.AsNoTracking()
            .Where(a => a.AffectedId == _articleId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => a.Action)
            .FirstAsync());

        action.Should().Be("send-notification");

        // Değişmez Kural #6: denetim izi ekranı ham İngilizce basmaz.
        PanelDisplay.AuditAction(action).Label.Should().NotContain("Bilinmeyen");
        PanelDisplay.PushSource(PushCampaignSources.News).Label.Should().NotContain("Bilinmeyen");
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2) Gönderim TERMİNAL: ikinci kez gönderilemez
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ASecondSend_IsRejected_AndDoesNotOpenASecondCampaign()
    {
        var client = await _factory.SuperAdminAsync();
        await SendAsync(client, _articleId);

        var before = await CampaignCountAsync();

        var second = await SendAsync(client, _articleId);
        second.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);

        (await CampaignCountAsync()).Should().Be(before, "şehre ikinci bir push atılamaz");
    }

    /// <summary>
    /// 🔴 Kural yalnız komutta değil <b>veritabanında</b> da duruyor.
    /// </summary>
    /// <remarks>
    /// Buton ve komut bir YARIŞI yakalayamaz: gönderim ile işaretleme aynı
    /// <c>SaveChanges</c>'te değil (kampanya kimliği ancak dispatcher yazdıktan sonra
    /// doğuyor). İki eşzamanlı istek ikisi de "gönderilmemiş" görüp şehre iki push atabilirdi.
    /// Bu test kısmi unique indeksin gerçekten orada olduğunu, kaydı <b>komuttan bağımsız</b>
    /// yazmayı deneyerek kanıtlıyor.
    /// </remarks>
    [Fact]
    public async Task TheDatabase_RefusesASecondNewsCampaignForTheSameArticle()
    {
        var client = await _factory.SuperAdminAsync();
        await SendAsync(client, _articleId);

        var write = async () => await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.PushCampaigns.Add(new PushCampaign
            {
                Title = $"{Marker} ikinci kampanya",
                Body = "gövde",
                TargetType = PushTargetTypes.All,
                Source = PushCampaignSources.News,
                SourceId = _articleId
            });
            await db.SaveChangesAsync();
        });

        await write.Should().ThrowAsync<DbUpdateException>();
    }

    /// <summary>
    /// ⚠️ İndeksin kapsamı <b>dar</b>: başka kaynaklarda ikinci gönderim MEŞRU.
    /// </summary>
    /// <remarks>
    /// Genel bir unique indeks, "yeniden gönderim yeni kampanya açar" kuralını (§7 madde 37)
    /// sessizce kapatırdı — duyurunun/kesintinin ikinci kampanyası patlamaya başlardı ve
    /// sebebi bu dosyada değil, hiç ilgisiz bir ekranda görünürdü.
    /// </remarks>
    [Fact]
    public async Task TheUniqueIndex_DoesNotAffectOtherSources()
    {
        var sourceId = Guid.NewGuid();

        var write = async () => await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            for (var i = 0; i < 2; i++)
            {
                db.PushCampaigns.Add(new PushCampaign
                {
                    Title = $"{Marker} duyuru kampanyası {i}",
                    Body = "gövde",
                    TargetType = PushTargetTypes.All,
                    Source = PushCampaignSources.Announcement,
                    SourceId = sourceId
                });
            }
            await db.SaveChangesAsync();
        });

        await write.Should().NotThrowAsync();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3) Görünmeyen haber bildirilemez — ÜÇ eksenin üçü de
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AnArchivedArticle_CannotBeNotified_AndThePanelSaysWhy()
    {
        await ArchiveAsync(_articleId);

        var preview = await PreviewAsync(_articleId);
        preview!.CanSend.Should().BeFalse();
        preview.Reason.Should().Contain("Yayına al");

        var client = await _factory.SuperAdminAsync();
        await SendAsync(client, _articleId);

        (await LoadAsync(_articleId)).NotificationSent.Should().BeFalse();
    }

    /// <summary>
    /// 🔴 <b>Planın listesinde olmayan dördüncü koşul</b> — ve gerçek Postgres'te ölçülüyor.
    /// </summary>
    [Fact]
    public async Task AnArticleHiddenByCategoryExclusion_CannotBeNotified()
    {
        // Önce kanıt: kayıt gerçekten görünmüyor (tanımın tek sahibi NewsVisibility).
        var visibleIds = await InDbAsync(db =>
            NewsVisibility.Published(db.Set<NewsArticle>().AsNoTracking()).Select(x => x.Id).ToListAsync());

        visibleIds.Should().NotContain(_excludedArticleId);
        visibleIds.Should().Contain(_articleId);

        var preview = await PreviewAsync(_excludedArticleId);
        preview!.CanSend.Should().BeFalse();
        preview.Reason.Should().Contain("kategori");

        var client = await _factory.SuperAdminAsync();
        await SendAsync(client, _excludedArticleId);

        // Gönderilseydi vatandaş bildirimi alır, dokunur ve BOŞ SAYFAYA düşerdi.
        (await LoadAsync(_excludedArticleId)).NotificationSent.Should().BeFalse();
        (await CampaignCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AGoneArticle_CannotBeNotified()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var article = await db.Set<NewsArticle>().FirstAsync(x => x.Id == _articleId);
            article.MarkSourceGone(DateTime.UtcNow);
            await db.SaveChangesAsync();
        });

        var preview = await PreviewAsync(_articleId);
        preview!.CanSend.Should().BeFalse();
        preview.Reason.Should().Contain("kaynakta bulunmuyor");
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4) Görünmez olan haberin bildirimleri DÜŞER (§7 madde 24)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ArchivingANotifiedArticle_DeletesItsNotifications_ButKeepsTheCampaign()
    {
        var client = await _factory.SuperAdminAsync();
        await SendAsync(client, _articleId);

        (await NotificationCountAsync(_articleId)).Should().BeGreaterThan(0);
        var campaignId = (await LoadAsync(_articleId)).NotificationCampaignId;

        await ArchiveAsync(_articleId);

        // 🔴 Bildirimler FİZİKSEL olarak düşer: kalsalardı vatandaş dokunup boş sayfaya
        // düşerdi (11.15c'de duyurularda 9 ölü bildirimle birebir yaşandı).
        (await NotificationCountAsync(_articleId)).Should().Be(0);

        // ⚠️ Kampanya satırı DURUR: "ne yollandı" tarihçesi silinmez (§7 madde 37/39).
        (await InDbAsync(db => db.PushCampaigns.AsNoTracking().AnyAsync(c => c.Id == campaignId)))
            .Should().BeTrue();

        // ⚠️ "Gönderildi" işareti de durur — geri alma ikinci bir push'a kapı açmamalı.
        (await LoadAsync(_articleId)).NotificationSent.Should().BeTrue();
    }

    [Fact]
    public async Task UnarchivingDoesNotAllowASecondNotification()
    {
        var client = await _factory.SuperAdminAsync();
        await SendAsync(client, _articleId);
        await ArchiveAsync(_articleId);

        await _factory.WithScopeAsync(async sp =>
            await sp.GetRequiredService<ISender>().Send(new UnarchiveNewsArticleCommand { Id = _articleId }));

        var preview = await PreviewAsync(_articleId);
        preview!.CanSend.Should().BeFalse();
        preview.Reason.Should().Contain("zaten gönderildi");
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5) İzin: "SendNotification" → approve (§7 madde 19'un dördüncü tekrarı)
    // ────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("SendNotification", "approve")]
    [InlineData("Archive", "approve")]
    [InlineData("Edit", "update")]
    public void SendNotification_MapsToTheApprovePermission(string actionName, string expected)
        => PanelPermissionFilter.ActionFor(actionName, "POST").Should().Be(expected);

    /// <summary>
    /// 🔴 Önek elle eklenmeseydi bu test <b>yeşil kalmazdı</b>: aksiyon POST olduğu için
    /// sessizce <c>update</c>'e düşer ve yalnız <i>başlık düzeltme</i> yetkisi olan bir
    /// moderatör tüm şehre push atabilirdi.
    /// </summary>
    [Fact]
    public async Task AModeratorWithOnlyUpdatePermission_CannotSendANotification()
    {
        await SetPermissionsAsync(new AdminPermission
        {
            UserId = _moderatorId, Module = "news",
            CanRead = true, CanUpdate = true, CanApprove = false
        });

        var client = _factory.CreatePanelClient();
        await client.LoginAsync(ModeratorUsername, ModeratorPassword);

        await SendAsync(client, _articleId);

        (await LoadAsync(_articleId)).NotificationSent.Should().BeFalse();
        (await CampaignCountAsync()).Should().Be(0);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6) Önizleme = gerçek (§7 madde 38'in dersi)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ThePreview_ShowsExactlyWhatWillBeSent()
    {
        var preview = await PreviewAsync(_articleId);
        preview!.CanSend.Should().BeTrue();
        preview.Reason.Should().BeNull();

        var client = await _factory.SuperAdminAsync();
        await SendAsync(client, _articleId);

        var campaign = await InDbAsync(db => db.PushCampaigns.AsNoTracking()
            .FirstAsync(c => c.Source == PushCampaignSources.News && c.SourceId == _articleId));

        // Metin: önizlemedeki başlık/gövde ile gidenin AYNISI olmalı — yönetici okuduğu
        // metni onaylamış olsun diye.
        campaign.Title.Should().Be(preview.Title);
        campaign.Body.Should().Be(preview.Body);

        // Sayı: önizleme gönderimin KENDİ sorgusundan geliyor (12.2b'nin dersi).
        campaign.RecipientCount.Should().Be(preview.EstimatedRecipients);
    }

    [Fact]
    public async Task ThePreviewUsesTheAdminOverride_NotTheSourceTitle()
    {
        await _factory.WithScopeAsync(async sp =>
            await sp.GetRequiredService<ISender>().Send(new UpdateNewsOverridesCommand
            {
                Id = _articleId,
                Title = $"{Marker} yönetici başlığı",
                AdminId = null
            }));

        var preview = await PreviewAsync(_articleId);

        preview!.Title.Should().Be($"{Marker} yönetici başlığı");
    }

    [Fact]
    public async Task TheDetailsPage_ShowsTheButton_AndSwitchesToALinkAfterSending()
    {
        var client = await _factory.SuperAdminAsync();

        var before = await (await client.GetAsync($"/NewsAdmin/Details/{_articleId}")).ReadDecodedBodyAsync();
        before.Should().Contain("Bildirim gönder");

        await SendAsync(client, _articleId);

        var after = await (await client.GetAsync($"/NewsAdmin/Details/{_articleId}")).ReadDecodedBodyAsync();

        // 🔴 Terminal alan geri alınmayı TEKLİF ETMEZ (§7 madde 37): buton yerine bağlantı.
        after.Should().NotContain("Bildirim gönder");
        after.Should().Contain("Gönderim kaydına git");
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7) "Bunu duyurdum mu?" — liste süzgeci
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TheNotifiedFilter_SeparatesSentFromUnsent()
    {
        var client = await _factory.SuperAdminAsync();
        await SendAsync(client, _articleId);

        var unsent = await ListAsync(notified: false);
        unsent.Should().Contain(_excludedArticleId);
        unsent.Should().NotContain(_articleId);

        var sent = await ListAsync(notified: true);
        sent.Should().Contain(_articleId);
        sent.Should().NotContain(_excludedArticleId);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Yardımcılar
    // ────────────────────────────────────────────────────────────────────────

    private Task<HttpResponseMessage> SendAsync(HttpClient client, Guid id) =>
        client.PostFormAsync("/NewsAdmin/SendNotification",
            new Dictionary<string, string> { ["id"] = id.ToString() }, $"/NewsAdmin/Details/{id}");

    private async Task<NewsNotificationPreviewDto?> PreviewAsync(Guid id)
    {
        NewsNotificationPreviewDto? dto = null;
        await _factory.WithScopeAsync(async sp =>
            dto = await sp.GetRequiredService<ISender>().Send(new GetNewsNotificationPreviewQuery(id)));
        return dto;
    }

    private async Task<List<Guid>> ListAsync(bool notified)
    {
        List<Guid> ids = null!;
        await _factory.WithScopeAsync(async sp =>
        {
            var result = await sp.GetRequiredService<ISender>().Send(new GetNewsAdminQuery(new QueryNewsAdminDto
            {
                Notified = notified,
                Limit = 200
            }));
            ids = result.Items.Select(i => i.Id).ToList();
        });
        return ids;
    }

    private Task ArchiveAsync(Guid id) => _factory.WithScopeAsync(async sp =>
        await sp.GetRequiredService<ISender>().Send(new ArchiveNewsArticleCommand
        {
            Id = id,
            Reason = "test gerekçesi"
        }));

    private Task<int> NotificationCountAsync(Guid articleId) => InDbAsync(db => db.Notifications
        .AsNoTracking()
        .CountAsync(n => n.RelatedType == NewsNotifications.RelatedType && n.RelatedId == articleId));

    private Task<int> CampaignCountAsync() => InDbAsync(db => db.PushCampaigns
        .AsNoTracking()
        .CountAsync(c => c.Source == PushCampaignSources.News));

    private async Task<NewsArticle> LoadAsync(Guid id)
    {
        NewsArticle article = null!;
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            article = await db.Set<NewsArticle>().AsNoTracking().FirstAsync(x => x.Id == id);
        });
        return article;
    }

    private async Task<T> InDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        T result = default!;
        await _factory.WithScopeAsync(async sp =>
            result = await action(sp.GetRequiredService<AppDbContext>()));
        return result;
    }

    /// <summary>
    /// 🐛 <b>12.15b'de imza DEĞİŞTİ ve değişmesi doğruydu.</b> Önce <c>announcements</c>
    /// alıyordu; 12.15b haber için ayrı bir eksen açınca "duyuruyu kapatmış" kullanıcı
    /// haber almaya <b>devam etti</b> ve bu test kırıldı. Kırılma, iki eksenin gerçekten
    /// ayrıldığının kanıtı: eskiden tek anahtar ikisini birden kapatıyordu.
    /// </summary>
    private static async Task<Guid> EnsureUserAsync(AppDbContext db, string phone, bool news)
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
        user.FcmToken = $"token-{phone}";
        user.NotificationPreferences = new NotificationPreferences { Announcements = true, News = news };

        await db.SaveChangesAsync();
        return user.Id;
    }

    private static NewsArticle NewArticle(int wpId, string title)
    {
        var article = new NewsArticle { WpId = wpId };
        article.ApplySourceSnapshot(new NewsArticleSnapshot(
            Title: title,
            Excerpt: "Birinci cümle burada bitiyor. İkinci cümle bildirime girmemeli.",
            ContentHtml: "<p>gövde</p>",
            PlainText: "gövde",
            Url: $"https://example.test/{wpId}",
            PublishedAtUtc: DateTime.UtcNow.AddHours(-1),
            ModifiedAtUtc: DateTime.UtcNow.AddHours(-1),
            Checksum: $"checksum-{wpId}",
            ImageUrl: null,
            ImageFileId: null,
            ImageWidth: null,
            ImageHeight: null,
            ReadingMinutes: 1), DateTime.UtcNow);
        return article;
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

    /// <summary>
    /// ⚠️ Sıra önemli: bildirimler kampanyaya FK ile bağlı ve haberler kampanyaya
    /// <c>SetNull</c> ile — kampanyayı önce silmek yetim satır bırakırdı.
    /// </summary>
    private async Task CleanAsync()
    {
        await SetPermissionsAsync();
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();

            await db.Notifications.Where(n => n.Title.StartsWith(Marker)).ExecuteDeleteAsync();
            await db.Set<NewsArticle>().Where(x => x.WpId >= WpIdFloor && x.WpId < WpIdCeiling).ExecuteDeleteAsync();
            await db.PushCampaigns.Where(c => c.Title.StartsWith(Marker)).ExecuteDeleteAsync();
            await db.Set<NewsCategory>().Where(x => x.WpId >= WpIdFloor && x.WpId < WpIdCeiling).ExecuteDeleteAsync();

            // 🧹 T1 (Faz 0 denetimi): bu sınıf kendi vatandaş kullanıcılarını da SİLER.
            // Panel listeleri sayfalı; biriken test kullanıcıları seed'deki süper admini ilk
            // sayfadan düşürüp **ilgisiz** testleri kırıyordu (12.15b'de birebir yaşandı).
            // ⚠️ Temizlik YALNIZ kendi telefonlarını kapsar — geniş bir silme başka bir
            // testin kurulumunu götürür (12.15b'nin ikinci dersi).
            await db.Users.IgnoreQueryFilters()
                .Where(u => u.Phone == "+905550000951" || u.Phone == "+905550000952")
                .ExecuteDeleteAsync();
        });
    }
}
