using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Application.Features.News.Services;

/// <summary>
/// Faz 12.12 — <b>haber alımının tek sahibi</b> (<see cref="INewsSyncService"/>).
///
/// Üç koşu türü, tek upsert:
/// <list type="bullet">
///   <item><b>Artımlı</b> (<c>modified_after</c>, 15 dk'da bir): yeni ve değişen haberler.</item>
///   <item><b>Arşiv derinleştirmesi</b> (<c>before=…</c>, istekle): geriye doğru derinlik.</item>
///   <item><b>Mutabakat</b> (kimlik taraması, gecelik): <b>silmeyi öğrenmenin tek yolu.</b></item>
/// </list>
///
/// 🔑 İkisi de <b>aynı upsert'e</b> düşer (<c>WpId</c> üzerinden), yani mükerrer çekiş zararsız.
/// 🔴 Dayanıklılık: bir sayfanın hatası <b>koşuyu düşürmez</b> — sayılır, <c>Failed</c>'a yazılır
/// ve koşu devam eder (§7 madde 29'un "kayıt başına hata partiyi durdurmamalı" kuralı).
/// Kaynak kararsız: örneklemede canlıda <c>520</c> görüldü.
/// </summary>
public class NewsSyncService : INewsSyncService
{
    private readonly IUnitOfWork _uow;
    private readonly INewsSourceClient _client;
    private readonly INewsHtmlSanitizer _sanitizer;
    private readonly NewsImageMirror _mirror;
    private readonly ICacheService _cache;
    private readonly IErrorLogSink _errors;
    private readonly NewsSyncOptions _options;
    private readonly ILogger<NewsSyncService> _log;

    public NewsSyncService(
        IUnitOfWork uow,
        INewsSourceClient client,
        INewsHtmlSanitizer sanitizer,
        NewsImageMirror mirror,
        ICacheService cache,
        IErrorLogSink errors,
        NewsSyncOptions options,
        ILogger<NewsSyncService> log)
    {
        _uow = uow;
        _client = client;
        _sanitizer = sanitizer;
        _mirror = mirror;
        _cache = cache;
        _errors = errors;
        _options = options;
        _log = log;
    }

    // ───────────────────────────── Artımlı (ileri imleç) ─────────────────────────────

    public async Task<NewsSyncOutcome> RunIncrementalAsync(string trigger, Guid? triggeredBy, CancellationToken ct)
    {
        var state = await LoadStateAsync(ct);

        // 🔑 İmleç yoksa (boş veritabanı) artımlı koşunun sorabileceği bir "ne zamandan beri"
        // yok. 27.284 haberin başına dönmek yerine ilk dolum arşiv derinleştirmesine düşer.
        if (state.ForwardCursorUtc is null)
            return await RunArchiveBackfillAsync(trigger, triggeredBy, ct);

        var run = await StartRunAsync(NewsSyncModes.Incremental, trigger, triggeredBy, ct);
        run.CursorFrom = state.ForwardCursorUtc;

        try
        {
            var categories = await SyncCategoriesAsync(run, ct);

            var cursor = state.ForwardCursorUtc.Value;
            var advancedTo = cursor;
            var cursorIsSafe = true;

            for (var page = 1; page <= _options.MaxPagesPerRun; page++)
            {
                NewsSourcePage result;
                try
                {
                    result = await _client.GetPostsModifiedSinceAsync(
                        WordPressTimeWindow.QueryFloor(cursor), page, _options.PageSize, ct);
                }
                catch (Exception ex)
                {
                    // Sayfa hatası koşuyu düşürmez — ama imleci de ilerletmez:
                    // ilerletseydi bu sayfadaki haberler bir daha HİÇ sorulmazdı.
                    //
                    // 📌 Bugün imleci koruyan şey aslında aşağıdaki `break` (bozma denemesinde
                    // yalnız bayrağı kaldırmak testi KIRMADI — dürüst not). Bayrak yine de
                    // duruyor: biri yarın "bir sayfa patladı diye durmayalım, sıradakine
                    // geçelim" derse koruma o gün kaybolurdu.
                    run.Failed++;
                    cursorIsSafe = false;
                    Report(ex, $"Haber senkronu: sayfa {page} alınamadı");
                    break;
                }

                if (result.Posts.Count == 0) break;

                run.Fetched += result.Posts.Count;

                foreach (var post in result.Posts)
                {
                    try
                    {
                        await UpsertAsync(post, run, categories, ct);
                        if (cursorIsSafe && post.ModifiedAtUtc > advancedTo)
                            advancedTo = post.ModifiedAtUtc;
                    }
                    catch (Exception ex)
                    {
                        run.Failed++;
                        cursorIsSafe = false;
                        Report(ex, $"Haber senkronu: WpId={post.WpId} işlenemedi");
                    }
                }

                await _uow.SaveChangesAsync(ct);

                if (page >= result.TotalPages) break;
            }

            state.ForwardCursorUtc = advancedTo;
            run.CursorTo = advancedTo;

            return await CompleteRunAsync(run, state, ct);
        }
        catch (Exception ex)
        {
            return await FailRunAsync(run, ex, ct);
        }
    }

