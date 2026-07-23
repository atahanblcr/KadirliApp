using System;
using System.Collections.Generic;
using MediatR;

namespace KadirliApp.Application.Features.Ads.Commands.UpdateAd;

public class UpdateAdCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal? Price { get; set; }
    public string? SellerName { get; set; }
    public string ContactPhone { get; set; } = default!;
    public string Status { get; set; } = default!;

    /// <summary>Mevcut görsellere eklenecek yeni dosya id'leri; Web'de UploadHelper, API'de istemci doldurur.</summary>
    public List<Guid> NewImageFileIds { get; set; } = new();

    /// <summary>Silinecek ad_images kayıt id'leri (AdImage.Id — file id değil).</summary>
    public List<Guid> RemoveImageIds { get; set; } = new();
}
