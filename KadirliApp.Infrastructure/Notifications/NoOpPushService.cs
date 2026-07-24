using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Notifications;

/// <summary>
/// Sağlayıcısız ortam için push adaptörü (Fcm:Provider=None, varsayılan). IsConfigured=false döndürdüğünden
/// SendPushNotificationsJob hiç göndermeye çalışmaz; SendAsync doğrudan çağrılırsa da tümünü "PUSH_DISABLED"
/// olarak işaretler. Gerçek gönderim için Fcm:Provider=Firebase yapılıp service-account bağlanır.
/// </summary>
public sealed class NoOpPushService : IPushService
{
    private readonly ILogger<NoOpPushService> _log;

    public NoOpPushService(ILogger<NoOpPushService> log) => _log = log;

    public bool IsConfigured => false;

    public Task<IReadOnlyList<PushResult>> SendAsync(IReadOnlyList<PushMessage> messages, CancellationToken cancellationToken = default)
    {
        if (messages.Count > 0)
            _log.LogWarning(
                "Push GÖNDERİLMEDİ (Fcm:Provider=None) — {Count} bildirim atlandı. Gerçek gönderim için Fcm:Provider=Firebase + Fcm:ServiceAccountKeyPath.",
                messages.Count);
        return Task.FromResult<IReadOnlyList<PushResult>>(
            messages.Select(_ => PushResult.Failed("PUSH_DISABLED")).ToList());
    }
}
