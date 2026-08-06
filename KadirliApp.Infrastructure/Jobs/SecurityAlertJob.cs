using System.Security.Cryptography;
using System.Text;
using Hangfire;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace KadirliApp.Infrastructure.Jobs;

/// <summary>
/// Faz 12.2 — **şüpheli giriş uyarısı: panele bakmayan yöneticiye haber vermek.**
///
/// <c>login_attempts</c> tablosu ve panel ekranı "sorulduğunda" cevap verir; bu iş
/// <b>sorulmadan</b> haber verir. 5 dakikada bir işlenmemiş şüpheli kayıtları toplar,
/// <b>tek e-postada gruplar</b> ve <c>super_admin</c> rolündeki, e-postası dolu
/// kullanıcılara yollar.
///
/// 🔴 <b>Kısma (throttle) opsiyonel değil.</b> Kısma olmadan bir kaba kuvvet saldırısı
/// yöneticinin posta kutusuna <b>kendi kendimize yaptığımız bir DoS</b>'a döner: dakikada
/// onlarca uyarı gelir, yönetici kuralı filtreye atar ve <b>gerçek</b> uyarı da o filtreye
/// düşer. Yani kısma kaldırıldığında sistem çalışmaya devam eder ama <b>işe yaramaz</b> —
/// bu fazın savaştığı sessiz hasar sınıfının tam örneği. Kural + alıcı başına saatte 1.
///
/// 🔑 <b>İki ayrı sınır var ve ikisi de gerekli:</b> (1) iş 5 dakikada bir koşuyor ve her
/// koşuda <b>tek</b> e-posta üretiyor → tavan zaten saatte 12; (2) Redis kısması aynı
/// <i>kuralın</i> tekrarını saatte 1'e indiriyor. İlki Redis çökse bile geçerli kalır,
/// yani kısmanın kendisi tek dayanak değil.
///
/// ⚠️ E-posta gövdesi <b>parola/OTP/token içermez</b> ve kimlik zaten maskeli gelir
/// (<c>LoginIdentifierMasker</c>): uyarı postası, korumaya çalıştığı şeyi sızdıran bir
/// kanal olmamalı.
/// </summary>
public class SecurityAlertJob
{
    /// <summary>Tek koşuda işlenecek en fazla kayıt — saldırı anında iş kendisi yük olmamalı.</summary>
    private const int BatchSize = 500;

    /// <summary>E-postada kural başına listelenecek en fazla örnek satır.</summary>
    private const int SamplesPerRule = 5;

    /// <summary>Kural + alıcı başına en az bekleme süresi.</summary>
    private static readonly TimeSpan ThrottleWindow = TimeSpan.FromHours(1);

    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly IConnectionMultiplexer _redis;
    private readonly IConfiguration _cfg;
    private readonly ILogger<SecurityAlertJob> _log;

