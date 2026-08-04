using KadirliApp.Application.Features.Search.Queries;
using KadirliApp.Web.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Web.Controllers;

/// <summary>
/// Faz 11.16b — panelin **global araması** (11.18'den kalan madde).
/// </summary>
/// <remarks>
/// <para>
/// Bugüne kadar bir kaydı bulmanın tek yolu doğru modüle gidip oranın kendi arama
/// kutusunu kullanmaktı. Ama yöneticinin elindeki ipucu genelde modülü değil kaydı
/// tarif eder ("Yılmaz diye bir şey vardı") — hangi listede olduğunu bilmek zorunda
/// kalmak, panelin en sık tekrarlanan gereksiz adımıydı.
/// </para>
/// <para>
/// ⚠️ <b>Yetki deseni farklı:</b> bu controller <c>[PanelPermission]</c> <b>taşımaz</b>,
/// çünkü tek bir modüle ait değil. Bunun yerine izin, sorgunun içinde uygulanır:
/// aranacak modüller <see cref="IPanelMenuProvider"/>'dan gelir — yani menüde
/// göremediği bir modülden moderatöre **tek sonuç bile** dönmez.
/// Bu istisna keyfî değil, <see cref="PanelMenu.PermissionFilteredControllers"/>'da
/// <b>bildirilmiş</b> ve davranış testiyle kanıtlanmış durumda.
/// </para>
/// <para>
/// 📌 Menüde satırı yok — giriş noktası her sayfanın üstündeki arama kutusu
/// (<c>_Layout</c>). Menüye satır konsaydı aynı işi iki yerden yapan bir gezinme
/// doğardı; kutu ayrıca dar ekranda da görünür.
/// </para>
/// </remarks>
[Authorize(Roles = "admin,super_admin,moderator")]
public class GlobalSearchController : Controller
{
    private readonly ISender _sender;
    private readonly IPanelMenuProvider _menu;

    public GlobalSearchController(ISender sender, IPanelMenuProvider menu)
    {
        _sender = sender;
        _menu = menu;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        // 🔑 İzin süzgeci TEK kaynaktan: menüyü çizen sağlayıcının aynısı.
        // Ayrı bir izin sorgusu yazılsaydı, menüde görünmeyen bir modül aramada
        // görünebilir (ya da tersi) ve ayrışmanın sebebi hiçbir yerde okunmazdı.
        var visible = await _menu.VisibleItemsAsync(User);

        var allowed = visible
            .Where(i => i.RequiresPermission)
            .Select(i => i.Module!)
            .Where(GlobalSearchQueryHandler.SearchableModules.Contains)
            .ToHashSet(StringComparer.Ordinal);

        var result = await _sender.Send(new GlobalSearchQuery(q, allowed));

        ViewBag.MinTermLength = GlobalSearchQueryHandler.MinTermLength;
        ViewBag.PerModuleLimit = GlobalSearchQueryHandler.PerModuleLimit;
        return View(result);
    }
}
