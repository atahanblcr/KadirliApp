using System;
using System.Collections.Generic;

namespace KadirliApp.Application.Features.Ads.Dtos;

/// <summary>Faz 10.5: mobil ilan detay ekranı — görseller, kategoriye özel alan değerleri ve iletişim bir arada.</summary>
public record AdDetailDto(
    Guid Id,
    string Title,
    string Description,
    decimal? Price,
    string Status,
    Guid CategoryId,
    string CategoryName,
    Guid UserId,
    string? SellerName,
    string ContactPhone,
    int ViewCount,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    List<AdImageDto> Images,
    List<AdPropertyValueDto> Properties
);

/// <summary>İlanın kategoriye özel alan değeri; PropertyType mobilin değeri nasıl göstereceğini belirler (Text/Number/Boolean/Select/MultiSelect).</summary>
public record AdPropertyValueDto(Guid PropertyId, string PropertyName, string PropertyType, string Value);
