using KadirliApp.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Notifications;

/// <summary>
/// Sağlayıcısız ortam için SMS adaptörü: göndermek yerine log'a yazar (Sms:Provider=Dev).
/// Mesaj içeriği (OTP dahil) bilinçli olarak loglanır — geliştiricinin kodu görebilmesi için.
/// Gerçek sağlayıcı implementasyonunda mesaj içeriği ASLA loglanmamalıdır.
/// </summary>
public sealed class DevLogSmsService : ISmsService
{
    private readonly ILogger<DevLogSmsService> _log;

    public DevLogSmsService(ILogger<DevLogSmsService> log) => _log = log;

    public Task SendAsync(string phone, string message, CancellationToken cancellationToken = default)
    {
        _log.LogWarning("SMS GÖNDERİLMEDİ (Dev sağlayıcı) → {Phone}: {Message}", phone, message);
        return Task.CompletedTask;
    }
}
