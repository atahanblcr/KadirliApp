using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using KadirliApp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Notifications;

/// <summary>
/// Firebase Cloud Messaging adaptörü (Fcm:Provider=Firebase). Service-account JSON yolu Fcm:ServiceAccountKeyPath'ten
/// okunur; yoksa/dosya bulunamazsa IsConfigured=false ile NO-OP'a düşer + uyarı loglar (Firebase'siz ortam çökmez).
/// FCM ücretsizdir; tek "bağlama" işi service-account JSON'unu sağlamaktır (Flutter da aynı projeyi kullanır).
/// </summary>
public sealed class FcmPushService : IPushService
{
    private const string AppName = "kadirliapp-fcm";
    private readonly ILogger<FcmPushService> _log;
    private readonly FirebaseMessaging? _messaging;

    public FcmPushService(IConfiguration cfg, ILogger<FcmPushService> log)
    {
        _log = log;
        var keyPath = cfg["Fcm:ServiceAccountKeyPath"];
        if (string.IsNullOrWhiteSpace(keyPath) || !File.Exists(keyPath))
        {
            _log.LogWarning(
                "FCM service-account bulunamadı (Fcm:ServiceAccountKeyPath='{Path}'). Push gönderimi DEVRE DIŞI (no-op). Dosya yolunu ayarlayın.",
                keyPath);
            return;
        }

        // ⚠️ Faz 11.13 hazırlığında düzeltildi: FirebaseAdmin **.NET** SDK'sında
        // FirebaseApp.GetInstance(name) uygulama yoksa ArgumentException FIRLATMAZ,
        // null DÖNDÜRÜR (Java SDK'sı fırlatır — kod o davranışa göre yazılmıştı).
        // Sonuç: catch hiç çalışmıyor, Create hiç çağrılmıyor ve GetMessaging(null)
        // "App argument must not be null" ile patlıyordu. Bu yol bugüne kadar
        // ÇALIŞTIRILMAMIŞTI çünkü Fcm:Provider varsayılanı "None" idi; gerçek bir
        // service-account bağlanır bağlanmaz her dakika Hangfire hatası üretti.
        try
        {
            var app = FirebaseApp.GetInstance(AppName)
                      ?? FirebaseApp.Create(
                          new AppOptions { Credential = GoogleCredential.FromFile(keyPath) },
                          AppName);

            _messaging = FirebaseMessaging.GetMessaging(app);
            _log.LogInformation("FCM push sağlayıcısı hazır (service-account: {Path}).", keyPath);
        }
        catch (System.Exception ex)
        {
            // Bozuk/geçersiz anahtar dosyası uygulamayı ÇÖKERTMEMELİ (sınıfın
            // sözleşmesi: "Firebase'siz ortam çökmez"). Push no-op'a düşer,
            // sebep loglanır — sessiz başarısızlık yok.
            _log.LogError(
                ex,
                "FCM başlatılamadı (service-account: {Path}). Push gönderimi DEVRE DIŞI (no-op).",
                keyPath);
        }
    }

    public bool IsConfigured => _messaging is not null;

    public async Task<IReadOnlyList<PushResult>> SendAsync(IReadOnlyList<PushMessage> messages, CancellationToken cancellationToken = default)
    {
        if (_messaging is null || messages.Count == 0)
            return messages.Select(_ => PushResult.Failed("PUSH_DISABLED")).ToList();

        var fcmMessages = messages.Select(m => new Message
        {
            Token = m.Token,
            Notification = new FirebaseAdmin.Messaging.Notification { Title = m.Title, Body = m.Body },
            Data = m.Data?.ToDictionary(kv => kv.Key, kv => kv.Value)
        }).ToList();

        // SendEachAsync: tek çağrıda ≤500 FARKLI mesaj; her mesajın sonucu ayrı (kısmi başarı desteklenir).
        var response = await _messaging.SendEachAsync(fcmMessages, cancellationToken);

        return response.Responses.Select(r =>
        {
            if (r.IsSuccess) return PushResult.Ok();
            var code = (r.Exception as FirebaseMessagingException)?.MessagingErrorCode;
            // Yalnız UNREGISTERED kalıcı geçersizliktir → token temizlenir. Diğer hatalar geçici sayılır.
            var invalid = code == MessagingErrorCode.Unregistered;
            return PushResult.Failed(code?.ToString() ?? r.Exception?.Message ?? "UNKNOWN", invalid);
        }).ToList();
    }
}
