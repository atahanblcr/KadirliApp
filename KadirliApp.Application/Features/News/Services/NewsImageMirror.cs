using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using File = KadirliApp.Domain.Entities.File;

namespace KadirliApp.Application.Features.News.Services;

/// <summary>
/// Faz 12.12 — kapak görselinin <b>aynalanması</b> (kullanıcı kararı: görseller indirilir).
///
/// 🔴 <b>Neden hotlink değil ayna:</b>
/// <list type="bullet">
///   <item>Kaynak kararsız — örnekleme sırasında canlıda <c>error code: 520</c> görüldü.</item>
///   <item>Panelde WordPress görselini basmak <b>CSP'ye takılır</b> (§7 madde 51: panel dış
///         origine bağlanamaz) — yönetici boş kutu görürdü.</item>
///   <item>Aynalayınca uçlar <b>göreli</b> URL döndürür (<c>/uploads/…</c>) → §7 madde 9
///         bedavaya korunur ve mobilin <c>AppImage.url</c>'ü zaten doğru davranır.</item>
/// </list>
///
/// 🔑 <b>Tekilleştirme:</b> aynı görsel birden çok haberde geçebilir. Kaynaktaki URL bir
/// haberde zaten aynalanmışsa <b>o dosya yeniden kullanılır</b>; yoksa <c>uploads/</c>
/// mükerrer dosyayla şişer ve kimse fark etmez (10.14/(3)'ün "sorun yıllar sonra fark edilir"
/// dersi). Koşu <b>içi</b> tekilleştirme ayrıca bellekte yapılır: aynı koşuda iki haber aynı
/// görseli isterse ikincisi henüz veritabanına yazılmamış olan ilkini göremezdi.
/// </summary>
public class NewsImageMirror
{
    private readonly IUnitOfWork _uow;
    private readonly IFileStorageService _storage;
    private readonly INewsImageDownloader _downloader;
    private readonly ILogger<NewsImageMirror> _log;

    /// <summary>Koşu içi ayna: kaynak URL → <c>files</c> satırı (yeni ya da var olan).</summary>
    /// <remarks>
    /// ⚠️ <c>Guid</c> değil <b>varlık</b> tutuluyor ve sebebi bir denetim bulgusu (7): yeni
    /// dosya artık <b>partiyle birlikte</b> kaydediliyor, yani kimliği o an henüz yok
    /// (<c>Id</c> kolonu <c>gen_random_uuid()</c> ile <i>store-generated</i>). Kimlik
    /// saklansaydı aynı koşudaki ikinci haber <c>Guid.Empty</c>'ye bağlanırdı — 12.2b'de
    /// canlıda yaşanan FK tuzağının birebir aynısı.
    /// </remarks>
    private readonly Dictionary<string, File> _mirroredInThisRun = new(StringComparer.OrdinalIgnoreCase);

    public NewsImageMirror(
        IUnitOfWork uow,
        IFileStorageService storage,
        INewsImageDownloader downloader,
        ILogger<NewsImageMirror> log)
    {
        _uow = uow;
        _storage = storage;
        _downloader = downloader;
        _log = log;
    }

    /// <summary>
    /// Faz 12.14 — aynalanmış görselin <b>göreli adresi</b> (<c>/uploads/…</c>), metin arası
    /// görselleri gövdede yeniden yazmak için. Başarısızlıkta <c>null</c>.
    /// </summary>
    /// <remarks>
    /// 🔑 Kapak görselinden farklı olarak burada <b>varlık gerekmiyor</b>: gövdeye yalnız bir
    /// URL yazılıyor, kurulan bir FK yok. Bu yüzden 12.2b'nin "aynı <c>SaveChanges</c>
    /// içindeki FK'yı gezinme özelliğinden kur" tuzağı bu yolda <b>hiç doğmuyor</b>.
    /// </remarks>
    public async Task<string?> MirrorToUrlAsync(string? sourceUrl, CancellationToken ct)
    {
        var file = await MirrorAsync(sourceUrl, ct);
        var url = file?.CdnUrl ?? file?.StoragePath;
        return string.IsNullOrWhiteSpace(url) ? null : url;
    }

