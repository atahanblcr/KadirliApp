using System.Linq;
using System.Threading.Tasks;
using KadirliApp.Application.Features.Performance.Commands;
using KadirliApp.Application.Features.Performance.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Web.Controllers;

/// <summary>
/// Faz 12.22a — <b>performans panosu.</b>
///
/// 🔴 Var olma sebebi, projenin "sessiz hasar" sınıfının performans karşılığı: bir uç
/// yavaşladığında <b>kimse hata almaz</b>. Uç 200 döner, liste dolu gelir, log temizdir;
/// yalnız vatandaş bekler. 12.22'ye kadar <i>"yavaş mı?"</i> sorusunun cevabı hiçbir yerde
/// yoktu — Seq istek süresini yazıyordu ama <b>handler başına</b> dağılım yoktu ve Seq
/// panelden erişilebilir bir yer değil (12.1'in <c>ErrorLogsAdmin</c> gerekçesinin aynısı).
///
/// 🔑 <b>Ekranın gösterdiği sayı BİRLEŞİK.</b> API ve panel ayrı süreçlerdir; ölçüm süreç
/// belleğinde kalsaydı bu tablo yalnız <i>panelin kendi</i> handler'larını sayar ve
/// <b>doğru görünen yanlış bir p95</b> basardı (bkz. <c>RedisRequestMetrics</c>).
///
/// ⚠️ <b>Yalnız-admin deseni</b> (<c>ARCHITECTURE.md</c> §3):
/// <c>[Authorize(Roles = "admin,super_admin")]</c> + <c>[PanelPermission]</c> <b>YOK</b> +
/// <c>PanelMenu.Items</c> satırının <c>Module</c>'ü <b>null</b> + adı
/// <c>AdminOnlyControllers</c>'ta. Gerekçe iki katmanlı: ekran sunucunun iç yapısını
/// (handler adlarını, hata sayılarını) döküyor ve sıfırlama <b>geri alınamaz</b>.
/// </summary>
[Authorize(Roles = "admin,super_admin")]
public class PerformanceAdminController : Controller
{
    /// <summary>Tabloda gösterilecek en fazla satır — "en sıcak" olanlar yeter, tam döküm değil.</summary>
    private const int MaxRows = 40;

    private readonly ISender _sender;

    public PerformanceAdminController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? kind, [FromQuery] bool slowOnly = false)
    {
        var snapshot = await _sender.Send(new GetRequestMetricsQuery());

        var rows = snapshot.Handlers.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(kind))
            rows = rows.Where(h => h.Kind == kind);
        if (slowOnly)
            rows = rows.Where(h => h.SlowCount > 0);

        ViewBag.Kind = kind;
        ViewBag.SlowOnly = slowOnly;
        ViewBag.Snapshot = snapshot;
        // Süzgeç uygulanmadan ÖNCEKİ sayı: "40 satırdan 3'ü gösteriliyor" diyebilmek için.
        ViewBag.TotalHandlers = snapshot.Handlers.Count;

        return View(rows.Take(MaxRows).ToList());
    }

    /// <summary>
    /// Bütün sayaçları sıfırlar — taban çizgisi ölçümünden önce temiz sayfa.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Aksiyon adı <c>Reset</c> DEĞİL</b> (§7 madde 19): <c>Reset</c> hiçbir önekle
    /// eşleşmez ve POST olduğu için sessizce <c>update</c> iznine düşerdi. <c>Delete…</c>
    /// hem semantik olarak doğru (ölçüm kayıtları <b>siliniyor</b>, geri gelmiyorlar) hem
    /// <c>delete</c> iznine düşer. Ekran bugün matris dışında olduğu için ikisi de aynı
    /// sonucu verir; ama ekran bir gün moderatöre açılırsa <b>doğru</b> olan ad bu
    /// (<c>NewsSyncAdminController.Create</c>'in gerekçesinin aynısı).
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMeasurements()
    {
        await _sender.Send(new ResetRequestMetricsCommand());

        TempData["Success"] =
            "Ölçüm sayaçları sıfırlandı. Yeni taban çizgisi bu andan itibaren birikiyor — " +
            "anlamlı bir p95 için birkaç dakikalık gerçek trafik gerekir.";

        return RedirectToAction(nameof(Index));
    }
}
