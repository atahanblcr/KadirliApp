using KadirliApp.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Notifications;

/// <summary>
/// Sağlayıcısız ortam için e-posta adaptörü: göndermek yerine log'a yazar (Email:Provider=Dev).
/// </summary>
public sealed class DevLogEmailService : IEmailService
{
    private readonly ILogger<DevLogEmailService> _log;

    public DevLogEmailService(ILogger<DevLogEmailService> log) => _log = log;

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _log.LogWarning("E-POSTA GÖNDERİLMEDİ (Dev sağlayıcı) → {To} | {Subject} | {Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}