    // ───────────────────────────── Arşiv (geri imleç) ────────────────────────────────

    public async Task<NewsSyncOutcome> RunArchiveBackfillAsync(string trigger, Guid? triggeredBy, CancellationToken ct)
    {
        var state = await LoadStateAsync(ct);
        var run = await StartRunAsync(NewsSyncModes.Archive, trigger, triggeredBy, ct);

        try
        {
            var categories = await SyncCategoriesAsync(run, ct);

            var have = await _uow.Repository<NewsArticle>().Query().CountAsync(ct);
            var remaining = _options.BackfillMaxPosts - have;

            // Derinlik zaten dolu (ya da kaynak tükendi) → hiçbir istek atma. İdempotent:
            // iş her açılışta koşabilir, ikinci koşuda hiçbir şey yapmaz.
            if (remaining > 0 && !state.ArchiveCompleted)
            {
                var before = state.ArchiveCursorUtc;

                for (var page = 0; page < _options.MaxPagesPerRun && remaining > 0; page++)
                {
                    var take = Math.Min(_options.PageSize, remaining);

                    NewsSourcePage result;
                    try
                    {
                        result = await _client.GetPostsByDateDescendingAsync(
                            before.HasValue ? WordPressTimeWindow.ToSiteLocal(before.Value) : null, take, ct);
                    }
                    catch (Exception ex)
                    {
                        run.Failed++;
                        Report(ex, "Haber arşiv derinleştirmesi: sayfa alınamadı");
                        break;
                    }

                    if (result.Posts.Count == 0)
                    {
                        // Kaynak tükendi: daha eski haber yok. Bir daha sorma.
                        state.ArchiveCompleted = true;
                        break;
                    }

                    run.Fetched += result.Posts.Count;

                    foreach (var post in result.Posts)
                    {
                        try
                        {
                            await UpsertAsync(post, run, categories, ct);
                        }
                        catch (Exception ex)
                        {
                            run.Failed++;
                            Report(ex, $"Haber arşivi: WpId={post.WpId} işlenemedi");
                        }
                    }

                    await _uow.SaveChangesAsync(ct);

                    // İmleç en eski işlenen habere çekilir; bir sonraki istek onun ÖNCESİNİ ister.
                    var oldest = result.Posts.Min(p => p.PublishedAtUtc);
                    if (before.HasValue && oldest >= before.Value)
                    {
                        // Kaynak `before`'u yok saydı (ya da eşit tarihli yığın var) — ilerleme
                        // yoksa döngü sonsuza kadar aynı sayfayı çeker. Durup bir sonraki koşuya bırak.
                        _log.LogWarning("Haber arşiv imleci ilerlemedi ({Before}) — koşu durduruldu.", before);
                        break;
                    }

                    before = oldest;
                    remaining -= result.Posts.Count;
                }

                state.ArchiveCursorUtc = before;
            }

            // İlk dolumdan sonra ileri imleç elimizdeki en yeni değişiklik damgasına kurulur:
            // artımlı koşu buradan devam eder ve geçmişi bir daha taramaz.
            state.ForwardCursorUtc ??= await _uow.Repository<NewsArticle>().Query()
                .OrderByDescending(x => x.SourceModifiedAt)
                .Select(x => (DateTime?)x.SourceModifiedAt)
                .FirstOrDefaultAsync(ct) ?? DateTime.UtcNow;

            run.CursorTo = state.ForwardCursorUtc;

            return await CompleteRunAsync(run, state, ct);
        }
        catch (Exception ex)
        {
            return await FailRunAsync(run, ex, ct);
        }
    }

