using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using KadirliApp.Application.Features.Announcements.Commands.TrackAnnouncement;
using KadirliApp.Application.Features.Announcements.Queries.GetAnnouncements;
using KadirliApp.Application.Features.Announcements.Queries.GetAnnouncementById;
using KadirliApp.Application.Features.Announcements.Queries.GetAnnouncementTypes;

namespace KadirliApp.Api.Controllers;

// Faz 10.1: Yazma uçları (POST/PUT/DELETE) kaldırıldı — admin karşılıkları v1/admin/announcements'ta
// (AdminPanel policy korumalı). Public yüzey mobil için salt-okunur.
public class AnnouncementsController : ApiControllerBase
{
    /// <summary>Mobil: yalnızca yayında olan ve görünürlük süresi dolmamış duyurular (Faz 10.8: paged + ?typeId=).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? typeId, [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        return Success(await Sender.Send(new GetAnnouncementsQuery
        {
            OnlyPublished = true,
            TypeId = typeId,
            Page = page,
            Limit = limit
        }));
    }

    [HttpGet("types")]
    public async Task<IActionResult> GetTypes()
    {
        return Success(await Sender.Send(new GetAnnouncementTypesQuery()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        // Faz 10.7: yayında olmayan (pending/scheduled) veya süresi dolmuş duyuru id bilinse bile dönmez.
        return Success(await Sender.Send(new GetAnnouncementByIdQuery { Id = id, OnlyPublished = true }));
    }

    /// <summary>Faz 10.12: görüntülenme sayacı (anonim; giriş yapmışsa announcement_views'a da iz düşer).</summary>
    [HttpPost("{id:guid}/view")]
    [EnableRateLimiting("public-write")] // anonim sayaç şişirme koruması (10.7 deseni)
    public async Task<IActionResult> TrackView(Guid id)
        => Success(await Sender.Send(new TrackAnnouncementCommand(id, AnnouncementTrackKind.View, CurrentUserId)));

    /// <summary>Faz 10.12: dış bağlantı tıklama sayacı (anonim).</summary>
    [HttpPost("{id:guid}/click")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> TrackClick(Guid id)
        => Success(await Sender.Send(new TrackAnnouncementCommand(id, AnnouncementTrackKind.Click, CurrentUserId)));
}
