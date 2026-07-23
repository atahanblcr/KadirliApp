using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Ads.Commands.CreateAd;
using KadirliApp.Application.Features.Ads.Commands.DeleteMyAd;
using KadirliApp.Application.Features.Ads.Commands.ExtendMyAd;
using KadirliApp.Application.Features.Ads.Commands.FavoriteAd;
using KadirliApp.Application.Features.Ads.Commands.TrackAdContact;
using KadirliApp.Application.Features.Ads.Commands.UpdateMyAd;
using KadirliApp.Application.Features.Ads.Dtos;
using KadirliApp.Application.Features.Ads.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;

namespace KadirliApp.Api.Controllers;

// Faz 10.5: mobil ilan uçları Bölüm 1 — kategori ağacı, kategori özellikleri, detay, kullanıcı ilan verme.
// Faz 10.6 (Bölüm 2): kendi ilanını güncelleme/silme, favoriler, uzatma, iletişim sayaçları.
// Me-scoped listeler (GET /v1/users/me/ads + /me/favorites) UsersController'da.
[Route("v1/ads")]
public class AdsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AdsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdResponseDto>>> FindAll([FromQuery] QueryAdDto dto)
    {
        var query = new GetAdsQuery(dto, OnlyPublished: true);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>Kategori ağacı: parametresiz kök kategoriler, ?parentId= ile alt kategoriler.</summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories([FromQuery] Guid? parentId)
        => Ok(await _mediator.Send(new GetAdCategoriesQuery(parentId)));

    /// <summary>Kategoriye özel form alanları (property) + seçenekleri — mobil ilan verme formu bununla kurulur.</summary>
    [HttpGet("categories/{id:guid}/properties")]
    public async Task<IActionResult> GetCategoryProperties(Guid id)
        => Ok(await _mediator.Send(new GetCategoryPropertiesQuery(id)));

    /// <summary>İlan detayı; approved olmayan ilanı yalnız sahibi görür (diğer herkese 404). Her başarılı çağrı view_count'u artırır.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdDetailDto>> FindById(Guid id)
        => Ok(await _mediator.Send(new GetAdByIdQuery(id, CurrentUserId)));

    /// <summary>Kullanıcı ilan verir: status=pending olarak admin onayına düşer.</summary>
    [HttpPost]
    [Authorize]
    [EnableRateLimiting("public-write")] // Faz 10.7: pending kuyruğunu doldurma koruması
    public async Task<ActionResult<Guid>> Create([FromBody] CreateAdDto dto)
    {
        var command = new CreateAdCommand
        {
            CategoryId = dto.CategoryId,
            Title = dto.Title,
            Description = dto.Description,
            Price = dto.Price,
            SellerName = dto.SellerName,
            ContactPhone = dto.ContactPhone,
            UserId = CurrentUserId!.Value,
            ImageFileIds = dto.ImageFileIds ?? new List<Guid>(),
            PropertyValues = dto.PropertyValues,
            IsUserSubmission = true
        };
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(FindById), new { id }, id);
    }

    /// <summary>Kendi ilanını günceller (sahiplik şart, kategori değiştirilemez); her düzenleme ilanı yeniden onaya (pending) düşürür.</summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<bool>> Update(Guid id, [FromBody] UpdateMyAdCommand command)
    {
        command.Id = id;
        command.UserId = CurrentUserId!.Value;
        return Ok(await _mediator.Send(command));
    }

    /// <summary>Kendi ilanını siler (soft delete; sahiplik şart — başkasının ilanı 403).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<bool>> Delete(Guid id)
        => Ok(await _mediator.Send(new DeleteMyAdCommand(id, CurrentUserId!.Value)));

    /// <summary>Favoriye ekler (idempotent: zaten favorideyse yine 200, data=false).</summary>
    [HttpPost("{id:guid}/favorite")]
    [Authorize]
    public async Task<ActionResult<bool>> AddFavorite(Guid id)
        => Ok(await _mediator.Send(new AddAdFavoriteCommand(id, CurrentUserId!.Value)));

    /// <summary>Favoriden çıkarır (idempotent: favoride değilse yine 200, data=false).</summary>
    [HttpDelete("{id:guid}/favorite")]
    [Authorize]
    public async Task<ActionResult<bool>> RemoveFavorite(Guid id)
        => Ok(await _mediator.Send(new RemoveAdFavoriteCommand(id, CurrentUserId!.Value)));

    /// <summary>İlan süresini 30 gün uzatır (sahiplik şart; hak: MaxExtensions, dolunca 409). Süresi dolmuş ilanı yeniden yayına alır.</summary>
    [HttpPost("{id:guid}/extend")]
    [Authorize]
    public async Task<ActionResult<ExtendAdResultDto>> Extend(Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ExtendAdDto? dto)
        => Ok(await _mediator.Send(new ExtendMyAdCommand(id, CurrentUserId!.Value, dto?.AdsWatched ?? 0)));

    /// <summary>Telefon tıklama sayacı (anonim — masterclass §13.1 kontratı).</summary>
    [HttpPost("{id:guid}/track-phone")]
    [EnableRateLimiting("public-write")] // Faz 10.7: anonim sayaç şişirme koruması
    public async Task<ActionResult<bool>> TrackPhone(Guid id)
        => Ok(await _mediator.Send(new TrackAdContactCommand(id, AdContactChannel.Phone)));

    /// <summary>WhatsApp tıklama sayacı (anonim).</summary>
    [HttpPost("{id:guid}/track-whatsapp")]
    [EnableRateLimiting("public-write")] // Faz 10.7: anonim sayaç şişirme koruması
    public async Task<ActionResult<bool>> TrackWhatsapp(Guid id)
        => Ok(await _mediator.Send(new TrackAdContactCommand(id, AdContactChannel.Whatsapp)));
}
