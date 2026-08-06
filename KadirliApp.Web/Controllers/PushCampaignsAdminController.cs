using System.Security.Claims;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.PushCampaigns;
using KadirliApp.Application.Features.PushCampaigns.Commands;
using KadirliApp.Application.Features.PushCampaigns.Dtos;
using KadirliApp.Application.Features.PushCampaigns.Queries;
using KadirliApp.Domain.Entities;
using KadirliApp.Web.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Web.Controllers;

/// <summary>
/// Faz 12.2b — **bildirim teslim panosu + bağımsız gönderim ekranı.**
///
/// İki boşluğu birden kapatıyor:
/// <list type="number">
///   <item><b>Gönderimin sonucu görünmüyordu.</b> FCM'in cevabı 10.11'den beri
///         <c>notifications.fcm_sent/fcm_sent_at/fcm_error</c> alanlarında saklanıyordu —
///         ama o satırlara bakan bir ekran yoktu. Yönetici "duyuruyu yayınladım, gitti mi?"
///         sorusuna ancak veritabanına girerek cevap verebiliyordu.</item>
///   <item><b>Tek seferlik bildirim atmanın yolu yoktu.</b> Bildirim satırı üreten tek şey
///         duyuru üreticisiydi; yönetici bir push atmak için vatandaşın duyurular listesine
///         kalıcı bir kayıt düşürmek zorundaydı.</item>
/// </list>
///
/// ⚠️ <b>Yalnız-admin deseni</b> (<c>ARCHITECTURE.md</c> §3):
/// <c>[Authorize(Roles = "admin,super_admin")]</c> + <c>[PanelPermission]</c> <b>YOK</b> +
/// <c>PanelMenu.Items</c> satırının <c>Module</c>'ü <b>null</b> + adı <c>AdminOnlyControllers</c>'ta.
///
/// 🔴 Gerekçe burada diğer yalnız-admin ekranlardan <b>daha ağır</b>: bu ekran yalnız
/// göstermiyor, tek tıkla <b>şehrin tamamına</b> bildirim gönderiyor. İzin matrisine
/// girseydi, aksiyon adı (<c>Send</c>) hiçbir moderasyon önekiyle eşleşmediği ve POST olduğu
/// için sessizce <c>update</c> iznine düşerdi (görünmez sözleşme #19) — yani yalnız
/// <i>düzenleme</i> yetkisi verilmiş bir moderatör herkese push atabilirdi.
/// </summary>
[Authorize(Roles = "admin,super_admin")]
public class PushCampaignsAdminController : Controller
{
    private const int PageSize = 25;

    private readonly ISender _sender;
    private readonly IUnitOfWork _uow;

    public PushCampaignsAdminController(ISender sender, IUnitOfWork uow)
    {
        _sender = sender;
        _uow = uow;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? source,
        [FromQuery] string? targetType,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int page = 1)
    {
        var result = await _sender.Send(new GetPushCampaignsQuery(
            BuildQuery(source, targetType, status, from, to, search, sort, page, PageSize)));

        ViewBag.Source = source;
        ViewBag.TargetType = targetType;
        ViewBag.Status = status;
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.Search = search;
        ViewBag.Sort = sort;

        // Süzgeç seçenekleri koddan gelir, DB'den DISTINCT ile değil: henüz hiç oluşmamış
        // bir kaynak/durum da süzülebilmeli (12.1'deki aynı karar).
        ViewBag.Sources = PanelDisplay.KnownPushSources
            .Select(s => (Key: s, Label: PanelDisplay.PushSource(s).Label)).ToList();
        ViewBag.Targets = PanelDisplay.KnownPushTargets
            .Select(t => (Key: t, Label: PanelDisplay.PushTarget(t).Label)).ToList();
        ViewBag.Statuses = PanelDisplay.KnownPushStatuses
            .Select(s => (Key: s, Label: PanelDisplay.PushStatus(s).Label)).ToList();

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var campaign = await _sender.Send(new GetPushCampaignByIdQuery(id));
        if (campaign is null) return NotFound();

        return View(campaign);
    }