    // ───────────────────────────── Mutabakat ─────────────────────────────────────────

    public async Task<NewsSyncOutcome> ReconcileAsync(string trigger, Guid? triggeredBy, CancellationToken ct)
    {
        var state = await LoadStateAsync(ct);
        var run = await StartRunAsync(NewsSyncModes.Reconcile, trigger, triggeredBy, ct);

        try
        {
            var window = await _client.GetPublishedIdWindowAsync(_options.BackfillMaxPosts, ct);
            run.Fetched = window.WpIds.Count;

            // 🔴 EN ÖNEMLİ KAPI: kaynak boş liste döndürdüyse (kesinti, yanlış yol, boş cevap)
            // "bizde olup listede olmayan her kayıt gitmiştir" kuralı BÜTÜN arşivi `gone`
            // yapardı — tek bir 200 yanıtıyla uygulamanın haber listesi boşalırdı.
            if (window.WpIds.Count == 0)
            {
                run.Failed++;
                return await FailRunAsync(run, new InvalidOperationException(
                    "Kaynak hiç kimlik döndürmedi — mutabakat güvenli değil, hiçbir kayıt işaretlenmedi."), ct);
            }

            var ids = window.WpIds.ToHashSet();

            // ⚠️ Pencere tabanı: iş yalnız DERİNLİĞİMİZ kadarını tarar. Kaynağın döndürdüğü en
            // eski tarih ile bizim arşiv imlecimizin GEÇ olanı taban alınır; aksi hâlde
            // taramanın altında kalan her eski haber "kaynakta yok" sanılır.
            var floor = Later(window.OldestPublishedAtUtc, state.ArchiveCursorUtc) ?? DateTime.MinValue;

            var ours = await _uow.Repository<NewsArticle>().Query(tracking: true)
                .Where(x => x.SourcePublishedAt >= floor)
                .ToListAsync(ct);

            var now = DateTime.UtcNow;

            foreach (var article in ours)
            {
                if (!ids.Contains(article.WpId))
                {
                    if (article.SourceState == NewsSourceStates.Published)
                    {
                        article.MarkSourceGone(now);
                        run.MarkedGone++;
                    }
                }
                else if (article.SourceState == NewsSourceStates.Gone)
                {
                    // 🔑 Ters yön: kaynağa geri dönen haber yeniden yayına girer (idempotent).
                    article.MarkSourcePublished(now);
                    run.Restored++;
                }
            }

            return await CompleteRunAsync(run, state, ct);
        }
        catch (Exception ex)
        {
            return await FailRunAsync(run, ex, ct);
        }
    }

    // ───────────────────────────── Upsert ────────────────────────────────────────────

