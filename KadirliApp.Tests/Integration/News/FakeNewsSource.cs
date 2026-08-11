using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.News;

namespace KadirliApp.Tests.Integration.News;

/// <summary>
/// Faz 12.12 — kaynağın sahtesi. <b>Ağ yok</b>: senkronun bütün davranış iddiaları
/// (idempotentlik, override'ın korunması, mutabakat, hata toleransı) gerçek Postgres
/// üzerinde ama <b>gerçek WordPress olmadan</b> koşuyor.
/// </summary>
/// <remarks>
/// 🔑 Bu sınıfın var olma sebebi katman kuralının somut faydası: <c>INewsSourceClient</c>
/// Application'da tanımlı olduğu için HTTP tarafını değiştirmeden alım mantığı denenebiliyor.
/// Aksi hâlde bu testlerin hepsi canlı siteye bağımlı olurdu — yani kaynağın kesintisinde
/// <b>bizim testlerimiz</b> kırmızıya dönerdi.
/// </remarks>
public class FakeNewsSource : INewsSourceClient
{
    public List<NewsSourcePost> Posts { get; } = new();
    public List<NewsSourceCategory> Categories { get; } = new();

    /// <summary>Kaç kez sayfa istendi (mükerrer çekişi görmek için).</summary>
    public int PostRequests { get; private set; }

    /// <summary>
    /// Sıradaki <b>gönderi</b> isteğini patlat — "bir sayfanın hatası koşuyu düşürmez" iddiası için.
    /// </summary>
    /// <remarks>
    /// 🐛 İlk yazımda tek bir <c>FailNextRequest</c> vardı ve testte <b>kategori</b> isteğini
    /// vuruyordu: gönderi sayfası başarıyla gelip imleç ilerliyordu, yani test niyet ettiği
    /// şeyi hiç denemiyordu. Hangi isteğin patladığı iddianın parçası.
    /// </remarks>
    public bool FailNextPostRequest { get; set; }

    /// <summary>Sıradaki kategori isteğini patlat.</summary>
    public bool FailNextCategoryRequest { get; set; }

    /// <summary>Mutabakatta kaynak boş liste döndürsün (en tehlikeli senaryo).</summary>
    public bool ReturnNoIds { get; set; }

    public Task<NewsSourcePage> GetPostsModifiedSinceAsync(
        DateTime siteLocalSince, int page, int perPage, CancellationToken ct)
    {
        ThrowIfPostFailureRequested();
        PostRequests++;

        // Sahte de olsa SEMANTİK gerçek: `modified_after` site-yerel karşılaştırılır.
        var sinceUtc = WordPressTimeWindow.ToUtc(siteLocalSince);

        var matching = Posts
            .Where(p => p.ModifiedAtUtc > sinceUtc)
            .OrderBy(p => p.ModifiedAtUtc)
            .ToList();

        var pageItems = matching.Skip((page - 1) * perPage).Take(perPage).ToList();
        var totalPages = Math.Max(1, (int)Math.Ceiling(matching.Count / (double)perPage));

        return Task.FromResult(new NewsSourcePage(pageItems, totalPages, matching.Count));
    }

    public Task<NewsSourcePage> GetPostsByDateDescendingAsync(
        DateTime? beforeSiteLocal, int perPage, CancellationToken ct)
    {
        ThrowIfPostFailureRequested();
        PostRequests++;

        var beforeUtc = beforeSiteLocal.HasValue ? WordPressTimeWindow.ToUtc(beforeSiteLocal.Value) : (DateTime?)null;

        var matching = Posts
            .Where(p => beforeUtc is null || p.PublishedAtUtc < beforeUtc)
            .OrderByDescending(p => p.PublishedAtUtc)
            .Take(perPage)
            .ToList();

        return Task.FromResult(new NewsSourcePage(matching, 1, matching.Count));
    }

    public Task<IReadOnlyList<NewsSourceCategory>> GetCategoriesAsync(CancellationToken ct)
    {
        if (FailNextCategoryRequest)
        {
            FailNextCategoryRequest = false;
            throw new HttpRequestException("Haber kaynağı kategori isteğinde 520 döndü (sahte).");
        }

        return Task.FromResult<IReadOnlyList<NewsSourceCategory>>(Categories);
    }

    public Task<NewsSourceIdWindow> GetPublishedIdWindowAsync(int maxPosts, CancellationToken ct)
    {
        ThrowIfPostFailureRequested();

        if (ReturnNoIds)
            return Task.FromResult(new NewsSourceIdWindow(Array.Empty<int>(), null));

        var window = Posts.OrderByDescending(p => p.PublishedAtUtc).Take(maxPosts).ToList();

        return Task.FromResult(new NewsSourceIdWindow(
            window.Select(p => p.WpId).ToList(),
            window.Count == 0 ? null : window.Min(p => p.PublishedAtUtc)));
    }

    private void ThrowIfPostFailureRequested()
    {
        if (!FailNextPostRequest) return;
        FailNextPostRequest = false;
        throw new HttpRequestException("Haber kaynağı 520 döndü (sahte).");
    }
}

/// <summary>Görsel indiricisinin sahtesi — baytları uydurur, ağa çıkmaz.</summary>
public class FakeNewsImageDownloader : INewsImageDownloader
{
    public int Downloads { get; private set; }

    /// <summary>İndirme başarısız olsun (görselsiz haber yine inmeli).</summary>
    public bool Fail { get; set; }

    public Task<NewsImageDownload?> TryDownloadAsync(string url, CancellationToken ct)
    {
        if (Fail) return Task.FromResult<NewsImageDownload?>(null);

        Downloads++;
        return Task.FromResult<NewsImageDownload?>(
            new NewsImageDownload(new byte[] { 1, 2, 3, 4 }, "image/webp", Path.GetFileName(new Uri(url).LocalPath)));
    }
}
