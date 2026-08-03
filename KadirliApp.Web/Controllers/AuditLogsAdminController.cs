using System;
using System.Linq;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Audit.Dtos;
using KadirliApp.Application.Features.Audit.Queries;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using KadirliApp.Web.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Web.Controllers;

/// <summary>
/// Faz 11.17 — **denetim izi ekranı.** <c>AuditBehavior</c> 10.9(i)'den beri her hassas
/// yazma komutunu <c>audit_logs</c>'a yazıyordu ama onu okuyan tek ekran/uç yoktu:
/// "bu ilanı kim sildi?" bugüne kadar ancak <c>psql</c> ile cevaplanıyordu. Moderatör rolü
/// 11.15b'den beri gerçekten çalıştığı (ve gerçekten silebildiği) için bu ekran kaçınılmazdı.
///
/// ⚠️ <b>Matrisin DIŞINDA, <c>StaffAdmin</c> gibi:</b> denetlenen kişiler denetim ekranını
/// yönetmemeli. Bu yüzden <c>[PanelPermission]</c> yok — rol kapısı tek başına yeterli ve
/// menü satırının <c>Module</c>'ü <c>null</c> (izin matrisinde **karşılığı olmayan bir
/// yetki** belirmesin — 11.15b'nin en büyük bulgusu tam olarak buydu).
/// </summary>
[Authorize(Roles = "admin,super_admin")]
public class AuditLogsAdminController : Controller
{
    private const int PageSize = 25;

    private readonly ISender _sender;
    private readonly IUnitOfWork _uow;

    public AuditLogsAdminController(ISender sender, IUnitOfWork uow)
    {
        _sender = sender;
        _uow = uow;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? module,
        [FromQuery] string? action,
        [FromQuery] Guid? userId,
        [FromQuery] Guid? affectedId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] int page = 1)
    {
        var result = await _sender.Send(new GetAuditLogsQuery(new QueryAuditLogDto
        {
            Module = module,
            Action = action,
            UserId = userId,
            AffectedId = affectedId,
            From = from,
            To = to,
            Search = search,
            Page = page,
            Limit = PageSize
        }));

        ViewBag.Module = module;
        ViewBag.Action = action;
        ViewBag.UserId = userId;
        ViewBag.AffectedId = affectedId;
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.Search = search;

        // Süzgeç seçenekleri koddan gelir, veritabanından değil: henüz hiç gerçekleşmemiş
        // bir eylem de listelenebilmeli (DISTINCT sorgusu onları göstermezdi).
        ViewBag.Modules = PanelMenu.Items
            .Where(i => i.RequiresPermission)
            .Select(i => (Key: i.Module!, i.Label))
            .OrderBy(x => x.Label, StringComparer.CurrentCulture)
            .ToList();

        ViewBag.Actions = PanelDisplay.KnownAuditActions
            .Select(a => (Key: a, Label: PanelDisplay.AuditAction(a).Label))
            .OrderBy(x => x.Label, StringComparer.CurrentCulture)
            .ToList();

        // Personel listesi küçüktür (tek haneli/onlu) — filtrelenmiş sorgu, bellek riski yok.
        // ⚠️ Silinen personelin izleri de süzülebilmeli → soft-delete filtresi kapalı.
        ViewBag.Staff = await _uow.Repository<User>().Query()
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.Moderator || u.Role == UserRole.Admin || u.Role == UserRole.SuperAdmin)
            .OrderBy(u => u.Username)
            .Select(u => new StaffOption(u.Id, u.Username ?? u.Phone, u.DeletedAt != null))
            .ToListAsync();

        return View(result);
    }

    public sealed record StaffOption(Guid Id, string Name, bool IsDeleted);
}
