using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Application.Common.Behaviors;

/// <summary>
/// Faz 9.4: ICacheableQuery isteklerini Redis'ten karşılar; miss durumunda handler'ı
/// çalıştırıp sonucu gruba bağlı olarak yazar. Redis erişilemezse istek cache'siz devam
/// eder (fail-open) — cache altyapısı hiçbir isteği düşürmemeli.
/// </summary>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICacheService _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _log;

    public CachingBehavior(ICacheService cache, ILogger<CachingBehavior<TRequest, TResponse>> log)
        => (_cache, _log) = (cache, log);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not ICacheableQuery cacheable)
            return await next();

        try
        {
            var cached = await _cache.GetAsync<TResponse>(cacheable.CacheKey, ct);
            if (cached is not null)
            {
                _log.LogDebug("Cache HIT {CacheKey}", cacheable.CacheKey);
                return cached;
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Cache okunamadı, handler'a düşülüyor: {CacheKey}", cacheable.CacheKey);
        }

        var response = await next();

        try
        {
            if (response is not null)
                await _cache.SetAsync(cacheable.CacheKey, response, cacheable.CacheDuration, cacheable.CacheGroup, ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Cache yazılamadı: {CacheKey}", cacheable.CacheKey);
        }

        return response;
    }
}
