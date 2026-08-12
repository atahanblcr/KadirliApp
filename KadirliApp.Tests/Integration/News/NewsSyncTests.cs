using FluentAssertions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.News;
using KadirliApp.Application.Features.News.Services;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using KadirliApp.Tests.Integration.Panel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace KadirliApp.Tests.Integration.News;

/// <summary>
/// Faz 12.12 — <b>alım çekirdeğinin davranış testleri.</b> Gerçek Postgres, sahte kaynak.
///
/// İddialar doğrudan bu bloğun <b>üç yeni hasar sınıfından</b> türetildi:
/// <list type="number">
///   <item><b>Kaynak sessizce susabilir</b> → koşu hatayı sayıp ayakta kalıyor, imleç
///         <b>ilerlemiyor</b> (ilerleseydi o haberler bir daha hiç sorulmazdı).</item>
///   <item><b>Kaynak panelin yaptığını ezebilir</b> → <c>TitleOverride</c> senkrondan sonra
///         <b>yerinde</b>; kaynağın alanları ise güncelleniyor.</item>
///   <item><b>Kaynakta silinen haber bizde sonsuza kadar yaşar</b> → mutabakat <c>gone</c>
///         yapıyor, geri gelince <c>published</c>'a dönüyor.</item>
/// </list>
/// </summary>
[Collection(PanelCollection.Name)]
public class NewsSyncTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;

    public NewsSyncTests(WebPanelApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => ResetAsync();

    public Task DisposeAsync() => ResetAsync();

    // ───────────────────────────── Kurulum yardımcıları ─────────────────────────────

    /// <summary>
    /// Haber tabloları her testin başında boşaltılır. ⚠️ Paylaşılan veritabanında sayı
    /// iddia eden testler kendi kitlesini kurmak zorunda (12.2b'nin dersi) — bu modülün
    /// tabloları yalnız bu testlerce kullanıldığı için temizlemek en ucuz yalıtım.
    /// </summary>
    private async Task ResetAsync()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync("DELETE FROM news_article_categories");
            await db.NewsArticles.ExecuteDeleteAsync();
            await db.NewsCategories.ExecuteDeleteAsync();
            await db.NewsSyncRuns.ExecuteDeleteAsync();
            await db.NewsSyncStates.ExecuteDeleteAsync();
        });
    }

    private static NewsSourcePost Post(
        int id,
        string title = "Kadirli'de haber",
        string content = "<p>Gövde</p>",
        DateTime? published = null,
        DateTime? modified = null,
        string? imageUrl = null,
        params int[] categories)
    {
        var publishedAt = published ?? new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);

        var sizes = new Dictionary<string, NewsSourceImage>();
        if (imageUrl is not null) sizes["full"] = new NewsSourceImage(imageUrl, 650, 368);

        return new NewsSourcePost(
            WpId: id,
            Title: title,
            ExcerptHtml: "<p>Özet</p>",
            ContentHtml: content,
            Url: $"https://ornek.com/haber-{id}/",
            PublishedAtUtc: publishedAt,
            ModifiedAtUtc: modified ?? publishedAt,
            CategoryWpIds: categories.Length == 0 ? new[] { 1 } : categories,
            ImageSizes: sizes);
    }

    private static FakeNewsSource SourceWith(params NewsSourcePost[] posts)
    {
        var source = new FakeNewsSource();
        source.Categories.Add(new NewsSourceCategory(1, "Gündem", "gundem", 9753));
        source.Categories.Add(new NewsSourceCategory(187, "E-Gazete", "e-gazete", 366));
        source.Posts.AddRange(posts);
        return source;
    }

    /// <summary>
    /// Senkron servisi <b>elle</b> kurulur: sahte kaynak ve sahte indirici dışındaki her şey
    /// gerçek (Postgres, temizleyici, dosya deposu, önbellek, hata günlüğü).
    /// </summary>
    private async Task<T> WithSyncAsync<T>(
        FakeNewsSource source,
        Func<INewsSyncService, AppDbContext, Task<T>> action,
        FakeNewsImageDownloader? downloader = null,
        NewsSyncOptions? options = null)
    {
        T result = default!;

        await _factory.WithScopeAsync(async sp =>
        {
            var uow = sp.GetRequiredService<IUnitOfWork>();
            var db = sp.GetRequiredService<AppDbContext>();

            var mirror = new NewsImageMirror(
                uow,
                sp.GetRequiredService<IFileStorageService>(),
                downloader ?? new FakeNewsImageDownloader(),
                sp.GetRequiredService<ILogger<NewsImageMirror>>());

            var service = new NewsSyncService(
                uow,
                source,
                sp.GetRequiredService<INewsHtmlSanitizer>(),
                mirror,
                sp.GetRequiredService<ICacheService>(),
                sp.GetRequiredService<IErrorLogSink>(),
                options ?? new NewsSyncOptions { MaxTotalPosts = 10, PageSize = 5, MaxPagesPerRun = 5 },
                sp.GetRequiredService<ILogger<NewsSyncService>>());

            result = await action(service, db);
        });

        return result;
    }

    // ───────────────────────────── İlk dolum ve idempotentlik ───────────────────────

    [Fact]
    public async Task FirstRun_FillsTheArchive_AndTheCategories()
    {
        var source = SourceWith(Post(101), Post(102), Post(103));

        var outcome = await WithSyncAsync(source, async (sync, db) =>
        {
            // İmleç yokken artımlı koşu arşiv derinleştirmesine düşmeli — 27k haberlik
            // akışın başına dönmek yerine.
            var run = await sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None);

            (await db.NewsArticles.CountAsync()).Should().Be(3);
            (await db.NewsCategories.CountAsync()).Should().Be(2);
            return run;
        });

        outcome.Mode.Should().Be(NewsSyncModes.Archive, "boş veritabanında ilk dolum arşiv koşusudur");
        outcome.Created.Should().Be(3);
        outcome.Succeeded.Should().BeTrue();
    }

    /// <summary>
    /// 🔑 <b>Bitti kriteri:</b> ikinci koşu hiçbir mükerrer satır üretmemeli. Sağlama
    /// eşleştiği için satırlara <b>hiç dokunulmaz</b> (<c>Skipped</c>).
    /// </summary>
    [Fact]
    public async Task SecondRun_IsIdempotent_AndSkipsUnchangedRows()
    {
        var source = SourceWith(Post(111), Post(112));

        await WithSyncAsync(source, (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None));

        var second = await WithSyncAsync(source, async (sync, db) =>
        {
            var run = await sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None);
            (await db.NewsArticles.CountAsync()).Should().Be(2, "aynı haber ikinci kez inmemeli");
            return run;
        });

        second.Created.Should().Be(0);
        second.Updated.Should().Be(0);
    }

    /// <summary>Kaynakta değişen bir başlık ikinci koşuda gelmeli — "hiç güncelleme" de bir hata.</summary>
    [Fact]
    public async Task ChangedTitle_IsPickedUpByTheNextRun()
    {
        var source = SourceWith(Post(121, title: "İlk başlık"));

        await WithSyncAsync(source, (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None));

        source.Posts[0] = Post(121, title: "Düzeltilmiş başlık",
            modified: new DateTime(2026, 8, 2, 9, 0, 0, DateTimeKind.Utc));

        await WithSyncAsync(source, async (sync, db) =>
        {
            await sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None);

            var article = await db.NewsArticles.SingleAsync(x => x.WpId == 121);
            article.SourceTitle.Should().Be("Düzeltilmiş başlık");
            return true;
        });
    }

    // ───────────────────────────── İki sahip ────────────────────────────────────────

    /// <summary>
    /// 🔴 <b>Bu bloğun 2 numaralı hasar sınıfı.</b> Yönetici başlığı düzeltir, bir sonraki
    /// senkron üstüne yazarsa panel yalan söyler ve <b>kimse hata almaz</b>. Override ayrı
    /// kolonda olduğu için senkron ona <b>ulaşamıyor</b> — testin yanında derleyici de
    /// koruyor (<c>CS8852</c>).
    /// </summary>
    [Fact]
    public async Task Sync_UpdatesTheSourceFields_ButNeverTouchesTheOverrides()
    {
        var source = SourceWith(Post(131, title: "Kaynak başlığı"));

        await WithSyncAsync(source, (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None));

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var article = await db.NewsArticles.SingleAsync(x => x.WpId == 131);
            article.SetOverrides("Yöneticinin başlığı", "Yöneticinin özeti", null, Guid.NewGuid(), DateTime.UtcNow);
            await db.SaveChangesAsync();
        });

        source.Posts[0] = Post(131, title: "Kaynakta düzeltildi",
            modified: new DateTime(2026, 8, 3, 9, 0, 0, DateTimeKind.Utc));

        await WithSyncAsync(source, async (sync, db) =>
        {
            await sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None);

            var article = await db.NewsArticles.SingleAsync(x => x.WpId == 131);
            article.TitleOverride.Should().Be("Yöneticinin başlığı", "senkron override'a YAZAMAZ");
            article.ExcerptOverride.Should().Be("Yöneticinin özeti");
            article.SourceTitle.Should().Be("Kaynakta düzeltildi", "kaynağın alanı ise güncellenmeli");
            return true;
        });
    }

    /// <summary>Override kaldırıldığında kayıt <b>deterministik</b> biçimde kaynağa döner.</summary>
    [Fact]
    public async Task ClearingAnOverride_ReturnsTheRecordToTheSource()
    {
        var source = SourceWith(Post(141, title: "Kaynak"));
        await WithSyncAsync(source, (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None));

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var article = await db.NewsArticles.SingleAsync(x => x.WpId == 141);

            article.SetOverrides("Elle", null, null, Guid.NewGuid(), DateTime.UtcNow);
            article.ClearOverrides();
            await db.SaveChangesAsync();

            var reloaded = await db.NewsArticles.AsNoTracking().SingleAsync(x => x.WpId == 141);
            reloaded.TitleOverride.Should().BeNull();
            reloaded.OverrideUpdatedAt.Should().BeNull("override kalmadıysa damga da kalmamalı");
        });
    }

    // ───────────────────────────── Mutabakat ────────────────────────────────────────

    /// <summary>
    /// 🔴 <b>Bu bloğun 3 numaralı hasar sınıfı:</b> <c>modified_after</c> silmeyi hiç
    /// bildirmez. İş olmadan kaldırılan haber uygulamada <b>sonsuza kadar</b> durur.
    /// </summary>
    [Fact]
    public async Task Reconcile_MarksMissingArticlesGone_AndRestoresThemWhenTheyComeBack()
    {
        var source = SourceWith(Post(151), Post(152));
        await WithSyncAsync(source, (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None));

        var removed = source.Posts.Single(p => p.WpId == 152);
        source.Posts.Remove(removed);

        await WithSyncAsync(source, async (sync, db) =>
        {
            var run = await sync.ReconcileAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None);
            run.MarkedGone.Should().Be(1);

            var gone = await db.NewsArticles.SingleAsync(x => x.WpId == 152);
            gone.SourceState.Should().Be(NewsSourceStates.Gone);
            gone.SourceStateChangedAt.Should().NotBeNull("'ne zaman gitti' sorusunun cevabı kalmalı");
            return true;
        });

        source.Posts.Add(removed);

        await WithSyncAsync(source, async (sync, db) =>
        {
            var run = await sync.ReconcileAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None);
            run.Restored.Should().Be(1);

            (await db.NewsArticles.SingleAsync(x => x.WpId == 152)).SourceState
                .Should().Be(NewsSourceStates.Published);
            return true;
        });
    }

    /// <summary>
    /// 🔴 <b>En tehlikeli senaryo:</b> kaynak boş liste döndürürse "bizde olup listede
    /// olmayan her kayıt gitmiştir" kuralı <b>bütün arşivi</b> siler ve uygulamanın haber
    /// listesi tek bir 200 yanıtıyla boşalır. Kapı bu yüzden var.
    /// </summary>
    [Fact]
    public async Task Reconcile_RefusesToRun_WhenTheSourceReturnsNothing()
    {
        var source = SourceWith(Post(161), Post(162));
        await WithSyncAsync(source, (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None));

        source.ReturnNoIds = true;

        await WithSyncAsync(source, async (sync, db) =>
        {
            var run = await sync.ReconcileAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None);

            run.Succeeded.Should().BeFalse();
            run.MarkedGone.Should().Be(0);

            (await db.NewsArticles.CountAsync(x => x.SourceState == NewsSourceStates.Gone))
                .Should().Be(0, "tek bir boş yanıt bütün arşivi düşürmemeli");
            return true;
        });
    }

    // ───────────────────────────── Sanitizasyon ─────────────────────────────────────

    /// <summary>
    /// 🔑 Temizlik <b>alım anında</b>: veritabanında temiz olmayan bir gövde durduğu sürece
    /// her yeni ekran yeni bir XSS yüzeyidir.
    /// </summary>
    [Fact]
    public async Task Sync_StoresSanitizedBody_AndAPlainTextCopy()
    {
        var source = SourceWith(Post(171,
            content: "<p>Haber metni</p><script>alert('x')</script><form action=\"https://kotu\"><input name=\"tc\"></form>"));

        await WithSyncAsync(source, async (sync, db) =>
        {
            await sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None);

            var article = await db.NewsArticles.SingleAsync(x => x.WpId == 171);
            article.SourceContentHtml.Should().Contain("Haber metni");
            article.SourceContentHtml.Should().NotContain("script").And.NotContain("<form");
            article.SourcePlainText.Should().Be("Haber metni");
            article.ReadingMinutes.Should().BeGreaterThan(0);
            return true;
        });
    }

    // ───────────────────────────── Görsel aynalama ──────────────────────────────────

    [Fact]
    public async Task Sync_MirrorsTheCoverImage_AndReusesItForTheSameUrl()
    {
        const string sharedImage = "https://ornek.com/wp-content/uploads/kapak.webp";
        var source = SourceWith(
            Post(181, imageUrl: sharedImage),
            Post(182, imageUrl: sharedImage));

        var downloader = new FakeNewsImageDownloader();

        await WithSyncAsync(source, async (sync, db) =>
        {
            await sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None);

            var articles = await db.NewsArticles.Where(x => x.WpId == 181 || x.WpId == 182).ToListAsync();
            articles.Should().OnlyContain(a => a.SourceImageFileId != null);
            articles.Select(a => a.SourceImageFileId).Distinct().Should().HaveCount(1,
                "aynı görsel iki haberde geçerse uploads/ mükerrer dosyayla şişmemeli");

            var fileId = articles[0].SourceImageFileId!.Value;
            var file = await db.Files.SingleAsync(f => f.Id == fileId);
            file.CdnUrl.Should().StartWith("/uploads/", "uçlar GÖRELİ URL döndürmeli (§7 madde 9)");
            return true;
        }, downloader);

        downloader.Downloads.Should().Be(1);
    }

    /// <summary>Görsel indirilemezse haber yine inmeli — görselsiz haber, hiç inmemiş haberden iyidir.</summary>
    [Fact]
    public async Task Sync_StillIngestsTheArticle_WhenTheImageCannotBeDownloaded()
    {
        var source = SourceWith(Post(191, imageUrl: "https://ornek.com/yok.webp"));

        await WithSyncAsync(source, async (sync, db) =>
        {
            await sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None);

            var article = await db.NewsArticles.SingleAsync(x => x.WpId == 191);
            article.SourceImageFileId.Should().BeNull();
            article.SourceTitle.Should().NotBeNullOrEmpty();
            return true;
        }, new FakeNewsImageDownloader { Fail = true });
    }

    // ───────────────────────────── Dayanıklılık ─────────────────────────────────────

    /// <summary>
    /// 🔴 Kaynak kararsız (canlıda <c>520</c> görüldü). Sayfa hatası koşuyu düşürmemeli —
    /// ama imleci de <b>ilerletmemeli</b>: ilerletseydi o penceredeki haberler bir daha
    /// <b>hiç sorulmazdı</b> ve kimse fark etmezdi.
    /// </summary>
    [Fact]
    public async Task PageFailure_IsCounted_AndTheCursorDoesNotAdvancePastIt()
    {
        var source = SourceWith(Post(201));
        await WithSyncAsync(source, (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None));

        DateTime? cursorBefore = null;
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            cursorBefore = (await db.NewsSyncStates.SingleAsync()).ForwardCursorUtc;
        });

        source.Posts.Add(Post(202, modified: new DateTime(2026, 8, 5, 9, 0, 0, DateTimeKind.Utc)));
        source.FailNextPostRequest = true;

        var outcome = await WithSyncAsync(source, async (sync, db) =>
        {
            var run = await sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None);

            (await db.NewsSyncStates.SingleAsync()).ForwardCursorUtc
                .Should().Be(cursorBefore, "hata alan pencere bir daha sorulabilmeli");
            return run;
        });

        outcome.Failed.Should().BeGreaterThan(0);
        outcome.Status.Should().Be(NewsSyncStatuses.Completed, "kısmi hata koşuyu düşürmez");

        // Kaynak düzelince kaçırılan haber gelir — imleç ilerlemediği için.
        await WithSyncAsync(source, async (sync, db) =>
        {
            await sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None);
            (await db.NewsArticles.AnyAsync(x => x.WpId == 202)).Should().BeTrue();
            return true;
        });
    }

    /// <summary>
    /// 🐛 <b>Bu testi yazdıran şey bir test bulgusuydu.</b> Kategori isteği patladığında
    /// koşu devam ediyordu (doğru) ama <c>Failed</c> sayacına <b>hiç yazmıyordu</b>: panelde
    /// tertemiz görünen bir koşu, aslında kategorileri hiç alamamış olabilirdi. "0 hata"
    /// diyen bir koşu defteri, hiç defter tutmamaktan kötüdür.
    /// </summary>
    [Fact]
    public async Task CategoryFailure_IsCountedToo_AndTheRunStillIngestsArticles()
    {
        var source = SourceWith(Post(221));
        source.FailNextCategoryRequest = true;

        var outcome = await WithSyncAsync(source, async (sync, db) =>
        {
            var run = await sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None);
            (await db.NewsArticles.AnyAsync(x => x.WpId == 221)).Should().BeTrue(
                "kategori alınamadı diye haber alınmamazlık edilmez");
            return run;
        });

        outcome.Failed.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// 🐛 <b>Bu test bir "kuralı bilerek boz" denemesinden doğdu.</b> Kolon tavanını aşan tek
    /// bir başlık, <c>SaveChanges</c> anında <b>bütün partiyi</b> düşürürdü — hata kayıt başına
    /// değil <b>batch başına</b> doğduğu için "kayıt başına hata partiyi durdurmamalı" kuralı
    /// (§7 madde 29) bu yolda <b>çalışmıyordu</b>. Kaynak bizim ama içeriğini biz yazmıyoruz:
    /// 500 karakterlik bir başlık bir gün gelir ve o gün o koşuda <b>hiçbir haber inmez</b>.
    /// </summary>
    [Fact]
    public async Task OversizedSourceFields_AreTruncated_SoOneBadPostCannotDropTheWholeBatch()
    {
        var longTitle = new string('A', NewsColumnLimits.Title + 200);
        var source = SourceWith(Post(231, title: longTitle), Post(232));

        var outcome = await WithSyncAsync(source, async (sync, db) =>
        {
            var run = await sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None);

            (await db.NewsArticles.CountAsync(x => x.WpId == 231 || x.WpId == 232))
                .Should().Be(2, "tek bir uzun başlık partideki DİĞER haberi de düşürmemeli");

            (await db.NewsArticles.SingleAsync(x => x.WpId == 231)).SourceTitle
                .Length.Should().BeLessThanOrEqualTo(NewsColumnLimits.Title);
            return run;
        });

        outcome.Status.Should().Be(NewsSyncStatuses.Completed);
    }

    /// <summary>Her koşu bir satır bırakır: "ne zaman koştu, ne getirdi, neyi kaçırdı?"</summary>
    [Fact]
    public async Task EveryRun_LeavesAnAuditableRow_AndUpdatesTheFreshnessStamp()
    {
        var source = SourceWith(Post(211));

        await WithSyncAsync(source, async (sync, db) =>
        {
            await sync.RunIncrementalAsync(NewsSyncTriggers.Manual, Guid.NewGuid(), CancellationToken.None);

            var run = await db.NewsSyncRuns.OrderByDescending(x => x.StartedAt).FirstAsync();
            run.Trigger.Should().Be(NewsSyncTriggers.Manual);
            run.CompletedAt.Should().NotBeNull();
            run.TriggeredBy.Should().NotBeNull();

            var state = await db.NewsSyncStates.SingleAsync();
            state.LastSuccessfulRunAt.Should().NotBeNull("bayatlık göstergesinin tek kaynağı bu damga");
            NewsSyncHealth.Evaluate(state.LastSuccessfulRunAt, DateTime.UtcNow)
                .Should().Be(NewsSyncFreshness.Fresh);
            return true;
        });
    }

    /// <summary>
    /// Derinlik ayarı büyütülünce arşiv <b>kaldığı yerden</b> devam etmeli — baştan
    /// başlasaydı her büyütme bütün arşivi yeniden indirirdi.
    /// </summary>
    [Fact]
    public async Task RaisingTheBackfillDepth_ContinuesWhereItLeftOff()
    {
        var posts = Enumerable.Range(0, 6)
            .Select(i => Post(300 + i, published: new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc).AddDays(-i)))
            .ToArray();

        var source = SourceWith(posts);

        await WithSyncAsync(source,
            (sync, _) => sync.RunArchiveBackfillAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None),
            options: new NewsSyncOptions { MaxTotalPosts = 2, PageSize = 2, MaxPagesPerRun = 5 });

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            (await db.NewsArticles.CountAsync()).Should().Be(2);
        });

        await WithSyncAsync(source, async (sync, db) =>
        {
            await sync.RunArchiveBackfillAsync(NewsSyncTriggers.Manual, null, CancellationToken.None);
            (await db.NewsArticles.CountAsync()).Should().Be(5, "derinlik 5'e çıkınca 3 haber daha inmeli");
            return true;
        }, options: new NewsSyncOptions { MaxTotalPosts = 5, PageSize = 2, MaxPagesPerRun = 5 });
    }

    // ───────────────────────────── Görünürlük ───────────────────────────────────────

    /// <summary>
    /// Görünürlüğün üç koşulu tek yerde (<c>NewsVisibility</c>) — panel sayacı ile public
    /// listenin ayrışması bu projede daha önce canlıda yaşandı (§7 madde 23).
    /// </summary>
    [Fact]
    public async Task Visibility_HidesArchived_Gone_AndExcludedCategories()
    {
        var source = SourceWith(
            Post(401, categories: 1),
            Post(402, categories: 1),
            Post(403, categories: 187),   // E-Gazete
            Post(404, categories: 1));

        await WithSyncAsync(source, (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None));

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();

            (await db.NewsArticles.SingleAsync(x => x.WpId == 402)).Archive("test", null, DateTime.UtcNow);
            (await db.NewsArticles.SingleAsync(x => x.WpId == 404)).MarkSourceGone(DateTime.UtcNow);
            (await db.NewsCategories.SingleAsync(c => c.WpId == 187)).SetVisibility(true, false, 0);
            await db.SaveChangesAsync();

            var visible = await NewsVisibility.Published(db.NewsArticles.AsNoTracking())
                .Select(x => x.WpId)
                .ToListAsync();

            visible.Should().BeEquivalentTo(new[] { 401 });
        });
    }

    // ───────────────────── 12.12 sonrası denetim bulgularının kilitleri ─────────────

    /// <summary>
    /// 🐛 <b>Denetim bulgusu 1.</b> Sözlükte olmayan bir kategori kimliği (kaynakta gizlenmiş
    /// ya da silinmiş bir kategori — public <c>/categories</c> yalnız görünenleri döndürür)
    /// eskiden o kimliği taşıyan <b>her haber için</b> yeni bir HTTP isteği tetikliyordu:
    /// 50 haber → 50 fazladan istek. Metodun kendi yorumu "koşu içinde bir kez tazelenir"
    /// diyordu ama bunu sağlayan bayrak <b>yoktu</b> — yani yorum yalan söylüyordu.
    /// </summary>
    [Fact]
    public async Task UnknownCategoryId_RefreshesTheDictionaryOnlyOncePerRun()
    {
        var source = SourceWith(
            Post(501, categories: 999),
            Post(502, categories: 999),
            Post(503, categories: 999));

        var before = source.CategoryRequests;

        await WithSyncAsync(source, async (sync, db) =>
        {
            await sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None);

            // Haberler yine iniyor — tanınmayan kategori kaydı düşürmüyor.
            (await db.NewsArticles.CountAsync(x => x.WpId >= 501 && x.WpId <= 503)).Should().Be(3);
            return true;
        });

        (source.CategoryRequests - before).Should().BeLessThanOrEqualTo(2,
            "koşu başında bir kez + tanınmayan kimlik için EN FAZLA bir kez tazelenmeli; " +
            "post başına tazeleme 500 haberde 500 fazladan istek demektir");
    }

    /// <summary>
    /// 🐛 <b>Denetim bulgusu 3.</b> <c>news_sync_state</c> "tek satır" olmalı ama bunu
    /// garanti eden bir şey yoktu: <c>SyncNewsJob</c> (15 dk, yani 03:00'te de) ile
    /// <c>ReconcileNewsJob</c> (03:00) boş durumda aynı anda başlarsa <b>iki satır</b> doğar
    /// ve o andan sonra ileri imleç koşular arasında <b>ileri-geri zıplar</b> — aradaki
    /// haberler atlanır, hiçbir hata oluşmaz. <c>DisableConcurrentExecution</c> yalnız
    /// <b>aynı</b> işi korur, iki farklı işi değil.
    /// </summary>
    [Fact]
    public async Task SyncState_CanNeverHaveASecondRow()
    {
        var source = SourceWith(Post(511));
        await WithSyncAsync(source, (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None));

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            (await db.NewsSyncStates.CountAsync()).Should().Be(1);

            db.NewsSyncStates.Add(new NewsSyncState());

            var act = async () => await db.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>(
                "ikinci imleç satırı veritabanı seviyesinde imkânsız olmalı");
        });
    }

    /// <summary>Mutabakat ile senkron aynı satırı paylaşmalı — imleç tek olmalı.</summary>
    [Fact]
    public async Task ReconcileAndSync_ShareTheSameCursorRow()
    {
        var source = SourceWith(Post(521));

        await WithSyncAsync(source, (sync, _) => sync.ReconcileAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None));
        await WithSyncAsync(source, (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None));

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            (await db.NewsSyncStates.CountAsync()).Should().Be(1);
            (await db.NewsSyncStates.SingleAsync()).ForwardCursorUtc.Should().NotBeNull();
        });
    }

    // ─────────────────── Faz 12.14: metin arası görsellerin aynalanması ──────────────
    //
    // ⚠️ Bu bloktaki testler **birbirinden farklı görsel adresleri** kullanıyor ve bu
    // zorunlu: 12.14'ün tekilleştirmesi `files.metadata` üzerinden **koşular arası**
    // çalışıyor, `ResetAsync` ise yalnız haber tablolarını boşaltıyor (dosyalar paylaşılan
    // veritabanında kalıyor). Adres paylaşan iki test yazıldığında ikincisi "zaten
    // aynalanmış" yoluna düşüyor ve indiriciye hiç uğramıyor — ilk yazımda tam bu oldu
    // ve iki test kırmızıya döndü. 🔑 Kırılma doğruydu: tekilleştirme ÇALIŞIYOR demekti.

    /// <summary>
    /// 🔴 <b>Bu bloğun var olma sebebi:</b> gövde görsellerinin %9'u <b>imzalı/süreli</b>
    /// adres (ölçüldü) — zamanla mutlaka 403'e düşecekler ve istemci onları
    /// <i>zarifçe gizlediği</i> için <b>hiç kimse hata almayacak</b>. Aynalama, kaynağın
    /// bizden bağımsız çürümesine karşı tek gerçek koruma.
    /// </summary>
    [Fact]
    public async Task BodyImages_AreMirroredAndRewrittenToRelativeUrls()
    {
        var source = SourceWith(Post(
            601,
            content: "<p>Metin</p><figure><img src=\"https://ornek.com/govde-601.jpg\"></figure>",
            imageUrl: "https://ornek.com/kapak.jpg"));

        var downloader = new FakeNewsImageDownloader();

        await WithSyncAsync(
            source,
            (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None),
            downloader);

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var article = await db.NewsArticles.SingleAsync(x => x.WpId == 601);

            // Gövdedeki adres artık BİZİM (§7 madde 9: göreli).
            article.SourceContentHtml.Should().NotContain("https://ornek.com/govde-601.jpg");
            article.SourceContentHtml.Should().Contain("/uploads/");
            NewsBodyImages.HasExternalImages(article.SourceContentHtml).Should().BeFalse();

            // Kapak + gövde = iki ayrı dosya.
            downloader.Downloads.Should().Be(2);
        });
    }

    /// <summary>
    /// İndirilemeyen gövde görseli <b>olduğu gibi kalır</b> — haber düşmez, gövdeden
    /// silinmez. Yani en kötü hâlde 12.14 <b>öncesine</b> düşülür.
    /// </summary>
    [Fact]
    public async Task BodyImage_ThatCannotBeDownloaded_StaysHotlinked_AndTheArticleStillLands()
    {
        var source = SourceWith(Post(
            602,
            content: "<p>Metin</p><img src=\"https://ornek.com/govde-602.jpg\">"));

        await WithSyncAsync(
            source,
            (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None),
            new FakeNewsImageDownloader { Fail = true });

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var article = await db.NewsArticles.SingleAsync(x => x.WpId == 602);

            article.SourceContentHtml.Should().Contain("https://ornek.com/govde-602.jpg");
        });
    }

    /// <summary>
    /// 🔴 <b>En sinsi regresyon buradaydı:</b> aynalanmış gövde kaynağınkine hiçbir zaman
    /// eşit olamaz. Sağlama aynalanmış gövdeyle hesaplansaydı her koşu haberi
    /// <b>"değişmiş"</b> sayar ve sonsuza kadar yeniden yazardı — ne hata, ne belirti,
    /// yalnız durmadan artan bir <c>Updated</c> sayacı.
    /// </summary>
    [Fact]
    public async Task SecondRun_DoesNotRewriteTheArticle_NorRedownloadTheBodyImage()
    {
        var source = SourceWith(Post(
            603,
            content: "<p>Metin</p><img src=\"https://ornek.com/govde-603.jpg\">"));

        var downloader = new FakeNewsImageDownloader();

        await WithSyncAsync(
            source,
            (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None),
            downloader);

        var second = await WithSyncAsync(
            source,
            (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None),
            downloader);

        second.Updated.Should().Be(0, "aynalama kaydı 'değişmiş' yapmamalı");
        second.Skipped.Should().BeGreaterThan(0);
        downloader.Downloads.Should().Be(1, "aynı görsel ikinci kez indirilmemeli");
    }

    /// <summary>
    /// Aynı görsel iki haberde geçiyorsa <b>tek dosya</b> olur. Tekilleştirme
    /// <c>files.metadata</c> üzerinden çalışıyor: gövde görselleri
    /// <c>news_articles.source_image_url</c>'de <b>görünmez</b> (o kolon yalnız kapağı tanır).
    /// </summary>
    [Fact]
    public async Task TheSameBodyImage_InTwoArticles_IsStoredOnce()
    {
        var source = SourceWith(
            Post(604, content: "<p>A</p><img src=\"https://ornek.com/ortak.jpg\">"),
            Post(605, content: "<p>B</p><img src=\"https://ornek.com/ortak.jpg\">"));

        var downloader = new FakeNewsImageDownloader();

        await WithSyncAsync(
            source,
            (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None),
            downloader);

        downloader.Downloads.Should().Be(1);

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var bodies = await db.NewsArticles
                .Where(x => x.WpId == 604 || x.WpId == 605)
                .Select(x => x.SourceContentHtml)
                .ToListAsync();

            bodies.Should().HaveCount(2);
            bodies.Should().AllSatisfy(b => b.Should().Contain("/uploads/"));
        });
    }

    /// <summary>
    /// Aynalama kapatılabilir olmalı (<c>News:MirrorImages=false</c>): kapalıyken haber yine
    /// iner, yalnız gövde hotlink kalır. ⚠️ Bayrakla kapalı yol = hiç test edilmemiş yol
    /// (§7 kod-dışı) — bu yüzden bayrağın <b>her iki</b> durumu deneniyor.
    /// </summary>
    [Fact]
    public async Task WhenMirroringIsDisabled_TheBodyIsLeftAlone()
    {
        var source = SourceWith(Post(
            606,
            content: "<p>Metin</p><img src=\"https://ornek.com/govde-606.jpg\">"));

        var downloader = new FakeNewsImageDownloader();

        await WithSyncAsync(
            source,
            (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None),
            downloader,
            new NewsSyncOptions { MaxTotalPosts = 10, PageSize = 5, MaxPagesPerRun = 5, MirrorImages = false });

        downloader.Downloads.Should().Be(0);

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var article = await db.NewsArticles.SingleAsync(x => x.WpId == 606);
            article.SourceContentHtml.Should().Contain("https://ornek.com/govde-606.jpg");
        });
    }

    // ─────────────────── Faz 12.14: 12.14 ÖNCESİ kayıtların geri doldurulması ────────

    /// <summary>
    /// 🔴 <b>Geri doldurmanın var olma sebebi:</b> senkron yalnız <i>kaynakta değişen</i>
    /// haberi yeniden yazar (sağlama eşitse satıra hiç dokunmaz). Yani 12.14'ten önce inmiş
    /// kayıtların gövdesi, kaynakta bir daha hiç değişmezse <b>sonsuza kadar</b> hotlink
    /// kalırdı — ve tam da o kayıtların görselleri en eski, yani en çok çürümeye yakın olanlar.
    /// </summary>
    [Fact]
    public async Task Backfill_MirrorsBodyImagesOfArticlesThatLandedBefore1214()
    {
        // 12.14 öncesi durumu birebir kur: aynalama kapalıyken bir haber indir.
        var source = SourceWith(Post(
            701,
            content: "<p>Metin</p><img src=\"https://ornek.com/eski-701.jpg\">"));

        await WithSyncAsync(
            source,
            (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None),
            new FakeNewsImageDownloader(),
            new NewsSyncOptions { MaxTotalPosts = 10, PageSize = 5, MaxPagesPerRun = 5, MirrorImages = false });

        var outcome = await RunBackfillAsync();

        outcome.Rewritten.Should().Be(1);
        outcome.ImagesMirrored.Should().Be(1);

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var article = await db.NewsArticles.SingleAsync(x => x.WpId == 701);
            article.SourceContentHtml.Should().Contain("/uploads/");
            NewsBodyImages.HasExternalImages(article.SourceContentHtml).Should().BeFalse();
        });
    }

    /// <summary>
    /// Geri doldurma <b>idempotent</b>: ikinci tur hiçbir şey yapmamalı. Yapsaydı iş
    /// saatlik koştuğu için aynı kayıtları sonsuza kadar yeniden yazardı.
    /// </summary>
    [Fact]
    public async Task Backfill_IsIdempotent()
    {
        var source = SourceWith(Post(
            702,
            content: "<p>Metin</p><img src=\"https://ornek.com/eski-702.jpg\">"));

        await WithSyncAsync(
            source,
            (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None),
            new FakeNewsImageDownloader(),
            new NewsSyncOptions { MaxTotalPosts = 10, PageSize = 5, MaxPagesPerRun = 5, MirrorImages = false });

        await RunBackfillAsync();
        var second = await RunBackfillAsync();

        second.Rewritten.Should().Be(0);
        second.ImagesMirrored.Should().Be(0);
    }

    /// <summary>
    /// 🔑 Geri doldurma <b>sağlamaya dokunmaz</b>: aynalama bizim yaptığımız bir şey,
    /// kaynağın değişmesi değil. Dokunsaydı bir sonraki senkron bu haberleri "değişmiş"
    /// sayar ve gereksiz yere yeniden yazardı — üstelik geri doldurmanın az önce yazdığı
    /// göreli adresleri kaynağınkilerle <b>geri</b> ezerek.
    /// </summary>
    [Fact]
    public async Task Backfill_DoesNotTouchTheChecksum_SoTheNextSyncStillSkips()
    {
        var source = SourceWith(Post(
            703,
            content: "<p>Metin</p><img src=\"https://ornek.com/eski-703.jpg\">"));

        await WithSyncAsync(
            source,
            (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None),
            new FakeNewsImageDownloader(),
            new NewsSyncOptions { MaxTotalPosts = 10, PageSize = 5, MaxPagesPerRun = 5, MirrorImages = false });

        var before = await ChecksumOfAsync(703);
        await RunBackfillAsync();
        var after = await ChecksumOfAsync(703);

        after.Should().Be(before);

        var next = await WithSyncAsync(
            source,
            (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None),
            new FakeNewsImageDownloader());

        next.Updated.Should().Be(0, "geri doldurma senkronu tetiklememeli");

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var article = await db.NewsArticles.SingleAsync(x => x.WpId == 703);
            article.SourceContentHtml.Should().Contain(
                "/uploads/",
                "senkron, geri doldurmanın yazdığı göreli adresi geri ezmemeli");
        });
    }

    /// <summary>Tur tavanı gerçekten uygulanıyor mu — tavansız bir tur kaynağı da bizi de yorar.</summary>
    [Fact]
    public async Task Backfill_RespectsItsBatchCeiling()
    {
        var source = SourceWith(
            Post(711, content: "<p>A</p><img src=\"https://ornek.com/a.jpg\">"),
            Post(712, content: "<p>B</p><img src=\"https://ornek.com/b.jpg\">"),
            Post(713, content: "<p>C</p><img src=\"https://ornek.com/c.jpg\">"));

        await WithSyncAsync(
            source,
            (sync, _) => sync.RunIncrementalAsync(NewsSyncTriggers.Schedule, null, CancellationToken.None),
            new FakeNewsImageDownloader(),
            new NewsSyncOptions { MaxTotalPosts = 10, PageSize = 5, MaxPagesPerRun = 5, MirrorImages = false });

        var first = await RunBackfillAsync(batchSize: 2);
        first.Rewritten.Should().Be(2);

        var second = await RunBackfillAsync(batchSize: 2);
        second.Rewritten.Should().Be(1, "kalan tek kayıt bir sonraki turda onarılmalı");

        var third = await RunBackfillAsync(batchSize: 2);
        third.Rewritten.Should().Be(0);
    }

    private async Task<NewsBodyImageBackfillOutcome> RunBackfillAsync(int batchSize = 10)
    {
        NewsBodyImageBackfillOutcome outcome = default!;

        await _factory.WithScopeAsync(async sp =>
        {
            var uow = sp.GetRequiredService<IUnitOfWork>();
            var mirror = new NewsImageMirror(
                uow,
                sp.GetRequiredService<IFileStorageService>(),
                new FakeNewsImageDownloader(),
                sp.GetRequiredService<ILogger<NewsImageMirror>>());

            var backfill = new NewsBodyImageBackfill(
                uow, mirror, sp.GetRequiredService<ILogger<NewsBodyImageBackfill>>());

            outcome = await backfill.RunAsync(batchSize, CancellationToken.None);
        });

        return outcome;
    }

    private async Task<string> ChecksumOfAsync(int wpId)
    {
        var checksum = string.Empty;
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            checksum = (await db.NewsArticles.SingleAsync(x => x.WpId == wpId)).SourceChecksum;
        });
        return checksum;
    }
}
