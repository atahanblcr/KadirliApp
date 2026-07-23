using System.Text.Json;
using KadirliApp.Application.Common.Interfaces;
using StackExchange.Redis;

namespace KadirliApp.Infrastructure.Caching;

/// <summary>
/// Faz 9.4: ICacheService'in Redis implementasyonu. Uygulamanın mevcut singleton
/// IConnectionMultiplexer'ını kullanır. Grup üyeliği Redis SET'inde tutulur
/// (cache-group:{grup} → anahtar listesi); invalidation set üyelerini + set'i siler.
/// </summary>
public class RedisCacheService : ICacheService
{
    private const string KeyPrefix = "cache:";
    private const string GroupPrefix = "cache-group:";
    // Grup set'i üye anahtarlardan uzun yaşamalı; süresi dolmuş üyeleri silmek zararsızdır.
    private static readonly TimeSpan GroupTtl = TimeSpan.FromHours(24);
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IConnectionMultiplexer redis) => _redis = redis;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var value = await _redis.GetDatabase().StringGetAsync(KeyPrefix + key);
        return value.HasValue ? JsonSerializer.Deserialize<T>(value!, JsonOpts) : default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, string? group = null, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var fullKey = KeyPrefix + key;
        await db.StringSetAsync(fullKey, JsonSerializer.Serialize(value, JsonOpts), ttl);

        if (group is not null)
        {
            var groupKey = GroupPrefix + group;
            await db.SetAddAsync(groupKey, fullKey);
            await db.KeyExpireAsync(groupKey, GroupTtl);
        }
    }

    public async Task InvalidateGroupsAsync(IReadOnlyCollection<string> groups, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        foreach (var group in groups)
        {
            var groupKey = GroupPrefix + group;
            var members = await db.SetMembersAsync(groupKey);
            if (members.Length > 0)
                await db.KeyDeleteAsync(members.Select(m => (RedisKey)m.ToString()).ToArray());
            await db.KeyDeleteAsync(groupKey);
        }
    }
}
