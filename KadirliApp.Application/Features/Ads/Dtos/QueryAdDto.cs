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
    string? Sort = null,

    /// <summary>
    /// Faz 11.15c: **yalnız panel/admin yolunda** anlamlıdır (pending | approved | rejected | expired).
    /// Public uç <c>OnlyPublished=true</c> geçer ve bu alanı YOK SAYAR — aksi hâlde
    /// <c>?status=pending</c> ile onaylanmamış ilanlar (iletişim telefonlarıyla) sızardı.
    /// Additive alan: mevcut istemcileri kırmaz (ARCHITECTURE.md §5).
    /// </summary>
    string? Status = null
);
