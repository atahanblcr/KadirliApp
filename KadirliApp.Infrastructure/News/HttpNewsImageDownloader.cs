using KadirliApp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.News;

/// <summary>
/// Faz 12.12 — kapak görselinin indiricisi.
///
/// 🔴 <b>Sözleşme: asla fırlatmaz, sınırları kendisi uygular.</b> Kaynak bizim olsa da
/// doğrulanmamış bir indiriciyi sınırsız bırakmak yanlış: bir yapılandırma hatası ya da
/// kaynağın hacklenmesi hâlinde 2 GB'lık bir "görsel" sunucunun diskini doldurabilir.
/// <list type="bullet">
///   <item><b>Boyut tavanı</b> (varsayılan 2 MB) — hem <c>Content-Length</c>'e hem de
///         gerçekten okunan bayta bakılır: başlık yalan söyleyebilir.</item>
///   <item><b><c>Content-Type</c> denetimi</b> — yalnız <c>image/*</c>.</item>
///   <item><b>Zaman aşımı</b> — adlandırılmış istemcide (30 sn).</item>
/// </list>
/// </summary>
public class HttpNewsImageDownloader : INewsImageDownloader
{
    public const string HttpClientName = "news-images";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpNewsImageDownloader> _log;
    private readonly long _maxBytes;

    public HttpNewsImageDownloader(
        IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<HttpNewsImageDownloader> log)
    {
        _httpClientFactory = httpClientFactory;
        _log = log;
        _maxBytes = configuration.GetValue("News:Images:MaxBytes", 2L * 1024 * 1024);
    }

    public async Task<NewsImageDownload?> TryDownloadAsync(string url, CancellationToken ct)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);

            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                _log.LogWarning("Haber görseli {Status} döndü: {Url}", (int)response.StatusCode, url);
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType is null || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                _log.LogWarning("Haber görseli beklenen tipte değil ({ContentType}): {Url}", contentType, url);
                return null;
            }

            if (response.Content.Headers.ContentLength is { } declared && declared > _maxBytes)
            {
                _log.LogWarning("Haber görseli çok büyük ({Bytes} bayt): {Url}", declared, url);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var buffer = new MemoryStream();

            var chunk = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(chunk, ct)) > 0)
            {
                buffer.Write(chunk, 0, read);

                // Başlık yalan söylemiş olabilir — gerçek bayt sayısı da denetlenir.
                if (buffer.Length > _maxBytes)
                {
                    _log.LogWarning("Haber görseli tavanı aştı (indirme kesildi): {Url}", url);
                    return null;
                }
            }

            var fileName = BuildFileName(uri, contentType);
            return new NewsImageDownload(buffer.ToArray(), contentType, fileName);
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Görsel indirilemedi = haber yine iner. Bu yolun sessiz kalması bilinçli değil:
            // çağıran (NewsImageMirror) uyarıyı log'a yazıyor.
            _log.LogWarning(ex, "Haber görseli indirilemedi: {Url}", url);
            return null;
        }
    }

    /// <summary>
    /// Dosya adı kaynaktan alınır ama <b>yalnız adı</b> (<c>Path.GetFileName</c>) — yol
    /// bileşenleri depolamaya taşınmamalı (10.8'deki path traversal düzeltmesinin dersi).
    /// </summary>
    private static string BuildFileName(Uri uri, string contentType)
    {
        var name = Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrWhiteSpace(name)) name = "news-image";

        if (!Path.HasExtension(name))
        {
            var extension = contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => ".img"
            };
            name += extension;
        }

        return name;
    }
}