    /// <summary>Faz 11.16b deseni — filtrelenmiş listeyi CSV olarak indirir.</summary>
    [HttpGet]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] string? source,
        [FromQuery] string? targetType,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] string? sort)
    {
        // ⚠️ PanelCsv.CollectAsync sayfaları DOLAŞIR — tek istekle Limit=5000 göndermek
        // Pagination.Clamp yüzünden 200 satırı "tüm liste" sanmak demektir (11.16b tuzağı).
        var (rows, total) = await PanelCsv.CollectAsync<PushCampaignResponseDto>(
            (p, size) => _sender.Send(new GetPushCampaignsQuery(
                BuildQuery(source, targetType, status, from, to, search, sort, p, size))));

        if (PanelCsv.RejectIfTooLarge(total) is { } tooLarge)
        {
            TempData["Error"] = tooLarge;
            return RedirectToAction(nameof(Index), new { source, targetType, status, from, to, search, sort });
        }

        return PanelCsv.File(rows, CsvColumns, "bildirim-gonderimleri");
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadNeighborhoodsAsync();
        return View(new SendPushCampaignDto());
    }

    /// <summary>
    /// Elle bildirim gönderimi.
    /// </summary>
    /// <remarks>
    /// ⚠️ Aksiyon adı <b>bilinçli olarak</b> <c>Create</c>: görünmez sözleşme #19 gereği
    /// izin eylemi aksiyon adının önekinden türer ve <c>Create…</c> → <c>create</c> demektir.
    /// <c>Send</c> yazılsaydı hiçbir önekle eşleşmez, POST olduğu için sessizce
    /// <c>update</c>'e düşerdi. Ekran bugün matris dışında olduğu için ikisi de aynı sonucu
    /// verir — ama ekran bir gün moderatöre açılırsa <b>doğru</b> olan ad bu.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SendPushCampaignDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadNeighborhoodsAsync();
            return View(dto);
        }

        var result = await _sender.Send(new SendPushCampaignCommand(dto, GetAdminId()));

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error?.Message ?? "Bildirim gönderilemedi.");
            await LoadNeighborhoodsAsync();
            return View(dto);
        }

        var campaign = await _sender.Send(new GetPushCampaignByIdQuery(result.Data));

        // 🔑 Mesaj alıcı sayısını SÖYLER. "Bildirim gönderildi" deseydi, hedeflemeye kimse
        // uymadığında (yeni bir mahalle, kayıtlı kullanıcısı olmayan) yönetici gitmeyen bir
        // bildirimi gitmiş sanırdı — bu fazın savaştığı sessiz hasar sınıfı.
        TempData["Success"] = campaign is { RecipientCount: > 0 }
            ? $"Bildirim {campaign.RecipientCount} kişiye yazıldı. Gönderim durumu bu sayfada takip edilebilir."
            : "Hedeflemeye uyan kullanıcı bulunamadı — hiç bildirim yazılmadı.";

        return RedirectToAction(nameof(Details), new { id = result.Data });
    }

    /// <summary>
    /// Gönderilmemiş satırları geri çeker (plan dışı ek).
    /// ⚠️ İletilmiş mesaja dokunmaz ve dokunmayı teklif de etmez — <c>FcmSent=true</c> terminaldir.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var cancelled = await _sender.Send(new CancelPushCampaignCommand(id));

        if (cancelled)
            TempData["Success"] = "Gönderilmemiş bildirimler geri çekildi. İletilmiş mesajlar geri alınamaz.";
        else
            TempData["Error"] = "Bu gönderim iptal edilemez — tamamlanmış ya da geri çekilecek bildirimi kalmamış.";

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Formun canlı "kaç kişiye gidecek?" önizlemesi (AJAX).
    /// Hesap <c>INotificationDispatcher</c>'a devredilir — yani <b>gönderimin kendisiyle
    /// aynı süzgeç</b>. Ayrı bir sayım yazılsaydı önizleme ile gerçek ayrışırdı.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> EstimateRecipients(
        [FromQuery] string targetType, [FromQuery] Guid[]? neighborhoodIds)
    {
        if (!PushTargetTypes.Supported.Contains(targetType ?? string.Empty))
            return Json(new { count = 0 });

        var count = await _sender.Send(new EstimatePushRecipientsQuery(targetType!, neighborhoodIds));
        return Json(new { count });
    }

    private async Task LoadNeighborhoodsAsync()
    {
        ViewBag.Neighborhoods = await _uow.Repository<Neighborhood>().Query()
            .Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();
    }

    private Guid? GetAdminId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static QueryPushCampaignDto BuildQuery(
        string? source, string? targetType, string? status,
        DateTime? from, DateTime? to, string? search, string? sort,
        int page, int limit) => new()
        {
            Source = source,
            TargetType = targetType,
            Status = status,
            From = from,
            To = to,
            Search = search,
            Sort = sort,
            Page = page,
            Limit = limit
        };

    private static readonly IReadOnlyList<PanelCsv.Column<PushCampaignResponseDto>> CsvColumns =
    [
        new("Tarih", x => PanelCsv.Date(x.CreatedAt)),
        new("Başlık", x => x.Title),
        new("Mesaj", x => x.Body),
        new("Kaynak", x => PanelDisplay.PushSource(x.Source).Label),
        new("Hedef", x => PanelDisplay.PushTarget(x.TargetType).Label),
        new("Mahalleler", x => string.Join(", ", x.TargetNeighborhoodNames)),
        new("Gönderen", x => x.CreatedByName),
        new("Alıcı", x => x.RecipientCount.ToString()),
        new("Gönderildi", x => x.SentCount.ToString()),
        new("Başarısız", x => x.FailedCount.ToString()),
        new("Geçersiz token", x => x.InvalidTokenCount.ToString()),
        new("Bekleyen", x => x.PendingCount.ToString()),
        new("Durum", x => PanelDisplay.PushStatus(x.Status).Label),
        new("Tamamlanma", x => x.CompletedAt is null ? null : PanelCsv.Date(x.CompletedAt.Value))
    ];
}
