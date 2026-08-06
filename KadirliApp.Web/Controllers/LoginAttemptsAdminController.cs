using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.LoginAttempts.Dtos;
using KadirliApp.Application.Features.LoginAttempts.Queries;
using KadirliApp.Application.Features.Staff.Queries;
using KadirliApp.Web.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Web.Controllers;

/// <summary>
/// Faz 12.2 — **giriş denemeleri ekranı: "kim, nereden, ne zaman girmeye çalıştı?"**
///
/// 12.2 öncesinde bu sorunun cevabı hiçbir yerde yoktu. 11.18 hesap kilidini getirdi ama
/// yalnız iki sayaç tutuyordu (<c>FailedLoginAttempts</c> + <c>LockedOutUntil</c>): kaç kez
/// denendiğini biliyorduk, kimin ve nereden denediğini bilmiyorduk. Vatandaş tarafında
/// (OTP) hiçbir iz yoktu.
///
/// ⚠️ <b>Yalnız-admin deseni</b> (<c>ARCHITECTURE.md</c> §3):
/// <c>[Authorize(Roles = "admin,super_admin")]</c> + <c>[PanelPermission]</c> <b>YOK</b> +
/// <c>PanelMenu.Items</c> satırının <c>Module</c>'ü <b>null</b> + adı
/// <c>AdminOnlyControllers</c>'ta. Kayıtlar IP ve tarayıcı bilgisi taşıyor; üstelik
/// <b>moderatörün kendi girişleri de</b> bu listede — denetlenen kişi denetim ekranını
/// yönetmemeli (denetim izi ve çöp kutusuyla aynı gerekçe).
/// </summary>
[Authorize(Roles = "admin,super_admin")]
public class LoginAttemptsAdminController : Controller
{
    private const int PageSize = 25;

    private readonly ISender _sender;
    private readonly IEmailService _email;