    private async Task UpsertAsync(
        NewsSourcePost post, NewsSyncRun run, NewsCategoryCache categories, CancellationToken ct)
    {
        var repo = _uow.Repository<NewsArticle>();
        var now = DateTime.UtcNow;

        var existing = await repo.Query(tracking: true)
            .Include(x => x.Categories)
            .FirstOrDefaultAsync(x => x.WpId == post.WpId, ct);

        var contentHtml = _sanitizer.Sanitize(post.ContentHtml);
        var plainText = _sanitizer.ToPlainText(post.ContentHtml);
        var excerpt = Trim(_sanitizer.ToPlainText(post.ExcerptHtml)) ?? Summarize(plainText);
        var cover = NewsImagePicker.PickCover(post.ImageSizes);

        var candidate = NewsChecksum.Compute(post.Title, excerpt, contentHtml, cover?.Url, post.CategoryWpIds);

        if (existing is not null && existing.SourceChecksum == candidate)
        {
            // Hiçbir alan değişmemiş → satıra DOKUNMA. Tek istisna: kaynakta yeniden
            // görünen bir haber `published`'a döner (mutabakatın ters yönü, bedava).
            if (existing.SourceState != NewsSourceStates.Published)
                existing.MarkSourcePublished(now);

            run.Skipped++;
            return;
        }

        // Görsel: URL değişmediyse yeniden indirme. İndirme başarısızsa ESKİ görseli koru —
        // ama eski URL'i de koru ki bir sonraki koşu yeniden denesin (sağlama URL'i içeriyor).
        var imageUrl = cover?.Url;
        Guid? imageFileId = null;
        int? width = cover?.Width;
        int? height = cover?.Height;

        if (existing is not null && string.Equals(existing.SourceImageUrl, imageUrl, StringComparison.OrdinalIgnoreCase))
        {
            imageFileId = existing.SourceImageFileId;
        }
        else if (imageUrl is not null && _options.MirrorImages)
        {
            imageFileId = await _mirror.MirrorAsync(imageUrl, ct);
            if (imageFileId is null && existing?.SourceImageFileId is not null)
            {
                imageUrl = existing.SourceImageUrl;
                imageFileId = existing.SourceImageFileId;
                width = existing.SourceImageWidth;
                height = existing.SourceImageHeight;
            }
        }

        // Sağlama, GERÇEKTEN saklanan görsel URL'iyle hesaplanır: yoksa indirme hatası
        // "değişmemiş" gibi görünür ve görsel bir daha asla denenmez.
        var checksum = string.Equals(imageUrl, cover?.Url, StringComparison.OrdinalIgnoreCase)
            ? candidate
            : NewsChecksum.Compute(post.Title, excerpt, contentHtml, imageUrl, post.CategoryWpIds);

        var snapshot = new NewsArticleSnapshot(
            // 🔴 KOLON SINIRLARI BURADA UYGULANIR ve bu bir dayanıklılık kararıdır:
            // 500 karakteri aşan tek bir başlık, `SaveChanges` anında **bütün partiyi**
            // düşürürdü (hata kayıt başına değil, batch başına doğar) ve o partideki
            // hiçbir haber inmezdi. Kaynak bizim ama içeriğini biz yazmıyoruz — bir
            // yazım hatası yüzünden akışın durması kabul edilemez.
            Title: Limit(post.Title, NewsColumnLimits.Title),
            Excerpt: Limit(excerpt, NewsColumnLimits.Excerpt),
            ContentHtml: contentHtml,
            PlainText: plainText,
            Url: Limit(post.Url, NewsColumnLimits.Url),
            PublishedAtUtc: post.PublishedAtUtc,
            ModifiedAtUtc: post.ModifiedAtUtc,
            Checksum: checksum,
            ImageUrl: Limit(imageUrl, NewsColumnLimits.Url),
            ImageFileId: imageFileId,
            ImageWidth: width,
            ImageHeight: height,
            ReadingMinutes: NewsReadingTime.Minutes(plainText));

        var linked = await ResolveCategoriesAsync(post.CategoryWpIds, categories, run, ct);

        if (existing is null)
        {
            var article = new NewsArticle { WpId = post.WpId };
            article.ApplySourceSnapshot(snapshot, now);
            article.ReplaceCategories(linked);

            await repo.AddAsync(article, ct);
            run.Created++;
        }
        else
        {
            existing.ApplySourceSnapshot(snapshot, now);
            existing.ReplaceCategories(linked);
            run.Updated++;
        }
    }

    // ───────────────────────────── Kategoriler ───────────────────────────────────────

