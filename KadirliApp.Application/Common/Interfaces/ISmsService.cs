namespace KadirliApp.Application.Common.Interfaces;

/// <summary>
/// SMS gönderim soyutlaması (Faz 9.2). Gerçek sağlayıcı (NetGSM, Twilio vb.) henüz yok:
/// aktif implementasyon "Sms:Provider" config anahtarıyla seçilir (varsayılan: Dev → log'a yazar).
/// Sağlayıcı anlaşması yapıldığında Infrastructure/Notifications altına yeni bir implementasyon
/// eklenip DependencyInjection'daki switch'e kaydedilmesi yeterli — çağıran kod değişmez.
/// </summary>
public interface ISmsService
{
    /// <summary>Tek bir telefona SMS gönderir. Başarısızlıkta exception fırlatır.</summary>
    Task SendAsync(string phone, string message, CancellationToken cancellationToken = default);
}
