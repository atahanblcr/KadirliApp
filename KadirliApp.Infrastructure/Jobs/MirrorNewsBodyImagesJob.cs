using Hangfire;
using KadirliApp.Application.Features.News.Services;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Jobs;

/// <summary>
/// Faz 12.14 — <b>12.14 öncesinde inmiş</b> haberlerin gövdesindeki dış görselleri aynalar
/// (saatlik, turlu).
///
/// 🔴 <b>Var olma sebebi:</b> senkron yalnız <i>kaynakta değişen</i> haberi yeniden yazıyor.
/// Bu iş olmasaydı 12.14 öncesinden kalan haberlerin gövde görselleri sonsuza kadar hotlink
/// kalır ve ölçülen <b>%9'luk imzalı/süreli</b> kısım zamanla 403'e düşerdi — istemci onları
/// zarifçe gizlediği için <b>hiç kimse hata almadan</b> haberler görselsizleşirdi.
///
/// 🔑 <b>Saatlik ve turlu</b> (15 dakikalık senkronla aynı sıklıkta değil): geri doldurma
/// acil değil, <b>tamamlanabilir</b> olmalı. Her turda en fazla birkaç haber onarılıyor;
/// iş kendini bitirdiğinde (taranan kayıtların hiçbirinde dış görsel kalmadığında)
/// hiçbir şey yapmadan dönüyor — yani sonsuza kadar koşması zararsız.
/// </summary>
public class MirrorNewsBodyImagesJob
{
    /// <summary>Tur başına onarılacak en fazla haber. Tavansız bir tur, kaynağı da bizi de yorar.</summary>
    private const int BatchSize = 5;

    private readonly NewsBodyImageBackfill _backfill;
    private readonly ILogger<MirrorNewsBodyImagesJob> _log;

    public MirrorNewsBodyImagesJob(NewsBodyImageBackfill backfill, ILogger<MirrorNewsBodyImagesJob> log)
        => (_backfill, _log) = (backfill, log);

    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 900)]
    public async Task RunAsync()
    {
        var outcome = await _backfill.RunAsync(BatchSize, CancellationToken.None);

        if (outcome.Rewritten > 0)
            _log.LogInformation(
                "Haber gövde görselleri: {Rewritten} haber onarıldı, {Images} görsel aynalandı.",
                outcome.Rewritten, outcome.ImagesMirrored);
    }
}
