using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.News.Dtos;
using KadirliApp.Application.Features.News.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers;

/// <summary>
/// Faz 12.12 — <b>haberlerin public yüzü.</b>
/// </summary>
/// <remarks>
/// 🔑 <b>Mobil WordPress'e ASLA bağlanmaz.</b> Zincir tek yönlü:
/// <c>WordPress → (Hangfire senkron) → bizim Postgres → /v1/news → mobil</c>.
/// Mobil kaynağa bağlansaydı override, kategori görünürlüğü, bildirim, arama ve önbellek
/// imkânsız olurdu; üstelik uygulama <b>başka birinin çalışma süresine</b> bağımlı olurdu.
///
/// 🔴 Görünürlük filtresi <b>sorguda zorlanır</b> (<c>NewsVisibility</c>) — istemciden gelen
/// hiçbir bayrak onu gevşetemez (Değişmez Kural #3): arşivlenmiş, kaynakta kalkmış ya da
/// dışlanmış kategorideki haber buradan <b>hiç</b> dönmez.
/// </remarks>
[Route("v1/news")]
public class NewsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public NewsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Sayfalı haber listesi. Gövde taşınmaz — detayda gelir.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<NewsArticleDto>>> GetNews([FromQuery] QueryNewsDto dto)
        => Ok(await _mediator.Send(new GetNewsQuery(dto)));

    /// <summary>
    /// Kategori listesi. ⚠️ Rota <c>{id}</c>'den ÖNCE tanımlı olmalı, yoksa "categories"
    /// bir GUID gibi ayrıştırılmaya çalışılır.
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<List<NewsCategoryDto>>> GetCategories()
        => Ok(await _mediator.Send(new GetNewsCategoriesQuery()));

    [HttpGet("{id}")]
    public async Task<ActionResult<NewsArticleDto>> GetById(Guid id)
    {
        var article = await _mediator.Send(new GetNewsByIdQuery(id));
        if (article is null) return NotFound();
        return Ok(article);
    }
}
