using System;
using System.Collections.Generic;

namespace KadirliApp.Application.Features.Ads.Dtos;

public record UpdateAdDto(
    Guid? CategoryId,
    string? Title,
    string? Description,
    decimal? Price,
    string? ContactPhone,
    string? SellerName,
    List<Guid>? ImageFileIds,
    Dictionary<Guid, string>? PropertyValues
);
