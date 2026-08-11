using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using KadirliApp.Application.Features.News.Commands;
using KadirliApp.Application.Features.News.Dtos;
using KadirliApp.Application.Features.News.Queries;
using KadirliApp.Web.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Web.Controllers;

/// <summary>
/// Faz 12.13 — <b>haber senkron panosu.</b>
///
/// 🔴 Var olma sebebi bu bloğun 1 numaralı hasar sınıfı: <i>kaynak sessizce susabilir.</i>
/// Senkron durursa uygulama <b>eski haberi göstermeye devam eder</b> — uçlar 200 döner,
/// liste dolu görünür, log temizdir ve hiç kimse hata almaz. Projedeki diğer 26 modülde
/// veriyi <b>biz</b> giriyoruz ve girilmediğini bilen bir insan var; burada yok.
///
/// 📌 <c>/hangfire</c> panosu bu sorunun cevabı DEĞİL: "job koştu mu"yu gösterir, "kaç haber
/// geldi"yi göstermez — üstelik <c>ARCHITECTURE.md</c> §3 panoya erişimin kendisini bir risk
/// olarak işaretliyor (oradan <c>PurgeLoginAttemptsJob</c> elle tetiklenebiliyor).
///
/// ⚠️ <b>Yalnız-admin deseni</b> (<c>ARCHITECTURE.md</c> §3):
/// <c>[Authorize(Roles = "admin,super_admin")]</c> + <c>[PanelPermission]</c> <b>YOK</b> +
/// <c>PanelMenu.Items</c> satırının <c>Module</c>'ü <b>null</b> + adı
/// <c>AdminOnlyControllers</c>'ta. Gerekçe <c>PushCampaignsAdmin</c>'inkiyle birebir: bu ekran
/// yalnız göstermiyor, <b>tüm içerik kümesini</b> etkileyen bir işi tetikliyor.
/// </summary>
[Authorize(Roles = "admin,super_admin")]
public class NewsSyncAdminController : Controller
{
    private const int PageSize = 25;

    private readonly ISender _sender;

    public NewsSyncAdminController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? mode,
        [FromQuery] string? status,
        [FromQuery] string? trigger,
        [FromQuery] int page = 1)
    {
        var result = await _sender.Send(new GetNewsSyncRunsQuery(mode, status, trigger, page, PageSize));

        ViewBag.Mode = mode;
        ViewBag.Status = status;
        ViewBag.Trigger = trigger;
        ViewBag.SyncStatus = await _sender.Send(new GetNewsSyncStatusQuery());

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var run = await _sender.Send(new GetNewsSyncRunByIdQuery(id));
        if (run is null) return NotFound();

        return View(run);
    }

    /// <summary>
    /// Senkronu <b>elle</b> tetikler.
    /// </summary>
    /// <remarks>
    /// 🔑 <b>Neden bu buton var:</b> checklist §11 — <i>"kanalın kendisi bayrakla kapalı bir
    /// yoldur; ilk kez gerçek bir olay sırasında koşacaksa yanlış yapılandırmayı en kötü anda
    /// öğrenirsin. Panele elle tetiklenen bir 'kanalı dene' yolu koy."</i>
    ///
    /// 🔴 <b>Aksiyon adı <c>Create</c>, <c>SyncNow</c> DEĞİL</b> (§7 madde 19): <c>SyncNow</c>
    /// hiçbir önekle eşleşmez ve POST olduğu için sessizce <c>update</c> iznine düşerdi.
    /// <c>Create</c> hem semantik olarak doğru ("yeni bir <b>koşu kaydı</b> oluştur") hem
    /// <c>create</c> iznine düşer — <c>PushCampaignsAdminController.Create</c>'in <c>Send</c>
    /// yerine seçilme gerekçesinin aynısı. Ekran bugün matris dışında olduğu için ikisi de
    /// aynı sonucu verir; ama ekran bir gün moderatöre açılırsa <b>doğru</b> olan ad bu.
    ///
    /// 🔴 <b>Koşu istek içinde çalıştırılmaz, kuyruğa atılır</b> (<c>INewsSyncQueue</c>): bir
    /// koşu dakikalarca sürebiliyor; istek içinde koşsaydı panelin zaman aşımı dolar, yönetici
    /// F5'ler ve <b>ikinci bir koşu</b> başlatırdı.
    ///
    /// ⚠️ Mesaj <b>"başlatıldı" demez, "kuyruğa alındı" der</b>: kuyruğa atmak koşunun
    /// açılacağını garanti etmiyor — veritabanındaki kilit o sırada süren bir koşu varsa
    /// ikincisini reddeder. Söylediğimiz şey, bildiğimiz şeyden fazla olamaz.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string? mode)
    {
        var status = await _sender.Send(new GetNewsSyncStatusQuery());

        // 🔑 Koşul SUNUCUDAN gelir ve burada BİR KEZ DAHA denetlenir: görünüm butonu
        // kapalı çizse bile eski bir sekmeden gelen POST'u durduran şey bu (12.2b'nin
        // `CanCancel` dersi — görünümün koşulu bir bilgidir, kapı değildir).
        if (!status.CanTrigger)
        {
            TempData["Error"] = status.RunningSince is { } since
                ? $"Bir senkron zaten çalışıyor — {since.AddHours(3):HH:mm}'de başladı. Bitmesini bekleyin."
                : "Bir senkron zaten çalışıyor.";
            return RedirectToAction(nameof(Index));
        }

        var requested = ParseMode(mode);
        await _sender.Send(new TriggerNewsSyncCommand { Mode = requested, AdminId = GetAdminId() });

        TempData["Success"] = requested switch
        {
            NewsSyncRequestMode.Archive =>
                "Arşiv derinleştirmesi kuyruğa alındı. Bu koşu kaynaktan daha eski haberleri çeker.",
            NewsSyncRequestMode.Reconcile =>
                "Mutabakat kuyruğa alındı. Kaynaktan kaldırılmış haberler işaretlenecek, geri dönenler yayına alınacak.",
            _ =>
                "Senkron kuyruğa alındı. Yeni ve güncellenen haberler çekiliyor; panelde yaptığınız düzenlemeler korunur."
        };

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Bilinmeyen değer <b>varsayılana düşer</b> (§5) — 400 vermez.</summary>
    private static NewsSyncRequestMode ParseMode(string? mode) => mode?.ToLowerInvariant() switch
    {
        "archive" => NewsSyncRequestMode.Archive,
        "reconcile" => NewsSyncRequestMode.Reconcile,
        _ => NewsSyncRequestMode.Incremental
    };

    private Guid? GetAdminId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
