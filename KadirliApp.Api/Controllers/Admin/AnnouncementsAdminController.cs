using KadirliApp.Api.Authorization;
using KadirliApp.Application.Features.Announcements.Commands.CreateAnnouncement;
using KadirliApp.Application.Features.Announcements.Commands.CreateAnnouncementType;
using KadirliApp.Application.Features.Announcements.Commands.DeleteAnnouncement;
using KadirliApp.Application.Features.Announcements.Commands.UpdateAnnouncement;
using KadirliApp.Application.Features.Announcements.DTOs;
using KadirliApp.Application.Features.Announcements.Queries.GetAnnouncementById;
using KadirliApp.Application.Features.Announcements.Queries.GetAnnouncements;
using KadirliApp.Application.Features.Announcements.Queries.GetAnnouncementTypes;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers.Admin;

[Route("v1/admin/announcements")]
public class AnnouncementsAdminController : AdminApiControllerBase
{
    /// <summary>Admin taslak/zamanlanmış dahil tüm duyuruları görür (Faz 10.8: paged + ?typeId=).</summary>
    [HttpGet]
    [RequirePermission("announcements", "read")]
    public async Task<IActionResult> GetAll([FromQuery] Guid? typeId, [FromQuery] int page = 1, [FromQuery] int limit = 50)
    {
        return Success(await Sender.Send(new GetAnnouncementsQuery
        {
            OnlyPublished = false,
            TypeId = typeId,
            Page = page,
            Limit = limit
        }));
    }

    [HttpGet("types")]
    [RequirePermission("announcements", "read")]
    public async Task<IActionResult> GetTypes()
    {
        return Success(await Sender.Send(new GetAnnouncementTypesQuery()));
    }

    [HttpPost("types")]
    [RequirePermission("announcements", "create")]
    public async Task<IActionResult> CreateType([FromBody] CreateAnnouncementTypeCommand command)
    {
        return Success(await Sender.Send(command));
    }

    [HttpGet("{id}")]
    [RequirePermission("announcements", "read")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Success(await Sender.Send(new GetAnnouncementByIdQuery { Id = id }));
    }

    [HttpPost]
    [RequirePermission("announcements", "create")]
    public async Task<IActionResult> Create([FromBody] CreateAnnouncementDto dto)
    {
        return Success(await Sender.Send(new CreateAnnouncementCommand { Dto = dto }));
    }

    [HttpPut("{id}")]
    [RequirePermission("announcements", "update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAnnouncementDto dto)
    {
        return Success(await Sender.Send(new UpdateAnnouncementCommand { Id = id, Dto = dto }));
    }

    [HttpDelete("{id}")]
    [RequirePermission("announcements", "delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return Success(await Sender.Send(new DeleteAnnouncementCommand { Id = id }));
    }
}
