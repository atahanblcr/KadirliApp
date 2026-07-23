using KadirliApp.Api.Authorization;
using KadirliApp.Application.Features.Ads.Commands;
using KadirliApp.Application.Features.Ads.Queries;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers.Admin;

/// <summary>
/// Faz 10.9(c): ilan kategori ağacı + kategori özellikleri (property/option) yönetimi.
/// "categories" literal segmenti v1/admin/ads/{id}'den önceliklidir — route çakışması yok.
/// Tüm mutasyonlar ads-lookup cache grubunu invalidate eder (public mobil uçları taze döner).
/// </summary>
[Route("v1/admin/ads/categories")]
public class AdCategoriesAdminController : AdminApiControllerBase
{
    [HttpGet]
    [RequirePermission("ads", "read")]
    public async Task<IActionResult> GetAll()
    {
        return Success(await Sender.Send(new GetAdCategoriesAdminQuery()));
    }

    [HttpPost]
    [RequirePermission("ads", "create")]
    public async Task<IActionResult> Create([FromBody] CreateAdCategoryCommand command)
    {
        return Success(await Sender.Send(command));
    }

    [HttpPut("{id}")]
    [RequirePermission("ads", "update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAdCategoryCommand command)
    {
        return Success(await Sender.Send(command with { Id = id }));
    }

    [HttpDelete("{id}")]
    [RequirePermission("ads", "delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return Success(await Sender.Send(new DeleteAdCategoryCommand(id)));
    }

    [HttpGet("{id}/properties")]
    [RequirePermission("ads", "read")]
    public async Task<IActionResult> GetProperties(Guid id)
    {
        return Success(await Sender.Send(new GetCategoryPropertiesAdminQuery(id)));
    }

    [HttpPost("{id}/properties")]
    [RequirePermission("ads", "create")]
    public async Task<IActionResult> CreateProperty(Guid id, [FromBody] CreateCategoryPropertyCommand command)
    {
        return Success(await Sender.Send(command with { CategoryId = id }));
    }

    [HttpPut("properties/{propertyId}")]
    [RequirePermission("ads", "update")]
    public async Task<IActionResult> UpdateProperty(Guid propertyId, [FromBody] UpdateCategoryPropertyCommand command)
    {
        return Success(await Sender.Send(command with { Id = propertyId }));
    }

    [HttpDelete("properties/{propertyId}")]
    [RequirePermission("ads", "delete")]
    public async Task<IActionResult> DeleteProperty(Guid propertyId)
    {
        return Success(await Sender.Send(new DeleteCategoryPropertyCommand(propertyId)));
    }

    [HttpPost("properties/{propertyId}/options")]
    [RequirePermission("ads", "create")]
    public async Task<IActionResult> CreateOption(Guid propertyId, [FromBody] CreatePropertyOptionCommand command)
    {
        return Success(await Sender.Send(command with { PropertyId = propertyId }));
    }

    [HttpDelete("options/{optionId}")]
    [RequirePermission("ads", "delete")]
    public async Task<IActionResult> DeleteOption(Guid optionId)
    {
        return Success(await Sender.Send(new DeletePropertyOptionCommand(optionId)));
    }
}