    /// <summary>
    /// Görseli aynalar ve <c>files</c> satırını döner. <b>Başarısızlıkta <c>null</c> döner,
    /// fırlatmaz</b> — görselsiz bir haber, hiç inmemiş bir haberden iyidir.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Dönen satır HENÜZ KAYDEDİLMİŞ OLMAYABİLİR</b> ve bu bilinçli (12.12 sonrası
    /// denetim, bulgu 7). İlk yazımda bu metot <b>paylaşılan</b> <c>IUnitOfWork</c> üzerinde
    /// <c>SaveChanges</c> çağırıyordu; iki yan etkisi vardı:
    /// <list type="number">
    ///   <item>Partinin yarısı erken commit ediliyordu — "parti" semantiği (ve onun üstüne
    ///         kurulu kolon tavanı dersi) sessizce bozuluyordu.</item>
    ///   <item>🐛 O <c>SaveChanges</c> <b>başka bir varlığın</b> hatasıyla patladığında hata
    ///         buradaki <c>catch</c>'e düşüyor ve <i>"Haber görseli kaydedilemedi"</i> diye
    ///         <b>yanlış</b> loglanıyordu — arızayı arayan insanı yanlış yere gönderen bir iz.</item>
    /// </list>
    /// Artık satır yalnız <c>Add</c> ediliyor; kaydı çağıranın parti <c>SaveChanges</c>'i yazıyor.
    /// ⚠️ Bu yüzden çağıran, kaydı <b>FK skaleri ile değil gezinme özelliği ile</b> bağlamak
    /// zorunda (<c>NewsArticleSnapshot.ImageFile</c>): <c>Id</c> store-generated olduğu için
    /// o an hâlâ <c>Guid.Empty</c>'dir (12.2b'nin canlı FK tuzağı).
    /// </remarks>
    public async Task<File?> MirrorAsync(string? sourceUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl)) return null;

        if (_mirroredInThisRun.TryGetValue(sourceUrl, out var cached)) return cached;

        // Faz 12.14 — metin arası görseller `news_articles.source_image_url`'de GÖRÜNMEZ
        // (o kolon yalnız kapağı tanır). İkinci bir kapı olmasaydı aynı gövde görseli her
        // koşuda yeniden inip `uploads/`'u mükerrer dosyayla şişirirdi — ve bu, "sorun
        // yıllar sonra fark edilir" sınıfının ta kendisi.
        // ⚠️ Eşleştirme BELLEKTE yapılıyor: `files.metadata` **jsonb** ve LINQ'te ona
        // `Contains`/`==` yazmak sağlayıcı seviyesinde tuzaklı (ARCHITECTURE §8'in
        // `audit_logs.details` dersi). Tek seferlik, dar bir sorgu daha okunaklı ve
        // davranışı sürprizsiz.
        var alreadyMirrored = await FindMirroredFileAsync(sourceUrl, ct);
        if (alreadyMirrored is not null)
        {
            _mirroredInThisRun[sourceUrl] = alreadyMirrored;
            return alreadyMirrored;
        }

        var existingId = await _uow.Repository<NewsArticle>().Query()
            .Where(x => x.SourceImageUrl == sourceUrl && x.SourceImageFileId != null)
            .Select(x => x.SourceImageFileId)
            .FirstOrDefaultAsync(ct);

        if (existingId is not null)
        {
            // Kimlik değil varlık gerekiyor: bağ gezinme özelliğinden kuruluyor.
            var known = await _uow.Repository<File>().Query(tracking: true)
                .FirstOrDefaultAsync(f => f.Id == existingId.Value, ct);

            if (known is not null)
            {
                _mirroredInThisRun[sourceUrl] = known;
                return known;
            }
            // Dosya silinmişse (soft-delete) aşağıya düşüp yeniden indiriyoruz.
        }

        var download = await _downloader.TryDownloadAsync(sourceUrl, ct);
        if (download is null)
        {
            // Sessiz değil: sayaç tutulmuyor ama iz kalıyor. Görsel indirilemeyen haber
            // yine iner — bu yolun "haberi düşürmemesi" bilinçli bir karar.
            _log.LogWarning("Haber görseli aynalanamadı: {Url}", sourceUrl);
            return null;
        }

        try
        {
            using var stream = new MemoryStream(download.Content);
            var storedUrl = await _storage.UploadFileAsync(stream, download.FileName, download.ContentType, ct);

            var file = new File
            {
                OriginalName = download.FileName,
                FileName = Path.GetFileName(storedUrl),
                MimeType = download.ContentType,
                SizeBytes = download.Content.LongLength,
                StoragePath = storedUrl,
                CdnUrl = storedUrl,
                ModuleType = "news",
                // Kaynağın izi kaybolmasın: "bu dosya nereden geldi?" sorusunun cevabı.
                Metadata = System.Text.Json.JsonSerializer.Serialize(new { sourceUrl })
            };

            await _uow.Repository<File>().AddAsync(file, ct);

            _mirroredInThisRun[sourceUrl] = file;
            return file;
        }
        catch (Exception ex)
        {
            // Artık gerçekten yalnız "görsel kaydedilemedi": burada kalan tek iş depolamaya
            // yazmak (dosya sistemi) ve varlığı izleyiciye eklemek.
            _log.LogWarning(ex, "Haber görseli kaydedilemedi: {Url}", sourceUrl);
            return null;
        }
    }

    /// <summary>Daha önce aynalanmış dosyayı <c>files.metadata</c>'daki kaynak adresten bulur.</summary>
    /// <remarks>
    /// Harita koşu başına <b>bir kez</b> kuruluyor: her görsel için ayrı sorgu atmak 50 haberlik
    /// bir koşuda yüzlerce gidiş-dönüş demekti. ⚠️ Kapsam <c>ModuleType = "news"</c> ile dar
    /// tutuldu — bütün <c>files</c> tablosunu belleğe almak, panelin en büyük tablolarından
    /// birini filtresiz çekmek olurdu (Faz 11 denetiminin <c>UsersAdmin</c> bulgusu).
    /// </remarks>
    private async Task<File?> FindMirroredFileAsync(string sourceUrl, CancellationToken ct)
    {
        if (_knownByUrl is null)
        {
            var rows = await _uow.Repository<File>().Query()
                .Where(f => f.ModuleType == "news" && f.Metadata != null)
                .Select(f => new { f.Id, f.Metadata })
                .ToListAsync(ct);

            _knownByUrl = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                var url = ReadSourceUrl(row.Metadata);
                if (url is not null) _knownByUrl[url] = row.Id;
            }
        }

        if (!_knownByUrl.TryGetValue(sourceUrl, out var id)) return null;

        return await _uow.Repository<File>().Query(tracking: true)
            .FirstOrDefaultAsync(f => f.Id == id, ct);
    }

    private Dictionary<string, Guid>? _knownByUrl;

    private static string? ReadSourceUrl(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(metadata);
            return doc.RootElement.TryGetProperty("sourceUrl", out var value)
                ? value.GetString()
                : null;
        }
        catch (System.Text.Json.JsonException)
        {
            // Elle yazılmış ya da eski biçimli bir metadata bütün haritayı düşürmemeli.
            return null;
        }
    }
}