    public SecurityAlertJob(
        AppDbContext db,
        IEmailService email,
        IConnectionMultiplexer redis,
        IConfiguration cfg,
        ILogger<SecurityAlertJob> log)
        => (_db, _email, _redis, _cfg, _log) = (db, email, redis, cfg, log);

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 300 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 240)]
    public async Task RunAsync()
    {
        var pending = await _db.LoginAttempts
            .Where(x => x.IsSuspicious && x.AlertedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Take(BatchSize)
            .ToListAsync();

        if (pending.Count == 0)
            return;

        var now = DateTime.UtcNow;

        if (!_cfg.GetValue("Security:AlertEmailEnabled", true))
        {
            // Kapalıysa kayıtlar yine işaretlenir: panelde duruyorlar, ekran kalıcı kayıt —
            // e-posta yalnızca bir bildirim kanalı. İşaretlemeseydik iş her 5 dakikada
            // aynı satırları tekrar tarardı ve tablo büyüdükçe boşuna yavaşlardı.
            MarkAlerted(pending, now);
            await _db.SaveChangesAsync();
            _log.LogInformation(
                "SecurityAlertJob: {Count} şüpheli giriş kaydı işaretlendi, e-posta KAPALI (Security:AlertEmailEnabled=false).",
                pending.Count);
            return;
        }

        var recipients = await _db.Users
            .Where(u => u.Role == UserRole.SuperAdmin && u.IsActive && u.Email != null && u.Email != "")
            .Select(u => u.Email!)
            .ToListAsync();

        if (recipients.Count == 0)
        {
            // 🔑 İş PATLAMAZ. Alıcı olmaması bir yapılandırma eksiği; Hangfire'da kırmızı
            // bir iş bırakmak, gerçek arızaların görünürlüğünü azaltır.
            MarkAlerted(pending, now);
            await _db.SaveChangesAsync();
            _log.LogWarning(
                "SecurityAlertJob: {Count} şüpheli giriş kaydı var ama e-postası dolu super_admin YOK — " +
                "uyarı gönderilemedi. Kayıtlar panelde (Giriş Denemeleri) görülebilir.",
                pending.Count);
            return;
        }

        var groups = pending
            .GroupBy(x => x.SuspicionRule ?? "?", StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .ToList();

        var sent = 0;
        foreach (var recipient in recipients)
        {
            var allowed = new List<IGrouping<string, LoginAttempt>>();
            foreach (var group in groups)
            {
                if (await TryClaimThrottleSlotAsync(group.Key, recipient))
                    allowed.Add(group);
            }

            if (allowed.Count == 0)
            {
                _log.LogInformation(
                    "SecurityAlertJob: {Recipient} için tüm kurallar kısma penceresinde — e-posta gönderilmedi.",
                    Obfuscate(recipient));
                continue;
            }

            try
            {
                await _email.SendAsync(recipient, BuildSubject(allowed), BuildBody(allowed, now));
                sent++;
            }
            catch (Exception ex)
            {
                // Gönderim patlarsa kayıtları YİNE işaretliyoruz ama kısma anahtarını
                // bırakıyoruz (TTL'i dolunca yeni olaylar tekrar denenir). Alternatif —
                // işaretlememek — aynı satırları sonsuza dek yeniden denemek olurdu ve
                // kalıcı bir SMTP arızasında iş her 5 dakikada bir patlardı.
                _log.LogError(ex, "SecurityAlertJob: uyarı e-postası gönderilemedi → {Recipient}", Obfuscate(recipient));
            }
        }

        MarkAlerted(pending, now);
        await _db.SaveChangesAsync();

        _log.LogInformation(
            "SecurityAlertJob: {Count} şüpheli giriş kaydı işlendi, {Sent} e-posta gönderildi ({Rules}).",
            pending.Count, sent, string.Join(", ", groups.Select(g => $"{g.Key}×{g.Count()}")));
    }

    private static void MarkAlerted(IEnumerable<LoginAttempt> attempts, DateTime now)
    {
        foreach (var attempt in attempts)
            attempt.AlertedAt = now;
    }

    /// <summary>
    /// Kural + alıcı için kısma yuvasını atomik olarak kapar. <c>true</c> → gönderebilirsin.
    /// </summary>
    /// <remarks>
    /// <c>SETNX + TTL</c> tek çağrıda: önce "var mı" bakıp sonra yazmak, iki Hangfire
    /// sunucusu aynı anda koştuğunda ikisinin de "yok" görüp <b>iki</b> e-posta yollamasına
    /// izin verirdi.
    ///
    /// ⚠️ Redis erişilemezse <b>gönderim tarafına düşülür</b> (fail-open). Gerekçe:
    /// güvenlik uyarısını sessizce yutmak, fazladan e-postadan daha kötü — ve zaten
    /// koşu başına tek e-posta üretildiği için tavan yine saatte 12'dir.
    /// </remarks>
    private async Task<bool> TryClaimThrottleSlotAsync(string rule, string recipient)
    {
        try
        {
            var key = $"security_alert:{Hash($"{rule}|{recipient}")}";
            return await _redis.GetDatabase().StringSetAsync(key, "1", ThrottleWindow, When.NotExists);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "SecurityAlertJob: kısma anahtarı yazılamadı — uyarı yine gönderiliyor.");
            return true;
        }
    }

    /// <summary>Alıcı adresi Redis anahtarında ham durmasın (anahtarlar dökülebilir).</summary>
    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..32];

    /// <summary>Log satırında tam adres yazmamak için: <c>ali@x.com</c> → <c>ali***@x.com</c>.</summary>
    private static string Obfuscate(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return "***";
        var head = at <= 3 ? email[..1] : email[..3];
        return head + "***" + email[at..];
    }

    private static string BuildSubject(IReadOnlyList<IGrouping<string, LoginAttempt>> groups)
    {
        var total = groups.Sum(g => g.Count());
        return $"[KadirliApp] {total} şüpheli giriş denemesi";
    }

    private string BuildBody(IReadOnlyList<IGrouping<string, LoginAttempt>> groups, DateTime now)
    {
        var panelUrl = _cfg["Security:PanelBaseUrl"]?.TrimEnd('/');
        var sb = new StringBuilder();

        sb.Append("<div style=\"font-family:system-ui,Segoe UI,Arial,sans-serif;font-size:14px;color:#111\">");
        sb.Append("<h2 style=\"margin:0 0 4px\">Şüpheli giriş denemeleri</h2>");
        sb.Append("<p style=\"margin:0 0 16px;color:#555\">Aşağıdaki denemeler otomatik kurallarla işaretlendi. ")
          .Append("Bu bir bilgilendirmedir; hiçbir hesap bu e-posta yüzünden kilitlenmedi.</p>");

        foreach (var group in groups)
        {
            var first = group.Min(x => x.CreatedAt);
            var last = group.Max(x => x.CreatedAt);
            var ips = group.Where(x => x.IpAddress != null)
                .Select(x => x.IpAddress!.ToString())
                .Distinct(StringComparer.Ordinal)
                .Take(5)
                .ToList();

            sb.Append("<div style=\"border:1px solid #e5e7eb;border-radius:8px;padding:12px;margin-bottom:12px\">");
            sb.Append("<div style=\"font-weight:600;margin-bottom:6px\">")
              .Append(Escape(RuleTitle(group.Key)))
              .Append(" — ").Append(group.Count()).Append(" kayıt</div>");
            sb.Append("<div style=\"color:#555\">Zaman aralığı: ")
              .Append(Escape(Local(first))).Append(" – ").Append(Escape(Local(last))).Append("</div>");
            sb.Append("<div style=\"color:#555\">IP: ")
              .Append(ips.Count == 0 ? "bilinmiyor" : Escape(string.Join(", ", ips))).Append("</div>");

            sb.Append("<ul style=\"margin:8px 0 0;padding-left:18px;color:#374151\">");
            foreach (var attempt in group.OrderByDescending(x => x.CreatedAt).Take(SamplesPerRule))
            {
                sb.Append("<li>")
                  // ⚠️ Identifier zaten MASKELİ geliyor; burada ham hâline erişim yok.
                  .Append(Escape(attempt.Identifier))
                  .Append(" · ").Append(Escape(ChannelTitle(attempt.Channel)))
                  .Append(" · ").Append(Escape(Local(attempt.CreatedAt)))
                  .Append("</li>");
            }
            sb.Append("</ul></div>");
        }

        if (!string.IsNullOrWhiteSpace(panelUrl))
        {
            sb.Append("<p style=\"margin:16px 0 0\"><a href=\"").Append(Escape(panelUrl))
              .Append("/LoginAttemptsAdmin/Index?suspicious=true\">Panelde tüm şüpheli denemeleri görüntüle</a></p>");
        }
        else
        {
            sb.Append("<p style=\"margin:16px 0 0;color:#555\">Ayrıntı: panel → <strong>Giriş Denemeleri</strong> ekranı.</p>");
        }

        sb.Append("<p style=\"margin:16px 0 0;color:#9ca3af;font-size:12px\">")
          .Append("Bu e-posta ").Append(Escape(Local(now)))
          .Append(" itibarıyla otomatik üretildi. Aynı kural için saatte en fazla bir uyarı gönderilir.</p>");
        sb.Append("</div>");

        return sb.ToString();
    }

    /// <summary>
    /// Kural adlarının Türkçe karşılığı. ⚠️ Panelin <c>PanelDisplay.SuspicionRule</c>'ıyla
    /// aynı anlamı taşımalı; e-posta ile ekran farklı isimler kullanırsa yönetici
    /// "hangi uyarı hangi satır?" diye arar.
    /// </summary>
    private static string RuleTitle(string rule) => rule switch
    {
        SuspicionRules.RepeatedAccountFailure => "Aynı hesaba yoğun başarısız deneme",
        SuspicionRules.CredentialStuffing => "Aynı IP'den çok sayıda hesaba deneme",
        SuspicionRules.NewIpForPanelUser => "Panel kullanıcısının yeni IP'sinden giriş",
        SuspicionRules.SuccessRightAfterLockout => "Kilit biter bitmez başarılı giriş",
        _ => $"Bilinmeyen kural ({rule})"
    };

    private static string ChannelTitle(string channel) => channel switch
    {
        LoginChannels.Panel => "Panel",
        LoginChannels.MobileOtp => "Mobil (OTP)",
        _ => channel
    };

    /// <summary>UTC → Türkiye saati. Mobil <c>AppDate</c> ve panel görünümleriyle aynı sabit +03.</summary>
    private static string Local(DateTime utc) => utc.AddHours(3).ToString("dd.MM.yyyy HH:mm");

    /// <summary>
    /// 🔴 Gövde HTML. <c>Identifier</c> ve <c>UserAgent</c> dolaylı olarak <b>istemciden</b>
    /// gelir; kaçırılmazsa uyarı e-postası HTML enjeksiyonu taşıyıcısına döner
    /// (12.1'in panel XSS kararının e-posta karşılığı).
    /// </summary>
    private static string Escape(string? value) =>
        System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}
