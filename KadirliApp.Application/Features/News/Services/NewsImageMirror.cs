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

    /// <summary>Koşu içi ayna: kaynak URL → yeni <c>files.id</c>.</summary>
    private readonly Dictionary<string, Guid> _mirroredInThisRun = new(StringComparer.OrdinalIgnoreCase);

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
    /// Görseli aynalar ve <c>files.id</c> döner. <b>Başarısızlıkta <c>null</c> döner, fırlatmaz</b> —
    /// görselsiz bir haber, hiç inmemiş bir haberden iyidir.
    /// </summary>
    public async Task<Guid?> MirrorAsync(string? sourceUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl)) return null;

        if (_mirroredInThisRun.TryGetValue(sourceUrl, out var cached)) return cached;

        var existing = await _uow.Repository<NewsArticle>().Query()
            .Where(x => x.SourceImageUrl == sourceUrl && x.SourceImageFileId != null)
            .Select(x => x.SourceImageFileId)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            _mirroredInThisRun[sourceUrl] = existing.Value;
            return existing;
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
            await _uow.SaveChangesAsync(ct);

            _mirroredInThisRun[sourceUrl] = file.Id;
            return file.Id;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Haber görseli kaydedilemedi: {Url}", sourceUrl);
            return null;
        }
    }
}