    private async Task<NewsCategoryCache> SyncCategoriesAsync(NewsSyncRun run, CancellationToken ct)
    {
        var repo = _uow.Repository<NewsCategory>();
        var known = new NewsCategoryCache
        {
            Items = await repo.Query(tracking: true).ToDictionaryAsync(x => x.WpId, ct)
        };

        IReadOnlyList<NewsSourceCategory> sourceCategories;
        try
        {
            sourceCategories = await _client.GetCategoriesAsync(ct);
        }
        catch (Exception ex)
        {
            // Kategoriler alınamazsa koşu DEVAM eder: haberler kategorisiz inmez, elimizdeki
            // sözlükle bağlanır. Sözlüğü hiç olmayan bir kurulumda ilk koşu kategorisiz kalır
            // ve bir sonraki koşu düzeltir — haber almamaktan iyidir.
            //
            // 🐛 Ama sayaca YAZILIR (test bulgusu): yazılmasaydı kategorileri alamamış bir koşu
            // panelde tertemiz görünürdü — "0 hata" diyen bir koşu defteri, hiç defter
            // tutmamaktan kötüdür.
            run.Failed++;
            Report(ex, "Haber kategorileri alınamadı");
            return known;
        }

        foreach (var source in sourceCategories)
        {
            if (known.Items.TryGetValue(source.WpId, out var existing))
            {
                if (existing.Name != source.Name || existing.Slug != source.Slug || existing.ArticleCount != source.ArticleCount)
                    existing.ApplySourceSnapshot(source.Name, source.Slug, source.ArticleCount);
                continue;
            }

            var category = new NewsCategory { WpId = source.WpId };
            category.ApplySourceSnapshot(source.Name, source.Slug, source.ArticleCount);
            await repo.AddAsync(category, ct);
            known.Items[source.WpId] = category;
        }

        await _uow.SaveChangesAsync(ct);
        return known;
    }

