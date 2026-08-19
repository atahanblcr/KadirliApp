using System.Diagnostics;
using KadirliApp.Application.Common.Performance;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KadirliApp.Application.Common.Behaviors;

/// <summary>
/// Faz 12.22a — <b>her komut/sorgunun süresini ölçen boru hattı halkası.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b>Neden MediatR davranışı, neden middleware değil?</b> Çünkü kapsam böyle
/// <b>tipten türer</b>: yarın yazılacak her handler kendiliğinden ölçülür, kimsenin bu
/// dosyaya bir satır eklemesi gerekmez (12.19a'nın dersi, aynen geçerli). Bir HTTP
/// middleware'i yalnız <i>istek yolunu</i> görür — Hangfire işlerinden gönderilen
/// komutları, panelin Razor aksiyonlarını ve tek istekte koşan <b>birden çok</b>
/// handler'ı göremezdi. <c>UseSerilogRequestLogging</c> zaten HTTP tarafını ölçüyor;
/// bu halka onun <b>göremediğini</b> ölçer.
/// </para>
/// <para>
/// 🔴 <b>Sıra kuralın parçası ve bu halka BİRİNCİ DEĞİL, İKİNCİDİR.</b> Birinci sıra
/// <c>DevelopmentOnlyBehavior</c>'ındır ve bu bir tercih değil kuraldır
/// (<c>DevelopmentOnlyCommandTests.TheGuard_RunsBeforeEveryOtherBehavior</c>). Ölçüm
/// hemen ardından gelir — yani <b><c>CachingBehavior</c>'ı sarar</b>. Bu bilinçlidir:
/// ölçülmek istenen şey <i>handler ne kadar sürdü</i> değil, <b>çağıran ne kadar
/// bekledi</b>; cache HIT'te handler hiç koşmaz ama bekleyen yine bekler. Halka
/// cache'in <i>içine</i> konsaydı sıcak uçların p95'i sistematik olarak <b>iyi</b>
/// görünürdü — ölçümün yalan söylemesinin en sinsi biçimi.
/// </para>
/// <para>
/// ⚠️ <b>İstisna yutulmaz.</b> Ölçüm alınır, "başarısız" olarak işaretlenir ve istisna
/// <b>yeniden fırlatılır</b> — bir gözlem halkası davranış değiştirirse gözlem olmaktan
/// çıkar.
/// </para>
/// </remarks>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IRequestMetricsRecorder _metrics;
    private readonly IOptions<PerformanceSettings> _settings;
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _log;

    public PerformanceBehavior(
        IRequestMetricsRecorder metrics,
        IOptions<PerformanceSettings> settings,
        ILogger<PerformanceBehavior<TRequest, TResponse>> log)
        => (_metrics, _settings, _log) = (metrics, settings, log);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var settings = _settings.Value;
        if (!settings.Enabled)
            return await next();

        var name = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        var failed = false;

        try
        {
            return await next();
        }
        catch
        {
            failed = true;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            var slow = elapsedMs >= settings.SlowRequestThresholdMs;

            // Kayıt asla fırlatmamalı; yine de gözlem halkası isteği düşürebilecek
            // TEK yol olmamalı (ölçüm, ölçtüğü şeyi bozmaz).
            try
            {
                _metrics.Record(name, elapsedMs, failed, slow);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "İstek ölçümü kaydedilemedi: {Handler}", name);
            }

            // 🔴 Yalnız eşiği aşan istek Warning'e düşer. Eşik altı Debug'da kalır:
            // varsayılan seviye Information olduğu için üretimde hiç yazılmaz.
            if (slow)
            {
                _log.LogWarning(
                    "YAVAŞ {Kind} {Handler} {ElapsedMs:0.0} ms (eşik {ThresholdMs} ms){FailedSuffix}",
                    RequestKind.FromName(name), name, elapsedMs, settings.SlowRequestThresholdMs,
                    failed ? " — istisnayla bitti" : string.Empty);
            }
            else
            {
                _log.LogDebug("{Handler} {ElapsedMs:0.0} ms", name, elapsedMs);
            }
        }
    }
}
