namespace KadirliApp.Application.Common.Interfaces;

/// <summary>
/// E-posta gönderim soyutlaması (Faz 9.2). Gerçek sağlayıcı (SMTP, SendGrid vb.) henüz yok:
/// aktif implementasyon "Email:Provider" config anahtarıyla seçilir (varsayılan: Dev → log'a yazar).
/// Sağlayıcı bağlandığında Infrastructure/Notifications altına yeni implementasyon eklenip
/// DependencyInjection'daki switch'e kaydedilir — çağıran kod değişmez.
/// </summary>
public interface IEmailService
{
    /// <summary>Tek bir adrese e-posta gönderir. Başarısızlıkta exception fırlatır.</summary>
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
