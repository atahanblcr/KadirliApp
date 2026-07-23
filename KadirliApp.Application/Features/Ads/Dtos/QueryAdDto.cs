using System;

namespace KadirliApp.Application.Features.Ads.Dtos;

public record QueryAdDto(
    Guid? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Search,
    int Page = 1,
    int Limit = 20,
    /// <summary>Faz 10.8: newest (varsayılan) | oldest | price_asc | price_desc — whitelist dışı 400.</summary>
    string? Sort = null
);
