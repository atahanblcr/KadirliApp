using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KadirliApp.Application.Features.Legal.Commands;
using KadirliApp.Application.Features.Legal.Queries;
using KadirliApp.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Web.Controllers;

/// <summary>
/// Faz 12.16 — <b>hukuki belge yönetimi</b> (KVKK aydınlatma · açık rıza · kullanım koşulları…).
/// </summary>
/// <remarks>
/// <para>
/// 🔑 Ekranın var olma sebebi tek cümle: <i>metin panelden değiştirilebiliyorsa, rıza kaydı
/// metnin HANGİ HÂLİNE verildiğini bilmek zorundadır.</i> Bu yüzden burada bir "metin
/// düzenle" formu <b>yoktur</b>: yayınlanmış sürüm salt-okunurdur, değişiklik <b>yeni bir
/// sürümdür</b> (§7 madde 72).
/// </para>
/// <para>
/// ⚠️ <b>Matris deseni</b> (§3): <c>[Authorize(Roles = "admin,super_admin,moderator")]</c> +
/// <c>[PanelPermission("legal")]</c> + <c>PanelMenu.Items</c> satırı — üçü <b>aynı</b> modül
/// anahtarını kullanır. Ekranın matris <b>içinde</b> olması bilinçli: metni yazmak/düzeltmek
/// bir içerik işidir. Kritik olan <b>yayınlama</b> ve o ayrı bir izne bağlandı (aşağıya bak).
/// ⚠️ <b>Rıza defteri bu controller'da DEĞİL</b> — IP ve tarayıcı taşıdığı için yalnız-admin
/// (<see cref="ConsentLedgerAdminController"/>).
/// </para>
/// <para>
/// 📌 <b>Admin API controller'ı (<c>/v1/admin/legal</c>) bilinçli olarak YOK</b> — modül
/// panel-only. Aynı karar 12.12–12.13'te Haberler, 12.1'de Hata Kayıtları, 12.2'de Giriş
/// Denemeleri ve 12.2b'de Bildirim Gönderimleri için verildi: hiçbir istemcinin çağırmadığı
/// bir uç kümesi, bakımı yapılmayan ikinci bir yüzeydir.
/// </para>
/// </remarks>
[Authorize(Roles = "admin,super_admin,moderator")]
[PanelPermission("legal")]
public class LegalAdminController : Controller
{
    private readonly ISender _sender;

    public LegalAdminController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var documents = await _sender.Send(new GetLegalDocumentsAdminQuery());
        return View(documents);
    }

    /// <summary>Bir belgenin sürüm geçmişi + yeni sürüm formu.</summary>
    [HttpGet]
    public async Task<IActionResult> Versions(Guid id)
    {
        var documents = await _sender.Send(new GetLegalDocumentsAdminQuery());
        var document = documents.FirstOrDefault(d => d.Id == id);
        if (document is null) return NotFound();

        ViewBag.Document = document;
        return View(await _sender.Send(new GetLegalDocumentVersionsQuery(id)));
    }

    /// <summary>Belgenin kimlik ayarları (başlık · zorunluluk · sıra · aktiflik).</summary>
    /// <remarks>⚠️ Metin buradan değişmez — o <see cref="CreateVersion"/>'ın işi.</remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateLegalDocumentCommand command)
    {
        var response = await _sender.Send(command);

        if (response.Success) TempData["Success"] = "Belge ayarları güncellendi.";
        else TempData["Error"] = response.Error?.Message ?? "Belge güncellenemedi.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Yeni <b>taslak</b> sürüm açar — metni değiştirmenin tek yolu.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVersion(CreateLegalVersionCommand command)
    {
        var response = await _sender.Send(command);

        if (response.Success)
            TempData["Success"] = "Taslak sürüm oluşturuldu. Önizleyip yayınlayabilirsiniz.";
        else
            TempData["Error"] = response.Error?.Message ?? "Sürüm oluşturulamadı.";

        return RedirectToAction(nameof(Versions), new { id = command.DocumentId });
    }

    /// <summary>Taslağı düzenler. Yayınlanmış sürümde komut <b>reddeder ve sebebini söyler</b>.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateVersion(UpdateLegalVersionCommand command, Guid documentId)
    {
        var response = await _sender.Send(command);

        if (response.Success) TempData["Success"] = "Taslak güncellendi.";
        else TempData["Error"] = response.Error?.Message ?? "Taslak güncellenemedi.";

        return RedirectToAction(nameof(Versions), new { id = documentId });
    }

    /// <summary>
    /// Taslağı <b>yayına alır</b>: bu andan sonra kayıt ekranı bu metni sorar.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Aksiyon adı <c>Publish</c> ve öneki <c>PanelPermissionFilter.ActionFor</c>'a
    /// ELLE eklendi</b> → <c>approve</c>. §7 madde 19'un <b>yedinci</b> tekrarı. Eklenmeseydi
    /// POST olduğu için sessizce <c>update</c>'e düşerdi ve sonuç listedeki en ağırlarından
    /// olurdu: <i>yalnız başlık düzeltme yetkisi olan bir moderatör, şehrin tamamının
    /// onayladığı hukuki metni değiştirebilirdi.</i>
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid id, Guid documentId)
    {
        var response = await _sender.Send(new PublishLegalVersionCommand { Id = id, AdminId = GetAdminId() });

        if (response.Success)
            TempData["Success"] = "Sürüm yayınlandı. Kayıt ekranı bu metni göstermeye başladı.";
        else
            TempData["Error"] = response.Error?.Message ?? "Sürüm yayınlanamadı.";

        return RedirectToAction(nameof(Versions), new { id = documentId });
    }

    private Guid? GetAdminId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
