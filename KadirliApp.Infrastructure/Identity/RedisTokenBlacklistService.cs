using KadirliApp.Application.Common.Interfaces;
using StackExchange.Redis;

namespace KadirliApp.Infrastructure.Identity;

/// <summary>
/// Refresh token iptal listesi: "revoked_jti:{jti}" anahtarı token'ın kalan ömrü kadar yaşar,
/// TTL dolunca Redis kendisi siler (token zaten süresinden geçersiz olacağı için liste şişmez).
/// </summary>
public sealed class RedisTokenBlacklistService : ITokenBlacklistService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisTokenBlacklistService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task RevokeAsync(string jti, TimeSpan ttl)
    {
        await _redis.GetDatabase().StringSetAsync($"revoked_jti:{jti}", "1", ttl);
    }

    public async Task<bool> IsRevokedAsync(string jti)
    {
        return await _redis.GetDatabase().KeyExistsAsync($"revoked_jti:{jti}");
    }
}
