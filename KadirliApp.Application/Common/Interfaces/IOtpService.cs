using System.Threading.Tasks;

namespace KadirliApp.Application.Common.Interfaces;

/// <summary>
/// OTP isteği sonucu (Faz 9.2 — OTP artık API yanıtında dönmez, SMS ile gönderilir).
/// <paramref name="DevOtp"/> YALNIZCA Otp:DevMode=true iken doludur (sağlayıcısız geliştirme/test için).
/// </summary>
public sealed record OtpRequestResult(int ExpiresInSeconds, int RetryAfterSeconds, string? DevOtp);

public interface IOtpService
{
    /// <summary>
    /// 6 haneli OTP üretir, Redis'e TTL ile yazar ve SMS ile gönderir (DevMode'da göndermek yerine
    /// kodu <see cref="OtpRequestResult.DevOtp"/> içinde döndürür). Saatlik istek limitini ve
    /// hatalı deneme bloğunu uygular; aşımda RateLimitedException fırlatır.
    /// </summary>
    Task<OtpRequestResult> RequestAsync(string phone, CancellationToken cancellationToken = default);

    /// <summary>
    /// OTP'yi doğrular; başarılıysa tek kullanımlık olması için siler. Otp:MaxAttempts kadar hatalı
    /// denemeden sonra telefonu 5 dakika bloklar (RateLimitedException).
    /// </summary>
    Task<bool> ValidateAsync(string phone, string otp);
}
