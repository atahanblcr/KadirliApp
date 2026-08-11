using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.News;
using KadirliApp.Application.Features.News.Commands;
using KadirliApp.Application.Features.News.Dtos;
using KadirliApp.Application.Features.News.Queries;
using KadirliApp.Web.Authorization;
using KadirliApp.Web.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Web.Controllers;

/// <summary>
/// Faz 12.13 — <b>Haberler paneli.</b>
///
/// 🔑 <b>Bu modülde MODERASYON YOK</b> ve bu bilinçli bir karar: kaynak bizim gazetemiz,
/// <c>status=publish</c> demek insanın zaten onayladığı demek. İkinci bir onay kuyruğu aynı
/// kararı iki kez vermek olurdu ve günde ~5 haberde 6 saatlik bir gecikme "Son Dakika"yı
/// <b>yanlış bilgiye</b> çevirirdi. Yerine <b>otomatik yayın + geri alınabilir gizleme</b>.
///
/// ⚠️ Bunun somut bir bedeli var ve ödenmedi: <c>Features/News/</c> altına
/// <c>Approve*.cs</c> koyulduğu an <c>ModerationSingleOwnerTests</c> bu modülde karşılığı
/// olmayan <b>beş moderasyon kuralını</b> zorunlu kılıyor. Geçişlerin adı bu yüzden
/// <c>Archive</c>/<c>Unarchive</c>.
///
/// 🔴 <b>SİLME YOK.</b> <c>NewsArticle</c> <c>ISoftDeletable</c> değil, panelde "Sil" butonu
/// yok ve <c>TrashModules.Supported</c>'a <c>news</c> girmiyor. Sebep: kaynak hâlâ
/// yayındayken silinen kayıt <b>bir sonraki senkronda geri gelir</b> — yönetici "sildim ama
/// döndü" der ve sebebi hiçbir yerde yazmaz. <i>İşlevini gizlemenin yaptığı bir butonu
/// koymamak, "işlevsiz buton yok" kuralının doğru okunuşudur.</i>
///
/// 📌 Yetki deseni <b>standart</b> (moderatöre açık): veri hassas değil, zaten yayınlanmış
/// içerik. Senkron panosu ise ayrı bir controller ve matris <b>dışında</b> — o ekran
/// göstermiyor, tüm içerik kümesini etkileyen bir işi tetikliyor.
/// </summary>
[Authorize(Roles = "admin,super_admin,moderator")]
[PanelPermission("news")]
public class NewsAdminController : Controller
{
    private const int PageSize = 20;

    private readonly ISender _sender;

