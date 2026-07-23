using KadirliApp.Api.Authorization;
using KadirliApp.Application.Features.Businesses.Commands;
using KadirliApp.Application.Features.Businesses.Dtos;
using KadirliApp.Application.Features.Businesses.Queries;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers.Admin;

/// <summary>Faz 10.9(b): işletme yönetimi — kampanya modülünün ön koşulu (öncesinde Business CRUD hiçbir katmanda yoktu).</summary>
[Route("v1/admin/businesses")]
public class BusinessesAdminController : AdminApiControllerBase
{
    [HttpGet]
    [RequirePermission("businesses", "read")]
    public async Task<IActionResult> GetAll([FromQuery] QueryBusinessDto dto)
    {
        return Success(await Sender.Send(new GetBusinessesQuery(dto)));
    }

    [HttpGet("categories")]
    [RequirePermission("businesses", "read")]
    public async Task<IActionResult> GetCategories()
    {
        return Success(await Sender.Send(new GetBusinessCategoriesQuery()));
    }

    [HttpPost("categories")]
    [RequirePermission("businesses", "create")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateBusinessCategoryCommand command)
    {
        return Success(await Sender.Send(command));
    }

    [HttpGet("{id}")]
    [RequirePermission("businesses", "read")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Success(await Sender.Send(new GetBusinessByIdQuery(id)));
    }

    [HttpPost]
    [RequirePermission("businesses", "create")]
    public async Task<IActionResult> Create([FromBody] CreateBusinessDto dto)
    {
        return Success(await Sender.Send(new CreateBusinessCommand(dto)));
    }

    [HttpPut("{id}")]
    [RequirePermission("businesses", "update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBusinessDto dto)
    {
        return Success(await Sender.Send(new UpdateBusinessCommand(id, dto)));
    }

    [HttpPost("{id}/verify")]
    [RequirePermission("businesses", "approve")]
    public async Task<IActionResult> Verify(Guid id)
    {
        return Success(await Sender.Send(new SetBusinessVerificationCommand(id, true, CurrentAdminId)));
    }

    [HttpPost("{id}/unverify")]
    [RequirePermission("businesses", "approve")]
    public async Task<IActionResult> Unverify(Guid id)
    {
        return Success(await Sender.Send(new SetBusinessVerificationCommand(id, false, CurrentAdminId)));
    }

    [HttpDelete("{id}")]
    [RequirePermission("businesses", "delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return Success(await Sender.Send(new DeleteBusinessCommand(id)));
    }
}
