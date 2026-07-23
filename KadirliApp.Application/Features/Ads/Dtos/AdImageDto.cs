using System;

namespace KadirliApp.Application.Features.Ads.Dtos;

/// <summary>İlan görseli; Id = ad_images kayıt id'si (silme bununla yapılır), FileId = files kaydı.</summary>
public record AdImageDto(Guid Id, Guid FileId, string? Url, bool IsCover, int DisplayOrder);
