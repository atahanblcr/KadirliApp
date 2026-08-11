using System.Globalization;
using System.Net;
using System.Text.Json;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.News;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.News;

/// <summary>
/// Faz 12.12 — WordPress REST API istemcisi (<see cref="INewsSourceClient"/>).
///
/// 🔴 <b>Kaynak KARARSIZ ve bu ölçülmüş bir gerçek:</b> 400 haberlik örnekleme sırasında bir
/// sayfa <c>error code: 520</c> döndürdü. Bu yüzden istemci üstel geri çekilmeli üç deneme
/// yapar; başarısız olursa <b>fırlatır</b> ve sayfayı çağıran koşu onu <c>Failed</c> olarak
/// sayıp devam eder (§7 madde 29'un kuralı).
///
/// 📌 <c>User-Agent</c> açıkça set edilir (<c>KadirliApp-Sync/1.0</c>): kaynak tarafında
/// tanınabilir olalım — bir gün trafiğimiz sorulursa cevabı olsun.
/// </summary>
public class WordPressNewsSourceClient : INewsSourceClient
{
    public const string HttpClientName = "news-source";

    /// <summary>
    /// ⚠️ <c>_fields</c> listesi <b>kontrattır</b>: <c>modified_gmt</c>/<c>date_gmt</c> buradan
    /// düşerse damgalar 3 saat ileri kayar ve haberler <b>gelecekten</b> görünür
    /// (<c>WordPressTimeWindow.NormalizeToUtc</c> yedek yola düşer).
    /// </summary>
    private const string PostFields =
        "id,date,date_gmt,modified,modified_gmt,link,title,excerpt,content,categories,_links";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WordPressNewsSourceClient> _log;
    private readonly string _baseUrl;
    private readonly int _maxAttempts;

    public WordPressNewsSourceClient(
        IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<WordPressNewsSourceClient> log)
    {
        _httpClientFactory = httpClientFactory;
        _log = log;
        _baseUrl = (configuration["News:Source:BaseUrl"] ?? "https://www.silagazetesi.com.tr/wp-json/wp/v2")
            .TrimEnd('/');
        _maxAttempts = configuration.GetValue("News:Source:MaxAttempts", 3);
    }

    public async Task<NewsSourcePage> GetPostsModifiedSinceAsync(
        DateTime siteLocalSince, int page, int perPage, CancellationToken ct)
    {
        // 🔴 modified_after SİTE-YEREL saatle karşılaştırılıyor (ölçüldü). Damgayı burada
        // üretmiyoruz — dönüşümün tek sahibi WordPressTimeWindow.
        var url = $"{_baseUrl}/posts?_embed=wp:featuredmedia&_fields={PostFields},_embedded" +
                  $"&per_page={perPage}&page={page}&orderby=modified&order=asc" +
                  $"&modified_after={Uri.EscapeDataString(WordPressTimeWindow.Format(siteLocalSince))}";

        return await GetPageAsync(url, ct);
    }

    public async Task<NewsSourcePage> GetPostsByDateDescendingAsync(
        DateTime? beforeSiteLocal, int perPage, CancellationToken ct)
    {
        var url = $"{_baseUrl}/posts?_embed=wp:featuredmedia&_fields={PostFields},_embedded" +
                  $"&per_page={perPage}&page=1&orderby=date&order=desc";

        if (beforeSiteLocal.HasValue)
            url += $"&before={Uri.EscapeDataString(WordPressTimeWindow.Format(beforeSiteLocal.Value))}";

        return await GetPageAsync(url, ct);
    }

