using System;
using System.Threading.Tasks;
using KadirliApp.Application.Features.Legal.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Web.Controllers;

/// <summary>
/// Faz 12.16 — <b>rıza defteri</b>: kim · hangi belge/sürüm · ne zaman · nereden onayladı.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 Var olma sebebi: bir KVKK denetiminde sorulan soru <i>"rıza aldınız mı?"</i> değil
/// <b><i>"kanıtlayın"</i></b>dır. Bu ekran o kanıtın okunabilir hâli.
/// </para>
/// <para>
/// ⚠️ <b>Yalnız-admin deseni</b> (<c>ARCHITECTURE.md</c> §3):
/// <c>[Authorize(Roles = "admin,super_admin")]</c> + <c>[PanelPermission]</c> <b>YOK</b> +
/// <c>PanelMenu.Items</c> satırının <c>Module</c>'ü <b>null</b> + adı
/// <c>AdminOnlyControllers</c>'ta. Gerekçe <c>LoginAttemptsAdmin</c>'inkiyle <b>birebir</b>
/// aynı: satırlar <b>IP adresi ve tarayıcı imzası</b> taşıyor — "kim nereden onayladı"
/// moderatöre dağıtılabilir bir yetki değil. Modül anahtarı verilseydi izin matrisinde
/// <b>karşılığı olmayan bir yetki</b> belirirdi (11.15b'nin en büyük bulgusu).
/// </para>
/// <para>
/// 📌 <b>Defter yalnız okunur.</b> Silme/düzeltme aksiyonu <b>bilinçli olarak yok</b>:
/// düzeltilebilen bir kanıt kanıt değildir.
/// </para>
/// </remarks>
[Authorize(Roles = "admin,super_admin")]
public class ConsentLedgerAdminController : Controller
{
    private const int PageSize = 25;

    private readonly ISender _sender;

    public ConsentLedgerAdminController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? type,
        [FromQuery] bool? granted,
        [FromQuery] Guid? userId,
        [FromQuery] int page = 1)
    {
        var result = await _sender.Send(new GetConsentLedgerQuery
        {
            Type = type,
            Granted = granted,
            UserId = userId,
            Page = page,
            PageSize = PageSize
        });

        ViewBag.Type = type;
        ViewBag.Granted = granted;
        ViewBag.UserId = userId;

        return View(result);
    }
}
