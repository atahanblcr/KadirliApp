using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Application.Common.Behaviors;

/// <summary>
/// Faz 9.4: ICacheInvalidator command'i başarıyla tamamlandıktan sonra ilgili cache
/// gruplarını temizler. Invalidation hatası isteği düşürmez (veri zaten yazıldı) —
/// loglanır; TTL üst sınırı bayat veriyi en geç süre sonunda temizler.
/// </summary>
public class CacheInvalidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICacheService _cache;
    private readonly ILogger<CacheInvalidationBehavior<TRequest, TResponse>> _log;

    public CacheInvalidationBehavior(ICacheService cache, ILogger<CacheInvalidationBehavior<TRequest, TResponse>> log)
        => (_cache, _log) = (cache, log);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var response = await next();

        if (request is ICacheInvalidator invalidator && invalidator.CacheGroupsToInvalidate.Count > 0)
        {
            try
            {
                await _cache.InvalidateGroupsAsync(invalidator.CacheGroupsToInvalidate, ct);
                _log.LogDebug("Cache grupları temizlendi: {Groups}", string.Join(",", invalidator.CacheGroupsToInvalidate));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Cache invalidation başarısız (TTL dolana dek bayat veri dönebilir): {Groups}",
                    string.Join(",", invalidator.CacheGroupsToInvalidate));
            }
        }

        return response;
    }
}
