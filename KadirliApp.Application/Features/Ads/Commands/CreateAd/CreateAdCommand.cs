using System;
using System.Collections.Generic;
using MediatR;

namespace KadirliApp.Application.Features.Ads.Commands.CreateAd;

public class CreateAdCommand : IRequest<Guid>
{
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal? Price { get; set; }
    public string? SellerName { get; set; }
    public string ContactPhone { get; set; } = default!;

    /// <summary>İlanı oluşturan kullanıcı; controller tarafından claim'lerden set edilir, formdan bind edilmez.</summary>
    public Guid UserId { get; set; }

    /// <summary>Yüklenmiş dosya id'leri; ilk görsel kapak (IsCover) olur. Web'de UploadHelper, API'de istemci doldurur.</summary>
    public List<Guid> ImageFileIds { get; set; } = new();

    /// <summary>Faz 10.5: kategoriye özel alan değerleri (CategoryProperty.Id → değer). Panel bu alanı göndermez (UI'ı yok).</summary>
    public Dictionary<Guid, string>? PropertyValues { get; set; }

    /// <summary>
    /// Faz 10.5: yalnız public POST /v1/ads controller'ı true set eder, body'den bind edilmez.
    /// true iken ek kurallar: telefon format kontrolü, görsel sahipliği (files.uploaded_by == UserId)
    /// ve kategorinin zorunlu (IsRequired) property'lerinin doldurulmuş olması.
    /// Panel/admin akışları false kalır — panelde property UI'ı ve dosya sahipliği kısıtı yoktur.
    /// </summary>
    public bool IsUserSubmission { get; set; }
}
