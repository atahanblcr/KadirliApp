using System.Net;
using System.Net.Mail;
using KadirliApp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Notifications;

/// <summary>
/// Faz 12.2 — <see cref="IEmailService"/>'in ilk <b>gerçek</b> gerçeklemesi.
///
/// Soyutlama ve <c>Email:Smtp</c> yapılandırma bloğu 9.2'den beri hazır bekliyordu; tek
/// eksik buydu. 9.2'nin "sağlayıcı bağlama talimatı" birebir uygulandı: sınıf
/// <c>Notifications/</c> altına kondu, <c>DependencyInjection</c> switch'ine
/// <c>case "smtp"</c> eklendi — <b>çağıran hiçbir kod değişmedi</b>.
///
/// 🔴 <b>Yapılandırma eksikse KURULUŞTA patlar, gönderim anında değil.</b> Sebep:
/// "bayrakla kapalı yol = hiç test edilmemiş yol" dersinin (10.11 FCM) tam karşılığı.
/// Eksik ayar gönderim anında ortaya çıksaydı, hata <c>SecurityAlertJob</c>'ın içinde,
/// yani <b>bir saldırı sırasında</b> ve yalnız log'da görünürdü — uyarı sistemi tam
/// ihtiyaç duyulduğu anda sessizce çalışmıyor olurdu.
///
/// ⚠️ Kimlik bilgileri <c>secrets/</c> ya da ortam değişkeninden gelir; depoya girmez.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly ILogger<SmtpEmailService> _log;
    private readonly string _host;
    private readonly int _port;
    private readonly string? _username;
    private readonly string? _password;
    private readonly string _fromAddress;
    private readonly string _fromName;
    private readonly bool _enableSsl;
    private readonly TimeSpan _timeout;

    public SmtpEmailService(IConfiguration cfg, ILogger<SmtpEmailService> log)
    {
        _log = log;

        _host = cfg["Email:Smtp:Host"] ?? string.Empty;
        _port = cfg.GetValue("Email:Smtp:Port", 587);
        _username = cfg["Email:Smtp:Username"];
        _password = cfg["Email:Smtp:Password"];
        _fromAddress = cfg["Email:Smtp:FromAddress"] ?? string.Empty;
        _fromName = cfg["Email:Smtp:FromName"] ?? "KadirliApp";
        // 587 (STARTTLS) ve 465 için varsayılan açık; yerel yakalayıcılarda (MailHog,
        // Papercut) kapatılabilsin diye yapılandırılabilir.
        _enableSsl = cfg.GetValue("Email:Smtp:EnableSsl", true);
        _timeout = TimeSpan.FromSeconds(cfg.GetValue("Email:Smtp:TimeoutSeconds", 20));

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(_host)) missing.Add("Email:Smtp:Host");
        if (string.IsNullOrWhiteSpace(_fromAddress)) missing.Add("Email:Smtp:FromAddress");

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Email:Provider=Smtp seçili ama zorunlu ayar(lar) boş: {string.Join(", ", missing)}. " +
                "Sağlayıcıyı bağlamadan Provider'ı Smtp yapmayın — uyarı e-postaları sessizce gitmez.");
        }
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_host, _port)
        {
            EnableSsl = _enableSsl,
            Timeout = (int)_timeout.TotalMilliseconds,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        // Kimlik bilgisi verilmemişse anonim SMTP (yerel yakalayıcı / iç posta sunucusu).
        // UseDefaultCredentials'ı `true` bırakmak Windows kimliğiyle denemeye yol açar.
        client.UseDefaultCredentials = false;
        if (!string.IsNullOrWhiteSpace(_username))
            client.Credentials = new NetworkCredential(_username, _password);

        using var message = new MailMessage
        {
            From = new MailAddress(_fromAddress, _fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
            // Türkçe karakterler konu satırında da bozulmasın.
            SubjectEncoding = System.Text.Encoding.UTF8,
            BodyEncoding = System.Text.Encoding.UTF8
        };
        message.To.Add(to);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            _log.LogInformation("E-posta gönderildi → {To} | {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            // Arayüz sözleşmesi "başarısızlıkta fırlatır" diyor — çağıran (SecurityAlertJob)
            // hatayı görmeli ki kısma anahtarını YAZMASIN ve bir sonraki turda tekrar denesin.
            _log.LogError(ex, "E-posta gönderilemedi → {To} | {Subject}", to, subject);
            throw;
        }
    }
}
