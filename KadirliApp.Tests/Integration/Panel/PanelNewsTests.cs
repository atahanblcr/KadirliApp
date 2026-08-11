extern alias WebPanel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.News;
using KadirliApp.Application.Features.News.Commands;
using KadirliApp.Application.Features.News.Queries;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PanelDisplay = WebPanel::KadirliApp.Web.Common.PanelDisplay;
using PanelMenu = WebPanel::KadirliApp.Web.Common.PanelMenu;
using PanelPermissionFilter = WebPanel::KadirliApp.Web.Authorization.PanelPermissionFilter;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 12.13 — <b>Haberler paneli.</b>
///
/// Bu testlerin iddiası "ekran açılıyor" değil; 12.13'ün kapattığı (ya da açmamaya söz
/// verdiği) sessiz hasar sınıfları:
/// <list type="number">
///   <item><b>Görünürlüğü yazan ikinci yol yok</b> — Düzenle formunda anahtar bulunmuyor
///         (12.10'un dersi) ve <c>Unarchive</c> <c>approve</c> iznine düşüyor
///         (§7 madde 19; <c>Archive</c> öneki onu <b>yakalamıyor</b>).</item>
///   <item><b>Senkron paneli ezmiyor</b> — override koşudan sonra yerinde ve "kaynağı
///         güncellenmiş" sayacı 0'dan 1'e çıkıyor.</item>
///   <item><b>Dışlama önizlemesi GERÇEK sorgudan</b> geliyor (§7 madde 38'in dersi:
///         önizleme "342" der, gerçek 280 yazar ve fark hiçbir yerde görünmez).</item>
///   <item><b>Eşzamanlı koşu açılamıyor</b> — kilit veritabanında (§7 madde 32'nin dersi;
///         Redis bu iş için bilinçli olarak fail-open).</item>
///   <item><b>Arama indeksi gerçekten kullanılıyor</b> — sorgu planıyla ölçülüyor
///         (12.12 sonrası denetim, bulgu 4).</item>
/// </list>
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelNewsTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;

    private const string Marker = "CLAUDE-NEWS";

    /// <summary>Testin kendi kayıtlarını tanıdığı <b>değişmeyen</b> aralık (bkz. <c>CleanAsync</c>).</summary>
    private const int WpIdFloor = 990000;
    private const int WpIdCeiling = 991000;
    private const string ModeratorUsername = "news-moderator";
    private const string ModeratorPassword = "Moderator123!";

    private Guid _moderatorId;
    private Guid _articleId;      // yayında, "CLAUDE-NEWS-A" kategorisinde
    private Guid _otherArticleId; // yayında, iki kategoride birden
    private Guid _categoryAId;
    private Guid _categoryBId;

    public PanelNewsTests(WebPanelApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        var moderator = await _factory.EnsureModeratorAsync(ModeratorUsername, ModeratorPassword);
        _moderatorId = moderator.Id;
        await _factory.ClearMustChangePasswordAsync(ModeratorUsername);

        await CleanAsync();

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();

            var categoryA = new NewsCategory { WpId = 990001 };
            categoryA.ApplySourceSnapshot($"{Marker}-A", $"{Marker.ToLowerInvariant()}-a", 100);
            var categoryB = new NewsCategory { WpId = 990002 };
            categoryB.ApplySourceSnapshot($"{Marker}-B", $"{Marker.ToLowerInvariant()}-b", 200);

            db.Set<NewsCategory>().AddRange(categoryA, categoryB);
            await db.SaveChangesAsync();

            _categoryAId = categoryA.Id;
            _categoryBId = categoryB.Id;

            var article = NewArticle(990101, $"{Marker} birinci haber");
            article.ReplaceCategories(new[] { categoryA });

            var other = NewArticle(990102, $"{Marker} ikinci haber");
            // İkinci haber İKİ kategoride birden: dışlama semantiğinin sınavı bu kayıt.
            other.ReplaceCategories(new[] { categoryA, categoryB });

            db.Set<NewsArticle>().AddRange(article, other);
            await db.SaveChangesAsync();

            _articleId = article.Id;
            _otherArticleId = other.Id;
        });
    }

    public Task DisposeAsync() => CleanAsync();

    // ────────────────────────────────────────────────────────────────────────
    // 1) Yetki deseni: Haberler moderatöre açık, senkron panosu DEĞİL
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Moderator_WithNewsPermission_SeesTheList_ButNotTheSyncScreen()
    {
        await SetPermissionsAsync(new AdminPermission
        {
            UserId = _moderatorId, Module = "news",
            CanRead = true, CanUpdate = true, CanApprove = true
        });

        var client = await ModeratorClientAsync();

        var list = await client.GetAsync("/NewsAdmin");
        list.StatusCode.Should().Be(HttpStatusCode.OK);

        // 🔑 Senkron ekranı yalnız-admin: tüm içerik kümesini etkileyen bir işi tetikliyor.
        var sync = await client.GetAsync("/NewsSyncAdmin");
        sync.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect, HttpStatusCode.Found);
    }

    [Fact]
    public async Task SyncScreen_IsOutsideThePermissionMatrix()
    {
        // §7 madde 20 + 12.2'nin yapısal kuralı: AdminOnlyControllers'taki her controller'ın
        // menü satırında Module NULL olmak zorunda — yoksa izin matrisinde KARŞILIĞI OLMAYAN
        // bir yetki belirir (11.15b'nin en büyük bulgusu).
        PanelMenu.AdminOnlyControllers.Should().Contain("NewsSyncAdmin");
        PanelMenu.Items.Single(i => i.Controller == "NewsSyncAdmin").Module.Should().BeNull();

        // Haberler ekranı ise matriste: veri hassas değil, zaten yayınlanmış içerik.
        PanelMenu.Items.Single(i => i.Controller == "NewsAdmin").Module.Should().Be("news");
    }

    /// <summary>
    /// 🔴 <c>Unarchive</c> <b>elle eklenmek zorundaydı</b>: <c>Archive</c> öneki onu
    /// yakalamıyor (eşleşme baştan yapılır) ve POST olduğu için sessizce <c>update</c>'e
    /// düşerdi — <i>yayından kaldırmak `approve` isterken yayına döndürmek `update` ile
    /// yapılabilirdi.</i>
    /// </summary>
    [Theory]
    [InlineData("Archive", "approve")]
    [InlineData("Unarchive", "approve")]
    [InlineData("ArchiveSelected", "approve")]
    [InlineData("UnarchiveSelected", "approve")]
    [InlineData("Edit", "update")]
    [InlineData("ResetOverrides", "update")]
    public void NewsActions_MapToTheExpectedPermission(string actionName, string expected)
        => PanelPermissionFilter.ActionFor(actionName, "POST").Should().Be(expected);

    [Fact]
    public async Task Unarchive_IsRejected_ForAModeratorWithOnlyUpdatePermission()
    {
        await SetPermissionsAsync(new AdminPermission
        {
            UserId = _moderatorId, Module = "news",
            CanRead = true, CanUpdate = true, CanApprove = false
        });

        var client = await ModeratorClientAsync();

        var response = await client.PostFormAsync("/NewsAdmin/Unarchive",
            new Dictionary<string, string> { ["id"] = _articleId.ToString() }, "/NewsAdmin");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect, HttpStatusCode.Found);
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2) Görünürlüğü yazan İKİNCİ yol yok (12.10'un dersi)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EditForm_HasNoVisibilityToggle()
    {
        var client = await _factory.SuperAdminAsync();
        var body = await (await client.GetAsync($"/NewsAdmin/Edit/{_articleId}")).ReadDecodedBodyAsync();

        // Form yalnız yöneticinin sahibi olduğu üç alanı taşır.
        body.Should().Contain("name=\"title\"");
        body.Should().Contain("name=\"excerpt\"");

        // 🔴 Görünürlük/durum alanı YOK: geçişin tek sahibi Yayından kaldır / Geri al.
        body.Should().NotContain("name=\"isArchived\"");
        body.Should().NotContain("name=\"state\"");
        body.Should().NotContain("name=\"Status\"");
    }

    [Fact]
    public async Task Archive_RequiresAReason_AndDoesNotTouchTheRecordWithoutOne()
    {
        var client = await _factory.SuperAdminAsync();

        await client.PostFormAsync("/NewsAdmin/Archive",
            new Dictionary<string, string> { ["id"] = _articleId.ToString(), ["reason"] = "  " }, $"/NewsAdmin/Details/{_articleId}");

        // ⚠️ Reddetme kaydı EZMEMELİ (§7 madde 46'nın kuralı): gerekçesiz istek kaydı
        // yayından kaldırmamalı.
        (await LoadAsync(_articleId)).IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task ArchiveThenUnarchive_MovesTheArticleOutOfAndBackIntoThePublicList()
    {
        var client = await _factory.SuperAdminAsync();

        await client.PostFormAsync("/NewsAdmin/Archive",
            new Dictionary<string, string> { ["id"] = _articleId.ToString(), ["reason"] = "test gerekçesi" }, $"/NewsAdmin/Details/{_articleId}");

        var archived = await LoadAsync(_articleId);
        archived.IsArchived.Should().BeTrue();
        archived.ArchivedReason.Should().Be("test gerekçesi");
        (await VisibleIdsAsync()).Should().NotContain(_articleId);

        await client.PostFormAsync("/NewsAdmin/Unarchive",
            new Dictionary<string, string> { ["id"] = _articleId.ToString() }, $"/NewsAdmin/Details/{_articleId}");

        var restored = await LoadAsync(_articleId);
        restored.IsArchived.Should().BeFalse();
        // Bayat gerekçe temizlenir (onay/red izi simetrisi).
        restored.ArchivedReason.Should().BeNull();
        (await VisibleIdsAsync()).Should().Contain(_articleId);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3) Senkron paneli EZMİYOR + bayatlama sayacı
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Override_SurvivesASourceUpdate_AndTheStaleCounterGoesFromZeroToOne()
    {
        var client = await _factory.SuperAdminAsync();

        await client.PostFormAsync("/NewsAdmin/Edit",
            new Dictionary<string, string>
            {
                ["id"] = _articleId.ToString(),
                ["title"] = "Panelde düzeltilmiş başlık"
            }, $"/NewsAdmin/Details/{_articleId}");

        (await StaleCountAsync()).Should().Be(0, "düzenleme kaynaktan yeni");

        // Kaynak, düzenlemeden SONRA değişiyor (senkronun yaptığı şey).
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var article = await db.Set<NewsArticle>().FirstAsync(x => x.Id == _articleId);
            article.ApplySourceSnapshot(SnapshotFor(article, "Kaynakta değişmiş başlık"), DateTime.UtcNow.AddMinutes(5));
            await db.SaveChangesAsync();
        });

        var updated = await LoadAsync(_articleId);
        // 🔑 Senkron override'a DOKUNAMAZ (alanlar `init`, ihlal CS8852).
        updated.TitleOverride.Should().Be("Panelde düzeltilmiş başlık");
        updated.SourceTitle.Should().Be("Kaynakta değişmiş başlık");

        // 🔴 12.12 senkronun ezmesini engelledi; bu, o kararın ikinci yüzü: override artık
        // BAYAT ve bunu kimse bilmiyor. Sayı görünmezse yönetici sebebini hiçbir zaman anlamaz.
        (await StaleCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ResetOverrides_ReturnsTheArticleToItsSourceForm()
    {
        var client = await _factory.SuperAdminAsync();

        await client.PostFormAsync("/NewsAdmin/Edit",
            new Dictionary<string, string> { ["id"] = _articleId.ToString(), ["title"] = "geçici" }, $"/NewsAdmin/Details/{_articleId}");

        await client.PostFormAsync("/NewsAdmin/ResetOverrides",
            new Dictionary<string, string> { ["id"] = _articleId.ToString() }, $"/NewsAdmin/Details/{_articleId}");

        var article = await LoadAsync(_articleId);
        // "Kilidi aç → ne olacağı belirsiz" yerine: kaynakta ne yazıyorsa o. Deterministik.
        article.TitleOverride.Should().BeNull();
        article.OverrideUpdatedAt.Should().BeNull();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4) Kategori dışlaması: semantik + önizlemenin GERÇEK sorgudan gelmesi
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExcludingACategory_HidesEveryArticleInIt_EvenIfTheyHaveOtherVisibleCategories()
    {
        var preview = await CategoryAsync(_categoryAId);

        // 🔴 Önizleme GERÇEK görünürlük sorgusundan gelir; ayrı bir sayım yazılsaydı
        // "2 haber kalkar" der, 1 tanesi kalkardı ve fark hiçbir yerde görünmezdi.
        preview.AffectedCount.Should().Be(2);

        var client = await _factory.SuperAdminAsync();
        await client.PostFormAsync("/LookupsAdmin/NewsCategoryUpdate",
            new Dictionary<string, string>
            {
                ["id"] = _categoryAId.ToString(),
                ["isExcluded"] = "true",
                ["showInFilterStrip"] = "true",
                ["displayOrder"] = "0"
            }, "/LookupsAdmin?open=news-categories");

        var visible = await VisibleIdsAsync();

        // ⚠️ İkinci haber "B" kategorisinde de: OR semantiğinde ("en az bir görünür kategori")
        // görünmeye DEVAM ederdi — yönetici anahtarı çevirir, hiçbir şey olmazdı.
        visible.Should().NotContain(_articleId);
        visible.Should().NotContain(_otherArticleId);
    }

    [Fact]
    public async Task TheReversePreview_CountsOnlyArticlesThatWouldActuallyComeBack()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            // İki kategori de dışlanmış: "B"yi geri açmak ikinci haberi GERİ GETİRMEZ,
            // çünkü hâlâ dışlanmış bir "A" kategorisi var.
            foreach (var id in new[] { _categoryAId, _categoryBId })
            {
                var category = await db.Set<NewsCategory>().FirstAsync(x => x.Id == id);
                category.SetVisibility(isExcluded: true, showInFilterStrip: true, displayOrder: 0);
            }
            await db.SaveChangesAsync();
        });

        // 🔑 Sayılsaydı yönetici "1 haber geri gelecek" diye okur, hiçbiri gelmezdi.
        (await CategoryAsync(_categoryBId)).AffectedCount.Should().Be(0);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5) Eşzamanlı koşu: kilit VERİTABANINDA
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ASecondRunCannotBeOpenedWhileOneIsStillActive()
    {
        await CleanRunsAsync();

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.Set<NewsSyncRun>().Add(new NewsSyncRun
            {
                Mode = NewsSyncModes.Incremental,
                Trigger = NewsSyncTriggers.Manual,
                StartedAt = DateTime.UtcNow,
                Status = NewsSyncStatuses.Running
            });
            await db.SaveChangesAsync();
        });

        // 🔴 Kilit Redis'te DEĞİL: bu projede Redis bilinçli olarak fail-open (§7 madde 36),
        // yani kilidin gerektiği anda açabilir. Kısmi unique indeks veritabanında.
        var second = await Record.ExceptionAsync(() => _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.Set<NewsSyncRun>().Add(new NewsSyncRun
            {
                Mode = NewsSyncModes.Archive,
                Trigger = NewsSyncTriggers.Manual,
                StartedAt = DateTime.UtcNow,
                Status = NewsSyncStatuses.Running
            });
            await db.SaveChangesAsync();
        }));

        second.Should().BeOfType<DbUpdateException>();

        await CleanRunsAsync();
    }

    [Fact]
    public async Task TheSyncService_ReportsBlockedInsteadOfFailing_WhenAnotherRunIsActive()
    {
        await CleanRunsAsync();

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.Set<NewsSyncRun>().Add(new NewsSyncRun
            {
                Mode = NewsSyncModes.Incremental,
                Trigger = NewsSyncTriggers.Schedule,
                StartedAt = DateTime.UtcNow,
                Status = NewsSyncStatuses.Running
            });
            await db.SaveChangesAsync();
        });

        await _factory.WithScopeAsync(async sp =>
        {
            var sync = sp.GetRequiredService<INewsSyncService>();
            var outcome = await sync.ReconcileAsync(NewsSyncTriggers.Manual, null, default);

            // 🔑 "Başarısız" DEĞİL "atlandı": koşu düşmedi, hiç açılmadı — ve bu korumanın
            // çalışmasıdır. Hata sayılsaydı panonun hata sayacı yalancı olurdu.
            outcome.Blocked.Should().BeTrue();
            outcome.Succeeded.Should().BeFalse();
            outcome.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        });

        await CleanRunsAsync();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6) Arama indeksi GERÇEKTEN kullanılıyor (12.12 sonrası denetim, bulgu 4)
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🐛 <b>Bu testin ilk hâli bir bozma denemesinde YEŞİL KALDI</b> ve dersi bu projede
    /// tanıdık: <i>test doğru şeyi doğrulasa da yanlış yere bakıyordu.</i> Yalnız ham SQL
    /// üzerinden plan ölçüyordu, yani <c>EF.Functions.Like</c> geri <c>Contains</c>'e
    /// çevrilse (sağlayıcıda <c>strpos</c>) plan aynı kalıyor ve test hiçbir şey söylemiyordu.
    /// Artık iki ayak var: <b>bizim sorgumuzun ürettiği SQL</b> ve <b>o şeklin indekse
    /// ulaşabilmesi</b>. Biri olmadan diğeri kanıt değil.
    /// </summary>
    [Fact]
    public async Task TheSearchQuery_ProducesALikeShape_ThatCanUseTheTrigramIndex()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();

            // 1. ayak: handler'ın GERÇEKTEN ürettiği SQL.
            var sql = GetNewsAdminQueryHandler
                .Filter(db.Set<NewsArticle>().AsNoTracking(),
                        new Application.Features.News.Dtos.QueryNewsAdminDto { Search = "kadirli" },
                        DateTime.UtcNow)
                .ToQueryString();

            sql.Should().Contain("LIKE", "trigram indeksi yalnız LIKE'ı karşılayabilir");
            // ⚠️ İfade indeksi `lower(kolon)` üzerinde: sorgu ham kolona bakarsa derlenir,
            // çalışır ve indeksi SESSİZCE kullanmaz — hata yok, yalnız tam tarama.
            sql.Should().Contain("lower");
            // `strpos` bir daha asla üretilmemeli: trigram indeksi onu karşılayamaz.
            // 📌 Ölçüm notu — bugünkü Npgsql `Contains`'i de LIKE'a çeviriyor, yani bu
            // iddia bir REGRESYON KAPISIDIR (sağlayıcı çevirisi bir gün değişebilir),
            // 12.12'nin hatasının kanıtı değil.
            sql.Should().NotContain("strpos");
            var connection = db.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            // 2. ayak: o şekil indekse ulaşabiliyor mu?
            // ⚠️ `enable_seqscan=off` bilinçli: birkaç satırlık test verisinde planlayıcı
            // her hâlükârda tam tarama seçer. Sorulan soru "bugün kullanıyor mu" değil,
            // **"sorgunun şekli indeksi kullanmaya izin veriyor mu"**. `Contains`
            // (yani `strpos`) yazılsaydı bu plan indekse HİÇ ulaşamazdı.
            command.CommandText =
                "SET enable_seqscan = off; " +
                "EXPLAIN SELECT id FROM news_articles WHERE lower(source_title) LIKE '%kadirli%';";

            var plan = new StringBuilder();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) plan.AppendLine(reader.GetString(0));

            plan.ToString().Should().Contain("ix_news_articles_source_title_trgm");
        });
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7) Panelin görsel dili: ham İngilizce sızmıyor
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EveryNewsBadge_HasATurkishLabel()
    {
        foreach (var state in NewsStates.All)
            PanelDisplay.NewsState(state).Label.Should().NotContain("Bilinmeyen");

        foreach (var mode in NewsSyncModes.All)
            PanelDisplay.NewsSyncMode(mode).Label.Should().NotContain("Bilinmeyen");

        foreach (var status in NewsSyncStatuses.All.Append(NewsSyncStatuses.Skipped))
            PanelDisplay.NewsSyncStatus(status).Label.Should().NotContain("Bilinmeyen");

        foreach (var trigger in NewsSyncTriggers.All)
            PanelDisplay.NewsSyncTrigger(trigger).Label.Should().NotContain("Bilinmeyen");

        // 12.12'nin geçici NonMatrixModules["news"] satırı menüye satır eklendiği an
        // ölü koda dönmüştü; "news-sync" ise hâlâ gerekli (menüde Module = null).
        PanelDisplay.ModuleLabel("news").Should().Be("Haberler");
        PanelDisplay.ModuleLabel("news-sync").Should().Be("Haber Senkronu");
    }

    [Fact]
    public async Task ExportCsv_UsesBomAndSemicolons_AndRespectsTheCurrentFilter()
    {
        var client = await _factory.SuperAdminAsync();

        var response = await client.GetAsync($"/NewsAdmin/ExportCsv?categoryId={_categoryBId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        // UTF-8 BOM yoksa Excel Türkçe karakteri bozar; ayraç virgül olursa Türkçe yerelde
        // her satır tek sütuna düşer (11.16b'nin dört sessiz tuzağı).
        bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);

        var csv = Encoding.UTF8.GetString(bytes);
        csv.Should().Contain(";");
        // Süzgeç uygulanmalı: yalnız "B" kategorisindeki ikinci haber.
        csv.Should().Contain($"{Marker} ikinci haber");
        csv.Should().NotContain($"{Marker} birinci haber");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Yardımcılar
    // ────────────────────────────────────────────────────────────────────────

    private static NewsArticle NewArticle(int wpId, string title)
    {
        var article = new NewsArticle { WpId = wpId };
        article.ApplySourceSnapshot(new NewsArticleSnapshot(
            Title: title,
            Excerpt: "özet",
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

    private static NewsArticleSnapshot SnapshotFor(NewsArticle article, string title) => new(
        Title: title,
        Excerpt: article.SourceExcerpt,
        ContentHtml: article.SourceContentHtml,
        PlainText: article.SourcePlainText,
        Url: article.SourceUrl,
        PublishedAtUtc: article.SourcePublishedAt,
        ModifiedAtUtc: DateTime.UtcNow.AddMinutes(5),
        Checksum: article.SourceChecksum + "-v2",
        ImageUrl: article.SourceImageUrl,
        ImageFileId: article.SourceImageFileId,
        ImageWidth: article.SourceImageWidth,
        ImageHeight: article.SourceImageHeight,
        ReadingMinutes: article.ReadingMinutes);

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

    private async Task<List<Guid>> VisibleIdsAsync()
    {
        List<Guid> ids = null!;
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            // Panelin sayacı ile vatandaşın listesi AYNI tanımdan geçer (§7 madde 23).
            ids = await NewsVisibility.Published(db.Set<NewsArticle>().AsNoTracking())
                .Select(x => x.Id)
                .ToListAsync();
        });
        return ids;
    }

    private async Task<int> StaleCountAsync()
    {
        var count = 0;
        await _factory.WithScopeAsync(async sp =>
        {
            var status = await sp.GetRequiredService<ISender>().Send(new GetNewsSyncStatusQuery());
            count = status.StaleOverrides;
        });
        return count;
    }

    private async Task<Application.Features.News.Dtos.NewsCategoryAdminDto> CategoryAsync(Guid id)
    {
        Application.Features.News.Dtos.NewsCategoryAdminDto dto = null!;
        await _factory.WithScopeAsync(async sp =>
        {
            var categories = await sp.GetRequiredService<ISender>().Send(new GetNewsCategoriesAdminQuery());
            dto = categories.Single(c => c.Id == id);
        });
        return dto;
    }

    private async Task<HttpClient> ModeratorClientAsync()
    {
        var client = _factory.CreatePanelClient();
        await client.LoginAsync(ModeratorUsername, ModeratorPassword);
        return client;
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

    private async Task CleanRunsAsync() => await _factory.WithScopeAsync(async sp =>
    {
        var db = sp.GetRequiredService<AppDbContext>();
        await db.Set<NewsSyncRun>().ExecuteDeleteAsync();
    });

    private async Task CleanAsync()
    {
        await SetPermissionsAsync();
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();

            // ⚠️ Temizlik BAŞLIĞA göre yapılamaz ve bu ders pahalıya mal oldu: bir test
            // `ApplySourceSnapshot` ile kaynağın başlığını değiştiriyor (senkronun yaptığı
            // şey), yani işaretçi başlıktan DÜŞÜYOR. O tek kayıt temizlenmeyince sonraki
            // her testin kurulumu `wp_id` çakışmasıyla patlıyordu. Kimlik aralığı, testin
            // kendi verisini tanımanın değişmeyen tek yolu.
            await db.Set<NewsArticle>().Where(x => x.WpId >= WpIdFloor && x.WpId < WpIdCeiling).ExecuteDeleteAsync();
            await db.Set<NewsCategory>().Where(x => x.WpId >= WpIdFloor && x.WpId < WpIdCeiling).ExecuteDeleteAsync();
        });
    }
}
