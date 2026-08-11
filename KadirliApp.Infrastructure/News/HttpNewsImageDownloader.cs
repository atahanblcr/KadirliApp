using System.Net;
using System.Net.Sockets;
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
///   <item><b>İç ağ kapısı</b> (12.13, denetim bulgusu 10) — aşağıda.</item>
/// </list>
///
/// 🔴 <b>İç ağ / yönlendirme kapısı.</b> 12.12'de indirici yönlendirmeleri <b>otomatik</b>
/// takip ediyordu ve hedefin nereye çıktığını hiç sorgulamıyordu. Kaynak bizim ama tam da
/// bu yüzden "doğrulanmış" sayılamaz: <c>source_url</c> kaynağın veritabanından geliyor ve
/// kaynak bir gün ele geçirilirse o adres <c>169.254.169.254</c> (bulut metadata servisi),
/// <c>127.0.0.1:5005</c> (kendi API'miz) ya da yerel ağdaki bir yönetim arayüzü olabilir —
/// klasik <b>SSRF</b>. Sonucun <c>image/*</c> olması gerektiği doğru ama <b>yeterli değil</b>:
/// istek atılmış olur, iç servis onu görür ve yanıt bir hata mesajı olarak bize döner.
/// Bu yüzden <b>her sıçrama</b> (ilk istek + her yönlendirme) ayrı ayrı denetlenir ve
/// yönlendirme takibi <b>elle</b> yapılır — otomatik takipte ara adımları göremezdik.
/// ⚠️ Kapı <b>çözümlenmiş IP'ye</b> bakar, ada değil: <c>metadata.example.com</c> pekâlâ
/// <c>169.254.169.254</c>'e çözülebilir.
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

            using var response = await GetFollowingSafeRedirectsAsync(client, uri, ct);
            if (response is null) return null;

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

            // Uzantı, yönlendirme sonrası GERÇEK adresten türetilir: kaynak sık sık
            // `…/wp-content/uploads/x.jpg`'e yönlendiren bir CDN adresi verebiliyor.
            var fileName = BuildFileName(response.RequestMessage?.RequestUri ?? uri, contentType);
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

    /// <summary>Yönlendirmeleri <b>elle</b> takip eder ve <b>her sıçramayı</b> denetler.</summary>
    /// <remarks>
    /// ⚠️ Otomatik takip (varsayılan) bu denetimi imkânsız kılar: <c>HttpClient</c> ara
    /// adımları göstermez, elimize yalnız son yanıt geçer — yani iç ağa atılmış istek çoktan
    /// atılmış olur. Sınır <b>3 sıçrama</b>: gerçek bir CDN zincirine yeter, döngüye girmez.
    /// </remarks>
    private async Task<HttpResponseMessage?> GetFollowingSafeRedirectsAsync(
        HttpClient client, Uri uri, CancellationToken ct)
    {
        const int maxHops = 3;
        var current = uri;

        for (var hop = 0; hop <= maxHops; hop++)
        {
            if (!await IsPublicallyRoutableAsync(current, ct))
            {
                _log.LogWarning("Haber görseli iç ağ adresine çıkıyor, indirilmedi: {Url}", current);
                return null;
            }

            var response = await client.GetAsync(current, HttpCompletionOption.ResponseHeadersRead, ct);

            var location = response.Headers.Location;
            var isRedirect = (int)response.StatusCode is >= 300 and < 400 && location is not null;
            if (!isRedirect) return response;

            // Göreli `Location` da olabilir; mutlaklaştırıp bir sonraki turda yeniden denetlenir.
            var next = location!.IsAbsoluteUri ? location : new Uri(current, location);
            response.Dispose();

            if (next.Scheme != Uri.UriSchemeHttp && next.Scheme != Uri.UriSchemeHttps)
            {
                _log.LogWarning("Haber görseli desteklenmeyen şemaya yönlendirdi: {Url}", next);
                return null;
            }

            current = next;
        }

        _log.LogWarning("Haber görseli çok fazla yönlendirdi: {Url}", uri);
        return null;
    }

    /// <summary>Adres <b>çözümlenip</b> özel/iç ağ aralıklarına düşüyor mu?</summary>
    /// <remarks>
    /// 🔑 Karar <b>"hiçbiri değilse geç"</b> değil, <b>"biri bile öyleyse geçme"</b>: bir ad
    /// hem genel hem iç bir adrese çözülüyorsa (DNS rebinding) hangisine bağlanacağımızı
    /// biz seçmiyoruz.
    /// ⚠️ Çözümleme başarısızsa da geçilmez — bilinmeyen bir hedefe istek atmanın hiçbir
    /// faydası yok; görselsiz bir haber zaten kabul edilebilir (indirici asla fırlatmaz).
    /// 📌 Kapı <c>Uri.IsLoopback</c> ile yetinmez: asıl hedef <c>169.254.169.254</c> gibi
    /// <b>link-local</b> metadata adresleri ve RFC1918 aralıkları.
    /// </remarks>
    private static async Task<bool> IsPublicallyRoutableAsync(Uri uri, CancellationToken ct)
    {
        IPAddress[] addresses;

        if (IPAddress.TryParse(uri.IdnHost, out var literal))
        {
            addresses = new[] { literal };
        }
        else
        {
            try
            {
                addresses = await Dns.GetHostAddressesAsync(uri.IdnHost, ct);
            }
            catch (Exception)
            {
                return false;
            }
        }

        return addresses.Length > 0 && !addresses.Any(IsPrivate);
    }

    private static bool IsPrivate(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)) return true;

        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return b[0] switch
            {
                0 => true,                                   // "bu ağ"
                10 => true,                                  // RFC1918
                127 => true,                                 // loopback
                169 when b[1] == 254 => true,                // 🔴 link-local — bulut metadata
                172 when b[1] >= 16 && b[1] <= 31 => true,   // RFC1918
                192 when b[1] == 168 => true,                // RFC1918
                100 when b[1] >= 64 && b[1] <= 127 => true,  // CGNAT (RFC6598)
                >= 224 => true,                              // multicast + ayrılmış
                _ => false
            };
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast) return true;
            // fc00::/7 — benzersiz yerel adresler (IPv6'nın RFC1918'i).
            return (address.GetAddressBytes()[0] & 0xFE) == 0xFC;
        }

        return true;
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