    public LoginAttemptsAdminController(ISender sender, IEmailService email)
        => (_sender, _email) = (sender, email);

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? channel,
        [FromQuery] bool? succeeded,
        [FromQuery] bool? suspicious,
        [FromQuery] string? rule,
        [FromQuery] string? reason,
        [FromQuery] string? ip,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int page = 1)
    {
        var result = await _sender.Send(new GetLoginAttemptsQuery(BuildQuery(
            channel, succeeded, suspicious, rule, reason, ip, from, to, search, sort, page, PageSize)));

        ViewBag.Channel = channel;
        ViewBag.Succeeded = succeeded;
        ViewBag.Suspicious = suspicious;
        ViewBag.Rule = rule;
        ViewBag.Reason = reason;
        ViewBag.Ip = ip;
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.Search = search;
        ViewBag.Sort = sort;

        // Süzgeç seçenekleri koddan gelir, DB'den DISTINCT ile değil: henüz hiç oluşmamış
        // bir kanal/kural da süzülebilmeli (12.1'deki aynı karar).
        ViewBag.Channels = PanelDisplay.KnownLoginChannels
            .Select(c => (Key: c, Label: PanelDisplay.LoginChannel(c).Label)).ToList();
        ViewBag.Rules = PanelDisplay.KnownSuspicionRules
            .Select(r => (Key: r, Label: PanelDisplay.SuspicionRule(r).Label)).ToList();
        ViewBag.Reasons = PanelDisplay.KnownLoginFailureReasons
            .Select(r => (Key: r, Label: PanelDisplay.LoginFailureReason(r).Label)).ToList();

        return View(result);
    }

    /// <summary>Faz 11.16b deseni — filtrelenmiş listeyi CSV olarak indirir.</summary>
    [HttpGet]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] string? channel,
        [FromQuery] bool? succeeded,
        [FromQuery] bool? suspicious,
        [FromQuery] string? rule,
        [FromQuery] string? reason,
        [FromQuery] string? ip,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] string? sort)
    {
        // ⚠️ PanelCsv.CollectAsync sayfaları DOLAŞIR — tek istekle Limit=5000 göndermek
        // Pagination.Clamp yüzünden 200 satırı "tüm liste" sanmak demektir (11.16b tuzağı).
        var (rows, total) = await PanelCsv.CollectAsync<LoginAttemptResponseDto>(
            (p, size) => _sender.Send(new GetLoginAttemptsQuery(BuildQuery(
                channel, succeeded, suspicious, rule, reason, ip, from, to, search, sort, p, size))));

        if (PanelCsv.RejectIfTooLarge(total) is { } tooLarge)
        {
            TempData["Error"] = tooLarge;
            return RedirectToAction(nameof(Index), new { channel, succeeded, suspicious, rule, reason, ip, from, to, search, sort });
        }

        return PanelCsv.File(rows, CsvColumns, "giris-denemeleri");
    }

    /// <summary>
    /// Plan dışı ek (12.2): **uyarı kanalını şimdi dene.**
    /// </summary>
    /// <remarks>
    /// 🔑 Gerekçe doğrudan bu projenin kendi dersi: <b>"bayrakla kapalı yol = hiç test
    /// edilmemiş yol"</b> (10.11'de <c>FcmPushService</c> gerçek anahtar bağlanınca ilk
    /// çalıştırmada patlamıştı). <c>SecurityAlertJob</c>'ın e-posta yolu ancak <b>gerçek bir
    /// saldırı sırasında</b> ilk kez koşar — yani SMTP yapılandırması yanlışsa bunu tam da
    /// en kötü anda öğreniriz. Bu buton o yolu <b>bugün</b>, istemli olarak çalıştırır.
    ///
    /// ⚠️ Kendi kendine gönderir: alıcı, oturumu açık yöneticinin <b>kendi</b> e-postasıdır.
    /// Serbest alıcı alanı olsaydı panel bir spam aracına dönerdi.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTestAlert()
    {
        var email = await _sender.Send(new GetCurrentPanelUserEmailQuery(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value));

        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Hesabınızda kayıtlı e-posta adresi yok. Personel ekranından ekleyebilirsiniz.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _email.SendAsync(
                email,
                "[KadirliApp] Uyarı kanalı testi",
                "<p>Bu bir <strong>test</strong> e-postasıdır: şüpheli giriş uyarılarının " +
                "gerçekten ulaşıp ulaşmadığını doğrulamak için panelden gönderildi.</p>" +
                "<p>Bu mesajı aldıysanız uyarı kanalı çalışıyor demektir.</p>");

            TempData["Success"] =
                $"Test e-postası gönderildi. Sağlayıcı \"Dev\" ise mesaj yalnız sunucu günlüğüne yazılır — " +
                "posta kutunuza düşmediyse önce yapılandırmayı kontrol edin.";
        }
        catch (Exception ex)
        {
            // 🔑 Ham istisna metni ekrana basılmaz (Değişmez Kural #6: kullanıcıya teknik
            // İngilizce mesaj gösterilmez) ama tamamen yutulmaz da — tipi bir ipucu.
            TempData["Error"] =
                $"Test e-postası gönderilemedi ({ex.GetType().Name}). Ayrıntı sunucu günlüğünde " +
                "ve Hata Kayıtları ekranında.";
        }

        return RedirectToAction(nameof(Index));
    }

    private static QueryLoginAttemptDto BuildQuery(
        string? channel, bool? succeeded, bool? suspicious, string? rule, string? reason, string? ip,
        DateTime? from, DateTime? to, string? search, string? sort,
        int page, int limit) => new()
        {
            Channel = channel,
            Succeeded = succeeded,
            Suspicious = suspicious,
            Rule = rule,
            Reason = reason,
            Ip = ip,
            From = from,
            To = to,
            Search = search,
            Sort = sort,
            Page = page,
            Limit = limit
        };

    private static readonly IReadOnlyList<PanelCsv.Column<LoginAttemptResponseDto>> CsvColumns =
    [
        new("Zaman", x => PanelCsv.Date(x.CreatedAt)),
        new("Kanal", x => PanelDisplay.LoginChannel(x.Channel).Label),
        // ⚠️ Maskeli değer. Ham kimlik hiçbir zaman saklanmadığı için CSV'ye de çıkamaz —
        // maskelemenin asıl kazancı tam olarak burada görünür.
        new("Kimlik (maskeli)", x => x.Identifier),
        new("Kullanıcı", x => x.UserName),
        new("Sonuç", x => x.Succeeded ? "Başarılı" : "Başarısız"),
        new("Sebep", x => x.FailureReason is null ? null : PanelDisplay.LoginFailureReason(x.FailureReason).Label),
        new("IP", x => x.IpAddress),
        new("Şüpheli", x => x.IsSuspicious ? "Evet" : "Hayır"),
        new("Kural", x => x.SuspicionRule is null ? null : PanelDisplay.SuspicionRule(x.SuspicionRule).Label),
        new("Uyarıldı", x => x.AlertedAt is null ? null : PanelCsv.Date(x.AlertedAt.Value)),
        new("Tarayıcı", x => x.UserAgent)
    ];
}
