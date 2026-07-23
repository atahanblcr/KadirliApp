using KadirliApp.Api.Authorization;
using KadirliApp.Application.Features.Lookups;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers.Admin;

/// <summary>
/// Faz 10.9(d): lookup tablolarının yönetimi (mahalle/mezarlık/cami/etkinlik+mekan kategorisi).
/// KARAR: DELETE yok — hepsi FK ile referanslanan kalıcı sözlük verisi (mahalle pasife alınır).
/// Mutasyonlar `lookups` cache grubunu invalidate eder → public lookup uçları taze döner.
/// </summary>
[Route("v1/admin/lookups")]
public class LookupsAdminController : AdminApiControllerBase
{
    // ---- Mahalleler ----

    [HttpGet("neighborhoods")]
    [RequirePermission("lookups", "read")]
    public async Task<IActionResult> GetNeighborhoods()
        => Success(await Sender.Send(new GetNeighborhoodsAdminQuery()));

    [HttpPost("neighborhoods")]
    [RequirePermission("lookups", "create")]
    public async Task<IActionResult> CreateNeighborhood([FromBody] CreateNeighborhoodCommand command)
        => Success(await Sender.Send(command));

    [HttpPut("neighborhoods/{id}")]
    [RequirePermission("lookups", "update")]
    public async Task<IActionResult> UpdateNeighborhood(Guid id, [FromBody] UpdateNeighborhoodCommand command)
        => Success(await Sender.Send(command with { Id = id }));

    // ---- Mezarlıklar ----

    [HttpGet("cemeteries")]
    [RequirePermission("lookups", "read")]
    public async Task<IActionResult> GetCemeteries()
        => Success(await Sender.Send(new GetCemeteriesQuery()));

    [HttpPost("cemeteries")]
    [RequirePermission("lookups", "create")]
    public async Task<IActionResult> CreateCemetery([FromBody] CreateCemeteryCommand command)
        => Success(await Sender.Send(command));

    [HttpPut("cemeteries/{id}")]
    [RequirePermission("lookups", "update")]
    public async Task<IActionResult> UpdateCemetery(Guid id, [FromBody] UpdateCemeteryCommand command)
        => Success(await Sender.Send(command with { Id = id }));

    // ---- Camiler ----

    [HttpGet("mosques")]
    [RequirePermission("lookups", "read")]
    public async Task<IActionResult> GetMosques()
        => Success(await Sender.Send(new GetMosquesQuery()));

    [HttpPost("mosques")]
    [RequirePermission("lookups", "create")]
    public async Task<IActionResult> CreateMosque([FromBody] CreateMosqueCommand command)
        => Success(await Sender.Send(command));

    [HttpPut("mosques/{id}")]
    [RequirePermission("lookups", "update")]
    public async Task<IActionResult> UpdateMosque(Guid id, [FromBody] UpdateMosqueCommand command)
        => Success(await Sender.Send(command with { Id = id }));

    // ---- Etkinlik kategorileri ----

    [HttpGet("event-categories")]
    [RequirePermission("lookups", "read")]
    public async Task<IActionResult> GetEventCategories()
        => Success(await Sender.Send(new GetEventCategoriesQuery()));

    [HttpPost("event-categories")]
    [RequirePermission("lookups", "create")]
    public async Task<IActionResult> CreateEventCategory([FromBody] CreateEventCategoryCommand command)
        => Success(await Sender.Send(command));

    [HttpPut("event-categories/{id}")]
    [RequirePermission("lookups", "update")]
    public async Task<IActionResult> UpdateEventCategory(Guid id, [FromBody] UpdateEventCategoryCommand command)
        => Success(await Sender.Send(command with { Id = id }));

    // ---- Mekan kategorileri ----

    [HttpGet("place-categories")]
    [RequirePermission("lookups", "read")]
    public async Task<IActionResult> GetPlaceCategories()
        => Success(await Sender.Send(new GetPlaceCategoriesAdminQuery()));

    [HttpPost("place-categories")]
    [RequirePermission("lookups", "create")]
    public async Task<IActionResult> CreatePlaceCategory([FromBody] CreatePlaceCategoryCommand command)
        => Success(await Sender.Send(command));

    [HttpPut("place-categories/{id}")]
    [RequirePermission("lookups", "update")]
    public async Task<IActionResult> UpdatePlaceCategory(Guid id, [FromBody] UpdatePlaceCategoryCommand command)
        => Success(await Sender.Send(command with { Id = id }));
}
