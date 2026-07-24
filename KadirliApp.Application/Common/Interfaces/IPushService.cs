using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KadirliApp.Application.Common.Interfaces;

/// <summary>
/// Push bildirim gönderim soyutlaması (Faz 10.11). Gerçek sağlayıcı (Firebase Cloud Messaging) config ile
/// seçilir: varsayılan "Fcm:Provider=None" → NoOpPushService (Firebase'siz ortamda sistem çalışmaya devam
/// eder; gönderim yapılmaz). Gerçek gönderim için Fcm:Provider=Firebase + Fcm:ServiceAccountKeyPath ayarlanır,
/// çağıran kod (SendPushNotificationsJob) DEĞİŞMEZ — SMS/e-posta adaptör deseninin (Faz 9.2) aynısı.
/// </summary>
public interface IPushService
{
    /// <summary>
    /// Sağlayıcı gerçekten gönderime hazır mı (service-account yüklendi mi). false ise push job hiç göndermez.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Bir grup bildirimi tek batch'te (FCM ≤500) gönderir. Dönen sonuçlar giriş listesiyle SIRA SIRA eşleşir.
    /// </summary>
    Task<IReadOnlyList<PushResult>> SendAsync(IReadOnlyList<PushMessage> messages, CancellationToken cancellationToken = default);
}

/// <summary>Tek bir cihaza gönderilecek push: hedef token + başlık/gövde + opsiyonel veri yükü (deep-link için).</summary>
public sealed record PushMessage(string Token, string Title, string Body, IReadOnlyDictionary<string, string>? Data = null);

/// <summary>Tek bir mesajın gönderim sonucu. TokenInvalid=true ise token kalıcı olarak geçersiz (UNREGISTERED) → temizlenmeli.</summary>
public sealed record PushResult(bool Success, bool TokenInvalid, string? Error)
{
    public static PushResult Ok() => new(true, false, null);
    public static PushResult Failed(string error, bool tokenInvalid = false) => new(false, tokenInvalid, error);
}