    public async Task<IReadOnlyList<NewsSourceCategory>> GetCategoriesAsync(CancellationToken ct)
    {
        var url = $"{_baseUrl}/categories?per_page=100&_fields=id,name,slug,count";
        using var response = await SendAsync(url, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        using var document = JsonDocument.Parse(json);
        var categories = new List<NewsSourceCategory>();

        foreach (var element in document.RootElement.EnumerateArray())
        {
            var id = element.GetProperty("id").GetInt32();
            var name = WebUtility.HtmlDecode(GetString(element, "name") ?? $"Kategori {id}")!;
            var slug = GetString(element, "slug") ?? id.ToString(CultureInfo.InvariantCulture);
            var count = element.TryGetProperty("count", out var c) ? c.GetInt32() : 0;

            categories.Add(new NewsSourceCategory(id, name, slug, count));
        }

        return categories;
    }

    public async Task<NewsSourceIdWindow> GetPublishedIdWindowAsync(int maxPosts, CancellationToken ct)
    {
        var ids = new List<int>();
        DateTime? oldest = null;

        var perPage = Math.Clamp(maxPosts, 1, 100);
        var remaining = maxPosts;
        DateTime? before = null;

        // Sayfa numarası yerine tarih imleci: koşu sırasında yeni bir haber yayınlanırsa
        // sayfa numaraları kayar ve tam sınırdaki kimlik listeden düşerdi — o kimlik bizde
        // varsa "kaynakta yok" sanılıp `gone` işaretlenirdi. Sessiz ve yanlış.
        while (remaining > 0)
        {
            var take = Math.Min(perPage, remaining);
            var url = $"{_baseUrl}/posts?_fields=id,date_gmt&per_page={take}&page=1&orderby=date&order=desc";
            if (before.HasValue)
                url += $"&before={Uri.EscapeDataString(WordPressTimeWindow.Format(WordPressTimeWindow.ToSiteLocal(before.Value)))}";

            using var response = await SendAsync(url, ct);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

            var count = 0;
            foreach (var element in document.RootElement.EnumerateArray())
            {
                ids.Add(element.GetProperty("id").GetInt32());
                var published = WordPressTimeWindow.NormalizeToUtc(GetDate(element, "date_gmt"), null);
                if (oldest is null || published < oldest) oldest = published;
                count++;
            }

            if (count == 0) break;
            if (before.HasValue && oldest >= before) break; // ilerleme yok → sonsuz döngüyü kes

            before = oldest;
            remaining -= count;
        }

        return new NewsSourceIdWindow(ids, oldest);
    }

    // ───────────────────────────── HTTP ──────────────────────────────────────────────

    private async Task<NewsSourcePage> GetPageAsync(string url, CancellationToken ct)
    {
        using var response = await SendAsync(url, ct);

        var totalPages = ReadHeaderInt(response, "X-WP-TotalPages", 1);
        var totalCount = ReadHeaderInt(response, "X-WP-Total", 0);

        var json = await response.Content.ReadAsStringAsync(ct);
        using var document = JsonDocument.Parse(json);

        var posts = new List<NewsSourcePost>();
        foreach (var element in document.RootElement.EnumerateArray())
            posts.Add(ReadPost(element));

        return new NewsSourcePage(posts, totalPages, totalCount);
    }

    private async Task<HttpResponseMessage> SendAsync(string url, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        Exception? last = null;

        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            try
            {
                var response = await client.GetAsync(url, ct);
                if (response.IsSuccessStatusCode) return response;

                // 4xx'te yeniden denemek anlamsız (yanlış parametre, silinmiş kayıt);
                // 5xx ve 429 geçici sayılır — kaynakta canlı olarak 520 görüldü.
                var retryable = (int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.TooManyRequests;
                var body = await response.Content.ReadAsStringAsync(ct);
                response.Dispose();

                last = new HttpRequestException(
                    $"Haber kaynağı {(int)response.StatusCode} döndü: {Truncate(body, 200)}");

                if (!retryable) break;
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                last = ex;
            }

            if (attempt < _maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 2sn, 4sn
                _log.LogWarning(last, "Haber kaynağı denemesi {Attempt} başarısız — {Delay} sonra tekrar.", attempt, delay);
                await Task.Delay(delay, ct);
            }
        }

        throw last ?? new HttpRequestException("Haber kaynağına ulaşılamadı.");
    }

    // ───────────────────────────── Ayrıştırma ────────────────────────────────────────

    private static NewsSourcePost ReadPost(JsonElement element)
    {
        var id = element.GetProperty("id").GetInt32();

        // WordPress başlıkları HTML varlığı olarak veriyor ("Osmaniye&#8217;de").
        // Çözülmezse başlık ekranda ham varlık kodlarıyla görünür.
        var title = WebUtility.HtmlDecode(GetRendered(element, "title") ?? string.Empty) ?? string.Empty;

        var published = WordPressTimeWindow.NormalizeToUtc(GetDate(element, "date_gmt"), GetDate(element, "date"));
        var modified = WordPressTimeWindow.NormalizeToUtc(GetDate(element, "modified_gmt"), GetDate(element, "modified"));

        var categories = new List<int>();
        if (element.TryGetProperty("categories", out var cats) && cats.ValueKind == JsonValueKind.Array)
            foreach (var c in cats.EnumerateArray())
                categories.Add(c.GetInt32());

        return new NewsSourcePost(
            WpId: id,
            Title: title,
            ExcerptHtml: GetRendered(element, "excerpt"),
            ContentHtml: GetRendered(element, "content") ?? string.Empty,
            Url: GetString(element, "link") ?? string.Empty,
            PublishedAtUtc: published,
            ModifiedAtUtc: modified,
            CategoryWpIds: categories,
            ImageSizes: ReadImageSizes(element));
    }

    /// <summary>
    /// <c>_embedded["wp:featuredmedia"][0].media_details.sizes</c> — öne çıkan görselin boyutları.
    /// </summary>
    /// <remarks>
    /// Hangi boyutun seçileceği burada değil <c>NewsImagePicker</c>'da kararlaştırılır:
    /// zincirin kuralı ürün kararıdır, HTTP ayrıntısı değil.
    /// </remarks>
    private static IReadOnlyDictionary<string, NewsSourceImage> ReadImageSizes(JsonElement element)
    {
        var sizes = new Dictionary<string, NewsSourceImage>(StringComparer.OrdinalIgnoreCase);

        if (!element.TryGetProperty("_embedded", out var embedded)) return sizes;
        if (!embedded.TryGetProperty("wp:featuredmedia", out var media) || media.ValueKind != JsonValueKind.Array) return sizes;

        foreach (var item in media.EnumerateArray())
        {
            if (!item.TryGetProperty("media_details", out var details)) continue;

            if (details.TryGetProperty("sizes", out var sizeMap) && sizeMap.ValueKind == JsonValueKind.Object)
            {
                foreach (var size in sizeMap.EnumerateObject())
                {
                    var url = GetString(size.Value, "source_url");
                    if (string.IsNullOrWhiteSpace(url)) continue;

                    sizes[size.Name] = new NewsSourceImage(
                        url,
                        size.Value.TryGetProperty("width", out var w) && w.ValueKind == JsonValueKind.Number ? w.GetInt32() : null,
                        size.Value.TryGetProperty("height", out var h) && h.ValueKind == JsonValueKind.Number ? h.GetInt32() : null);
                }
            }

            // Boyut haritası hiç yoksa ham `source_url` son çare olarak "full" sayılır —
            // yoksa görseli olan bir haber görselsiz inerdi.
            if (sizes.Count == 0)
            {
                var raw = GetString(item, "source_url");
                if (!string.IsNullOrWhiteSpace(raw))
                    sizes["full"] = new NewsSourceImage(raw, null, null);
            }

            break; // yalnız ilk öne çıkan görsel
        }

        return sizes;
    }

    private static string? GetRendered(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Object
            ? GetString(value, "rendered")
            : null;

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static DateTime? GetDate(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) &&
        value.ValueKind == JsonValueKind.String &&
        DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var parsed)
            ? parsed
            : null;

    private static int ReadHeaderInt(HttpResponseMessage response, string header, int fallback) =>
        response.Headers.TryGetValues(header, out var values) &&
        int.TryParse(values.FirstOrDefault(), out var parsed)
            ? parsed
            : fallback;

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
