using System.Security.Cryptography;
using System.Text;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace KadirliApp.Infrastructure.Identity;

/// <summary>
/// auth.service.ts'teki OTP akışının karşılığı (Masterclass 12.2): kod Redis'te "otp:{phone}"
/// anahtarında TTL ile saklanır; saatlik istek limiti "otp_count:{phone}", hatalı deneme sayacı
/// "otp_attempts:{phone}", MaxAttempts aşımında 5 dakikalık blok "otp_block:{phone}".
/// Faz 9.2: OTP artık yanıtta dönmez — DevMode değilse ISmsService ile gönderilir
/// (aktif sağlayıcı config "Sms:Provider" ile seçilir; bugün Dev → log).
/// </summary>
public sealed class RedisOtpService : IOtpService
{
    private static readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(5);
    private const int RetryAfterSeconds = 60;

    private readonly IConnectionMultiplexer _redis;
    private readonly ISmsService _sms;
    private readonly bool _devMode;
    private readonly int _ttlSeconds;
    private readonly int _maxAttempts;
    private readonly int _rateLimitPerHour;

    public RedisOtpService(IConnectionMultiplexer redis, ISmsService sms, IConfiguration cfg)
    {
        _redis = redis;
        _sms = sms;
        _devMode = cfg.GetValue("Otp:DevMode", false);
        _ttlSeconds = cfg.GetValue("Otp:TtlSeconds", 300);
        _maxAttempts = cfg.GetValue("Otp:MaxAttempts", 3);
        _rateLimitPerHour = cfg.GetValue("Otp:RateLimitPerHour", 10);
    }

    public async Task<OtpRequestResult> RequestAsync(string phone, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();

        if (await db.KeyExistsAsync($"otp_block:{phone}"))
            throw new RateLimitedException("Çok fazla hatalı deneme. Lütfen 5 dakika sonra tekrar deneyin.");

        var countKey = $"otp_count:{phone}";
        var count = await db.StringIncrementAsync(countKey);
        if (count == 1)
            await db.KeyExpireAsync(countKey, TimeSpan.FromHours(1));
        if (count > _rateLimitPerHour)
            throw new RateLimitedException("Çok fazla OTP isteği. Lütfen daha sonra tekrar deneyin.");

        // Masterclass 12.2: Otp:DevMode=true iken sabit kod — SMS entegrasyonu olmadan test için.
        var otp = _devMode ? "123456" : RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        await db.StringSetAsync($"otp:{phone}", otp, TimeSpan.FromSeconds(_ttlSeconds));

        if (!_devMode)
            await _sms.SendAsync(phone, $"KadirliApp doğrulama kodunuz: {otp}", cancellationToken);

        return new OtpRequestResult(_ttlSeconds, RetryAfterSeconds, _devMode ? otp : null);
    }

    public async Task<bool> ValidateAsync(string phone, string otp)
    {
        var db = _redis.GetDatabase();

        if (await db.KeyExistsAsync($"otp_block:{phone}"))
            throw new RateLimitedException("Çok fazla hatalı deneme. Lütfen 5 dakika sonra tekrar deneyin.");

        var key = $"otp:{phone}";
        var attemptsKey = $"otp_attempts:{phone}";
        var stored = await db.StringGetAsync(key);
        if (stored.IsNullOrEmpty)
            return false;

        // OTP başına MaxAttempts hatalı deneme hakkı; aşımda telefon 5 dk bloklanır (brute-force koruması).
        var attempts = await db.StringIncrementAsync(attemptsKey);
        if (attempts == 1)
            await db.KeyExpireAsync(attemptsKey, TimeSpan.FromSeconds(_ttlSeconds));
        if (attempts > _maxAttempts)
        {
            await db.StringSetAsync($"otp_block:{phone}", "1", BlockDuration);
            await db.KeyDeleteAsync(new RedisKey[] { key, attemptsKey });
            throw new RateLimitedException("Çok fazla hatalı deneme. Lütfen 5 dakika sonra tekrar deneyin.");
        }

        // Timing-safe karşılaştırma (crypto.timingSafeEqual karşılığı)
        var valid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(stored.ToString()), Encoding.UTF8.GetBytes(otp));
        if (!valid)
            return false;

        await db.KeyDeleteAsync(new RedisKey[] { key, attemptsKey });
        return true;
    }
}
