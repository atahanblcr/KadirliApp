using KadirliApp.Api.Authorization;
using KadirliApp.Application.Features.Deaths.Commands;
using KadirliApp.Application.Features.Deaths.Dtos;
using KadirliApp.Application.Features.Deaths.Queries;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers.Admin;

[Route("v1/admin/deaths")]
public class DeathsAdminController : AdminApiControllerBase
{
    [HttpGet]
    [RequirePermission("deaths", "read")]
    public async Task<IActionResult> GetAll([FromQuery] QueryDeathNoticeDto dto)
    {
        return Success(await Sender.Send(new GetDeathNoticesQuery(dto)));
    }

    [HttpGet("{id}")]
    [RequirePermission("deaths", "read")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Success(await Sender.Send(new GetDeathNoticeByIdQuery(id)));
    }

    /// <summary>Admin kaydı doğrudan onaylı girer (panel davranışıyla aynı).</summary>
    [HttpPost]
    [RequirePermission("deaths", "create")]
    public async Task<IActionResult> Create([FromBody] CreateDeathNoticeDto dto)
    {
        return Success(await Sender.Send(new CreateDeathNoticeCommand(dto, CurrentAdminId, AutoApprove: true)));
    }

    [HttpPut("{id}")]
    [RequirePermission("deaths", "update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDeathNoticeDto dto)
    {
        return Success(await Sender.Send(new UpdateDeathNoticeCommand(id, dto)));
    }

    [HttpPost("{id}/approve")]
    [RequirePermission("deaths", "approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        return Success(await Sender.Send(new ApproveDeathNoticeCommand(id, CurrentAdminId)));
    }

    // Faz 12.10: reddetme ve arşivleme artık komut — panelin Düzenle formundaki durum
    // menüsü kaldırıldığı için admin API'sinde de karşılıkları olmalı, yoksa iki yüzey
    // ayrışır (aynı işi bir yüzeyde yapabilip diğerinde yapamamak).
    [HttpPost("{id}/reject")]
    [RequirePermission("deaths", "approve")]
    public async Task<IActionResult> Reject(Guid id, [FromQuery] string? reason = null)
    {
        return Success(await Sender.Send(new RejectDeathNoticeCommand(id, CurrentAdminId, reason)));
    }

    [HttpPost("{id}/archive")]
    [RequirePermission("deaths", "approve")]
    public async Task<IActionResult> Archive(Guid id)
    {
        return Success(await Sender.Send(new ArchiveDeathNoticeCommand(id, CurrentAdminId)));
    }

    [HttpDelete("{id}")]
    [RequirePermission("deaths", "delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return Success(await Sender.Send(new DeleteDeathNoticeCommand(id)));
    }
}