    public NewsAdminController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? state,
        [FromQuery] bool? edited,
        [FromQuery] bool? sourceUpdated,
        [FromQuery] bool? featured,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? sort,
        [FromQuery] int page = 1)
    {
        var result = await _sender.Send(new GetNewsAdminQuery(
            BuildQuery(search, categoryId, state, edited, sourceUpdated, featured, from, to, sort, page, PageSize)));

        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.State = state;
        ViewBag.Edited = edited;
        ViewBag.SourceUpdated = sourceUpdated;
        ViewBag.Featured = featured;
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.Sort = sort;

        ViewBag.Categories = await _sender.Send(new GetNewsCategoriesAdminQuery());

        // 🔑 Senkron durumu LİSTENİN BAŞINDA: "bugün hiç haber yok" diye bakan yönetici,
        // sebebin senkronun durması olduğunu ancak burada görebilir. Kutu ayrı bir ekranda
        // kalsaydı, oraya bakmayı akıl etmesi gerekirdi — bu bloğun 1 numaralı hasar sınıfı
        // tam olarak "kimse fark etmez" üzerine kurulu.
        ViewBag.SyncStatus = await _sender.Send(new GetNewsSyncStatusQuery());

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var article = await _sender.Send(new GetNewsAdminByIdQuery(id));
        if (article is null) return NotFound();

        return View(article);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var article = await _sender.Send(new GetNewsAdminByIdQuery(id));
        if (article is null) return NotFound();

        return View(article);
    }

    /// <summary>
    /// Yalnız <b>override</b> alanlarını yazar (başlık · özet · kapak görseli).
    /// </summary>
    /// <remarks>
    /// 🔴 Formda <b>görünürlük anahtarı YOK</b> ve bu 12.10'un dersinin harfi harfine
    /// uygulanması: bir kaydın görünürlüğünü yazan ikinci bir yol açılmıyor. Geçişin tek
    /// sahibi Yayından kaldır / Geri al. Alan komuta hiç eklenmediği için burada bir
    /// <c>ModerationStatusGuard</c>'a da gerek yok — <b>olmayan bir alanın</b> yutulması
    /// mümkün değil.
    /// ⚠️ Kaynağın alanlarına (<c>Source*</c>) yazmak zaten <b>derleme hatası</b> (CS8852).
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id, string? title, string? excerpt, IFormFile? coverImage, bool removeCoverImage = false)
    {
        var article = await _sender.Send(new GetNewsAdminByIdQuery(id));
        if (article is null) return NotFound();

        var coverImageFileId = article.CoverImageFileIdOverride;

        if (removeCoverImage) coverImageFileId = null;

        var uploaded = await UploadHelper.UploadAsync(_sender, coverImage, "news", GetAdminId());
        if (uploaded is not null) coverImageFileId = uploaded;

        var result = await _sender.Send(new UpdateNewsOverridesCommand
        {
            Id = id,
            Title = title,
            Excerpt = excerpt,
            CoverImageFileId = coverImageFileId,
            AdminId = GetAdminId()
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error?.Message ?? "Haber güncellenemedi.");
            // ⚠️ Formu DB'den tazeliyoruz (12.10'un `RedisplayEditAsync` dersi): elde kalan
            // eski nesneyle çizilen form, kaydın gerçek durumunu yanlış gösterebilir.
            return View(await _sender.Send(new GetNewsAdminByIdQuery(id)));
        }

        TempData["Success"] = "Haber güncellendi. Panelde yaptığınız düzenleme, sonraki senkronlarda korunur.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Tüm override'ları kaldırır — kayıt <b>deterministik</b> biçimde kaynağa döner.
    /// </summary>
    /// <remarks>
    /// 🔑 Bu buton, "kilit bayrağı" tasarımının reddedilme sebebinin ekrandaki karşılığı:
    /// orada "kilidi aç" dendiğinde ne olacağı belirsizdi; burada cevap net — kaynakta ne
    /// yazıyorsa o.
    /// ⚠️ Form <b>içinde</b> ikinci bir aksiyon olduğu için görünümde <c>formaction</c> +
    /// <c>formenctype="application/x-www-form-urlencoded"</c> şart: iç içe <c>&lt;form&gt;</c>
    /// tarayıcı tarafından <b>sessizce atılır</b> ve buton hiçbir şey yapmaz.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetOverrides(Guid id)
    {
        var result = await _sender.Send(new UpdateNewsOverridesCommand { Id = id, AdminId = GetAdminId() });

        if (result.Success)
            TempData["Success"] = "Düzenlemeler kaldırıldı; haber kaynaktaki hâline döndü.";
        else
            TempData["Error"] = result.Error?.Message ?? "Düzenlemeler kaldırılamadı.";

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Haberi uygulamadan kaldırır (geri alınabilir). Gerekçe <b>zorunlu</b>.</summary>
    /// <remarks>
    /// 🔑 Gerekçenin zorunlu olması bir "form doğrulaması" değil, kaydın kendisi:
    /// *"bu haberi neden kaldırdık?"* sorusu üç ay sonra da sorulacak ve cevabı ancak
    /// kayıtta duruyorsa var.
    /// ⚠️ Aksiyon adı <c>Archive…</c> → izin eylemi <c>approve</c> (§7 madde 19): yayından
    /// kaldırmak, düzenlemekten <b>ayrı bir güven</b>.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, string? reason, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Yayından kaldırma gerekçesi zorunludur.";
            return BackToList(returnUrl, id);
        }

        var result = await _sender.Send(new ArchiveNewsArticleCommand
        {
            Id = id,
            Reason = reason,
            AdminId = GetAdminId()
        });

        if (result.Success) TempData["Success"] = "Haber uygulamadan kaldırıldı. Gerektiğinde geri alabilirsiniz.";
        else TempData["Error"] = result.Error?.Message ?? "Haber kaldırılamadı.";

        return BackToList(returnUrl, id);
    }

    /// <summary>
    /// Haberi yeniden yayına alır.
    /// </summary>
    /// <remarks>
    /// 🔴 Aksiyon adı <c>Unarchive…</c> ve <c>PanelPermissionFilter.ActionFor</c>'a bu önek
    /// 12.13'te <b>elle eklendi</b>: "Archive" öneki onu yakalamıyor (eşleşme baştan yapılır)
    /// ve POST olduğu için sessizce <c>update</c>'e düşerdi — yani <i>yayından kaldırmak
    /// `approve` isterken yayına döndürmek `update` ile yapılabilirdi</i>.
    /// ⚠️ Kaynağı <c>gone</c> olan kayıt geri alınsa da <b>görünmez</b> (NewsVisibility);
    /// mesaj bunu söylemek zorunda, yoksa panel "yayına aldım" der ve vatandaş hiçbir şey
    /// görmez.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unarchive(Guid id, string? returnUrl)
    {
        var article = await _sender.Send(new GetNewsAdminByIdQuery(id));
        if (article is null) return NotFound();

        var result = await _sender.Send(new UnarchiveNewsArticleCommand { Id = id });

        if (!result.Success)
        {
            TempData["Error"] = result.Error?.Message ?? "Haber yayına alınamadı.";
            return BackToList(returnUrl, id);
        }

        TempData["Success"] = article.SourceState == Domain.Entities.NewsSourceStates.Gone
            ? "Haber yayına alındı — ancak kaynakta bulunmadığı için uygulamada görünmeyecek."
            : "Haber yeniden yayına alındı.";

        return BackToList(returnUrl, id);
    }

    /// <summary>Öne çıkarır / kaldırır. Süre <b>zorunlu değil ama önerilir</b>.</summary>
    /// <remarks>
    /// 🔑 <c>FeaturedUntil</c> boş bırakılabilir ama form varsayılan olarak bir tarih önerir:
    /// süresiz bir manşet, unutulduğu gün <b>bayat</b> bir haberi aylarca en üstte tutar ve
    /// bunu kimse fark etmez.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Feature(Guid id, bool isFeatured, DateTime? featuredUntil, string? returnUrl)
    {
        var result = await _sender.Send(new SetNewsFeaturedCommand
        {
            Id = id,
            IsFeatured = isFeatured,
            // Form yerel (TR) saat gönderiyor; kayıt UTC (§7 madde 6'nın sınıfı).
            FeaturedUntil = featuredUntil.HasValue
                ? DateTime.SpecifyKind(featuredUntil.Value, DateTimeKind.Local).ToUniversalTime()
                : null
        });

        if (result.Success)
            TempData["Success"] = isFeatured ? "Haber öne çıkarıldı." : "Haber öne çıkarılanlardan kaldırıldı.";
        else
            TempData["Error"] = result.Error?.Message ?? "İşlem tamamlanamadı.";

        return BackToList(returnUrl, id);
    }

    // ── Toplu işlem ─────────────────────────────────────────────────────────────
    // ⚠️ Ad "…Selected" ile biter, "Bulk…" ile başlamaz: izin eylemi aksiyon adının
    // ÖNEKİNDEN türetilir (§7 madde 19/29) ve `BulkArchive` sessizce `update`'e düşerdi.

    [HttpPost]
    public async Task<IActionResult> ArchiveSelected(Guid[] ids, string? reason, [FromQuery] string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Yayından kaldırma gerekçesi zorunludur.";
            return BackToList(returnUrl, null);
        }

        var adminId = GetAdminId();

        // 🔑 Toplu işlem YENİ İŞ MANTIĞI YAZMAZ — her kimlik için modülün tek-kayıt komutu
        // çağrılır (§7 madde 29): toplu SQL yazılsaydı denetim izi, önbellek temizliği ve
        // gerekçenin kaydı sessizce kaybolurdu.
        var outcome = await PanelBulk.RunAsync(ids, async id =>
            (await _sender.Send(new ArchiveNewsArticleCommand { Id = id, Reason = reason, AdminId = adminId })).Success);

        outcome.Report(TempData, "haber", "yayından kaldırıldı");
        return BackToList(returnUrl, null);
    }

    [HttpPost]
    public async Task<IActionResult> UnarchiveSelected(Guid[] ids, [FromQuery] string? returnUrl)
    {
        var outcome = await PanelBulk.RunAsync(ids, async id =>
            (await _sender.Send(new UnarchiveNewsArticleCommand { Id = id })).Success);

        outcome.Report(TempData, "haber", "yayına alındı");
        return BackToList(returnUrl, null);
    }

    /// <summary>Faz 11.16b deseni — <b>ekrandaki süzgeçle</b> CSV.</summary>
    [HttpGet]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? state,
        [FromQuery] bool? edited,
        [FromQuery] bool? sourceUpdated,
        [FromQuery] bool? featured,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? sort)
    {
        // ⚠️ PanelCsv.CollectAsync sayfaları DOLAŞIR — tek istekle büyük bir Limit göndermek
        // Pagination.Clamp yüzünden ilk sayfayı "tüm liste" sanmak demektir (11.16b tuzağı).
        var (rows, total) = await PanelCsv.CollectAsync<NewsAdminDto>(
            (p, size) => _sender.Send(new GetNewsAdminQuery(
                BuildQuery(search, categoryId, state, edited, sourceUpdated, featured, from, to, sort, p, size))));

        if (PanelCsv.RejectIfTooLarge(total) is { } tooLarge)
        {
            TempData["Error"] = tooLarge;
            return RedirectToAction(nameof(Index), new { search, categoryId, state, edited, sourceUpdated, featured, from, to, sort });
        }

        return PanelCsv.File(rows, CsvColumns, "haberler");
    }

    private IActionResult BackToList(string? returnUrl, Guid? id) =>
        Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!)
        : id is { } value ? RedirectToAction(nameof(Details), new { id = value })
        : RedirectToAction(nameof(Index));

    private Guid? GetAdminId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static QueryNewsAdminDto BuildQuery(
        string? search, Guid? categoryId, string? state, bool? edited, bool? sourceUpdated,
        bool? featured, DateTime? from, DateTime? to, string? sort, int page, int limit) => new()
        {
            Search = search,
            CategoryId = categoryId,
            State = state,
            Edited = edited,
            SourceUpdated = sourceUpdated,
            Featured = featured,
            From = from,
            To = to,
            Sort = sort,
            Page = page,
            Limit = limit
        };

    private static readonly IReadOnlyList<PanelCsv.Column<NewsAdminDto>> CsvColumns =
    [
        new("Yayın tarihi", x => PanelCsv.Date(x.PublishedAt)),
        new("Başlık", x => x.Title),
        new("Kaynaktaki başlık", x => x.SourceTitle),
        new("Kategoriler", x => string.Join(", ", x.Categories.Select(c => c.Name))),
        new("Durum", x => PanelDisplay.NewsState(x.State).Label),
        new("Elle düzenlendi", x => x.HasOverrides ? "Evet" : "Hayır"),
        new("Kaynağı güncellendi", x => x.OverrideIsStale ? "Evet" : "Hayır"),
        new("Öne çıkan", x => x.IsFeatured ? "Evet" : "Hayır"),
        new("Düzenleyen", x => x.OverrideUpdatedByName),
        new("Kaldırma gerekçesi", x => x.ArchivedReason),
        new("Kaynakta değişiklik", x => PanelCsv.Date(x.ModifiedAt)),
        new("Kaynak adresi", x => x.SourceUrl)
    ];
}
