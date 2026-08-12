using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Application.Features.News.Services;

/// <summary>Bir geri doldurma turunun sonucu (log ve test için).</summary>
public sealed record NewsBodyImageBackfillOutcome(int Scanned, int Rewritten, int ImagesMirrored);

/// <summary>
/// Faz 12.14 — <b>12.14 öncesinde inmiş</b> haberlerin gövdesindeki dış görselleri aynalar.
/// </summary>
/// <remarks>
/// 🔴 <b>Neden ayrı bir iş, neden senkron yetmiyor:</b> senkron yalnız <i>kaynakta değişen</i>
/// haberi yeniden yazıyor (sağlama eşitse satıra hiç dokunmuyor — 12.12'nin bilinçli kararı).
/// Yani 12.14'ten önce inmiş 54 haberin gövdesi, kaynakta bir daha hiç değişmezse
/// <b>sonsuza kadar</b> hotlink kalırdı ve %9'u zamanla 403'e düşerdi. Geri doldurma bu
/// boşluğun tek kapatıcısı.
///
/// 🔑 <b>Idempotent:</b> ölçüt kaydın kendi gövdesi — içinde <c>http(s)</c> ile başlayan bir
/// <c>&lt;img src&gt;</c> kalmadıysa kayıt bir daha taranmaz. Tur tur ilerler, iş yeniden
/// koşturulduğunda kaldığı yerden devam eder.
///
/// ⚠️ <b>Tur başına tavan var</b> (<paramref name="batchSize"/>): tavansız bir geri doldurma,
/// derinlik 500'e çıkarıldığında tek koşuda yüzlerce görsel indirmeye çalışır — hem kaynağı
/// hem kendimizi yorar. Adım adım ilerlemek, "hepsini bir seferde" den her zaman güvenli.
///
/// ⚠️ <b>Sağlamaya dokunulmaz</b> (<c>NewsArticle.ReplaceSourceBodyImages</c>): aynalama
/// bizim yaptığımız bir şey, kaynağın değişmesi değil. Sağlama karışsaydı bir sonraki
/// senkron bu haberleri "değişmiş" sayar ve gereksiz yere yeniden yazardı.
/// </remarks>
public class NewsBodyImageBackfill
{
    private readonly IUnitOfWork _uow;
    private readonly NewsImageMirror _mirror;
    private readonly ILogger<NewsBodyImageBackfill> _log;

    public NewsBodyImageBackfill(IUnitOfWork uow, NewsImageMirror mirror, ILogger<NewsBodyImageBackfill> log)
        => (_uow, _mirror, _log) = (uow, mirror, log);

    public async Task<NewsBodyImageBackfillOutcome> RunAsync(int batchSize, CancellationToken ct)
    {
        if (batchSize <= 0) return new NewsBodyImageBackfillOutcome(0, 0, 0);

        // ⚠️ Süzme SQL'de yapılamıyor (`LIKE '%<img src="http%'` hem kırılgan hem indekssiz),
        // bu yüzden aday kümesi tarih sırasıyla ve TAVANLI çekiliyor. Sıra **en yeniden**:
        // vatandaşın bugün açacağı haberler önce onarılır.
        // ⚠️ `IgnoreQueryFilters` bilinçli DEĞİL: silinmiş kayıtları onarmak boşuna iş.
        var candidates = await _uow.Repository<NewsArticle>().Query(tracking: true)
            .OrderByDescending(x => x.SourcePublishedAt)
            .ThenBy(x => x.Id)
            .Take(batchSize * 4)
            .ToListAsync(ct);

        var scanned = 0;
        var rewritten = 0;
        var images = 0;

        foreach (var article in candidates)
        {
            if (rewritten >= batchSize) break;

            scanned++;
            var urls = NewsBodyImages.ExternalUrls(article.SourceContentHtml);
            if (urls.Count == 0) continue;

            var mirrored = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var url in urls)
            {
                var stored = await _mirror.MirrorToUrlAsync(url, ct);
                if (stored is not null) mirrored[url] = stored;
            }

            if (mirrored.Count == 0)
            {
                // Hepsi başarısız: kayıt olduğu gibi kalır (hotlink) ve bir sonraki turda
                // yeniden denenir. Sonsuz döngü riski yok — tur tavanlı ve elle tetikleniyor.
                continue;
            }

            article.ReplaceSourceBodyImages(
                NewsBodyImages.Rewrite(article.SourceContentHtml, mirrored));

            rewritten++;
            images += mirrored.Count;
        }

        if (rewritten > 0)
        {
            await _uow.SaveChangesAsync(ct);
            _log.LogInformation(
                "Haber gövde görselleri aynalandı: {Rewritten} haber, {Images} görsel ({Scanned} kayıt tarandı).",
                rewritten, images, scanned);
        }

        return new NewsBodyImageBackfillOutcome(scanned, rewritten, images);
    }
}