    /// <summary>
    /// Gönderinin kategori kimliklerini sözlükteki satırlara bağlar.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Tanınmayan kimlik sessizce atılmaz.</b> Kaynakta koşu başladıktan sonra açılan
    /// bir kategori, bağ kurulmadan geçseydi kayıt "kategorisiz" kalır ve sağlaması eşleştiği
    /// için <b>bir daha asla düzelmezdi</b>. Bu yüzden sözlük koşu içinde <b>bir kez</b> tazelenir.
    /// </remarks>
    private async Task<List<NewsCategory>> ResolveCategoriesAsync(
        IReadOnlyList<int> wpIds, NewsCategoryCache categories, NewsSyncRun run, CancellationToken ct)
    {
        var missing = wpIds.Where(id => !categories.Items.ContainsKey(id)).ToList();

        // 🐛 12.12 sonrası denetim bulgusu: burada eskiden bayrak YOKTU ve metodun kendi
        // yorumu "koşu içinde bir kez tazelenir" diyordu — yani yorum yalan söylüyordu.
        // Kaynakta gizlenmiş/silinmiş bir kategori kimliği (public `/categories` yalnız
        // görünenleri döndürür) o kimliği taşıyan HER haber için yeni bir HTTP isteği +
        // SaveChanges tetikliyordu: 50 haber → 50 fazladan istek, 500 → 500.
        if (missing.Count > 0 && !categories.Refreshed)
        {
            categories.Refreshed = true;
            var refreshed = await SyncCategoriesAsync(run, ct);
            foreach (var (key, value) in refreshed.Items)
                categories.Items[key] = value;

            missing = wpIds.Where(id => !categories.Items.ContainsKey(id)).ToList();
        }

        if (missing.Count > 0)
        {
            // Tazelemeden SONRA hâlâ tanınmayan kimlik: kaynakta gizli/silinmiş bir kategori.
            // Sessizce atlanmıyor — haber yine iniyor ama bağ kurulamadığı log'a düşüyor,
            // yoksa "kategorisiz görünen haber"in sebebi hiçbir yerde olmazdı.
            _log.LogWarning(
                "Haber senkronu: sözlükte olmayan kategori kimlikleri atlandı: {Ids}",
                string.Join(", ", missing));
        }

        return wpIds
            .Where(categories.Items.ContainsKey)
            .Select(id => categories.Items[id])
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Koşu boyunca taşınan kategori sözlüğü + <b>"bu koşuda tazelendi mi?"</b> bayrağı.
    /// </summary>
    /// <remarks>
    /// Bayrağın sınıfa değil <b>koşuya</b> ait olması bilinçli: servis scoped olsa da tek bir
    /// scope içinde birden çok koşu çalışabilir (artımlı koşu boş imleçte arşiv koşusuna düşer).
    /// Alan olarak tutulsaydı ikinci koşu sözlüğü hiç tazelemezdi.
    /// </remarks>
    private sealed class NewsCategoryCache
    {
        public Dictionary<int, NewsCategory> Items { get; init; } = new();
        public bool Refreshed { get; set; }
    }

    // ───────────────────────────── Koşu defteri ──────────────────────────────────────

    /// <summary>
    /// İmleç satırını getirir; yoksa açar. <b>Her zaman tek satır</b> (unique indeks).
    /// </summary>
    /// <remarks>
    /// 🐛 12.12 sonrası denetim bulgusu: iki farklı iş (15 dk'lık senkron + 03:00 mutabakatı)
    /// boş durumda aynı anda başlayıp <b>iki satır</b> açabiliyordu ve o andan sonra ileri
    /// imleç koşular arasında zıplıyordu. Kısıt veritabanına kondu; buradaki yakalama ise
    /// yarışı kaybeden koşunun <b>düşmemesi</b> için: satırı rakibi açtıysa onu okur.
    /// </remarks>
    private async Task<NewsSyncState> LoadStateAsync(CancellationToken ct)
    {
        var repo = _uow.Repository<NewsSyncState>();
        var state = await repo.Query(tracking: true).SingleOrDefaultAsync(ct);
        if (state is not null) return state;

        state = new NewsSyncState();
        await repo.AddAsync(state, ct);

        try
        {
            await _uow.SaveChangesAsync(ct);
            return state;
        }
        catch (DbUpdateException)
        {
            // Yarışı kaybettik: satırı diğer iş açtı. Kendi (kaydedilememiş) örneğimizi
            // bırakıp onunkini okuyoruz — iki koşu da aynı imleci paylaşmak zorunda.
            _uow.Repository<NewsSyncState>().Remove(state);
            return await repo.Query(tracking: true).SingleAsync(ct);
        }
    }

    private async Task<NewsSyncRun> StartRunAsync(string mode, string trigger, Guid? triggeredBy, CancellationToken ct)
    {
        var run = new NewsSyncRun
        {
            Mode = mode,
            Trigger = NewsSyncTriggers.All.Contains(trigger) ? trigger : NewsSyncTriggers.Schedule,
            TriggeredBy = triggeredBy,
            StartedAt = DateTime.UtcNow,
            Status = NewsSyncStatuses.Running
        };

        await _uow.Repository<NewsSyncRun>().AddAsync(run, ct);
        await _uow.SaveChangesAsync(ct);
        return run;
    }

    private async Task<NewsSyncOutcome> CompleteRunAsync(NewsSyncRun run, NewsSyncState state, CancellationToken ct)
    {
        run.CompletedAt = DateTime.UtcNow;
        run.Status = NewsSyncStatuses.Completed;

        state.LastRunId = run.Id;
        // ⚠️ "Son başarılı koşu" bayatlık göstergesinin tek kaynağı. Kısmi hatalı bir koşu da
        // başarılıdır: veri akmaya devam ediyor demektir. Tümden düşen koşu buraya yazmaz.
        state.LastSuccessfulRunAt = run.CompletedAt;

        await _uow.SaveChangesAsync(ct);

        if (run.Created + run.Updated + run.MarkedGone + run.Restored > 0)
            await InvalidateAsync(ct);

        _log.LogInformation(
            "Haber senkronu ({Mode}) bitti: {Fetched} okundu, {Created} yeni, {Updated} güncel, {Skipped} atlandı, {Failed} hata.",
            run.Mode, run.Fetched, run.Created, run.Updated, run.Skipped, run.Failed);

        return Outcome(run);
    }

    private async Task<NewsSyncOutcome> FailRunAsync(NewsSyncRun run, Exception ex, CancellationToken ct)
    {
        run.CompletedAt = DateTime.UtcNow;
        run.Status = NewsSyncStatuses.Failed;
        run.ErrorMessage = ex.Message;

        Report(ex, $"Haber senkronu düştü ({run.Mode})");

        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception saveEx)
        {
            // 🔴 Buraya düşmenin TİPİK sebebi, koşuyu düşüren şeyin kendisidir: değişiklik
            // izleyicisinde geçersiz bir varlık varsa `SaveChanges` **yeniden** patlar ve
            // koşu satırı sonsuza kadar "çalışıyor" görünür — panelde (12.13) hiç bitmeyen
            // bir koşu, hiç koşu kaydı tutmamaktan kötüdür.
            // Bu yüzden son çare SQL seviyesinde yazar: `ExecuteUpdate` izleyiciye hiç
            // dokunmaz, dolayısıyla zehirli partiden etkilenmez.
            _log.LogError(saveEx, "Haber senkron koşusu kaydedilemedi — SQL seviyesinde yazılıyor.");

            try
            {
                await _uow.Repository<NewsSyncRun>().Query()
                    .Where(x => x.Id == run.Id)
                    .ExecuteUpdateAsync(u => u
                        .SetProperty(x => x.Status, NewsSyncStatuses.Failed)
                        .SetProperty(x => x.CompletedAt, run.CompletedAt)
                        .SetProperty(x => x.ErrorMessage, run.ErrorMessage), ct);
            }
            catch (Exception fallbackEx)
            {
                _log.LogError(fallbackEx, "Haber senkron koşusu SQL seviyesinde de kaydedilemedi.");
            }
        }

        return Outcome(run);
    }

