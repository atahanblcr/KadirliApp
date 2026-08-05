using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.ErrorLogs.Commands;
using KadirliApp.Application.Features.ErrorLogs.Dtos;
using KadirliApp.Application.Features.ErrorLogs.Queries;
using KadirliApp.Web.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Web.Controllers;

/// <summary>
/// Faz 12.1 — **hata kayıtları ekranı.**
///
/// 12.1 öncesinde panel "ne yapıldığını" gösteriyordu ama "ne olduğunu" göstermiyordu:
/// <c>AuditLog</c> yöneticinin <b>başarılı</b> yazma eylemlerini tutar, vatandaşın aldığı
/// hata ise yalnız Seq'e akıyordu (<c>localhost:5341</c>) — yani panelden bakan kişinin
/// erişemediği bir yere. Mobilde oluşan hata ise hiçbir yere akmıyordu.
///
/// ⚠️ <b>Yalnız-admin deseni</b> (<c>StaffAdmin</c>/<c>AuditLogsAdmin</c>/<c>TrashAdmin</c> gibi):
/// <c>[Authorize(Roles = "admin,super_admin")]</c> + <c>[PanelPermission]</c> <b>YOK</b> +
/// <c>PanelMenu.Items</c> satırının <c>Module</c>'ü <b>null</b> + adı
/// <c>AdminOnlyControllers</c>'ta. Modül anahtarı verilseydi izin matrisinde moderatöre
/// dağıtılabilen ama rol kapısı yüzünden asla çalışmayacak bir yetki belirirdi
/// (11.15b'nin en büyük bulgusu). Kayıtlar yığın izi, istek yolu ve kullanıcı kimliği
/// taşıyor — moderatöre dağıtılabilir bir yetki değil.
/// </summary>
[Authorize(Roles = "admin,super_admin")]
public class ErrorLogsAdminController : Controller
{
    private const int PageSize = 25;

    private readonly ISender _sender;

    public ErrorLogsAdminController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? source,
        [FromQuery] string? level,
        [FromQuery] bool? resolved,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int page = 1)
    {
        var result = await _sender.Send(new GetErrorLogsQuery(BuildQuery(
            source, level, resolved, from, to, search, sort, page, PageSize)));

        ViewBag.Source = source;
        ViewBag.Level = level;
        ViewBag.Resolved = resolved;
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.Search = search;
        ViewBag.Sort = sort;

        // Süzgeç seçenekleri koddan gelir, DB'den DISTINCT ile değil: henüz hiç oluşmamış
        // bir kaynak/seviye de süzülebilmeli (AuditLogsAdmin'deki aynı karar).
        ViewBag.Sources = PanelDisplay.KnownErrorSources
            .Select(s => (Key: s, Label: PanelDisplay.ErrorSource(s).Label)).ToList();
        ViewBag.Levels = PanelDisplay.KnownErrorLevels
            .Select(l => (Key: l, Label: PanelDisplay.ErrorLevel(l).Label)).ToList();

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var item = await _sender.Send(new GetErrorLogByIdQuery(id));
        if (item is null)
        {
            TempData["Error"] = "Hata kaydı bulunamadı — saklama süresi dolmuş olabilir.";
            return RedirectToAction(nameof(Index));
        }

        return View(item);
    }

    /// <summary>
    /// Kaydı "çözüldü" işaretler. ⚠️ Bu bir <b>silme değildir</b>: kayıt durur ve aynı
    /// hata tekrar ederse yazıcı onu <b>kendiliğinden yeniden açar</b> — "çözdüm" denen
    /// bir hatanın sessizce devam etmesi bu ekranın varlık sebebine aykırı olurdu.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(Guid id, string? note, string? returnUrl)
    {
        var ok = await _sender.Send(new ResolveErrorLogCommand(id, Resolved: true, Note: note));
        TempData[ok ? "Success" : "Error"] = ok
            ? "Hata kaydı çözüldü olarak işaretlendi. Aynı hata tekrar ederse kayıt kendiliğinden yeniden açılır."
            : "Hata kaydı bulunamadı.";

        return Back(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(Guid id, string? returnUrl)
    {
        var ok = await _sender.Send(new ResolveErrorLogCommand(id, Resolved: false, Note: null));
        TempData[ok ? "Success" : "Error"] = ok
            ? "Hata kaydı yeniden açıldı."
            : "Hata kaydı bulunamadı.";

        return Back(returnUrl);
    }

    /// <summary>Faz 11.16b deseni — filtrelenmiş listeyi CSV olarak indirir.</summary>
    [HttpGet]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] string? source,
        [FromQuery] string? level,
        [FromQuery] bool? resolved,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] string? sort)
    {
        // ⚠️ PanelCsv.CollectAsync sayfaları DOLAŞIR — tek istekle Limit=5000 göndermek
        // Pagination.Clamp yüzünden 200 satırı "tüm liste" sanmak demektir (11.16b tuzağı).
        var (rows, total) = await PanelCsv.CollectAsync<ErrorLogResponseDto>(
            (p, size) => _sender.Send(new GetErrorLogsQuery(BuildQuery(
                source, level, resolved, from, to, search, sort, p, size))));

        if (PanelCsv.RejectIfTooLarge(total) is { } tooLarge)
        {
            TempData["Error"] = tooLarge;
            return RedirectToAction(nameof(Index), new { source, level, resolved, from, to, search, sort });
        }

        return PanelCsv.File(rows, CsvColumns, "hata-kayitlari");
    }

    private static QueryErrorLogDto BuildQuery(
        string? source, string? level, bool? resolved,
        DateTime? from, DateTime? to, string? search, string? sort,
        int page, int limit) => new()
        {
            Source = source,
            Level = level,
            IsResolved = resolved,
            From = from,
            To = to,
            Search = search,
            Sort = sort,
            Page = page,
            Limit = limit
        };

    /// <summary>
    /// Detay sayfasından gelen aksiyon listeye değil detaya dönmeli — yönetici notu yazıp
    /// "kayıt nerede?" diye aramamalı. <c>returnUrl</c> yalnız yerel olabilir (açık yönlendirme).
    /// </summary>
    private IActionResult Back(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));

    private static readonly IReadOnlyList<PanelCsv.Column<ErrorLogResponseDto>> CsvColumns =
    [
        new("Kaynak", x => PanelDisplay.ErrorSource(x.Source).Label),
        new("Seviye", x => PanelDisplay.ErrorLevel(x.Level).Label),
        new("Kod", x => x.Code),
        new("Mesaj", x => x.Message),
        new("Yol", x => x.Path),
        new("Yöntem", x => x.Method),
        new("Durum kodu", x => x.StatusCode?.ToString()),
        new("Adet", x => x.OccurrenceCount.ToString()),
        new("İlk görülme", x => PanelCsv.Date(x.FirstSeenAt)),
        new("Son görülme", x => PanelCsv.Date(x.LastSeenAt)),
        new("Durum", x => x.IsResolved ? "Çözüldü" : "Açık"),
        new("Çözen", x => x.ResolvedByName),
        new("Çözüm notu", x => x.ResolvedNote),
        new("Uygulama sürümü", x => x.AppVersion),
        new("Platform", x => x.Platform),
        new("TraceId", x => x.TraceId)
    ];
}
