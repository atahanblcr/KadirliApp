using KadirliApp.Api.Authorization;
using KadirliApp.Application.Features.Guide.Commands;
using KadirliApp.Application.Features.Guide.Dtos;
using KadirliApp.Application.Features.Guide.Queries;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers.Admin;

[Route("v1/admin/guide")]
public class GuideAdminController : AdminApiControllerBase
{
    // --- Kategoriler ---

    [HttpGet("categories")]
    [RequirePermission("guide", "read")]
    public async Task<IActionResult> GetCategories([FromQuery] QueryGuideCategoryDto dto)
    {
        return Success(await Sender.Send(new GetGuideCategoriesQuery(dto)));
    }

    [HttpGet("categories/{id}")]
    [RequirePermission("guide", "read")]
    public async Task<IActionResult> GetCategoryById(Guid id)
    {
        return Success(await Sender.Send(new GetGuideCategoryByIdQuery(id)));
    }

    [HttpPost("categories")]
    [RequirePermission("guide", "create")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateGuideCategoryCommand command)
    {
        return Success(await Sender.Send(command));
    }

    [HttpPut("categories/{id}")]
    [RequirePermission("guide", "update")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateGuideCategoryCommand command)
    {
        command.Id = id;
        return Success(await Sender.Send(command));
    }

    [HttpDelete("categories/{id}")]
    [RequirePermission("guide", "delete")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        return Success(await Sender.Send(new DeleteGuideCategoryCommand(id)));
    }

    // --- Rehber kayıtları ---

    [HttpGet("items")]
    [RequirePermission("guide", "read")]
    public async Task<IActionResult> GetItems([FromQuery] QueryGuideItemDto dto)
    {
        return Success(await Sender.Send(new GetGuideItemsQuery(dto)));
    }

    [HttpGet("items/{id}")]
    [RequirePermission("guide", "read")]
    public async Task<IActionResult> GetItemById(Guid id)
    {
        return Success(await Sender.Send(new GetGuideItemByIdQuery(id)));
    }

    [HttpPost("items")]
    [RequirePermission("guide", "create")]
    public async Task<IActionResult> CreateItem([FromBody] CreateGuideItemCommand command)
    {
        return Success(await Sender.Send(command));
    }

    [HttpPut("items/{id}")]
    [RequirePermission("guide", "update")]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateGuideItemCommand command)
    {
        command.Id = id;
        return Success(await Sender.Send(command));
    }

    [HttpDelete("items/{id}")]
    [RequirePermission("guide", "delete")]
    public async Task<IActionResult> DeleteItem(Guid id)
    {
        return Success(await Sender.Send(new DeleteGuideItemCommand(id)));
    }
}