    private async Task InvalidateAsync(CancellationToken ct)
    {
        try
        {
            await _cache.InvalidateGroupsAsync(new[] { CacheGroups.News }, ct);
        }
        catch (Exception ex)
        {
            // Fail-open: önbellek temizlenemediyse veri en geç TTL kadar (15 dk) bayat kalır;
            // senkronu düşürmek çok daha pahalı olurdu.
            _log.LogWarning(ex, "Haber önbelleği temizlenemedi.");
        }
    }

    private static NewsSyncOutcome Outcome(NewsSyncRun run) => new(
        run.Id, run.Mode, run.Status, run.Fetched, run.Created, run.Updated,
        run.Skipped, run.Failed, run.MarkedGone, run.Restored, run.ErrorMessage);

    /// <summary>
    /// Hatayı <c>error_logs</c>'a düşürür (12.1). <c>ErrorFingerprint</c> tekilleştirmesi
    /// bedava gelir: WP 20 dakika boyunca 502 verirse 300 satır değil <b>1 satır +
    /// OccurrenceCount</b> (§7 madde 32). ⚠️ Yazma yolu koşuyu düşüremez (§7 madde 31).
    /// </summary>
    private void Report(Exception ex, string message)
    {
        _log.LogWarning(ex, "{Message}", message);

        _errors.TryWrite(new ErrorLogEntry(
            Source: ErrorLogSources.Api,
            Level: ErrorLogLevels.Error,
            Code: "NEWS_SYNC",
            Message: $"{message}: {ex.Message}",
            StackTrace: ex.StackTrace,
            Path: "job/news-sync"));
    }

    /// <summary>Kolon tavanına kırpar (<c>null</c> güvenli). Kırpma sessiz değil: sonuna "…" konur.</summary>
    private static string? Limit(string? value, int max)
    {
        if (value is null) return null;
        return value.Length <= max ? value : value[..(max - 1)] + "…";
    }

    private static DateTime? Later(DateTime? a, DateTime? b) =>
        a is null ? b : b is null ? a : a > b ? a : b;

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Özet yoksa gövdeden üretilir — WP <c>excerpt</c>'i bazen tümüyle boş geliyor.</summary>
    private static string? Summarize(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText)) return null;
        var text = plainText.Trim();
        return text.Length <= 280 ? text : text[..277].TrimEnd() + "…";
    }
}
